namespace FruitShop.Shared.Contracts;

public class NotificationDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? BranchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
