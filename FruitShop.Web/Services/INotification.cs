namespace FruitShop.Web.Services;

public interface INotification
{
    Task<bool> Send(MessageNotification messageNotification);
}

public class MessageNotification
{
    public string To { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string subject { get; set; } = string.Empty;
}