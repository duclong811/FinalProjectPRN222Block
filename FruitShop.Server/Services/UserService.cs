using FruitShop.Server.Data;
using FruitShop.Shared.Contracts;
using FruitShop.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Server.Services;

public sealed class UserService
{
    private readonly string _connectionString;

    public UserService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<UserListResponse> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var users = await db.Users.AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Branch)
            .OrderBy(u => u.RoleId)
            .ThenBy(u => u.FullName)
            .Select(u => new UserDto
            {
                Id = u.Id,
                RoleId = u.RoleId,
                RoleName = u.Role.RoleName,
                BranchId = u.BranchId,
                BranchName = u.Branch != null ? u.Branch.BranchName : null,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                Username = u.Username,
                Avatar = u.Avatar,
                Address = u.Address,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new UserListResponse { Success = true, Items = users };
    }

    public async Task<TcpResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.FullName))
            return new TcpResponse { Status = "ERROR", Message = "Tên đăng nhập, mật khẩu và họ tên không được để trống." };

        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        if (await db.Users.AnyAsync(u => u.Username == request.Username.Trim(), cancellationToken))
            return new TcpResponse { Status = "ERROR", Message = "Tên đăng nhập đã tồn tại." };

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role is null)
            return new TcpResponse { Status = "ERROR", Message = "Vai trò không hợp lệ." };

        var user = new User
        {
            RoleId = request.RoleId,
            BranchId = request.BranchId,
            FullName = request.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Username = request.Username.Trim(),
            PasswordHash = PasswordVerifier.Hash(request.Password),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return new TcpResponse { Status = "SUCCESS", Message = "Tạo người dùng thành công." };
    }

    public async Task<TcpResponse> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (request.UserId <= 0 || string.IsNullOrWhiteSpace(request.FullName))
            return new TcpResponse { Status = "ERROR", Message = "Dữ liệu người dùng không hợp lệ." };

        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            return new TcpResponse { Status = "ERROR", Message = "Không tìm thấy người dùng." };

        user.RoleId = request.RoleId;
        user.BranchId = request.BranchId;
        user.FullName = request.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync(cancellationToken);
        return new TcpResponse { Status = "SUCCESS", Message = "Cập nhật người dùng thành công." };
    }

    public async Task<TcpResponse> ToggleActiveAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return new TcpResponse { Status = "ERROR", Message = "Không tìm thấy người dùng." };

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(cancellationToken);

        var statusText = user.IsActive ? "kích hoạt" : "khóa";
        return new TcpResponse { Status = "SUCCESS", Message = $"Đã {statusText} tài khoản '{user.Username}'." };
    }

    public async Task<TcpResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (request.UserId <= 0 || string.IsNullOrWhiteSpace(request.NewPassword))
            return new TcpResponse { Status = "ERROR", Message = "Mật khẩu mới không được để trống." };

        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            return new TcpResponse { Status = "ERROR", Message = "Không tìm thấy người dùng." };

        user.PasswordHash = PasswordVerifier.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(cancellationToken);

        return new TcpResponse { Status = "SUCCESS", Message = $"Đã đặt lại mật khẩu cho tài khoản '{user.Username}'." };
    }
}