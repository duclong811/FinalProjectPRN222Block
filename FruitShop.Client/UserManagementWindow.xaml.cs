using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;
using System.Windows.Controls;

namespace FruitShop.Client;

public partial class UserManagementWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);
    private List<UserDto> _users = new();

    public UserManagementWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadUsersAsync();
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

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadUsersAsync();

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
            var newPass = Microsoft.VisualBasic.Interaction.InputBox($"Nhập mật khẩu mới cho '{user.Username}':", "Reset Mật Khẩu", "admin123");
            if (string.IsNullOrWhiteSpace(newPass)) return;

            var res = await _clientService.ResetUserPasswordAsync(new ResetPasswordRequest { UserId = user.Id, NewPassword = newPass });
            MessageBox.Show(res.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}