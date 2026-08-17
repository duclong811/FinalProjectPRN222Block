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

    public async Task<InventoryListResponse> GetInventoryAsync(CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var items = await db.Inventories.AsNoTracking()
            .Include(i => i.Product)
            .Include(i => i.Branch)
            .OrderByDescending(i => i.ReceivedAt)
            .Select(i => new InventoryDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
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
