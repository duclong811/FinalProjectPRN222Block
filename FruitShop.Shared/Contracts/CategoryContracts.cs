namespace FruitShop.Shared.Contracts;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
}

public class CategoryListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<CategoryDto> Items { get; set; } = new();
}
