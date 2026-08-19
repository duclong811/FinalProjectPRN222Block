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

        int? branchId = HttpContext.Session.GetInt32("SelectedBranchId");
        if (branchId == 0) branchId = null;
        string? branchName = HttpContext.Session.GetString("SelectedBranchName");

        var model = await _productService.GetProductListAsync(new ProductListRequest
        {
            CategoryId = categoryId,
            BranchId = branchId,
            PriceRange = priceRange,
            Sort = sort,
            Page = page
        });

        // Attach branch info if not present
        return View(new ViewModels.ProductListViewModel
        {
            Products = model.Products,
            Categories = model.Categories,
            TotalItems = model.TotalItems,
            Page = model.Page,
            PageSize = model.PageSize,
            TotalPages = model.TotalPages,
            CategoryId = model.CategoryId,
            BranchId = branchId,
            BranchName = branchName,
            PriceRange = model.PriceRange,
            Sort = model.Sort
        });
    }
}
