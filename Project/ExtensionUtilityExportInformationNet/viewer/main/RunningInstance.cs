using System.Text;
using System.Text.Json;

namespace ProjectInfoViewer;

/// <summary>
/// Single-instance via a lock file in %TEMP% containing the URL of the running server.
/// A repeated launch does not start a second server, it activates the existing one.
/// </summary>
public static class RunningInstance
{
    private const string InstanceMarker = "ProjectInfoViewer";
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(1.5);

    private static string LockFilePath => Path.Combine(Path.GetTempPath(), "ProjectInfoViewer.lock");

    /// <summary>
    /// If the viewer is already running, passes it the new jsonPath and returns its URL.
    /// Otherwise returns null (a new server must be started).
    /// </summary>
    public static async Task<string?> TryActivateExistingAsync(string jsonPath)
    {
        string url;
        try
        {
            if (!File.Exists(LockFilePath))
                return null;
            url = (await File.ReadAllTextAsync(LockFilePath)).Trim();
            if (string.IsNullOrEmpty(url))
                return null;
        }
        catch (IOException)
        {
            return null;
        }

        try
        {
            using var client = new HttpClient { Timeout = ProbeTimeout };

            var instanceJson = await client.GetStringAsync($"{url}/api/instance");
            if (!instanceJson.Contains(InstanceMarker))
                return null;

            var payload = new StringContent(
                JsonSerializer.Serialize(new { jsonPath }), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{url}/api/activate", payload);
            return response.IsSuccessStatusCode ? url : null;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            DeleteLockFile(); // the lock is stale: no server behind it anymore
            return null;
        }
    }

    /// <summary>Response for GET /api/instance — the marker lets a repeated launch recognize its own kind.</summary>
    public static object Describe() => new { app = InstanceMarker, pid = Environment.ProcessId };

    public static void WriteLockFile(string url)
    {
        try
        {
            File.WriteAllText(LockFilePath, url);
        }
        catch (IOException)
        {
            // without the lock file single-instance won't work, but the server itself stays functional
        }
    }

    public static void DeleteLockFile()
    {
        try
        {
            if (File.Exists(LockFilePath))
                File.Delete(LockFilePath);
        }
        catch (IOException)
        {
            // a stale lock will be overwritten by the next launch
        }
    }
}
