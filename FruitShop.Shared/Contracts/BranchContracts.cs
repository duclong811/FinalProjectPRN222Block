namespace FruitShop.Shared.Contracts;

public class BranchDto
{
    public int Id { get; set; }
    public int ManagerId { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BranchListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<BranchDto> Items { get; set; } = new();
}

public class CreateBranchRequest
{
    public int ManagerId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class UpdateBranchRequest
{
    public int BranchId { get; set; }
    public int ManagerId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
}
