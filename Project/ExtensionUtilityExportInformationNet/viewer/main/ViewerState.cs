namespace ProjectInfoViewer;

/// <summary>
/// Mutable state of a running viewer: the current json and the last activity time.
/// JsonPath changes when the instance is activated by a repeated launch (single-instance).
/// </summary>
public class ViewerState
{
    private string _jsonPath;
    private long _lastActivityTicks;

    public ViewerState(string jsonPath)
    {
        _jsonPath = jsonPath;
        Touch();
    }

    public string JsonPath
    {
        get => Volatile.Read(ref _jsonPath);
        set => Volatile.Write(ref _jsonPath, value);
    }

    public DateTime LastActivityUtc => new(Interlocked.Read(ref _lastActivityTicks), DateTimeKind.Utc);

    /// <summary>Record activity (any HTTP request).</summary>
    public void Touch()
    {
        Interlocked.Exchange(ref _lastActivityTicks, DateTime.UtcNow.Ticks);
    }
}
