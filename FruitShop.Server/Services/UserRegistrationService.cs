using FruitShop.Server.Data;
using FruitShop.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Server.Services;

public sealed class UserRegistrationService
{
    private readonly string _connectionString;

    public UserRegistrationService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return new LoginResponse { Success = false, Message = "Tên đăng nhập và mật khẩu không được để trống." };

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phone = request.Phone.Trim();
            if (phone.Length != 10 || !phone.StartsWith("0") || !phone.All(char.IsDigit))
            {
                return new LoginResponse { Success = false, Message = "Số điện thoại phải có 10 chữ số và bắt đầu bằng số 0." };
            }
        }

        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var exists = await db.Users.AnyAsync(u => u.Username == request.Username, cancellationToken);
        if (exists)
            return new LoginResponse { Success = false, Message = "Tên đăng nhập đã tồn tại." };

        var customerRole = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer", cancellationToken);
        if (customerRole is null)
            return new LoginResponse { Success = false, Message = "Vai trò 'Customer' chưa được tạo trong hệ thống." };

        var user = new User
        {
            RoleId = customerRole.Id,
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

        return new LoginResponse
        {
            Success = true,
            Message = "Đăng ký tài khoản thành công.",
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            RoleName = customerRole.RoleName
        };
    }
}
