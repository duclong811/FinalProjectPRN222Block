using FruitShop.Web.ViewModels;

namespace FruitShop.Web.Services;

public interface IShoppingCartService
{
    Task<bool> AddAsync(int productId, int inventoryId, int quantity);
    Task<CartViewModel> GetCartAsync(int? branchId = null);
    Task UpdateQuantityAsync(int productId, int inventoryId, int quantity, int? branchId = null);
    Task RemoveAsync(int productId, int inventoryId, int? branchId = null);
    Task ClearAsync(int? branchId = null);
    Task<int> GetItemCountAsync(int? branchId = null);
}
