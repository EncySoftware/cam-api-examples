using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CAMAPI.DotnetHelper;
using CAMAPI.FilesInStream;
using CAMAPI.ResultStatus;

namespace ExtensionUtilityExportInformationNet;

/// <summary>
/// Extracts screenshots from an .stcp project via ICAMAPIFilesInStreamStorage.
///
/// Project storage layout:
///  - ActiveSnapshot.ref — text (UTF-8) with the name of the active snapshot (*.snp);
///  - *.snp — XML, the Thumbnails/ProjectPreviewFile node holds the preview path;
///  - Thumbnails/ — folder with the images themselves.
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

        var lib = loader.Lib
            ?? throw new InvalidOperationException("ICAMAPIFilesInStreamStorageLib is not available.");
        using var storageCom = ComWrapper.Create(lib.CreateNewStorage());
        var storage = storageCom.Instance
            ?? throw new InvalidOperationException("CreateNewStorage returned null.");

        var openStatus = storage.Open(projectFilePath, TFISStorageOpenMode.fsomRead);
        if (openStatus.Code == TResultStatusCode.rsError)
            throw new InvalidOperationException("Cannot open project storage: " + openStatus.Description);
        try
        {
            var previewStoragePath = ReadPreviewPathFromActiveSnapshot(storage);
            return CollectThumbnailFiles(loader, storage, previewStoragePath);
        }
        finally
        {
            storage.Close(false);
        }
    }

    /// <summary>
    /// Reads ActiveSnapshot.ref, then the active snapshot XML, and returns
    /// the normalized preview path inside the storage (Thumbnails/...); "" — if not found.
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
    /// Takes the Thumbnails/ProjectPreviewFile node value from the snapshot XML and
    /// trims it down to the path inside the storage (the snapshot may hold an absolute path).
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
    /// Enumerates files of the Thumbnails folder via the ItemChildIndex/ItemSiblingIndex chain.
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
