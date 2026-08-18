namespace ExtensionUtilityExportInformationNet;

/// <summary>
/// A screenshot extracted from an .stcp project.
/// </summary>
/// <param name="Name">File name inside the storage (e.g. prv.jpeg).</param>
/// <param name="StoragePath">Full path inside the storage (e.g. Thumbnails/prv.jpeg).</param>
/// <param name="IsProjectPreview">true — this is the main project preview (Thumbnails.ProjectPreviewFile from the snapshot).</param>
/// <param name="Data">File content (image bytes).</param>
public sealed record ProjectScreenshot(string Name, string StoragePath, bool IsProjectPreview, byte[] Data);
