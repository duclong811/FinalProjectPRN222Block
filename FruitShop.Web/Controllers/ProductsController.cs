using FruitShop.Web.Services;
using FruitShop.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Web.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> ListProduct(int? categoryId, string? priceRange, string? sort, int page = 1)
    {
        ViewData["Title"] = "Bộ Sưu Tập Trái Cây Thượng Hạng";
        var model = await _productService.GetProductListAsync(new ProductListRequest
        {
            CategoryId = categoryId,
            PriceRange = priceRange,
            Sort = sort,
            Page = page
        });

        return View(model);
    }
}
