namespace FruitShop.Shared.Contracts;

public class CartItemDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal SubTotal => Price * Quantity;
}
