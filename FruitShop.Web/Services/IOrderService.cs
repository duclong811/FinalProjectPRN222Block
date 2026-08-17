using FruitShop.Web.ViewModels;

namespace FruitShop.Web.Services;

public interface IOrderService
{
    Task<string?> CreateCashOrderAsync(CheckoutViewModel checkout);
}
