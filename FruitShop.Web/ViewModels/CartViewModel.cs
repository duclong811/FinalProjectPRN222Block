using FruitShop.Web.Models;

namespace FruitShop.Web.ViewModels;

public sealed class CartViewModel
{
    public required IReadOnlyList<CartItem> Items { get; init; }
    public int ItemCount => Items.Sum(item => item.Quantity);
    public decimal TotalAmount => Items.Sum(item => item.LineTotal);
    public bool IsEmpty => Items.Count == 0;
}
