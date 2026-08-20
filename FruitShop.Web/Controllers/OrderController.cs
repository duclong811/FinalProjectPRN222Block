using FruitShop.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Web.Controllers;

public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(IOrderService orderService, ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> MyOrders()
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem danh sách đơn hàng của bạn.";
            return RedirectToAction("Login", "Account", new { returnUrl = "/Order/MyOrders" });
        }

        var orders = await _orderService.GetMyOrdersAsync(userId.Value);
        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(int orderId)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var response = await _orderService.CancelOrderAsync(orderId);
        if (response.Status == "SUCCESS")
        {
            TempData["OrderSuccessMessage"] = "Hủy đơn hàng thành công! Số lượng sản phẩm đã được hoàn trả.";
        }
        else
        {
            TempData["OrderErrorMessage"] = response.Message ?? "Không thể hủy đơn hàng. Vui lòng thử lại sau.";
        }

        return RedirectToAction(nameof(MyOrders));
    }
}
