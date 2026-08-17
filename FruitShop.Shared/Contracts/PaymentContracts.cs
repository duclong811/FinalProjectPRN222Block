namespace FruitShop.Shared.Contracts;

public class MarkOrderPaidRequest
{
    public int OrderId { get; set; }
    public string PaymentMethod { get; set; } = "COD";
    public string? TransactionCode { get; set; }
}

public class PaymentDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? TransactionCode { get; set; }
    public DateTime? PaidAt { get; set; }
}
