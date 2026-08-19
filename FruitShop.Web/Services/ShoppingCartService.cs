using System.Text.Json;
using FruitShop.Shared.Contracts;
using FruitShop.Web.Models;
using FruitShop.Web.ViewModels;
using Microsoft.AspNetCore.Http;

namespace FruitShop.Web.Services;

public sealed class ShoppingCartService : IShoppingCartService
{
    private readonly TcpClientService _tcpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ShoppingCartService(TcpClientService tcpClient, IHttpContextAccessor httpContextAccessor)
    {
        _tcpClient = tcpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    private int GetActiveBranchId()
    {
        var branchId = _httpContextAccessor.HttpContext?.Session.GetInt32("SelectedBranchId");
        return (branchId.HasValue && branchId.Value > 0) ? branchId.Value : 1;
    }

    private string GetSessionKey(int branchId) => $"ShoppingCart_Branch_{branchId}";

    public async Task<bool> AddAsync(int productId, int inventoryId, int quantity)
    {
        var product = await GetWebProductByIdAsync(productId);
        var batch = product?.SaleBatches.SingleOrDefault(b => b.InventoryId == inventoryId);
        if (product is null || batch is null)
            return false;

        int branchId = batch.BranchId > 0 ? batch.BranchId : GetActiveBranchId();
        string branchName = !string.IsNullOrEmpty(batch.BranchName) ? batch.BranchName : $"Chi nhánh #{branchId}";

        var cart = GetItems(branchId);
        var item = cart.SingleOrDefault(cartItem => cartItem.ProductId == productId && cartItem.InventoryId == inventoryId);
        if (item is null)
        {
            cart.Add(new CartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                InventoryId = batch.InventoryId,
                BranchId = branchId,
                BranchName = branchName,
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
            item.BatchCode = batch.BatchCode;
            item.ExpiryDate = batch.ExpiryDate;
            item.UnitPrice = batch.SalePrice;
            item.AvailableStock = batch.RemainingQuantity;
            item.Quantity = Math.Min(item.Quantity + quantity, batch.RemainingQuantity);
        }

        SaveItems(branchId, cart);
        return true;
    }

    public async Task<CartViewModel> GetCartAsync(int? branchId = null)
    {
        int targetBranchId = (branchId.HasValue && branchId.Value > 0) ? branchId.Value : GetActiveBranchId();
        var items = GetItems(targetBranchId);

        var branches = await GetBranchesAsync();
        var currentBranch = branches.FirstOrDefault(b => b.Id == targetBranchId);
        string currentBranchName = currentBranch?.BranchName ?? $"Chi nhánh #{targetBranchId}";

        // Collect other branches that have items in their carts
        var otherBranchCarts = new List<BranchCartSummary>();
        foreach (var b in branches)
        {
            if (b.Id != targetBranchId)
            {
                var otherItems = GetItems(b.Id);
                if (otherItems.Count > 0)
                {
                    otherBranchCarts.Add(new BranchCartSummary
                    {
                        BranchId = b.Id,
                        BranchName = b.BranchName,
                        ItemCount = otherItems.Sum(i => i.Quantity),
                        TotalAmount = otherItems.Sum(i => i.LineTotal)
                    });
                }
            }
        }

        return new CartViewModel
        {
            Items = items,
            CurrentBranchId = targetBranchId,
            CurrentBranchName = currentBranchName,
            OtherBranchCarts = otherBranchCarts
        };
    }

    public async Task UpdateQuantityAsync(int productId, int inventoryId, int quantity, int? branchId = null)
    {
        int targetBranchId = (branchId.HasValue && branchId.Value > 0) ? branchId.Value : GetActiveBranchId();
        var cart = GetItems(targetBranchId);
        var item = cart.SingleOrDefault(cartItem => cartItem.ProductId == productId && cartItem.InventoryId == inventoryId);
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
                    item.BatchCode = batch.BatchCode;
                    item.ExpiryDate = batch.ExpiryDate;
                    item.UnitPrice = batch.SalePrice;
                    item.AvailableStock = batch.RemainingQuantity;
                    item.Quantity = Math.Min(quantity, item.AvailableStock);
                }
            }

            SaveItems(targetBranchId, cart);
        }
    }

    public Task RemoveAsync(int productId, int inventoryId, int? branchId = null)
    {
        int targetBranchId = (branchId.HasValue && branchId.Value > 0) ? branchId.Value : GetActiveBranchId();
        var cart = GetItems(targetBranchId);
        cart.RemoveAll(item => item.ProductId == productId && item.InventoryId == inventoryId);
        SaveItems(targetBranchId, cart);
        return Task.CompletedTask;
    }

    public Task ClearAsync(int? branchId = null)
    {
        int targetBranchId = (branchId.HasValue && branchId.Value > 0) ? branchId.Value : GetActiveBranchId();
        _httpContextAccessor.HttpContext?.Session.Remove(GetSessionKey(targetBranchId));
        return Task.CompletedTask;
    }

    public Task<int> GetItemCountAsync(int? branchId = null)
    {
        int targetBranchId = (branchId.HasValue && branchId.Value > 0) ? branchId.Value : GetActiveBranchId();
        return Task.FromResult(GetItems(targetBranchId).Sum(item => item.Quantity));
    }

    private async Task<WebProductDto?> GetWebProductByIdAsync(int productId)
    {
        var response = await _tcpClient.SendRequestAsync("GET_WEB_PRODUCT_DETAIL", productId.ToString());
        if (response.Status == "SUCCESS" && !string.IsNullOrEmpty(response.Data))
        {
            return JsonSerializer.Deserialize<WebProductDto>(response.Data, JsonOptions);
        }
        return null;
    }

    private async Task<IReadOnlyList<BranchDto>> GetBranchesAsync()
    {
        var response = await _tcpClient.SendRequestAsync("GET_BRANCHES");
        if (response.Status == "SUCCESS" && !string.IsNullOrEmpty(response.Data))
        {
            var result = JsonSerializer.Deserialize<BranchListResponse>(response.Data, JsonOptions);
            return result?.Items ?? [];
        }
        return [];
    }

    private List<CartItem> GetItems(int branchId)
    {
        var json = _httpContextAccessor.HttpContext?.Session.GetString(GetSessionKey(branchId));
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<CartItem>>(json) ?? [];
    }

    private void SaveItems(int branchId, List<CartItem> items)
    {
        _httpContextAccessor.HttpContext?.Session.SetString(GetSessionKey(branchId), JsonSerializer.Serialize(items));
    }
}