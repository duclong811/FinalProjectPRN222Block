using FruitShop.Shared.Contracts;

namespace FruitShop.Web.ViewModels;

public sealed class ProductListViewModel
{
    public required IReadOnlyList<WebProductDto> Products { get; init; }
    public required IReadOnlyList<CategoryDto> Categories { get; init; }
    public required int TotalItems { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
    public int? CategoryId { get; init; }
    public int? BranchId { get; init; }
    public string? BranchName { get; init; }
    public string? PriceRange { get; init; }
    public string? Sort { get; init; }
}