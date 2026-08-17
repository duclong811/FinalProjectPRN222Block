namespace FruitShop.Shared.Helpers;

public class TcpResponse
{
    public string Status { get; set; } = "SUCCESS";
    public string Message { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}
