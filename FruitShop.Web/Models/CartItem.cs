namespace FruitShop.Web.Models;

public sealed class CartItem
{
    public int ProductId { get; set; }
    public int InventoryId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public int AvailableStock { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}
