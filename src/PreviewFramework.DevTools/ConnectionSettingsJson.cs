using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace PreviewFramework.DevTools;

public static class ConnectionSettingsJson
{
    public static void WriteConnectionSettingsJson(int appConnectionPort)
    {
        // Get all IPv4 addresses for the local machine (excluding loopback)
        var addresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
            .Where(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip.Address))
            .Select(ip => ip.Address.ToString())
            .Distinct()
            .ToList();

        // If no addresses found, fallback to 127.0.0.1
        if (addresses.Count == 0)
            addresses.Add("127.0.0.1");

        // Build the connection string
        string appConnectionString = string.Join(",", addresses.Select(ip => $"{ip}:{appConnectionPort}"));

        // Prepare the JSON object
        var jsonObj = new { app = appConnectionString };

        // Get the user's home directory
        string homeDir = Environment.GetFolderPath(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Environment.SpecialFolder.UserProfile
                : Environment.SpecialFolder.Personal);

        string configDir = Path.Combine(homeDir, ".previewdevtools");
        Directory.CreateDirectory(configDir);

        // Write the .previewdevtools/connectionSettings.json file
        string jsonPath = Path.Combine(configDir, "connectionSettings.json");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true }));

        // Ensure the file is deleted when the app exits
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                if (File.Exists(jsonPath))
                {
                    File.Delete(jsonPath);
                }
            }
            catch { }
        };
    }
}
