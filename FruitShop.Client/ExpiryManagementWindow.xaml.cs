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
                var today = DateTime.Today;
                var nearExpiry = response.Items
                    .Where(i => i.ExpiryDate.Date >= today && i.ExpiryDate.Date <= today.AddDays(2) && i.RemainingQuantity > 0)
                    .OrderBy(i => i.ExpiryDate)
                    .ToList();

                var expired = response.Items
                    .Where(i => i.ExpiryDate.Date < today && i.RemainingQuantity > 0)
                    .OrderBy(i => i.ExpiryDate)
                    .ToList();

                ExpiringSoonDataGrid.ItemsSource = nearExpiry;
                ExpiredDataGrid.ItemsSource = expired;

                StatusTextBlock.Text = $"Sắp hết hạn (0-2 ngày): {nearExpiry.Count} lô | Đã hết hạn: {expired.Count} lô";
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
