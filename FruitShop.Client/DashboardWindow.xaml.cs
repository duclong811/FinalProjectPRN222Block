using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;
using System.Windows.Controls;

namespace FruitShop.Client;

public partial class DashboardWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);
    private List<ProductDto> _allProducts = new();
    private List<UserDto> _users = new();
    private List<BranchDto> _branches = new();
    private List<OrderDto> _allOrders = new();
    private readonly string _roleName;
    private readonly int? _branchId;
    private readonly string? _branchName;
    private int? _currentSelectedBranchId;
    private string _selectedOrderStatusFilter = "ALL";
    private bool _isLoadingBranches = false;

    public DashboardWindow(string userName, string roleName = "Admin", int? branchId = null, string? branchName = null)
    {
        InitializeComponent();
        _roleName = roleName;
        _branchId = branchId;
        _branchName = branchName;
        _currentSelectedBranchId = branchId;

        AdminNameTextBlock.Text = userName;
        RoleNameTextBlock.Text = !string.IsNullOrEmpty(branchName) ? $"{roleName} ({branchName})" : roleName;

        ConfigureRoleUI();
        Loaded += async (_, _) =>
        {
            await LoadBranchesAsync();
            if (_roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                await LoadUsersAsync();
            }
            else if (_roleName.Equals("Staff", StringComparison.OrdinalIgnoreCase))
            {
                await LoadOrdersAsync();
            }
            else
            {
                await LoadProductsAsync();
            }
            await LoadNotificationCountAsync();
        };
    }

    private void ConfigureRoleUI()
    {
        if (_roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            Title = "FruitShop Admin Panel";
            DashboardSubtitleTextBlock.Text = "ADMIN PANEL";

            UsersNavigationButton.Visibility = Visibility.Visible;
            BranchesNavigationButton.Visibility = Visibility.Collapsed;
            ProductsNavigationButton.Visibility = Visibility.Collapsed;
            InventoryNavigationButton.Visibility = Visibility.Collapsed;
            ExpiryNavigationButton.Visibility = Visibility.Collapsed;
            OrdersNavigationButton.Visibility = Visibility.Collapsed;

            ProductActionPanel.Visibility = Visibility.Collapsed;
            UserActionPanel.Visibility = Visibility.Visible;
            OrderActionPanel.Visibility = Visibility.Collapsed;

            ProductsDataGrid.Visibility = Visibility.Collapsed;
            UsersDataGrid.Visibility = Visibility.Visible;
            OrdersDataGrid.Visibility = Visibility.Collapsed;

            BranchBadgeTextBlock.Visibility = Visibility.Collapsed;

            PageTitleTextBlock.Text = "Quản Lý Người Dùng & Phân Quyền";
            PageSubtitleTextBlock.Text = "Danh sách tài khoản Admin, Manager, Staff và Customer trong hệ thống.";
        }
        else if (_roleName.Equals("Manager", StringComparison.OrdinalIgnoreCase))
        {
            Title = $"FruitShop Manager - {_branchName ?? "Hệ thống"}";
            DashboardSubtitleTextBlock.Text = "MANAGER PANEL";

            UsersNavigationButton.Visibility = Visibility.Collapsed;
            BranchesNavigationButton.Visibility = Visibility.Visible;
            ProductsNavigationButton.Visibility = Visibility.Visible;
            InventoryNavigationButton.Visibility = Visibility.Visible;
            OrdersNavigationButton.Visibility = Visibility.Visible;
            ExpiryNavigationButton.Visibility = Visibility.Visible;

            ProductActionPanel.Visibility = Visibility.Visible;
            UserActionPanel.Visibility = Visibility.Collapsed;
            OrderActionPanel.Visibility = Visibility.Collapsed;

            ProductsDataGrid.Visibility = Visibility.Visible;
            UsersDataGrid.Visibility = Visibility.Collapsed;
            OrdersDataGrid.Visibility = Visibility.Collapsed;
        }
        else
        {
            Title = $"FruitShop Staff - {_branchName ?? "Chi Nhánh"}";
            DashboardSubtitleTextBlock.Text = "STAFF PANEL";

            UsersNavigationButton.Visibility = Visibility.Collapsed;
            BranchesNavigationButton.Visibility = Visibility.Collapsed;
            ProductsNavigationButton.Visibility = Visibility.Collapsed;
            InventoryNavigationButton.Visibility = Visibility.Collapsed;
            ExpiryNavigationButton.Visibility = Visibility.Collapsed;
            OrdersNavigationButton.Visibility = Visibility.Visible;

            ProductActionPanel.Visibility = Visibility.Collapsed;
            UserActionPanel.Visibility = Visibility.Collapsed;
            OrderActionPanel.Visibility = Visibility.Visible;

            ProductsDataGrid.Visibility = Visibility.Collapsed;
            UsersDataGrid.Visibility = Visibility.Collapsed;
            OrdersDataGrid.Visibility = Visibility.Visible;

            BranchBadgeTextBlock.Visibility = Visibility.Visible;
            BranchBadgeTextBlock.Text = $"🏢 Chi nhánh: {_branchName ?? "Chi nhánh"}";

            PageTitleTextBlock.Text = "Quản Lý Đơn Hàng";
            PageSubtitleTextBlock.Text = "Duyệt đơn, điều phối giao hàng và theo dõi thanh toán đơn hàng thời gian thực.";
        }
    }

    private async Task LoadBranchesAsync()
    {
        try
        {
            _isLoadingBranches = true;
            var response = await _clientService.GetBranchesAsync();
            var branchList = new List<BranchDto>
            {
                new BranchDto
                {
                    Id = 0,
                    BranchName = "Toàn bộ hệ thống (Tất cả kho)"
                }
            };

            if (response.Success && response.Items.Count > 0)
            {
                branchList.AddRange(response.Items);
            }

            _branches = branchList;
            BranchFilterComboBox.ItemsSource = _branches;

            if (_branchId.HasValue && _branchId.Value > 0 && _branches.Any(b => b.Id == _branchId.Value))
            {
                BranchFilterComboBox.SelectedValue = _branchId.Value;
                _currentSelectedBranchId = _branchId.Value;
            }
            else
            {
                BranchFilterComboBox.SelectedIndex = 0;
                _currentSelectedBranchId = null;
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
                _currentSelectedBranchId = selectedBranch.Id;
                BranchBadgeTextBlock.Text = $"🏢 Chi nhánh: {selectedBranch.BranchName}";
            }
            else
            {
                _currentSelectedBranchId = null;
                BranchBadgeTextBlock.Text = "🏢 Chi nhánh: Toàn bộ hệ thống";
            }

            await LoadProductsAsync();
            await LoadNotificationCountAsync();
        }
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            var response = await _clientService.GetProductsAsync(_currentSelectedBranchId);

            if (response.Success)
            {
                _allProducts = response.Items;
                ProductsDataGrid.ItemsSource = _allProducts;
                var branchName = _currentSelectedBranchId.HasValue
                    ? _branches.FirstOrDefault(b => b.Id == _currentSelectedBranchId.Value)?.BranchName ?? "Chi nhánh"
                    : "Toàn bộ hệ thống";

                StatusTextBlock.Text = $"Đã tải {_allProducts.Count} sản phẩm (Kho: {branchName}).";
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

    private async Task LoadNotificationCountAsync()
    {
        try
        {
            var response = await _clientService.GetNotificationsAsync(_currentSelectedBranchId);
            if (response.Success)
            {
                int unreadCount = response.Items.Count(n => !n.IsRead);
                if (unreadCount > 0)
                {
                    NotificationBadgeBorder.Visibility = Visibility.Visible;
                    NotificationBadgeTextBlock.Text = unreadCount > 99 ? "99+" : unreadCount.ToString();
                }
                else
                {
                    NotificationBadgeBorder.Visibility = Visibility.Collapsed;
                }
            }
        }
        catch { }
    }

    private void NotificationBellButton_Click(object sender, RoutedEventArgs e)
    {
        var notifWindow = new NotificationsWindow(_currentSelectedBranchId) { Owner = this };
        notifWindow.ShowDialog();
        _ = LoadNotificationCountAsync();
    }

    private void AddProductButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new AddProductWindow { Owner = this };
        if (window.ShowDialog() == true)
        {
            _ = LoadProductsAsync();
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadProductsAsync();
        await LoadNotificationCountAsync();
    }

    // ==========================================
    // ORDER MANAGEMENT (Staff Role)
    // ==========================================
    private async Task LoadOrdersAsync()
    {
        try
        {
            var response = await _clientService.GetOrdersAsync(_currentSelectedBranchId ?? _branchId);
            if (response.Success)
            {
                _allOrders = response.Items;
                ApplyOrderStatusFilter();
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

    private void OrderStatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OrderStatusFilterComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            _selectedOrderStatusFilter = tag;
            ApplyOrderStatusFilter();
        }
    }

    private void ApplyOrderStatusFilter()
    {
        if (_allOrders == null) return;

        var filtered = _allOrders.AsEnumerable();

        if (!string.IsNullOrEmpty(_selectedOrderStatusFilter) && !_selectedOrderStatusFilter.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            if (_selectedOrderStatusFilter.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(o => o.OrderStatus.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) || o.OrderStatus.Equals("Confirm", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                filtered = filtered.Where(o => o.OrderStatus.Equals(_selectedOrderStatusFilter, StringComparison.OrdinalIgnoreCase));
            }
        }

        var list = filtered.ToList();
        OrdersDataGrid.ItemsSource = list;

        var branchName = _branchName ?? "Chi nhánh";
        StatusTextBlock.Text = $"Hiển thị {list.Count}/{_allOrders.Count} đơn hàng (Chi nhánh: {branchName}).";
    }

    private async void RefreshOrdersButton_Click(object sender, RoutedEventArgs e) => await LoadOrdersAsync();

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

    // ==========================================
    // USER MANAGEMENT (Admin Role)
    // ==========================================
    private async Task LoadUsersAsync()
    {
        try
        {
            var response = await _clientService.GetUsersAsync();
            if (response.Success)
            {
                _users = response.Items;
                UsersDataGrid.ItemsSource = _users;
                StatusTextBlock.Text = $"Đã tải {_users.Count} người dùng.";
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

    private void RefreshUsersButton_Click(object sender, RoutedEventArgs e) => _ = LoadUsersAsync();

    private async void AddUserButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CreateAccountWindow { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            var request = new CreateUserRequest
            {
                Username = dialog.AccountUsername,
                FullName = dialog.AccountFullName,
                Password = dialog.AccountPassword,
                RoleId = dialog.RoleId,
                BranchId = dialog.BranchId,
                Email = dialog.Email,
                Phone = dialog.Phone,
                Address = dialog.Address
            };

            var res = await _clientService.CreateUserAsync(request);
            MessageBox.Show(res.Message, "Thông báo", MessageBoxButton.OK, res.Status == "SUCCESS" ? MessageBoxImage.Information : MessageBoxImage.Warning);
            await LoadUsersAsync();
        }
    }

    private async void ToggleActive_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is UserDto user)
        {
            var res = await _clientService.ToggleUserActiveAsync(user.Id);
            MessageBox.Show(res.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadUsersAsync();
        }
    }

    private async void ResetPassword_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is UserDto user)
        {
            var newPass = Microsoft.VisualBasic.Interaction.InputBox($"Nhập mật khẩu mới cho '{user.Username}':", "Reset Mật Khẩu", "");
            if (string.IsNullOrWhiteSpace(newPass)) return;

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn đổi mật khẩu cho tài khoản '{user.Username}' không?",
                "Xác nhận đổi mật khẩu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            var res = await _clientService.ResetUserPasswordAsync(new ResetPasswordRequest { UserId = user.Id, NewPassword = newPass });
            MessageBox.Show(res.Message, "Thông báo", MessageBoxButton.OK, res.Status == "SUCCESS" ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
    }

    private void UsersNavigationButton_Click(object sender, RoutedEventArgs e) => _ = LoadUsersAsync();
    private void BranchesNavigationButton_Click(object sender, RoutedEventArgs e) => new BranchManagementWindow { Owner = this }.ShowDialog();
    private void ProductsNavigationButton_Click(object sender, RoutedEventArgs e) => _ = LoadProductsAsync();
    private void InventoryNavigationButton_Click(object sender, RoutedEventArgs e) => new InventoryWindow(_currentSelectedBranchId) { Owner = this }.ShowDialog();
    private void OrdersNavigationButton_Click(object sender, RoutedEventArgs e)
    {
        if (_roleName.Equals("Staff", StringComparison.OrdinalIgnoreCase))
        {
            _ = LoadOrdersAsync();
        }
        else
        {
            new OrdersWindow(_currentSelectedBranchId) { Owner = this }.ShowDialog();
        }
    }
    private void ExpiryNavigationButton_Click(object sender, RoutedEventArgs e) => new ExpiryManagementWindow(_currentSelectedBranchId) { Owner = this }.ShowDialog();
    private void SignOutButton_Click(object sender, RoutedEventArgs e)
    {
        new MainWindow().Show();
        Close();
    }

    private void ProductsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductsDataGrid.SelectedItem is ProductDto product)
        {
            var window = new ProductDetailWindow(product.Id) { Owner = this };
            if (window.ShowDialog() == true)
            {
                _ = LoadProductsAsync();
            }
        }
    }
}