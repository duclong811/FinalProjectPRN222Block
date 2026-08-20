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
    private readonly string _roleName;
    private readonly int? _branchId;
    private readonly string? _branchName;
    private int? _currentSelectedBranchId;
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
            ProductsDataGrid.Visibility = Visibility.Collapsed;
            UsersDataGrid.Visibility = Visibility.Visible;
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
            PageTitleTextBlock.Text = "Quản Lý Đơn Hàng Nhân Viên";
            PageSubtitleTextBlock.Text = "Bấm nút 'Quản lý đơn hàng' ở menu bên trái để duyệt đơn.";
        }
    }

    private async Task LoadBranchesAsync()
    {
        try
        {
            _isLoadingBranches = true;
            var response = await _clientService.GetBranchesAsync();
            var branchList = new List<BranchDto>();

            // Option toàn bộ hệ thống cho Manager
            branchList.Add(new BranchDto
            {
                Id = 0,
                BranchName = "Toàn bộ hệ thống (Tất cả kho)"
            });

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
                BranchBadgeTextBlock.Text = $"🏢 Kho Chi Nhánh: {selectedBranch.BranchName}";
                PageSubtitleTextBlock.Text = $"Hiển thị tồn kho của chi nhánh: {selectedBranch.BranchName}";
            }
            else
            {
                _currentSelectedBranchId = null;
                BranchBadgeTextBlock.Text = "🏢 Chi nhánh: Toàn bộ hệ thống";
                PageSubtitleTextBlock.Text = "Hiển thị tổng tồn kho toàn bộ hệ thống các chi nhánh";
            }

            await LoadProductsAsync();
            await LoadNotificationCountAsync();
        }
    }

    private async Task LoadNotificationCountAsync()
    {
        try
        {
            var response = await _clientService.GetNotificationsAsync(_currentSelectedBranchId);
            if (response.Success && response.UnreadCount > 0)
            {
                NotificationBadgeBorder.Visibility = Visibility.Visible;
                NotificationBadgeTextBlock.Text = response.UnreadCount > 99 ? "99+" : response.UnreadCount.ToString();
            }
            else
            {
                NotificationBadgeBorder.Visibility = Visibility.Collapsed;
            }
        }
        catch
        {
            NotificationBadgeBorder.Visibility = Visibility.Collapsed;
        }
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            var response = await _clientService.GetProductsAsync(_currentSelectedBranchId);
            if (!response.Success)
            {
                StatusTextBlock.Text = response.Message;
                return;
            }

            _allProducts = response.Items;
            ProductsDataGrid.ItemsSource = _allProducts;

            var branchText = _currentSelectedBranchId.HasValue
                ? _branches.FirstOrDefault(b => b.Id == _currentSelectedBranchId.Value)?.BranchName ?? "Chi nhánh"
                : "Toàn bộ hệ thống";

            StatusTextBlock.Text = $"Đã tải {_allProducts.Count} sản phẩm (Kho: {branchText}).";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Lỗi: {ex.Message}";
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadProductsAsync();
        await LoadNotificationCountAsync();
    }

    private async void NotificationBellButton_Click(object sender, RoutedEventArgs e)
    {
        var branchName = _currentSelectedBranchId.HasValue
            ? _branches.FirstOrDefault(b => b.Id == _currentSelectedBranchId.Value)?.BranchName
            : "Toàn bộ hệ thống";

        var notifWindow = new NotificationsWindow(_currentSelectedBranchId, branchName) { Owner = this };
        notifWindow.ShowDialog();
        await LoadNotificationCountAsync();
        await LoadProductsAsync();
    }

    private async void AddProductButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new AddProductWindow { Owner = this };
        if (window.ShowDialog() == true)
        {
            await LoadProductsAsync();
        }
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            var response = await _clientService.GetUsersAsync();
            if (!response.Success)
            {
                StatusTextBlock.Text = response.Message;
                return;
            }

            _users = response.Items;
            UsersDataGrid.ItemsSource = _users;
            StatusTextBlock.Text = $"Đã tải {_users.Count} người dùng.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Lỗi: {ex.Message}";
        }
    }

    private async void RefreshUsersButton_Click(object sender, RoutedEventArgs e) => await LoadUsersAsync();

    private async void AddUserButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CreateAccountWindow { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            var request = new CreateUserRequest
            {
                Username = dialog.AccountUsername,
                Password = dialog.AccountPassword,
                FullName = dialog.AccountFullName,
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
    private void OrdersNavigationButton_Click(object sender, RoutedEventArgs e) => new OrdersWindow(_currentSelectedBranchId) { Owner = this }.ShowDialog();
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