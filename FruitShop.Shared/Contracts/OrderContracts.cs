namespace FruitShop.Shared.Contracts;

public class OrderDto
{
    public int Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string? Note { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string OrderStatus { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int? StaffId { get; set; }
    public string? StaffName { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
    public List<OrderDetailDto> Details { get; set; } = new();
}

public class OrderDetailDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int? BatchId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal SubTotal { get; set; }
}

public class CreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int? BatchId { get; set; }
    public decimal DiscountPercent { get; set; }
}

public class CreateOrderRequest
{
    public int? CustomerId { get; set; }
    public int? BranchId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string PaymentMethod { get; set; } = "COD";
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class UpdateOrderStatusRequest
{
    public int OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? StaffId { get; set; }
}

public class OrderListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<OrderDto> Items { get; set; } = new();
}
