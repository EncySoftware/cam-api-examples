using System.Runtime.InteropServices;
using System.Text.Json;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace ExtensionProjectSaveLoadNet;

[ComVisible(true), ClassInterface(ClassInterfaceType.None)]
public sealed class ProjectSerializer : IExtension, IExtensionToProjectSerializer
{
    public const string ExtensionId = "Examples.ProjectSaveLoad.OrderCard";
    public const string ManifestPurpose = "manifest";
    private const string AuxiliaryPurpose = "";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly byte[] BinaryPayload = Enumerable.Range(0, 256).Select(index => (byte)index).ToArray();

    public IExtensionInfo? Info { get; set; }

    public void NewProjectInitialized(
        ICamApiExtensionProjectInitializationContext projectContext,
        out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            using var context = ComWrapper.Create(projectContext);
            OrderCardStore.Initialize(context.ProjectDocumentId(), context.ProjectInstanceId());
        }
        catch (Exception e)
        {
            resultStatus = Failure(nameof(NewProjectInitialized), e);
        }
    }

    public void ProjectFinalized(
        ICamApiExtensionProjectFinalizationContext projectContext,
        out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            using var context = ComWrapper.Create(projectContext);
            OrderCardStore.FinalizeProject(context.ProjectInstanceId());
        }
        catch (Exception e)
        {
            resultStatus = Failure(nameof(ProjectFinalized), e);
        }
    }

    public void SerializeToProject(
        ICamApiExtensionProjectSaveContext saveContext,
        out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            using var context = ComWrapper.Create(saveContext);
            // Save As changes the document ID while keeping the active project state.
            OrderCardStore.UpdateDocumentId(context.ProjectDocumentId(), context.ProjectInstanceId());
            if (!context.IsUsualProjectSave() && !context.IsAutosave())
                return;

            var state = OrderCardStore.Current;
            if (state.LoadError is { } error)
                throw new InvalidDataException(error);

            var manifestFileName = context.GetFileNameInSnapshot(ManifestPurpose);
            var orderCardFileName = state.OrderCardFileName;
            var binaryFileName = state.BinaryFileName;
            var writeOrderCard = state.NeedsOrderCardWrite(context.IsNewPlacement());
            if (writeOrderCard)
            {
                var (stream, actualFileName) = context.GetFileWriteStream(
                    "data/order-card.json", AuxiliaryPurpose, TExtensionFileCompression.efcNormal);
                using (stream)
                    JsonSerializer.Serialize(stream, state.Current, JsonOptions);
                orderCardFileName = actualFileName;
            }

            var writeBinary = state.NeedsBinaryWrite(context.IsNewPlacement());
            if (writeBinary)
            {
                var (stream, actualFileName) = context.GetFileWriteStream(
                    "data/order-card.bin", AuxiliaryPurpose, TExtensionFileCompression.efcNormal);
                using (stream)
                    stream.Write(BinaryPayload);
                binaryFileName = actualFileName;
            }

            var writeManifest = state.NeedsManifestWrite(manifestFileName, context.IsNewPlacement()) ||
                writeOrderCard || writeBinary;
            if (writeManifest)
            {
                var manifest = new OrderCardManifest
                {
                    OrderCardFileName = orderCardFileName,
                    BinaryFileName = binaryFileName
                };
                var (stream, actualFileName) = context.GetFileWriteStream(
                    "data/order-card-manifest.json", ManifestPurpose, TExtensionFileCompression.efcNormal);
                using (stream)
                    JsonSerializer.Serialize(stream, manifest, JsonOptions);
                manifestFileName = actualFileName;
            }

            context.IncludeFileToSnapshot(orderCardFileName);
            context.IncludeFileToSnapshot(binaryFileName);
            context.IncludeFileToSnapshot(manifestFileName);
            state.Remember(manifestFileName, orderCardFileName, binaryFileName, state.Current);
        }
        catch (Exception e)
        {
            resultStatus = Failure(nameof(SerializeToProject), e);
        }
    }

    public void DeserializeFromProject(
        ICamApiExtensionProjectLoadContext loadContext,
        out TResultStatus resultStatus)
    {
        resultStatus = default;
        // Loading replaces all runtime state before applying the snapshot.
        OrderCardStore.Reset();
        try
        {
            using var context = ComWrapper.Create(loadContext);
            OrderCardStore.UpdateDocumentId(context.ProjectDocumentId(), context.ProjectInstanceId());
            var manifestFileName = context.GetFileNameInSnapshot(ManifestPurpose);
            var orderCard = new OrderCard();
            var orderCardFileName = "";
            var binaryFileName = "";
            if (manifestFileName.Length != 0)
            {
                using (var stream = context.GetFileReadStream(manifestFileName))
                {
                    var manifest = JsonSerializer.Deserialize<OrderCardManifest>(stream)
                        ?? throw new InvalidDataException("The order card manifest is null.");
                    if (manifest.Version != 1)
                        throw new InvalidDataException($"Unsupported order card manifest version: {manifest.Version}.");
                    orderCardFileName = manifest.OrderCardFileName;
                    binaryFileName = manifest.BinaryFileName;
                }

                if (orderCardFileName.Length != 0)
                {
                    using var stream = context.GetFileReadStream(orderCardFileName);
                    orderCard = JsonSerializer.Deserialize<OrderCard>(stream)
                        ?? throw new InvalidDataException("The order card is null.");
                    if (orderCard.Version != 1)
                        throw new InvalidDataException($"Unsupported order card version: {orderCard.Version}.");
                }
                if (binaryFileName.Length != 0)
                {
                    using var binaryStream = context.GetFileReadStream(binaryFileName);
                    using var binary = new MemoryStream();
                    binaryStream.CopyTo(binary);
                    if (!binary.ToArray().SequenceEqual(BinaryPayload))
                        throw new InvalidDataException("Restored binary content differs from the saved data.");
                }
            }

            var state = OrderCardStore.Current;
            state.Current = orderCard;
            state.Remember(manifestFileName, orderCardFileName, binaryFileName, orderCard);
        }
        catch (Exception e)
        {
            resultStatus = Failure(nameof(DeserializeFromProject), e);
            // Preserve unreadable data instead of overwriting it with defaults.
            OrderCardStore.Current.LoadError = resultStatus.Description;
        }
    }

    private static TResultStatus Failure(string operation, Exception error) => new()
    {
        Code = TResultStatusCode.rsError,
        Description = $"{ExtensionId}.{operation} ({ManifestPurpose}): {error.Message}"
    };
}
