using FruitShop.Server.Data;
using FruitShop.Shared.Contracts;
using FruitShop.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Server.Services;

public sealed class NotificationService
{
    private readonly string _connectionString;

    public NotificationService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<NotificationListResponse> GetNotificationsAsync(GetNotificationsRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);

        // Tự động kiểm tra mức tồn kho theo chi nhánh và tạo cảnh báo nếu sắp hết hàng
        await CheckAndGenerateLowStockNotificationsAsync(db, request.BranchId, request.UserId, cancellationToken);

        var query = db.Notifications.AsNoTracking()
            .Include(n => n.Branch)
            .AsQueryable();

        if (request.BranchId.HasValue && request.BranchId.Value > 0)
        {
            query = query.Where(n => n.BranchId == request.BranchId.Value || n.BranchId == null);
        }

        var rawNotifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        var products = await db.Products.AsNoTracking().Select(p => new { p.Id, p.Name }).ToListAsync(cancellationToken);

        var items = rawNotifications.Select(n =>
        {
            var matchedProduct = products.FirstOrDefault(p => n.Title.Contains(p.Name) || n.Message.Contains(p.Name));
            return new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                BranchId = n.BranchId,
                BranchName = n.Branch != null ? n.Branch.BranchName : "Toàn hệ thống",
                ProductId = matchedProduct?.Id,
                ProductName = matchedProduct?.Name,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            };
        }).ToList();

        int unreadCount = items.Count(n => !n.IsRead);

        return new NotificationListResponse
        {
            Success = true,
            Items = items,
            UnreadCount = unreadCount,
            Message = $"Đã tải {items.Count} thông báo ({unreadCount} chưa đọc)."
        };
    }

    public async Task<TcpResponse> MarkReadAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);
        if (notification is null)
            return new TcpResponse { Status = "ERROR", Message = "Không tìm thấy thông báo." };

        notification.IsRead = true;
        await db.SaveChangesAsync(cancellationToken);

        return new TcpResponse { Status = "SUCCESS", Message = "Đã đánh dấu đã đọc." };
    }

    public async Task<TcpResponse> MarkAllReadAsync(int? branchId = null, int? userId = null, CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var query = db.Notifications.Where(n => !n.IsRead);

        if (branchId.HasValue && branchId.Value > 0)
        {
            query = query.Where(n => n.BranchId == branchId.Value || n.BranchId == null);
        }

        var unreadList = await query.ToListAsync(cancellationToken);
        if (unreadList.Count > 0)
        {
            foreach (var n in unreadList)
            {
                n.IsRead = true;
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        return new TcpResponse { Status = "SUCCESS", Message = $"Đã đánh dấu đọc {unreadList.Count} thông báo." };
    }

    private async Task CheckAndGenerateLowStockNotificationsAsync(FruitStoreDbContext db, int? branchId, int? userId, CancellationToken cancellationToken)
    {
        try
        {
            var branchQuery = db.Branches.AsNoTracking().Where(b => b.IsActive);
            if (branchId.HasValue && branchId.Value > 0)
            {
                branchQuery = branchQuery.Where(b => b.Id == branchId.Value);
            }

            var activeBranches = await branchQuery.ToListAsync(cancellationToken);
            if (activeBranches.Count == 0) return;

            foreach (var branch in activeBranches)
            {
                int targetManagerId = branch.ManagerId > 0 ? branch.ManagerId : (userId ?? 1);
                var validManagerId = await db.Users.Where(u => u.Id == targetManagerId).Select(u => (int?)u.Id).FirstOrDefaultAsync(cancellationToken)
                    ?? await db.Users.Select(u => (int?)u.Id).FirstOrDefaultAsync(cancellationToken)
                    ?? 1;

                // Lấy các sản phẩm đang bán tại chi nhánh này (có phát sinh lô kho tại chi nhánh này)
                var branchProducts = await db.Products.AsNoTracking()
                    .Include(p => p.Inventories)
                    .Where(p => p.IsActive && p.Inventories.Any(i => i.BranchId == branch.Id))
                    .ToListAsync(cancellationToken);

                foreach (var product in branchProducts)
                {
                    var branchInvs = product.Inventories.Where(i => i.BranchId == branch.Id).ToList();

                    // Tính tổng số lượng tồn kho khả dụng của sản phẩm tại chi nhánh này
                    int remainingAtBranch = branchInvs
                        .Where(i => i.RemainingQuantity > 0)
                        .Sum(i => i.RemainingQuantity);

                    // Ngưỡng cảnh báo an toàn: tối thiểu là 10 hoặc theo MinStockThreshold của sản phẩm
                    int threshold = product.MinStockThreshold > 0 ? Math.Max(product.MinStockThreshold, 10) : 10;

                    // Nếu số lượng tồn <= ngưỡng cảnh báo
                    if (remainingAtBranch <= threshold)
                    {
                        // Lấy thông báo cảnh báo gần nhất của sản phẩm này tại chi nhánh
                        var latestNotification = await db.Notifications
                            .Where(n => n.BranchId == branch.Id &&
                                        n.Type == "LowStock" &&
                                        n.Title.Contains(product.Name))
                            .OrderByDescending(n => n.CreatedAt)
                            .FirstOrDefaultAsync(cancellationToken);

                        // Thời điểm nhập hàng gần nhất của sản phẩm tại chi nhánh này
                        var latestInventoryDate = branchInvs.Any()
                            ? (DateTime?)branchInvs.Max(i => i.ReceivedAt > i.CreatedAt ? i.ReceivedAt : i.CreatedAt)
                            : null;

                        bool shouldCreate = false;

                        if (latestNotification == null)
                        {
                            // 1. Chưa từng có thông báo cảnh báo nào -> Cần tạo thông báo mới
                            shouldCreate = true;
                        }
                        else if (!latestNotification.IsRead)
                        {
                            // 2. Đang có thông báo CHƯA ĐỌC -> Không tạo thêm thông báo mới để tránh trùng lặp
                            // Cập nhật lại số lượng tồn mới nhất vào nội dung thông báo nếu có thay đổi
                            latestNotification.Message = remainingAtBranch == 0
                                ? $"Sản phẩm '{product.Name}' tại {branch.BranchName} ĐÃ HẾT HÀNG (0 {product.Unit}). Vui lòng nhập thêm hàng gấp!"
                                : $"Sản phẩm '{product.Name}' tại {branch.BranchName} hiện chỉ còn {remainingAtBranch} {product.Unit} (Ngưỡng an toàn: {threshold}). Vui lòng nhập thêm hàng!";
                            shouldCreate = false;
                        }
                        else
                        {
                            // 3. Thông báo gần nhất ĐÃ ĐỌC (Manager đã xác nhận)
                            // CHỈ TẠO MỚI nếu sau thời điểm tạo thông báo cũ, có đợt NHẬP HÀNG MỚI (latestInventoryDate > latestNotification.CreatedAt)
                            // và hiện tại số lượng lại bị tụt xuống dưới ngưỡng cảnh báo một lần nữa!
                            if (latestInventoryDate.HasValue && latestInventoryDate.Value > latestNotification.CreatedAt)
                            {
                                shouldCreate = true;
                            }
                        }

                        if (shouldCreate)
                        {
                            var notification = new Notification
                            {
                                UserId = validManagerId,
                                BranchId = branch.Id,
                                Title = $"Cảnh báo hết hàng: {product.Name}",
                                Message = remainingAtBranch == 0
                                    ? $"Sản phẩm '{product.Name}' tại {branch.BranchName} ĐÃ HẾT HÀNG (0 {product.Unit}). Vui lòng nhập thêm hàng gấp!"
                                    : $"Sản phẩm '{product.Name}' tại {branch.BranchName} hiện chỉ còn {remainingAtBranch} {product.Unit} (Ngưỡng an toàn: {threshold}). Vui lòng nhập thêm hàng!",
                                Type = "LowStock",
                                IsRead = false,
                                CreatedAt = DateTime.Now
                            };

                            db.Notifications.Add(notification);
                        }
                    }
                    else
                    {
                        // Nếu sản phẩm đã được nhập hàng (tồn > ngưỡng cảnh báo), tự động đánh dấu đã đọc các thông báo cũ
                        var resolvedNotifs = await db.Notifications
                            .Where(n => n.BranchId == branch.Id && n.Type == "LowStock" && !n.IsRead && n.Title.Contains(product.Name))
                            .ToListAsync(cancellationToken);

                        foreach (var rn in resolvedNotifs)
                        {
                            rn.IsRead = true;
                        }
                    }
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] CheckAndGenerateLowStockNotificationsAsync: {ex.Message}");
        }
    }
}
