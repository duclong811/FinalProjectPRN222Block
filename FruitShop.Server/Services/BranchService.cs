using FruitShop.Server.Data;
using FruitShop.Shared.Contracts;
using FruitShop.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Server.Services;

public sealed class BranchService
{
    private readonly string _connectionString;

    public BranchService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<BranchListResponse> GetBranchesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var branches = await db.Branches.AsNoTracking()
            .Include(b => b.Manager)
            .OrderBy(b => b.BranchName)
            .Select(b => new BranchDto
            {
                Id = b.Id,
                ManagerId = b.ManagerId,
                ManagerName = b.Manager != null ? b.Manager.FullName : string.Empty,
                BranchName = b.BranchName,
                Address = b.Address,
                Phone = b.Phone,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new BranchListResponse { Success = true, Items = branches };
    }

    public async Task<TcpResponse> CreateBranchAsync(CreateBranchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BranchName) || string.IsNullOrWhiteSpace(request.Address))
            return new TcpResponse { Status = "ERROR", Message = "Tên chi nhánh và địa chỉ không được để trống." };

        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var manager = await db.Users.FirstOrDefaultAsync(u => u.Id == request.ManagerId, cancellationToken);
        if (manager is null)
            return new TcpResponse { Status = "ERROR", Message = "Quản lý chi nhánh không tồn tại." };

        var branch = new Branch
        {
            ManagerId = request.ManagerId,
            BranchName = request.BranchName.Trim(),
            Address = request.Address.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        db.Branches.Add(branch);
        await db.SaveChangesAsync(cancellationToken);

        return new TcpResponse { Status = "SUCCESS", Message = "Tạo chi nhánh mới thành công." };
    }

    public async Task<TcpResponse> UpdateBranchAsync(UpdateBranchRequest request, CancellationToken cancellationToken = default)
    {
        if (request.BranchId <= 0 || string.IsNullOrWhiteSpace(request.BranchName) || string.IsNullOrWhiteSpace(request.Address))
            return new TcpResponse { Status = "ERROR", Message = "Dữ liệu chi nhánh không hợp lệ." };

        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
        if (branch is null)
            return new TcpResponse { Status = "ERROR", Message = "Không tìm thấy chi nhánh." };

        var manager = await db.Users.FirstOrDefaultAsync(u => u.Id == request.ManagerId, cancellationToken);
        if (manager is null)
            return new TcpResponse { Status = "ERROR", Message = "Quản lý chi nhánh không tồn tại." };

        branch.ManagerId = request.ManagerId;
        branch.BranchName = request.BranchName.Trim();
        branch.Address = request.Address.Trim();
        branch.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        branch.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);
        return new TcpResponse { Status = "SUCCESS", Message = "Cập nhật chi nhánh thành công." };
    }
}