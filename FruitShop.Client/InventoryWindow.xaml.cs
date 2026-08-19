using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;
using System.Windows.Input;

namespace FruitShop.Client;

public partial class InventoryWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);

    public InventoryWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadInventoryAsync();
    }

    private async Task LoadInventoryAsync()
    {
        try
        {
            var response = await _clientService.GetProductsAsync();
            if (response.Success)
            {
                InventoryDataGrid.ItemsSource = response.Items;
                StatusTextBlock.Text = $"Đã tải {response.Items.Count} sản phẩm trong kho.";
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

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadInventoryAsync();

    private void ViewBatchesButton_Click(object sender, RoutedEventArgs e)
    {
        if (InventoryDataGrid.SelectedItem is ProductDto item)
        {
            new InventoryBatchesWindow(item.Id) { Owner = this }.ShowDialog();
        }
        else
        {
            MessageBox.Show("Vui lòng chọn một sản phẩm để xem lịch sử lô hàng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void UpdateStockButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedProduct = InventoryDataGrid.SelectedItem as ProductDto;
        var window = new UpdateStockWindow(selectedProduct?.Id) { Owner = this };
        if (window.ShowDialog() == true)
        {
            _ = LoadInventoryAsync();
        }
    }

    private void InventoryDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (InventoryDataGrid.SelectedItem is ProductDto item)
        {
            new InventoryBatchesWindow(item.Id) { Owner = this }.ShowDialog();
        }
    }
}
