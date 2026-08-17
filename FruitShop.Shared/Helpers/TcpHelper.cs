using System.Net.Sockets;
using System.Text;

namespace FruitShop.Shared.Helpers;

public static class TcpHelper
{
    public static async Task SendMessageAsync(NetworkStream stream, string message)
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);
        
        await stream.WriteAsync(lengthBytes, 0, 4);
        await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
        await stream.FlushAsync();
    }

    public static async Task<string> ReceiveMessageAsync(NetworkStream stream)
    {
        byte[] lengthBytes = new byte[4];
        int bytesRead = 0;
        while (bytesRead < 4)
        {
            int read = await stream.ReadAsync(lengthBytes, 0, 4 - bytesRead);
            if (read == 0) return string.Empty;
            bytesRead += read;
        }

        int length = BitConverter.ToInt32(lengthBytes, 0);
        if (length <= 0) return string.Empty;

        byte[] messageBytes = new byte[length];
        bytesRead = 0;
        while (bytesRead < length)
        {
            int read = await stream.ReadAsync(messageBytes, bytesRead, length - bytesRead);
            if (read == 0) return string.Empty;
            bytesRead += read;
        }

        return Encoding.UTF8.GetString(messageBytes);
    }
}
