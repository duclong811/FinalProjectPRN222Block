using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;
using System.Windows.Controls;

namespace FruitShop.Client;

public partial class NotificationsWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);
    private readonly int? _branchId;
    private readonly string? _branchName;
    private List<NotificationDto> _notifications = new();

    public NotificationsWindow(int? branchId = null, string? branchName = null)
    {
        InitializeComponent();
        _branchId = branchId;
        _branchName = branchName;

        BranchFilterTextBlock.Text = $"Chi nhánh: {(!string.IsNullOrEmpty(_branchName) ? _branchName : "Toàn bộ hệ thống")}";
        Loaded += async (_, _) => await LoadNotificationsAsync();
    }

    private async Task LoadNotificationsAsync()
    {
        try
        {
            var response = await _clientService.GetNotificationsAsync(_branchId);
            if (!response.Success)
            {
                StatusTextBlock.Text = response.Message;
                return;
            }

            _notifications = response.Items;
            NotificationsItemsControl.ItemsSource = _notifications;
            EmptyTextBlock.Visibility = _notifications.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            StatusTextBlock.Text = $"Đã tải {_notifications.Count} thông báo ({response.UnreadCount} chưa đọc).";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Lỗi: {ex.Message}";
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadNotificationsAsync();

    private async void MarkReadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int notificationId)
        {
            var res = await _clientService.MarkNotificationReadAsync(notificationId);
            if (res.Status == "SUCCESS")
            {
                await LoadNotificationsAsync();
            }
            else
            {
                MessageBox.Show(res.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private async void MarkAllReadButton_Click(object sender, RoutedEventArgs e)
    {
        var res = await _clientService.MarkAllNotificationsReadAsync(_branchId);
        if (res.Status == "SUCCESS")
        {
            await LoadNotificationsAsync();
        }
        else
        {
            MessageBox.Show(res.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void QuickRestockButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is NotificationDto notification)
        {
            var stockWindow = new UpdateStockWindow(notification.ProductId, notification.BranchId ?? _branchId) { Owner = this };
            if (stockWindow.ShowDialog() == true)
            {
                // Sau khi nhập hàng thành công, tự động đánh dấu đã đọc thông báo này
                await _clientService.MarkNotificationReadAsync(notification.Id);
                await LoadNotificationsAsync();
            }
            else
            {
                await LoadNotificationsAsync();
            }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
