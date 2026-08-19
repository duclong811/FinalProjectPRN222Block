namespace FruitShop.Shared.Contracts;

public class ProductDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int MinStockThreshold { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public int ProductId => Id;
    public string ProductName => Name;
}

public class CreateProductRequest
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? ImageBase64 { get; set; }
    public string? ImageFileName { get; set; }
    public int MinStockThreshold { get; set; } = 10;
}

public class UpdateProductRequest
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool ReplaceImage { get; set; }
    public string? ImageBase64 { get; set; }
    public string? ImageFileName { get; set; }
    public int MinStockThreshold { get; set; } = 10;
}

public class ProductListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ProductDto> Items { get; set; } = new();
}

public class GetProductsRequest
{
    public int? BranchId { get; set; }
}

public class GetProductsPagedRequest
{
    public int? CategoryId { get; set; }
    public int? BranchId { get; set; }
    public string? PriceRange { get; set; }
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 6;
}

public class WebProductDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal SalePrice { get; set; }
    public int StockQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public bool HasExpiryDiscount { get; set; }
    public int SaleBatchAvailableQuantity { get; set; }
    public DateTime? SaleBatchExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WebProductSaleBatchDto> SaleBatches { get; set; } = new();
}

public class WebProductSaleBatchDto
{
    public int InventoryId { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public int RemainingQuantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal SalePrice { get; set; }
    public bool HasExpiryDiscount { get; set; }
}

public class WebProductPagedResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<WebProductDto> Items { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
