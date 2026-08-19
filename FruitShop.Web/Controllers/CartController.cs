using FruitShop.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Web.Controllers;

public class CartController : Controller
{
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IOrderService _orderService;

    public CartController(IShoppingCartService shoppingCartService, IOrderService orderService)
    {
        _shoppingCartService = shoppingCartService;
        _orderService = orderService;
    }

    public async Task<IActionResult> Index() => View(await _shoppingCartService.GetCartAsync());


    // add new a product 
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int inventoryId, int quantity = 1, string? returnUrl = null)
    {
        var added = await _shoppingCartService.AddAsync(productId, inventoryId, quantity);
        TempData["CartMessage"] = added ? "Đã thêm sản phẩm vào giỏ hàng." : "Sản phẩm không tồn tại hoặc đã hết hàng.";

        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int productId, int inventoryId, int quantity)
    {
        await _shoppingCartService.UpdateQuantityAsync(productId, inventoryId, quantity);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId, int inventoryId)
    {
        await _shoppingCartService.RemoveAsync(productId, inventoryId);
        return RedirectToAction(nameof(Index));
    }
    /// <summary>
    /// /
    /// </summary>
    /// <returns></returns>

    public async Task<IActionResult> Checkout()
    {
        var cart = await _shoppingCartService.GetCartAsync();
        return cart.IsEmpty
            ? RedirectToAction(nameof(Index))
            : View(new ViewModels.CheckoutViewModel { Cart = cart });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(ViewModels.CheckoutViewModel checkout)
    {
        checkout.Cart = await _shoppingCartService.GetCartAsync();
        if (checkout.Cart.IsEmpty)
            return RedirectToAction(nameof(Index));

        if (!ModelState.IsValid)
            return View(checkout);

        var orderCode = await _orderService.CreateCashOrderAsync(checkout);
        if (orderCode is null)
        {
            ModelState.AddModelError(string.Empty, "Giỏ hàng đã trống. Vui lòng chọn sản phẩm lại.");
            return View(checkout);
        }

        return RedirectToAction(nameof(Confirmation), new { orderCode });
    }

    public IActionResult Confirmation(string orderCode)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
            return RedirectToAction(nameof(Index));

        return View(model: orderCode);
    }
}
