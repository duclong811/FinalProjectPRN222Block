using System.Text.Json;

namespace FruitShop.Server.Services;

public sealed class ServerSettings
{
    public required string ConnectionString { get; init; }
    public required string WebImageRoot { get; init; }
    public int Port { get; init; } = 5055;

    public static ServerSettings Load(string[] args)
    {
        var environmentConnectionString = Environment.GetEnvironmentVariable("FRUITSHOP_CONNECTION_STRING");
        var connectionString = environmentConnectionString;
        var port = 5055;
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        if (File.Exists(settingsPath))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
            var root = document.RootElement;
            if (string.IsNullOrWhiteSpace(connectionString) && root.TryGetProperty("StringUrlSQL", out var configuredConnection))
                connectionString = configuredConnection.GetString();

            if (root.TryGetProperty("Port", out var configuredPort) && configuredPort.TryGetInt32(out var parsedPort))
                port = parsedPort;
        }

        var portArgument = args.FirstOrDefault(argument => argument.StartsWith("--port=", StringComparison.OrdinalIgnoreCase));
        if (portArgument is not null && int.TryParse(portArgument["--port=".Length..], out var argumentPort))
            port = argumentPort;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A database connection string is required. Set FRUITSHOP_CONNECTION_STRING or fill FruitShop.Server/appsettings.json.");
        }

        var webImageRoot = Environment.GetEnvironmentVariable("FRUITSHOP_WEB_IMAGE_ROOT")
            ?? FindWebImageRoot();
        return new ServerSettings { ConnectionString = connectionString, WebImageRoot = webImageRoot, Port = port };
    }

    private static string FindWebImageRoot()
    {
        foreach (var startDirectory in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, "FruitShop.Web", "wwwroot", "images", "products");
                if (Directory.Exists(Path.Combine(directory.FullName, "FruitShop.Web", "wwwroot")))
                    return candidate;
            }
        }

        throw new InvalidOperationException("Could not find FruitShop.Web/wwwroot. Set FRUITSHOP_WEB_IMAGE_ROOT to the product-image folder.");
    }
}
