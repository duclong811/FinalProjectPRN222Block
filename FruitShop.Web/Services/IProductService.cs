using FruitShop.Shared.Contracts;
using FruitShop.Web.Models;
using FruitShop.Web.ViewModels;

namespace FruitShop.Web.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetActiveProductsAsync();
    Task<IReadOnlyList<BranchDto>> GetBranchesAsync();
    Task<ProductListViewModel> GetProductListAsync(ProductListRequest request);
}