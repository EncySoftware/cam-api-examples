namespace ProjectInfoViewer;

/// <summary>
/// Command-line options: [jsonPath] [--port N] [--no-browser] [--idle-timeout N]
/// </summary>
public class CliOptions
{
    private const string DefaultJsonFileName = "test.json";
    private const int DefaultIdleTimeoutSeconds = 60;

    public string JsonPath { get; private init; } = DefaultJsonFileName;
    public int Port { get; private init; }
    public bool OpenBrowser { get; private init; } = true;

    /// <summary>Seconds without requests before the server shuts itself down; 0 — run forever.</summary>
    public int IdleTimeoutSeconds { get; private init; } = DefaultIdleTimeoutSeconds;

    public static CliOptions Parse(string[] args)
    {
        string? jsonPath = null;
        var port = 0;
        var openBrowser = true;
        var idleTimeout = DefaultIdleTimeoutSeconds;

        for (var argIndex = 0; argIndex < args.Length; argIndex++)
        {
            switch (args[argIndex])
            {
                case "--port" when argIndex + 1 < args.Length && int.TryParse(args[argIndex + 1], out var parsedPort):
                    port = parsedPort;
                    argIndex++;
                    break;
                case "--idle-timeout" when argIndex + 1 < args.Length && int.TryParse(args[argIndex + 1], out var parsedTimeout):
                    idleTimeout = parsedTimeout;
                    argIndex++;
                    break;
                case "--no-browser":
                    openBrowser = false;
                    break;
                default:
                    jsonPath ??= args[argIndex];
                    break;
            }
        }

        return new CliOptions
        {
            JsonPath = ResolveJsonPath(jsonPath),
            Port = port,
            OpenBrowser = openBrowser,
            IdleTimeoutSeconds = idleTimeout,
        };
    }

    /// <summary>
    /// Explicit argument wins; otherwise test.json next to exe, then test.json in CWD.
    /// </summary>
    private static string ResolveJsonPath(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return Path.GetFullPath(explicitPath);

        var nearExe = Path.Combine(AppContext.BaseDirectory, DefaultJsonFileName);
        if (File.Exists(nearExe))
            return nearExe;

        return Path.GetFullPath(DefaultJsonFileName);
    }
}
