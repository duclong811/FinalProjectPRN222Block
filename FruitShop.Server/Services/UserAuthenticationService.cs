using FruitShop.Server.Data;
using FruitShop.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Server.Services;

public sealed class UserAuthenticationService
{
    private readonly string _connectionString;

    public UserAuthenticationService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<LoginResponse> AuthenticateAsync(string? username, string? password, CancellationToken cancellationToken = default)
    {
        username = username?.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return Failed("Tên đăng nhập và mật khẩu là bắt buộc.");

        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var user = await db.Users.AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Branch)
            .Where(u => u.Username == username && u.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null || !PasswordVerifier.Verify(password, user.PasswordHash))
            return Failed("Tên đăng nhập hoặc mật khẩu không chính xác.");

        return new LoginResponse
        {
            Success = true,
            Message = "Đăng nhập thành công.",
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            RoleName = user.Role.RoleName,
            BranchId = user.BranchId,
            BranchName = user.Branch?.BranchName
        };
    }

    private static LoginResponse Failed(string message) => new() { Success = false, Message = message };
}