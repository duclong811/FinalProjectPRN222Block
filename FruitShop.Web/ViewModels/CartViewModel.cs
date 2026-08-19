using FruitShop.Web.Models;

namespace FruitShop.Web.ViewModels;

public sealed class CartViewModel
{
    public required IReadOnlyList<CartItem> Items { get; init; }
    public int? CurrentBranchId { get; init; }
    public string? CurrentBranchName { get; init; }
    public IReadOnlyList<BranchCartSummary> OtherBranchCarts { get; init; } = [];
    public int ItemCount => Items.Sum(item => item.Quantity);
    public decimal TotalAmount => Items.Sum(item => item.LineTotal);
    public bool IsEmpty => Items.Count == 0;
}

public sealed class BranchCartSummary
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
}
