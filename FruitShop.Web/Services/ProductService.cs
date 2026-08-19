using System.Text.Json;
using FruitShop.Shared.Contracts;
using FruitShop.Web.Models;
using FruitShop.Web.ViewModels;

namespace FruitShop.Web.Services;

public sealed class ProductService : IProductService
{
    private readonly TcpClientService _tcpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ProductService(TcpClientService tcpClient)
    {
        _tcpClient = tcpClient;
    }

    public async Task<IReadOnlyList<ProductDto>> GetActiveProductsAsync()
    {
        var response = await _tcpClient.SendRequestAsync("GET_PRODUCTS");
        if (response.Status == "SUCCESS" && !string.IsNullOrEmpty(response.Data))
        {
            var result = JsonSerializer.Deserialize<ProductListResponse>(response.Data, JsonOptions);
            return result?.Items ?? [];
        }
        return [];
    }

    public async Task<IReadOnlyList<BranchDto>> GetBranchesAsync()
    {
        var response = await _tcpClient.SendRequestAsync("GET_BRANCHES");
        if (response.Status == "SUCCESS" && !string.IsNullOrEmpty(response.Data))
        {
            var result = JsonSerializer.Deserialize<BranchListResponse>(response.Data, JsonOptions);
            return result?.Items ?? [];
        }
        return [];
    }

    public async Task<ProductListViewModel> GetProductListAsync(ProductListRequest request)
    {
        var reqDto = new GetProductsPagedRequest
        {
            CategoryId = request.CategoryId,
            BranchId = request.BranchId,
            PriceRange = request.PriceRange,
            Sort = request.Sort,
            Page = request.Page,
            PageSize = request.PageSize
        };

        var response = await _tcpClient.SendRequestAsync("GET_PRODUCTS_PAGED", JsonSerializer.Serialize(reqDto, JsonOptions));
        if (response.Status == "SUCCESS" && !string.IsNullOrEmpty(response.Data))
        {
            var pagedRes = JsonSerializer.Deserialize<WebProductPagedResponse>(response.Data, JsonOptions);
            if (pagedRes != null)
            {
                return new ProductListViewModel
                {
                    Products = pagedRes.Items ?? [],
                    Categories = pagedRes.Categories ?? [],
                    TotalItems = pagedRes.TotalItems,
                    Page = pagedRes.Page,
                    PageSize = pagedRes.PageSize,
                    TotalPages = pagedRes.TotalPages,
                    CategoryId = request.CategoryId,
                    BranchId = request.BranchId,
                    PriceRange = request.PriceRange,
                    Sort = request.Sort
                };
            }
        }

        return new ProductListViewModel
        {
            Products = [],
            Categories = [],
            TotalItems = 0,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = 0,
            CategoryId = request.CategoryId,
            BranchId = request.BranchId,
            PriceRange = request.PriceRange,
            Sort = request.Sort
        };
    }
}