using System.Text.Json;
using FruitShop.Shared.Contracts;
using FruitShop.Web.Models;
using FruitShop.Web.ViewModels;
using Microsoft.AspNetCore.Http;

namespace FruitShop.Web.Services;

public sealed class ShoppingCartService : IShoppingCartService
{
    private const string SessionKey = "ShoppingCart";
    private readonly TcpClientService _tcpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ShoppingCartService(TcpClientService tcpClient, IHttpContextAccessor httpContextAccessor)
    {
        _tcpClient = tcpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> AddAsync(int productId, int inventoryId, int quantity)
    {
        var product = await GetWebProductByIdAsync(productId);
        var batch = product?.SaleBatches.SingleOrDefault(b => b.InventoryId == inventoryId);
        if (product is null || batch is null)
            return false;

        var cart = GetItems();
        var item = cart.SingleOrDefault(cartItem => cartItem.ProductId == productId);
        if (item is null)
        {
            cart.Add(new CartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                InventoryId = batch.InventoryId,
                BatchCode = batch.BatchCode,
                ExpiryDate = batch.ExpiryDate,
                UnitPrice = batch.SalePrice,
                Unit = product.Unit,
                ImageUrl = product.ImageUrl,
                Quantity = Math.Min(Math.Max(1, quantity), batch.RemainingQuantity),
                AvailableStock = batch.RemainingQuantity
            });
        }
        else
        {
            item.InventoryId = batch.InventoryId;
            item.BatchCode = batch.BatchCode;
            item.ExpiryDate = batch.ExpiryDate;
            item.UnitPrice = batch.SalePrice;
            item.AvailableStock = batch.RemainingQuantity;
            item.Quantity = Math.Min(Math.Max(1, quantity), batch.RemainingQuantity);
        }

        SaveItems(cart);
        return true;
    }

    public Task<CartViewModel> GetCartAsync()
    {
        IReadOnlyList<CartItem> items = GetItems();
        return Task.FromResult(new CartViewModel { Items = items });
    }

    public async Task UpdateQuantityAsync(int productId, int inventoryId, int quantity)
    {
        var cart = GetItems();
        var item = cart.SingleOrDefault(cartItem => cartItem.ProductId == productId);
        if (item is not null)
        {
            if (quantity <= 0)
                cart.Remove(item);
            else
            {
                var product = await GetWebProductByIdAsync(productId);
                var batch = product?.SaleBatches.SingleOrDefault(b => b.InventoryId == inventoryId);
                if (batch is null) cart.Remove(item);
                else
                {
                    item.InventoryId = batch.InventoryId;
                    item.BatchCode = batch.BatchCode;
                    item.ExpiryDate = batch.ExpiryDate;
                    item.UnitPrice = batch.SalePrice;
                    item.AvailableStock = batch.RemainingQuantity;
                    item.Quantity = Math.Min(quantity, item.AvailableStock);
                }
            }

            SaveItems(cart);
        }
    }

    public Task RemoveAsync(int productId, int inventoryId)
    {
        var cart = GetItems();
        cart.RemoveAll(item => item.ProductId == productId && item.InventoryId == inventoryId);
        SaveItems(cart);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        _httpContextAccessor.HttpContext?.Session.Remove(SessionKey);
        return Task.CompletedTask;
    }

    public Task<int> GetItemCountAsync() => Task.FromResult(GetItems().Sum(item => item.Quantity));

    private async Task<WebProductDto?> GetWebProductByIdAsync(int productId)
    {
        var response = await _tcpClient.SendRequestAsync("GET_WEB_PRODUCT_DETAIL", productId.ToString());
        if (response.Status == "SUCCESS" && !string.IsNullOrEmpty(response.Data))
        {
            return JsonSerializer.Deserialize<WebProductDto>(response.Data, JsonOptions);
        }
        return null;
    }

    private List<CartItem> GetItems()
    {
        var json = _httpContextAccessor.HttpContext?.Session.GetString(SessionKey);
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<CartItem>>(json) ?? [];
    }

    private void SaveItems(List<CartItem> items)
    {
        _httpContextAccessor.HttpContext?.Session.SetString(SessionKey, JsonSerializer.Serialize(items));
    }
}