using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;
using System.Windows.Controls;

namespace FruitShop.Client;

public partial class DashboardWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);
    private List<ProductDto> _allProducts = new();
    private readonly string _roleName;
    private readonly int? _branchId;
    private readonly string? _branchName;

    public DashboardWindow(string userName, string roleName = "Admin", int? branchId = null, string? branchName = null)
    {
        InitializeComponent();
        _roleName = roleName;
        _branchId = branchId;
        _branchName = branchName;

        AdminNameTextBlock.Text = userName;
        RoleNameTextBlock.Text = !string.IsNullOrEmpty(branchName) ? $"{roleName} ({branchName})" : roleName;

        ConfigureRoleUI();
        Loaded += async (_, _) => await LoadProductsAsync();
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
            PageTitleTextBlock.Text = "Quản Lý Hệ Thống Admin";
            PageSubtitleTextBlock.Text = "Bấm nút 'Quản lý tài khoản' ở menu bên trái để phân quyền và tạo tài khoản Manager/Staff.";
        }
        else if (_roleName.Equals("Manager", StringComparison.OrdinalIgnoreCase))
        {
            Title = $"FruitShop Manager - {_branchName ?? "Chi Nhánh"}";
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

            PageTitleTextBlock.Text = "Quản Lý Đơn Hàng Nhân Viên";
            PageSubtitleTextBlock.Text = "Bấm nút 'Quản lý đơn hàng' ở menu bên trái để duyệt đơn.";
        }
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            var response = await _clientService.GetProductsAsync();
            if (!response.Success)
            {
                StatusTextBlock.Text = response.Message;
                return;
            }

            _allProducts = response.Items;
            ProductsDataGrid.ItemsSource = _allProducts;
            StatusTextBlock.Text = $"Đã tải {_allProducts.Count} sản phẩm.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Lỗi: {ex.Message}";
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadProductsAsync();

    private async void AddProductButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new AddProductWindow { Owner = this };
        if (window.ShowDialog() == true)
        {
            await LoadProductsAsync();
        }
    }

    private void UsersNavigationButton_Click(object sender, RoutedEventArgs e) => new UserManagementWindow { Owner = this }.ShowDialog();
    private void BranchesNavigationButton_Click(object sender, RoutedEventArgs e) => new BranchManagementWindow { Owner = this }.ShowDialog();
    private void ProductsNavigationButton_Click(object sender, RoutedEventArgs e) => _ = LoadProductsAsync();
    private void InventoryNavigationButton_Click(object sender, RoutedEventArgs e) => new InventoryWindow { Owner = this }.ShowDialog();
    private void OrdersNavigationButton_Click(object sender, RoutedEventArgs e) => new OrdersWindow { Owner = this }.ShowDialog();
    private void ExpiryNavigationButton_Click(object sender, RoutedEventArgs e) => new ExpiryManagementWindow { Owner = this }.ShowDialog();
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