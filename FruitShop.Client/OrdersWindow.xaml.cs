using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;
using System.Windows.Controls;

namespace FruitShop.Client;

public partial class OrdersWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);
    private List<BranchDto> _branches = new();
    private List<OrderDto> _allOrders = new();
    private int? _currentBranchId;
    private string _selectedStatusFilter = "ALL";
    private bool _isLoadingBranches = false;

    public OrdersWindow(int? branchId = null)
    {
        InitializeComponent();
        _currentBranchId = branchId;
        Loaded += async (_, _) =>
        {
            await LoadBranchesAsync();
            await LoadOrdersAsync();
        };
    }

    private async Task LoadBranchesAsync()
    {
        try
        {
            _isLoadingBranches = true;
            var response = await _clientService.GetBranchesAsync();
            var branchList = new List<BranchDto>
            {
                new BranchDto { Id = 0, BranchName = "Toàn bộ hệ thống (Tất cả chi nhánh)" }
            };

            if (response.Success && response.Items.Count > 0)
            {
                branchList.AddRange(response.Items);
            }

            _branches = branchList;
            BranchFilterComboBox.ItemsSource = _branches;

            if (_currentBranchId.HasValue && _currentBranchId.Value > 0 && _branches.Any(b => b.Id == _currentBranchId.Value))
            {
                BranchFilterComboBox.SelectedValue = _currentBranchId.Value;
            }
            else
            {
                BranchFilterComboBox.SelectedIndex = 0;
                _currentBranchId = null;
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Lỗi tải chi nhánh: {ex.Message}";
        }
        finally
        {
            _isLoadingBranches = false;
        }
    }

    private async void BranchFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingBranches) return;

        if (BranchFilterComboBox.SelectedItem is BranchDto selectedBranch)
        {
            if (selectedBranch.Id > 0)
            {
                _currentBranchId = selectedBranch.Id;
                BranchBadgeTextBlock.Text = $"🏢 Chi nhánh: {selectedBranch.BranchName}";
            }
            else
            {
                _currentBranchId = null;
                BranchBadgeTextBlock.Text = "🏢 Chi nhánh: Toàn bộ hệ thống";
            }

            await LoadOrdersAsync();
        }
    }

    private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StatusFilterComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            _selectedStatusFilter = tag;
            ApplyOrderFilter();
        }
    }

    private void ApplyOrderFilter()
    {
        if (_allOrders == null) return;

        var filtered = _allOrders.AsEnumerable();

        if (!string.IsNullOrEmpty(_selectedStatusFilter) && !_selectedStatusFilter.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            if (_selectedStatusFilter.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(o => o.OrderStatus.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) || o.OrderStatus.Equals("Confirm", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                filtered = filtered.Where(o => o.OrderStatus.Equals(_selectedStatusFilter, StringComparison.OrdinalIgnoreCase));
            }
        }

        var list = filtered.ToList();
        OrdersDataGrid.ItemsSource = list;

        var branchName = _currentBranchId.HasValue
            ? _branches.FirstOrDefault(b => b.Id == _currentBranchId.Value)?.BranchName ?? "Chi nhánh"
            : "Toàn bộ hệ thống";

        StatusTextBlock.Text = $"Hiển thị {list.Count}/{_allOrders.Count} đơn hàng (Chi nhánh: {branchName}).";
    }

    private async Task LoadOrdersAsync()
    {
        try
        {
            var response = await _clientService.GetOrdersAsync(_currentBranchId);
            if (response.Success)
            {
                _allOrders = response.Items;
                ApplyOrderFilter();
            }
            else
            {
                StatusTextBlock.Text = response.Message;
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Lỗi: {ex.Message}";
        }
    }

    // 1. Xác nhận đơn: Pending -> Confirmed
    private async void ConfirmOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is OrderDto order)
        {
            var confirm = MessageBox.Show(
                $"Xác nhận đơn hàng '{order.OrderCode}' của khách '{order.CustomerName}'?",
                "Xác nhận đơn hàng",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var response = await _clientService.UpdateOrderStatusAsync(new UpdateOrderStatusRequest
                {
                    OrderId = order.Id,
                    Status = "Confirmed"
                });

                if (response.Status == "SUCCESS")
                {
                    MessageBox.Show("Xác nhận đơn hàng thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadOrdersAsync();
                }
                else
                {
                    MessageBox.Show(response.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi cập nhật trạng thái: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 2. Bắt đầu giao hàng: Confirmed -> Shipping
    private async void ShipOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is OrderDto order)
        {
            var confirm = MessageBox.Show(
                $"Bắt đầu giao hàng cho đơn '{order.OrderCode}' đến địa chỉ:\n{order.ShippingAddress}?",
                "Bắt đầu giao hàng",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var response = await _clientService.UpdateOrderStatusAsync(new UpdateOrderStatusRequest
                {
                    OrderId = order.Id,
                    Status = "Shipping"
                });

                if (response.Status == "SUCCESS")
                {
                    MessageBox.Show("Bắt đầu giao hàng thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadOrdersAsync();
                }
                else
                {
                    MessageBox.Show(response.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi cập nhật trạng thái: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 3. Đã giao hàng thành công: Shipping -> Completed
    private async void CompleteOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is OrderDto order)
        {
            var confirm = MessageBox.Show(
                $"Xác nhận đơn hàng '{order.OrderCode}' đã được giao thành công cho khách hàng '{order.CustomerName}' và thu đủ {order.TotalAmount:N0} đ?",
                "Xác nhận giao hàng thành công",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var response = await _clientService.UpdateOrderStatusAsync(new UpdateOrderStatusRequest
                {
                    OrderId = order.Id,
                    Status = "Completed"
                });

                if (response.Status == "SUCCESS")
                {
                    MessageBox.Show("Giao hàng thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadOrdersAsync();
                }
                else
                {
                    MessageBox.Show(response.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi cập nhật trạng thái: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 4. Hủy đơn hàng -> Cancelled
    private async void CancelOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is OrderDto order)
        {
            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn hủy đơn hàng '{order.OrderCode}' không?",
                "Xác nhận hủy đơn",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var response = await _clientService.UpdateOrderStatusAsync(new UpdateOrderStatusRequest
                {
                    OrderId = order.Id,
                    Status = "Cancelled"
                });

                if (response.Status == "SUCCESS")
                {
                    MessageBox.Show("Hủy đơn hàng thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadOrdersAsync();
                }
                else
                {
                    MessageBox.Show(response.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hủy đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadOrdersAsync();
}
