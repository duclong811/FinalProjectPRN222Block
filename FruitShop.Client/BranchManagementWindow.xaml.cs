using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;

namespace FruitShop.Client;

public partial class BranchManagementWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);
    private List<BranchDto> _branches = new();

    public BranchManagementWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadBranchesAsync();
    }

    private async Task LoadBranchesAsync()
    {
        try
        {
            var response = await _clientService.GetBranchesAsync();
            if (!response.Success)
            {
                StatusTextBlock.Text = response.Message;
                return;
            }

            _branches = response.Items;
            BranchesDataGrid.ItemsSource = _branches;
            StatusTextBlock.Text = $"Đã tải {_branches.Count} chi nhánh.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Lỗi: {ex.Message}";
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadBranchesAsync();

    private async void AddBranchButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CreateBranchWindow { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            await LoadBranchesAsync();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}