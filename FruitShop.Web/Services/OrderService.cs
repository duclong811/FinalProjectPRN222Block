using System.Net;
using System.Text.Json;
using FruitShop.Shared.Contracts;
using FruitShop.Web.ViewModels;

namespace FruitShop.Web.Services;

public sealed class OrderService : IOrderService
{
    private readonly TcpClientService _tcpClient;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly INotification _notification;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OrderService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public OrderService(
        TcpClientService tcpClient,
        IShoppingCartService shoppingCartService,
        INotification notification,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OrderService> logger)
    {
        _tcpClient = tcpClient;
        _shoppingCartService = shoppingCartService;
        _notification = notification;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<string?> CreateCashOrderAsync(CheckoutViewModel checkout)
    {
        var cart = await _shoppingCartService.GetCartAsync();
        if (cart.IsEmpty)
            return null;

        int? customerId = _httpContextAccessor.HttpContext?.Session.GetInt32("UserId");
        int? selectedBranchId = _httpContextAccessor.HttpContext?.Session.GetInt32("SelectedBranchId");
        if (selectedBranchId == 0) selectedBranchId = null;
        if (!selectedBranchId.HasValue)
        {
            selectedBranchId = 1; // Default to branch 1
        }

        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            BranchId = selectedBranchId,
            CustomerName = checkout.CustomerName.Trim(),
            CustomerPhone = checkout.CustomerPhone.Trim(),
            CustomerEmail = string.IsNullOrWhiteSpace(checkout.CustomerEmail) ? null : checkout.CustomerEmail.Trim(),
            ShippingAddress = checkout.ShippingAddress.Trim(),
            Note = string.IsNullOrWhiteSpace(checkout.Note) ? null : checkout.Note.Trim(),
            PaymentMethod = "COD",
            Items = cart.Items.Select(item => new CreateOrderItemRequest
            {
                ProductId = item.ProductId,
                BatchId = item.InventoryId,
                Quantity = item.Quantity,
                DiscountPercent = 0
            }).ToList()
        };

        var response = await _tcpClient.SendRequestAsync("CREATE_ORDER", JsonSerializer.Serialize(request, JsonOptions));
        if (response.Status != "SUCCESS" || string.IsNullOrEmpty(response.Data))
        {
            _logger.LogError("Lỗi tạo đơn hàng qua TCP: {Message}", response.Message);
            return null;
        }

        var orderCode = response.Data;
        await _shoppingCartService.ClearAsync();

        try
        {
            if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
            {
                var emailSent = await _notification.Send(new MessageNotification
                {
                    To = request.CustomerEmail,
                    subject = $"Xác nhận đơn hàng {orderCode}",
                    Content = CreateOrderConfirmationEmail(orderCode, request.CustomerName, cart)
                });

                if (!emailSent)
                    _logger.LogWarning("Order {OrderCode} was created but its confirmation email could not be sent.", orderCode);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Order {OrderCode} was created but its confirmation email could not be sent.", orderCode);
        }

        return orderCode;
    }

    private static string CreateOrderConfirmationEmail(string orderCode, string customerName, CartViewModel cart)
    {
        var items = string.Join("", cart.Items.Select(item =>
            $"<tr><td style=\"padding:8px;border-bottom:1px solid #eee\">{WebUtility.HtmlEncode(item.Name)}</td><td style=\"padding:8px;border-bottom:1px solid #eee\">{item.Quantity}</td><td style=\"padding:8px;border-bottom:1px solid #eee;text-align:right\">{item.UnitPrice * item.Quantity:N0} đ</td></tr>"));

        return $"""
            <div style="font-family:Arial,sans-serif;color:#2C2A29;max-width:600px;margin:auto">
                <h2>Le Fruit Boutique</h2>
                <p>Xin chào {WebUtility.HtmlEncode(customerName)},</p>
                <p>Đơn hàng <strong>{WebUtility.HtmlEncode(orderCode)}</strong> của quý khách đã được tiếp nhận.</p>
                <table style="border-collapse:collapse;width:100%"><thead><tr><th style="text-align:left;padding:8px">Sản phẩm</th><th style="text-align:left;padding:8px">SL</th><th style="text-align:right;padding:8px">Thành tiền</th></tr></thead><tbody>{items}</tbody></table>
                <p style="text-align:right"><strong>Tổng cộng: {cart.TotalAmount:N0} đ</strong></p>
                <p>Phương thức thanh toán: <strong>Tiền mặt khi nhận hàng (COD)</strong>.</p>
                <p>Cảm ơn quý khách đã tin tưởng Le Fruit Boutique.</p>
            </div>
            """;
    }
}