using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;

namespace FruitShop.Client;

public partial class OrdersWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);

    public OrdersWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync()
    {
        try
        {
            var response = await _clientService.GetOrdersAsync();
            if (response.Success)
            {
                OrdersDataGrid.ItemsSource = response.Items;
                StatusTextBlock.Text = $"Loaded {response.Items.Count} orders.";
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

    private async void MarkPaidButton_Click(object sender, RoutedEventArgs e)
    {
        if (OrdersDataGrid.SelectedItem is OrderDto order)
        {
            if (order.PaymentStatus == "Paid")
            {
                MessageBox.Show("This order is already marked as paid.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show($"Mark order '{order.OrderCode}' as paid?", "Confirm Payment", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _clientService.MarkOrderPaidAsync(order.Id);
                    if (response.Status == "SUCCESS")
                    {
                        MessageBox.Show(response.Message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadOrdersAsync();
                    }
                    else
                    {
                        MessageBox.Show(response.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error marking order as paid: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("Please select an order.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadOrdersAsync();
}
