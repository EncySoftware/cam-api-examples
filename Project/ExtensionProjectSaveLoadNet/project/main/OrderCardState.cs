namespace ExtensionProjectSaveLoadNet;

// The process-wide store contains the state of the current CAM project.
internal static class OrderCardStore
{
    private static OrderCardState _current = new();
    private static string _projectDocumentId = "";
    private static ulong _projectInstanceId;

    public static OrderCardState Current => _current;

    public static bool IsCurrent(string documentId) => _projectDocumentId == documentId;

    public static void Initialize(string documentId, ulong instanceId)
    {
        _projectDocumentId = documentId;
        _projectInstanceId = instanceId;
        _current = new OrderCardState();
    }

    public static void Reset()
    {
        _current = new OrderCardState();
    }

    public static void UpdateDocumentId(string documentId, ulong instanceId)
    {
        _projectDocumentId = documentId;
        _projectInstanceId = instanceId;
    }

    public static void FinalizeProject(ulong instanceId)
    {
        if (_projectInstanceId != instanceId)
            return;

        _projectDocumentId = "";
        _projectInstanceId = 0;
        _current = new OrderCardState();
    }
}

internal sealed class OrderCardState
{
    public OrderCard Current { get; set; } = new();
    public string? LoadError { get; set; }
    public string OrderCardFileName { get; private set; } = "";
    public string BinaryFileName { get; private set; } = "";
    private string _savedManifestFileName = "";
    private OrderCard? _savedCard;

    public bool NeedsManifestWrite(string previousFileName, bool isNewPlacement) =>
        isNewPlacement || previousFileName.Length == 0 ||
        previousFileName != _savedManifestFileName || Current != _savedCard;

    public bool NeedsOrderCardWrite(bool isNewPlacement) =>
        isNewPlacement || OrderCardFileName.Length == 0 || Current != _savedCard;

    public bool NeedsBinaryWrite(bool isNewPlacement) =>
        isNewPlacement || BinaryFileName.Length == 0;

    public void Remember(string manifestFileName, string orderCardFileName, string binaryFileName, OrderCard card)
    {
        // The next context supplies the committed references. If a project save fails,
        // the older references make the next attempt write the changed data again.
        _savedManifestFileName = manifestFileName;
        OrderCardFileName = orderCardFileName;
        BinaryFileName = binaryFileName;
        _savedCard = card;
    }
}
