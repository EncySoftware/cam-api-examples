using System.Diagnostics;
using ProjectInfoViewer;

var options = CliOptions.Parse(args);

// Single-instance: if the viewer is already running, pass it the new json and open its URL.
var existingUrl = await RunningInstance.TryActivateExistingAsync(options.JsonPath);
if (existingUrl is not null)
{
    Console.WriteLine($"Viewer is already running: {existingUrl} — reusing it.");
    if (options.OpenBrowser)
        OpenInDefaultBrowser(existingUrl);
    return;
}

var state = new ViewerState(options.JsonPath);

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = AppContext.BaseDirectory,
    WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot"),
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.WebHost.UseUrls($"http://127.0.0.1:{options.Port}");

var app = builder.Build();

// Any request (page heartbeat, static files, API) extends the server's lifetime.
app.Use(async (context, next) =>
{
    state.Touch();
    if (context.Request.Path.StartsWithSegments("/api"))
        context.Response.Headers.CacheControl = "no-store";
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/instance", () => Results.Json(RunningInstance.Describe()));

app.MapPost("/api/activate", (ActivateRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.JsonPath))
        return Results.BadRequest(new { error = "jsonPath is required" });
    state.JsonPath = Path.GetFullPath(request.JsonPath);
    Console.WriteLine($"Activated with new JSON: {state.JsonPath}");
    return Results.Ok();
});

app.MapGet("/api/heartbeat", () => Results.Ok());

app.MapGet("/api/meta", () =>
{
    var jsonPath = state.JsonPath;
    var fileExists = File.Exists(jsonPath);
    return Results.Json(new
    {
        jsonPath,
        exists = fileExists,
        size = fileExists ? new FileInfo(jsonPath).Length : 0,
        modified = fileExists ? File.GetLastWriteTime(jsonPath) : (DateTime?)null,
    });
});

app.MapGet("/api/project", () => File.Exists(state.JsonPath)
    ? Results.File(state.JsonPath, "application/json; charset=utf-8")
    : Results.NotFound(new { error = $"JSON file not found: {state.JsonPath}" }));

app.MapGet("/api/screenshot", (string file) =>
    ServeDataFile(DataRoot(), file, ImageContentType(file)));

// Groundwork for future iterations: operation toolpaths and 3D models (.osd/.stl).
app.MapGet("/api/toolpath", (string file) =>
    ServeDataFile(DataRoot(), file, "application/json; charset=utf-8"));

app.MapGet("/api/model/{name}", (string name) =>
    ServeDataFile(Path.Combine(DataRoot(), "Output"), name, "application/octet-stream"));

await app.StartAsync();

var url = app.Urls.First();
RunningInstance.WriteLockFile(url);
StartIdleMonitor(app, state, options.IdleTimeoutSeconds);

Console.WriteLine($"Project viewer: {url}");
Console.WriteLine($"JSON: {state.JsonPath}");
Console.WriteLine(options.IdleTimeoutSeconds > 0
    ? $"Auto-exit after {options.IdleTimeoutSeconds}s without page activity (Ctrl+C to stop now)."
    : "Press Ctrl+C to stop.");

if (options.OpenBrowser)
    OpenInDefaultBrowser(url);

try
{
    await app.WaitForShutdownAsync();
}
finally
{
    RunningInstance.DeleteLockFile();
}
return;

string DataRoot() => Path.GetDirectoryName(state.JsonPath) ?? AppContext.BaseDirectory;

// Shuts the server down once the page stops showing signs of life (tab closed).
static void StartIdleMonitor(WebApplication app, ViewerState state, int idleTimeoutSeconds)
{
    if (idleTimeoutSeconds <= 0)
        return;
    var timeout = TimeSpan.FromSeconds(idleTimeoutSeconds);
    _ = Task.Run(async () =>
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Min(5, Math.Max(1, idleTimeoutSeconds / 4)));
        while (!app.Lifetime.ApplicationStopping.IsCancellationRequested)
        {
            await Task.Delay(pollInterval);
            if (DateTime.UtcNow - state.LastActivityUtc > timeout)
            {
                Console.WriteLine($"No activity for {idleTimeoutSeconds}s — shutting down.");
                app.Lifetime.StopApplication();
                return;
            }
        }
    });
}

static string ImageContentType(string fileName) =>
    Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".bmp" => "image/bmp",
        ".gif" => "image/gif",
        _ => "image/jpeg",
    };

// Serves a file from the data directory with path traversal protection.
static IResult ServeDataFile(string allowedRoot, string relativePath, string contentType)
{
    var fullRoot = Path.GetFullPath(allowedRoot);
    var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath));
    if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Path outside of data root is not allowed" });
    return File.Exists(fullPath)
        ? Results.File(fullPath, contentType)
        : Results.NotFound(new { error = $"File not found: {relativePath}" });
}

static void OpenInDefaultBrowser(string url)
{
    try
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
    catch (Exception e)
    {
        Console.WriteLine($"Failed to open browser: {e.Message}. Open {url} manually.");
    }
}
