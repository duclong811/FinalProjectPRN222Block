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
            var response = await _clientService.GetInventoryAsync();
            if (response.Success)
            {
                InventoryDataGrid.ItemsSource = response.Items;
                StatusTextBlock.Text = $"Loaded {response.Items.Count} inventory batches.";
            }
            else
            {
                StatusTextBlock.Text = response.Message;
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadInventoryAsync();

    private void ViewBatchesButton_Click(object sender, RoutedEventArgs e)
    {
        if (InventoryDataGrid.SelectedItem is InventoryDto item)
        {
            new InventoryBatchesWindow(item.ProductId) { Owner = this }.ShowDialog();
        }
        else
        {
            MessageBox.Show("Please select an inventory item.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void UpdateStockButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new UpdateStockWindow { Owner = this };
        if (window.ShowDialog() == true)
        {
            _ = LoadInventoryAsync();
        }
    }

    private void InventoryDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (InventoryDataGrid.SelectedItem is InventoryDto item)
        {
            new InventoryBatchesWindow(item.ProductId) { Owner = this }.ShowDialog();
        }
    }
}
