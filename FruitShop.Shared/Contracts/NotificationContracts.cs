namespace FruitShop.Shared.Contracts;

public class NotificationDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<NotificationDto> Items { get; set; } = new();
    public int UnreadCount { get; set; }
}

public class GetNotificationsRequest
{
    public int? UserId { get; set; }
    public int? BranchId { get; set; }
}

public class MarkNotificationReadRequest
{
    public int? NotificationId { get; set; }
    public int? BranchId { get; set; }
    public int? UserId { get; set; }
    public bool MarkAll { get; set; }
}
