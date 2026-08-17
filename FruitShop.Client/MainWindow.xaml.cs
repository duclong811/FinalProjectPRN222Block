using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FruitShop.Client;

public partial class MainWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);

    public MainWindow()
    {
        InitializeComponent();
        UsernameTextBox.Focus();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e) => await LoginAsync();
    private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await LoginAsync();
    }

    private async void RegisterButton_Click(object sender, RoutedEventArgs e) => await RegisterAsync();
    private async void ConfirmPasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await RegisterAsync();
    }

    private void ShowRegister_Click(object sender, RoutedEventArgs e)
    {
        LoginPanel.Visibility = Visibility.Collapsed;
        RegisterPanel.Visibility = Visibility.Visible;
        RegisterStatusTextBlock.Text = string.Empty;
        FullNameTextBox.Focus();
    }

    private void ShowLogin_Click(object sender, RoutedEventArgs e)
    {
        RegisterPanel.Visibility = Visibility.Collapsed;
        LoginPanel.Visibility = Visibility.Visible;
        StatusTextBlock.Text = string.Empty;
        UsernameTextBox.Focus();
    }

    private async Task LoginAsync()
    {
        var username = UsernameTextBox.Text.Trim();
        var password = PasswordBox.Password;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            ShowStatus(StatusTextBlock, "Tên đăng nhập và mật khẩu là bắt buộc.", false);
            return;
        }

        LoginButton.IsEnabled = false;
        ShowStatus(StatusTextBlock, "Đang xác thực...", true);
        try
        {
            var response = await _clientService.LoginAsync(username, password);
            if (!response.Success)
            {
                ShowStatus(StatusTextBlock, response.Message, false);
                PasswordBox.SelectAll();
                PasswordBox.Focus();
                return;
            }

            var displayName = string.IsNullOrWhiteSpace(response.FullName) ? response.Username : response.FullName;
            ShowStatus(StatusTextBlock, $"Đăng nhập thành công. Chào {displayName}!", true);

            var dashboard = new DashboardWindow(displayName ?? "User", response.RoleName ?? "Staff", response.BranchId, response.BranchName);
            dashboard.Show();
            Close();
        }
        catch (Exception exception)
        {
            ShowStatus(StatusTextBlock, exception.Message, false);
        }
        finally { LoginButton.IsEnabled = true; }
    }

    private async Task RegisterAsync()
    {
        var fullName = FullNameTextBox.Text.Trim();
        var username = RegisterUsernameTextBox.Text.Trim();
        var password = RegisterPasswordBox.Password;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            ShowStatus(RegisterStatusTextBlock, "Tên đăng nhập và mật khẩu là bắt buộc.", false);
            return;
        }
        if (password != ConfirmPasswordBox.Password)
        {
            ShowStatus(RegisterStatusTextBlock, "Mật khẩu xác nhận không khớp.", false);
            ConfirmPasswordBox.Focus();
            return;
        }

        RegisterButton.IsEnabled = false;
        ShowStatus(RegisterStatusTextBlock, "Đang tạo tài khoản...", true);
        try
        {
            var regReq = new RegisterRequest
            {
                FullName = fullName,
                Username = username,
                Password = password
            };
            var response = await _clientService.RegisterAsync(regReq);
            ShowStatus(RegisterStatusTextBlock, response.Message, response.Success);
            if (!response.Success) return;

            UsernameTextBox.Text = username;
            PasswordBox.Clear();
            RegisterPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
            MessageBox.Show("Tạo tài khoản thành công. Bạn có thể đăng nhập ngay.", "FruitShop", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            ShowStatus(RegisterStatusTextBlock, exception.Message, false);
        }
        finally { RegisterButton.IsEnabled = true; }
    }

    private static void ShowStatus(TextBlock target, string message, bool isSuccess)
    {
        target.Text = message;
        target.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isSuccess ? "#26733A" : "#B42318"));
    }
}