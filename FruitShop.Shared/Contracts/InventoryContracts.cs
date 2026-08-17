namespace FruitShop.Shared.Contracts;

public class InventoryDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public int QuantityReceived { get; set; }
    public int RemainingQuantity { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal? UnitCost { get; set; }
    public string? SupplierName { get; set; }
    public string? Note { get; set; }
}

public class ReceiveInventoryRequest
{
    public int ProductId { get; set; }
    public int BranchId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal? UnitCost { get; set; }
    public string? SupplierName { get; set; }
    public string? Note { get; set; }
}

public class InventoryListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<InventoryDto> Items { get; set; } = new();
}
