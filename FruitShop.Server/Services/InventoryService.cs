using FruitShop.Server.Data;
using FruitShop.Shared.Contracts;
using FruitShop.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Server.Services;

public sealed class InventoryService
{
    private readonly string _connectionString;

    public InventoryService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<InventoryListResponse> GetInventoryAsync(int? branchId = null, CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var query = db.Inventories.AsNoTracking()
            .Include(i => i.Product)
                .ThenInclude(p => p.Category)
            .Include(i => i.Branch)
            .AsQueryable();

        if (branchId.HasValue && branchId.Value > 0)
        {
            query = query.Where(i => i.BranchId == branchId.Value);
        }

        var items = await query
            .OrderByDescending(i => i.ReceivedAt)
            .Select(i => new InventoryDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                CategoryName = i.Product.Category != null ? i.Product.Category.Name : string.Empty,
                Unit = i.Product.Unit,
                Price = i.SellingPrice ?? i.Product.Price,
                SellingPrice = i.SellingPrice ?? i.Product.Price,
                BranchId = i.BranchId,
                BranchName = i.Branch.BranchName,
                BatchCode = i.BatchCode,
                QuantityReceived = i.QuantityReceived,
                RemainingQuantity = i.RemainingQuantity,
                ReceivedAt = i.ReceivedAt,
                ExpiryDate = i.ExpiryDate,
                UnitCost = i.UnitCost,
                SupplierName = i.SupplierName,
                Note = i.Note
            })
            .ToListAsync(cancellationToken);

        return new InventoryListResponse { Success = true, Items = items };
    }

    public async Task<TcpResponse> ReceiveAsync(ReceiveInventoryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ProductId <= 0 || request.Quantity <= 0 || string.IsNullOrWhiteSpace(request.BatchCode))
            return new TcpResponse { Status = "ERROR", Message = "Dữ liệu nhập kho không hợp lệ." };

        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken);
        if (product is null)
            return new TcpResponse { Status = "ERROR", Message = "Sản phẩm không tồn tại." };

        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
        if (branch is null)
        {
            var defaultBranch = await db.Branches.FirstOrDefaultAsync(cancellationToken);
            if (defaultBranch is null)
                return new TcpResponse { Status = "ERROR", Message = "Hệ thống chưa có chi nhánh nào." };
            request.BranchId = defaultBranch.Id;
        }

        var sellingPrice = request.SellingPrice.HasValue && request.SellingPrice.Value > 0
            ? request.SellingPrice.Value
            : product.Price;

        if (request.UnitCost.HasValue && request.UnitCost.Value > 0 && sellingPrice < request.UnitCost.Value)
        {
            return new TcpResponse { Status = "ERROR", Message = "Giá bán ra không được thấp hơn giá nhập (vốn)." };
        }

        var inventory = new Inventory
        {
            ProductId = request.ProductId,
            BranchId = request.BranchId,
            BatchCode = request.BatchCode.Trim(),
            QuantityReceived = request.Quantity,
            RemainingQuantity = request.Quantity,
            ReceivedAt = DateTime.Now,
            ExpiryDate = request.ExpiryDate,
            UnitCost = request.UnitCost,
            SellingPrice = sellingPrice,
            SupplierName = string.IsNullOrWhiteSpace(request.SupplierName) ? null : request.SupplierName.Trim(),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedAt = DateTime.Now
        };

        product.StockQuantity += request.Quantity;
        db.Inventories.Add(inventory);
        await db.SaveChangesAsync(cancellationToken);

        return new TcpResponse { Status = "SUCCESS", Message = "Nhập lô hàng mới thành công." };
    }
}
