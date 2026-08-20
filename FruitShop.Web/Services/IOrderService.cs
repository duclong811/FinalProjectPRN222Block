using FruitShop.Shared.Contracts;
using FruitShop.Shared.Helpers;
using FruitShop.Web.ViewModels;

namespace FruitShop.Web.Services;

public interface IOrderService
{
    Task<string?> CreateCashOrderAsync(CheckoutViewModel checkout);
    Task<List<OrderDto>> GetMyOrdersAsync(int userId);
    Task<TcpResponse> CancelOrderAsync(int orderId);
}
