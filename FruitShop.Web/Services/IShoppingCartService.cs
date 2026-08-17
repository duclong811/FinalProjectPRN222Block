using FruitShop.Web.ViewModels;

namespace FruitShop.Web.Services;

public interface IShoppingCartService
{
    Task<bool> AddAsync(int productId, int inventoryId, int quantity);  
    Task<CartViewModel> GetCartAsync();
    Task UpdateQuantityAsync(int productId, int inventoryId, int quantity);
    Task RemoveAsync(int productId, int inventoryId);
    Task ClearAsync();
    Task<int> GetItemCountAsync();
}
