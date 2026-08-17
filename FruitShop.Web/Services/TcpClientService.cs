using System.Net.Sockets;
using System.Text.Json;
using FruitShop.Shared.Contracts;
using FruitShop.Shared.Helpers;

namespace FruitShop.Web.Services;

public class TcpClientService
{
    private readonly string _host;
    private readonly int _port;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TcpClientService(IConfiguration configuration)
    {
        _host = configuration["TcpServerHost"] ?? "127.0.0.1";
        _port = int.TryParse(configuration["TcpServerPort"], out var p) ? p : 5055;
    }

    public async Task<TcpResponse> SendRequestAsync(string action, string? data = null)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await client.ConnectAsync(_host, _port, cts.Token);
            using var stream = client.GetStream();

            var req = new TcpRequest { Action = action, Data = data ?? string.Empty };
            var reqJson = JsonSerializer.Serialize(req, JsonOptions);
            await TcpHelper.SendMessageAsync(stream, reqJson);

            var resJson = await TcpHelper.ReceiveMessageAsync(stream);
            if (string.IsNullOrEmpty(resJson))
                return new TcpResponse { Status = "ERROR", Message = "Máy chủ TCP không phản hồi." };

            return JsonSerializer.Deserialize<TcpResponse>(resJson, JsonOptions)
                ?? new TcpResponse { Status = "ERROR", Message = "Lỗi giải mã phản hồi TCP." };
        }
        catch (Exception ex)
        {
            return new TcpResponse { Status = "ERROR", Message = $"Không thể kết nối đến TCP Server ({_host}:{_port}): {ex.Message}" };
        }
    }
}