namespace ExtensionProjectSaveLoadNet;

// An extension-owned document: no changes to the project's XML schema are needed.
public sealed record OrderCard
{
    public int Version { get; init; } = 1;
    public string OrderNumber { get; init; } = "";
    public string Customer { get; init; } = "";
    public string Notes { get; init; } = "";
}

public sealed record OrderCardManifest
{
    public int Version { get; init; } = 1;
    public string OrderCardFileName { get; init; } = "";
    public string BinaryFileName { get; init; } = "";
}
