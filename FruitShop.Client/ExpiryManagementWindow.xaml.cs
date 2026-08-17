using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;
using System.Windows.Controls;

namespace FruitShop.Client;

public partial class ExpiryManagementWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);

    public ExpiryManagementWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadExpiryDataAsync();
    }

    private async Task LoadExpiryDataAsync()
    {
        try
        {
            var response = await _clientService.GetInventoryAsync();
            if (response.Success)
            {
                var nearExpiry = response.Items.Where(i => i.ExpiryDate > DateTime.Now && i.ExpiryDate <= DateTime.Now.AddDays(30)).ToList();
                var expired = response.Items.Where(i => i.ExpiryDate <= DateTime.Now).ToList();

                ExpiringSoonDataGrid.ItemsSource = nearExpiry;
                ExpiredDataGrid.ItemsSource = expired;

                StatusTextBlock.Text = $"Near expiry: {nearExpiry.Count} batches | Expired: {expired.Count} batches";
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
        }
    }

    private async void HideSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        InventoryDto? selected = ExpiringSoonDataGrid.SelectedItem as InventoryDto ?? ExpiredDataGrid.SelectedItem as InventoryDto;
        if (selected is null)
        {
            MessageBox.Show("Please select an inventory item.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show($"Hide product '{selected.ProductName}' from sale?", "Confirm Hide", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
        {
            var res = await _clientService.HideProductAsync(selected.ProductId);
            MessageBox.Show(res.Message, res.Status == "SUCCESS" ? "Success" : "Error", MessageBoxButton.OK, res.Status == "SUCCESS" ? MessageBoxImage.Information : MessageBoxImage.Error);
            await LoadExpiryDataAsync();
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadExpiryDataAsync();

    private void ExpiringSoonDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    private void ExpiredDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
}
