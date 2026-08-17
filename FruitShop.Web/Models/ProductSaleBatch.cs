namespace FruitShop.Web.Models;

public sealed class ProductSaleBatch
{
    public int InventoryId { get; init; }
    public string BatchCode { get; init; } = string.Empty;
    public int RemainingQuantity { get; init; }
    public DateTime ExpiryDate { get; init; }
    public decimal SalePrice { get; init; }
    public bool HasExpiryDiscount { get; init; }
}
