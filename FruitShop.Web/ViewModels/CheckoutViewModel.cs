using System.ComponentModel.DataAnnotations;

namespace FruitShop.Web.ViewModels;

public sealed class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(20)]
    public string CustomerPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email để nhận xác nhận đơn hàng.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(150)]
    public string? CustomerEmail { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
    [StringLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Note { get; set; }

    public CartViewModel Cart { get; set; } = new() { Items = [] };
}
