namespace FruitShop.Web.Models;

public sealed class ProductListRequest
{
    public int? CategoryId { get; init; }
    public int? BranchId { get; init; }
    public string? PriceRange { get; init; }
    public string? Sort { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 6;
}
