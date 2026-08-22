using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using FruitShop.Server.Services;
using FruitShop.Shared.Helpers;

namespace FruitShop.Server;

internal static class Program
{
    private const int DefaultPort = 5055;

    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("========================================");
        Console.WriteLine("  FRUITSHOP TCP SERVER STARTING...      ");
        Console.WriteLine("========================================");

        var settings = ServerSettings.Load(args);
        var port = settings.Port > 0 ? settings.Port : DefaultPort;
        var listener = new TcpListener(IPAddress.Any, port);
        using var cancellationSource = new CancellationTokenSource();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
            listener.Stop();
        };

        try
        {
            listener.Start();
            Console.WriteLine($"[INFO] FruitShop TCP Server đang lắng nghe trên cổng {port}.");
            Console.WriteLine("[INFO] Chờ các kết nối từ Client (WPF Admin & Web App)...");

            while (!cancellationSource.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationSource.Token);
                _ = Task.Run(() => HandleClientAsync(client, settings, cancellationSource.Token));
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on Ctrl+C stop
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CRITICAL] Lỗi Server: {ex.Message}");
        }
        finally
        {
            listener.Stop();
            Console.WriteLine("[INFO] Server đã dừng.");
        }
    }

    private static async Task HandleClientAsync(TcpClient client, ServerSettings settings, CancellationToken cancellationToken)
    {
        string clientEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        Console.WriteLine($"[CONNECT] Client mới kết nối từ: {clientEndPoint}");

        using (client)
        using (NetworkStream stream = client.GetStream())
        {
            var authService = new UserAuthenticationService(settings.ConnectionString);
            var regService = new UserRegistrationService(settings.ConnectionString);
            var productService = new ProductManagementService(settings.ConnectionString, settings.WebImageRoot);
            var inventoryService = new InventoryService(settings.ConnectionString);
            var orderService = new OrderService(settings.ConnectionString);
            var userService = new UserService(settings.ConnectionString);
            var branchService = new BranchService(settings.ConnectionString);
            var notificationService = new NotificationService(settings.ConnectionString);
            var reportService = new ReportService(settings.ConnectionString);

            var handler = new RequestHandler(authService, regService, productService, inventoryService, orderService, userService, branchService, notificationService, reportService);

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                try
                {
                    string requestJson = await TcpHelper.ReceiveMessageAsync(stream);
                    if (string.IsNullOrEmpty(requestJson))
                    {
                        break;
                    }

                    var request = JsonSerializer.Deserialize<TcpRequest>(requestJson, jsonOptions);
                    if (request == null)
                    {
                        var errResponse = new TcpResponse { Status = "ERROR", Message = "Yêu cầu không đúng định dạng." };
                        await TcpHelper.SendMessageAsync(stream, JsonSerializer.Serialize(errResponse, jsonOptions));
                        continue;
                    }

                    Console.WriteLine($"[REQUEST] [{clientEndPoint}] Action: {request.Action}");

                    var response = await handler.HandleRequestAsync(request, cancellationToken);
                    string responseJson = JsonSerializer.Serialize(response, jsonOptions);
                    await TcpHelper.SendMessageAsync(stream, responseJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Lỗi xử lý client {clientEndPoint}: {ex.Message}");
                    try
                    {
                        var errResponse = new TcpResponse { Status = "ERROR", Message = $"Lỗi Server: {ex.Message}" };
                        await TcpHelper.SendMessageAsync(stream, JsonSerializer.Serialize(errResponse, jsonOptions));
                    }
                    catch { }
                    break;
                }
            }
        }

        Console.WriteLine($"[DISCONNECT] Client đã ngắt kết nối: {clientEndPoint}");
    }
}