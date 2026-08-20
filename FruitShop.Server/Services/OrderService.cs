using FruitShop.Server.Data;
using FruitShop.Shared.Contracts;
using FruitShop.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Server.Services;

public sealed class OrderService
{
    private readonly string _connectionString;

    public OrderService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<OrderListResponse> GetOrdersAsync(int? branchId = null, CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var query = db.Orders.AsNoTracking()
            .Include(o => o.OrderDetails)
            .Include(o => o.Branch)
            .Include(o => o.Staff)
            .Include(o => o.Payments)
            .AsQueryable();

        if (branchId.HasValue && branchId.Value > 0)
        {
            query = query.Where(o => o.BranchId == branchId.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        var items = orders.Select(o => new OrderDto
        {
            Id = o.Id,
            OrderCode = o.OrderCode,
            CustomerId = o.CustomerId,
            CustomerName = o.CustomerName,
            CustomerPhone = o.CustomerPhone,
            CustomerEmail = o.CustomerEmail,
            ShippingAddress = o.ShippingAddress,
            Note = o.Note,
            TotalAmount = o.TotalAmount,
            DiscountAmount = o.DiscountAmount,
            FinalAmount = o.FinalAmount,
            OrderStatus = o.OrderStatus,
            CreatedAt = o.CreatedAt,
            BranchId = o.BranchId,
            BranchName = o.Branch?.BranchName,
            StaffId = o.StaffId,
            StaffName = o.Staff?.FullName,
            PaymentMethod = o.Payments.FirstOrDefault()?.PaymentMethod ?? "COD",
            PaymentStatus = o.Payments.FirstOrDefault()?.PaymentStatus ?? "Pending",
            Details = o.OrderDetails.Select(d => new OrderDetailDto
            {
                Id = d.Id,
                OrderId = d.OrderId,
                ProductId = d.ProductId,
                BatchId = d.BatchId,
                ProductName = d.ProductName,
                UnitPrice = d.UnitPrice,
                Quantity = d.Quantity,
                DiscountPercent = d.DiscountPercent,
                SubTotal = d.SubTotal
            }).ToList()
        }).ToList();

        return new OrderListResponse { Success = true, Items = items };
    }

    public async Task<OrderListResponse> GetOrdersByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var orders = await db.Orders.AsNoTracking()
            .Where(o => o.CustomerId == userId)
            .Include(o => o.OrderDetails)
            .Include(o => o.Payments)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        var items = orders.Select(o => new OrderDto
        {
            Id = o.Id,
            OrderCode = o.OrderCode,
            CustomerId = o.CustomerId,
            CustomerName = o.CustomerName,
            CustomerPhone = o.CustomerPhone,
            CustomerEmail = o.CustomerEmail,
            ShippingAddress = o.ShippingAddress,
            Note = o.Note,
            TotalAmount = o.TotalAmount,
            DiscountAmount = o.DiscountAmount,
            FinalAmount = o.FinalAmount,
            OrderStatus = o.OrderStatus,
            CreatedAt = o.CreatedAt,
            PaymentMethod = o.Payments.FirstOrDefault()?.PaymentMethod ?? "COD",
            PaymentStatus = o.Payments.FirstOrDefault()?.PaymentStatus ?? "Pending",
            Details = o.OrderDetails.Select(d => new OrderDetailDto
            {
                Id = d.Id,
                OrderId = d.OrderId,
                ProductId = d.ProductId,
                BatchId = d.BatchId,
                ProductName = d.ProductName,
                UnitPrice = d.UnitPrice,
                Quantity = d.Quantity,
                DiscountPercent = d.DiscountPercent,
                SubTotal = d.SubTotal
            }).ToList()
        }).ToList();

        return new OrderListResponse { Success = true, Items = items };
    }

    public async Task<TcpResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items == null || !request.Items.Any())
            return new TcpResponse { Status = "ERROR", Message = "Đơn hàng phải có ít nhất 1 sản phẩm." };

        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var today = DateTime.Today;
            var orderCode = $"ORD{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
            decimal totalAmount = 0;

            var order = new Order
            {
                OrderCode = orderCode,
                CustomerId = request.CustomerId,
                BranchId = request.BranchId,
                CustomerName = request.CustomerName.Trim(),
                CustomerPhone = request.CustomerPhone.Trim(),
                CustomerEmail = string.IsNullOrWhiteSpace(request.CustomerEmail) ? null : request.CustomerEmail.Trim(),
                ShippingAddress = request.ShippingAddress.Trim(),
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                OrderStatus = "Pending",
                CreatedAt = DateTime.Now
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync(cancellationToken);

            foreach (var item in request.Items)
            {
                var product = await db.Products
                    .Include(p => p.Inventories)
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId && p.IsActive, cancellationToken);

                if (product is null)
                    throw new Exception($"Sản phẩm ID {item.ProductId} không tồn tại.");

                Inventory? batch = null;
                if (item.BatchId.HasValue && item.BatchId.Value > 0)
                {
                    batch = product.Inventories.FirstOrDefault(inv => inv.Id == item.BatchId.Value && inv.RemainingQuantity >= item.Quantity && inv.ExpiryDate >= today);
                }

                if (batch is null)
                {
                    batch = product.Inventories
                        .Where(inv => (!order.BranchId.HasValue || inv.BranchId == order.BranchId.Value) && inv.RemainingQuantity >= item.Quantity && inv.ExpiryDate >= today)
                        .OrderBy(inv => inv.ExpiryDate)
                        .ThenBy(inv => inv.ReceivedAt)
                        .FirstOrDefault();
                }

                if (batch is null)
                {
                    throw new Exception($"Sản phẩm '{product.Name}' không đủ tồn kho lô khả dụng.");
                }

                decimal unitPrice = product.Price;
                if ((batch.ExpiryDate.Date - today).Days <= 2)
                {
                    unitPrice = product.Price * 0.5m;
                }

                batch.RemainingQuantity -= item.Quantity;
                product.StockQuantity -= item.Quantity;
                product.UpdatedAt = DateTime.Now;

                var subtotal = unitPrice * item.Quantity * (1 - item.DiscountPercent / 100);
                totalAmount += subtotal;

                var detail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    BatchId = batch.Id,
                    ProductName = product.Name,
                    UnitPrice = unitPrice,
                    Quantity = item.Quantity,
                    DiscountPercent = item.DiscountPercent
                };
                db.OrderDetails.Add(detail);
            }

            order.TotalAmount = totalAmount;
            order.FinalAmount = totalAmount;

            var payment = new Payment
            {
                OrderId = order.Id,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = "Pending",
                Amount = totalAmount,
                CreatedAt = DateTime.Now
            };
            db.Payments.Add(payment);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new TcpResponse { Status = "SUCCESS", Message = $"Tạo đơn hàng {orderCode} thành công.", Data = orderCode };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new TcpResponse { Status = "ERROR", Message = ex.Message };
        }
    }

    public async Task<TcpResponse> UpdateOrderStatusAsync(UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var order = await db.Orders
            .Include(o => o.Payments)
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
            return new TcpResponse { Status = "ERROR", Message = "Không tìm thấy đơn hàng." };

        var newStatus = request.Status.Trim();
        if (newStatus.Equals("Confirm", StringComparison.OrdinalIgnoreCase))
        {
            newStatus = "Confirmed";
        }

        var oldStatus = order.OrderStatus;
        order.OrderStatus = newStatus;
        if (request.StaffId.HasValue && request.StaffId.Value > 0)
        {
            order.StaffId = request.StaffId;
        }
        order.UpdatedAt = DateTime.Now;

        // Auto mark payment as Paid when Completed
        if (newStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase))
        {
            var payment = order.Payments.FirstOrDefault();
            if (payment is not null)
            {
                payment.PaymentStatus = "Paid";
                payment.PaidAt = DateTime.Now;
                payment.UpdatedAt = DateTime.Now;
            }
            else
            {
                db.Payments.Add(new Payment
                {
                    OrderId = order.Id,
                    PaymentMethod = "COD",
                    PaymentStatus = "Paid",
                    Amount = order.FinalAmount,
                    PaidAt = DateTime.Now,
                    CreatedAt = DateTime.Now
                });
            }
        }
        // Rollback stock when Cancelled from an active order
        else if (newStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) && !oldStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var detail in order.OrderDetails)
            {
                var product = await db.Products.FirstOrDefaultAsync(p => p.Id == detail.ProductId, cancellationToken);
                if (product is not null)
                {
                    product.StockQuantity += detail.Quantity;
                    product.UpdatedAt = DateTime.Now;
                }

                if (detail.BatchId.HasValue)
                {
                    var batch = await db.Inventories.FirstOrDefaultAsync(b => b.Id == detail.BatchId.Value, cancellationToken);
                    if (batch is not null)
                    {
                        batch.RemainingQuantity += detail.Quantity;
                    }
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        string successMessage = newStatus switch
        {
            "Confirmed" => "Xác nhận đơn hàng thành công.",
            "Shipping" => "Bắt đầu giao hàng thành công.",
            "Completed" => "Giao hàng thành công.",
            "Cancelled" => "Hủy đơn hàng thành công.",
            _ => "Cập nhật trạng thái đơn hàng thành công."
        };

        return new TcpResponse { Status = "SUCCESS", Message = successMessage };
    }

    public async Task<TcpResponse> MarkAsPaidAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
        if (payment is null)
        {
            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            if (order is null) return new TcpResponse { Status = "ERROR", Message = "Đơn hàng không tồn tại." };

            payment = new Payment
            {
                OrderId = orderId,
                PaymentMethod = "COD",
                PaymentStatus = "Paid",
                Amount = order.FinalAmount,
                PaidAt = DateTime.Now,
                CreatedAt = DateTime.Now
            };
            db.Payments.Add(payment);
        }
        else
        {
            payment.PaymentStatus = "Paid";
            payment.PaidAt = DateTime.Now;
            payment.UpdatedAt = DateTime.Now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new TcpResponse { Status = "SUCCESS", Message = "Đã đánh dấu đơn hàng là Đã thanh toán." };
    }
}
