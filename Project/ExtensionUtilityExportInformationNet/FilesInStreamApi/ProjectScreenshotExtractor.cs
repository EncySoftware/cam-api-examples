using System.Runtime.InteropServices;
using System.Xml.Linq;
using CAMAPI.FilesInStream;
using CAMAPI.ResultStatus;

namespace ExtensionUtilityExportInformationNet;

/// <summary>
/// Extracts screenshots from an .stcp project via ICAMAPIFilesInStreamStorage.
/// </summary>
public static class ProjectScreenshotExtractor
{
    private const string ActiveSnapshotRefName = "ActiveSnapshot.ref";
    private const string ThumbnailsFolderName = "Thumbnails";
    private const string SnapshotEncoding = "UTF-8";
    private const int RootParentIndex = 0;

    /// <summary>
    /// Returns all images from the Thumbnails folder; the main preview is marked with IsProjectPreview.
    /// Throws if the dll failed to load or the storage could not be opened.
    /// </summary>
    /// <param name="projectFilePath">Path to the .stcp project file.</param>
    /// <param name="filesInStreamDllPath">Path to FilesInStream.dll; null — take it from the current process folder.</param>
    public static List<ProjectScreenshot> Extract(string projectFilePath, string? filesInStreamDllPath = null)
    {
        var dllPath = filesInStreamDllPath ?? FilesInStreamLibLoader.GetDefaultDllPath();
        using var loader = new FilesInStreamLibLoader();
        if (!loader.LoadDll(dllPath))
            throw new InvalidOperationException("Failed to load " + dllPath);

        var libCom = loader.LibCom
            ?? throw new InvalidOperationException("ICAMAPIFilesInStreamStorageLib is not available.");

        List<ProjectScreenshot>? screenshots = null;
        libCom.Invoke(lib =>
        {
            var storage = lib.CreateNewStorage()
                ?? throw new InvalidOperationException("CreateNewStorage returned null.");
            try
            {
                var openStatus = storage.Open(projectFilePath, TFISStorageOpenMode.fsomRead);
                try
                {
                    if (openStatus.Code == TResultStatusCode.rsError)
                        throw new InvalidOperationException("Cannot open project storage: " + openStatus.Description);

                    var previewStoragePath = ReadPreviewPathFromActiveSnapshot(storage);
                    screenshots = CollectThumbnailFiles(loader, storage, previewStoragePath);
                }
                finally
                {
                    storage.Close(false);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(storage);
            }
        });
        return screenshots ?? new List<ProjectScreenshot>();
    }

    /// <summary>
    /// Returns the project preview path inside the storage; "" — if not found.
    /// </summary>
    private static string ReadPreviewPathFromActiveSnapshot(ICAMAPIFilesInStreamStorage storage)
    {
        var refIndex = storage.IndexOfFullName(ActiveSnapshotRefName, RootParentIndex);
        if (refIndex < 0)
            return string.Empty;
        if (storage.ReadAllTextOfFile(refIndex, SnapshotEncoding, out string snapshotName).Code == TResultStatusCode.rsError)
            return string.Empty;

        var snapshotIndex = storage.IndexOfFullName(snapshotName.Trim(), RootParentIndex);
        if (snapshotIndex < 0)
            return string.Empty;
        if (storage.ReadAllTextOfFile(snapshotIndex, SnapshotEncoding, out string snapshotXml).Code == TResultStatusCode.rsError)
            return string.Empty;

        return ParsePreviewPath(snapshotXml);
    }

    /// <summary>
    /// Returns the preview path from the snapshot XML; "" — if the node is missing.
    /// </summary>
    private static string ParsePreviewPath(string snapshotXml)
    {
        try
        {
            var document = XDocument.Parse(snapshotXml);
            var previewElement = document.Root?
                .Descendants(ThumbnailsFolderName)
                .Elements("ProjectPreviewFile")
                .FirstOrDefault();
            if (previewElement is null)
                return string.Empty;

            var value = previewElement.Value.Trim();
            if (value.Length == 0)
                value = previewElement.Attribute("Value")?.Value.Trim() ?? string.Empty;
            return NormalizeStoragePath(value);
        }
        catch (System.Xml.XmlException)
        {
            return string.Empty;
        }
    }

    private static string NormalizeStoragePath(string path)
    {
        var thumbnailsPos = path.IndexOf(ThumbnailsFolderName, StringComparison.OrdinalIgnoreCase);
        if (thumbnailsPos < 0)
            return string.Empty;
        return path.Substring(thumbnailsPos).Replace('\\', '/');
    }

    /// <summary>
    /// Collects all files of the Thumbnails folder.
    /// </summary>
    private static List<ProjectScreenshot> CollectThumbnailFiles(
        FilesInStreamLibLoader loader,
        ICAMAPIFilesInStreamStorage storage,
        string previewStoragePath)
    {
        var screenshots = new List<ProjectScreenshot>();
        var folderIndex = storage.IndexOfFullName(ThumbnailsFolderName, RootParentIndex);
        if (folderIndex < 0)
            return screenshots;

        var itemIndex = storage.get_ItemChildIndex(folderIndex);
        while (itemIndex >= 0)
        {
            if (storage.get_ItemType(itemIndex) == TFISStorageItemType.fsitFile)
            {
                var storagePath = storage.get_ItemFullName(itemIndex).Replace('\\', '/');
                var isPreview = storagePath.Equals(previewStoragePath, StringComparison.OrdinalIgnoreCase);
                screenshots.Add(new ProjectScreenshot(
                    storage.get_ItemName(itemIndex),
                    storagePath,
                    isPreview,
                    loader.ReadAllBytes(storage, itemIndex)));
            }
            itemIndex = storage.get_ItemSiblingIndex(itemIndex);
        }
        return screenshots;
    }
}
