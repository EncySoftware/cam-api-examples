using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.NCMaker;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;


namespace ExtensionBatchNCExportNet;

/// <summary>Generate NC programs in batches and pass the results to an installed PLM exporter.</summary>
public sealed class BatchNCExportUtility : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    private const string OpenFromPlmMessage = "To export NC programs, open a project from PLM and run the utility again.";

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        var completed = 0;
        string? runFolder = null;
        PostprocessorHistory? history = null;
        string? projectKey = null;
        try
        {
            using var application = ComWrapper.Create(context.CamApplication);
            var snapshot = application.Invoke(app =>
            {
                var project = app.GetActiveProject(out var status);
                Check(status);
                if (project == null) throw new InvalidOperationException("Open a project in the CAM system.");
                var root = project.Technologist.RootOperation;
                if (project.PLMConnectionId == Guid.Empty || string.IsNullOrWhiteSpace(project.PLMItemId))
                    throw new ProjectNotFromPlmException();
                return (ProjectId: project.Id, RootId: root.Id, Scope: root.FullName,
                    Identity: string.IsNullOrWhiteSpace(project.FilePath) ? project.Id : project.FilePath,
                    Name: string.IsNullOrWhiteSpace(project.FilePath) ? "Unsaved project" : Path.GetFileNameWithoutExtension(project.FilePath),
                    ConnectionId: project.PLMConnectionId, ItemId: project.PLMItemId);
            });
            using var paths = SystemExtensionFactory.GetPathsHelper();

            var targets = application.Invoke(app =>
            {
                var manager = app.GetExtensionManager(out var status);
                if (status.Code != TResultStatusCode.rsSuccess || manager == null)
                    return new ExportTargetList([], "Could not retrieve integrations. Check the application extension settings.");
                return ExportTargets.Read(manager);
            });
            var standardFolder = paths.Invoke(p => p.PostprocessorsFolder);
            history = PostprocessorHistory.Load();
            projectKey = PostprocessorHistory.ProjectKey(snapshot.Identity);
            var options = BatchExportDialog.Choose(targets, snapshot.ConnectionId != Guid.Empty && !string.IsNullOrWhiteSpace(snapshot.ItemId), standardFolder,
                snapshot.Name, projectKey, history.GroupModes?.GetValueOrDefault(projectKey, true) ?? true, history);
            if (options == null) return;
            var postprocessor = options.Postprocessor.FilePath;
            var historyWarning = history.Remember(projectKey, options.Postprocessor, options.Target, options.ByGroups);
            application.Invoke(app =>
            {
                var project = app.GetActiveProject(out var status);
                Check(status);
                if (project == null)
                    throw new InvalidOperationException("Open a project in the CAM system.");
                var root = project.Technologist.RootOperation;
                if (root == null || !root.Enabled)
                    throw new InvalidOperationException("Select an operation or group in the technology tree.");
                if (project.Id != snapshot.ProjectId || root.Id != snapshot.RootId)
                    throw new InvalidOperationException("The project or operation tree has changed. Run the utility again.");
                if (project.PLMConnectionId != snapshot.ConnectionId || project.PLMItemId != snapshot.ItemId || project.PLMConnectionId == Guid.Empty || string.IsNullOrWhiteSpace(project.PLMItemId))
                    throw new InvalidOperationException("The project PLM association has changed or is missing. Open the project from PLM and run the utility again.");
                var batches = new List<ICamApiTechOperation>();
                if (options.ByGroups)
                    foreach (var child in Children(root)) CollectGroups(child, batches);
                else CollectLeaves(root, batches);
                if (batches.Count == 0)
                    throw new InvalidOperationException("There are no calculated operations to export.");
                var manager = app.GetExtensionManager(out status);
                Check(status);
                var exporter = ExportTargets.Require(manager, options.Target);
                runFolder = Path.Combine(options.OutputRoot, "NC_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(runFolder);
                var logPath = Path.Combine(runFolder, "export.log");
                File.AppendAllText(logPath, $"Target: {exporter.GetTargetCaption(options.Target.TargetIndex)}{Environment.NewLine}");
                if (historyWarning != null) File.AppendAllText(logPath, historyWarning + Environment.NewLine);
                var maker = project.NCMaker;
                foreach (var batch in batches)
                {
                    // Separate folders prevent collisions between equally named operations and auxiliary NC files.
                    var folder = Path.Combine(runFolder, (completed + 1).ToString("D4") + "_" + BatchExportFiles.SafeName(batch.Name));
                    Directory.CreateDirectory(folder);
                    File.AppendAllText(logPath, $"Starting: {batch.Id}; {batch.Name}; {folder}{Environment.NewLine}");
                    var clData = project.SaveCLDataInfo(
                        Path.Combine(folder, "program.inpcld"), new SubtreeIterator(batch), out status);
                    using var clDataLifetime = ComWrapper.Create(clData);
                    Check(status);
                    var dotnet = Path.GetExtension(postprocessor).Equals(".dll", StringComparison.OrdinalIgnoreCase);
                    var settings = maker.CreateSettings(dotnet ? TCamApiNCMakerSettingsType.ncsDotnet : TCamApiNCMakerSettingsType.ncsSppx, out status);
                    using var settingsLifetime = ComWrapper.Create(settings);
                    Check(status);
                    if (dotnet)
                        ((ICamApiMakeCncDotnetSettings)settings).SettingsFilePath =
                            BatchExportFiles.WriteDotnetSettings(options.Postprocessor.SettingsPath, folder, postprocessor);
                    else
                    {
                        var sppx = (ICamApiMakeCncSppxSettings)settings;
                        sppx.OutputFolder = folder;
                        sppx.NcFileName = "program.nc";
                    }
                    var result = maker.GenerateNC(clData, postprocessor, settings, out status);
                    using var generationLifetime = ComWrapper.Create(result);
                    Check(status);
                    if (result == null)
                        throw new InvalidOperationException("No generation result was returned.");
                    var messages = result.Messages;
                    if (messages != null)
                        for (var i = 0; i < messages.Count(); i++)
                            File.AppendAllText(logPath, messages.Get(i) + Environment.NewLine);
                    if (!result.GeneratedOK)
                        throw new InvalidOperationException($"Could not generate NC for {batch.Name}. See export.log.");
                    var files = result.NCFileNames;
                    if (files == null || files.Count() == 0)
                        throw new InvalidOperationException($"No NC files were generated for {batch.Name}.");
                    for (var i = 0; i < files.Count(); i++)
                        if (!File.Exists(files.Get(i)))
                            throw new InvalidOperationException("NC file not found: " + files.Get(i));
                    exporter.ExportToTarget(options.Target.TargetIndex, result, out status);
                    Check(status);
                    completed++;
                    File.AppendAllText(logPath, $"Export completed: {batch.Id}. {status.Description}{Environment.NewLine}");
                }
            });
            resultStatus.Description = $"Export completed. Batches exported: {completed}. Results: {runFolder}";
        }
        catch (ProjectNotFromPlmException)
        {
            ShowStartupError(OpenFromPlmMessage);
            return;
        }
        catch (Exception ex)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = ex.Message;
            if (runFolder != null)
                resultStatus.Description += $" Batches exported: {completed}. Results: {runFolder}.";
            if (completed > 0)
                resultStatus.Description += " Previously completed exports are not rolled back.";
            if (runFolder != null)
            {
                try { File.AppendAllText(Path.Combine(runFolder, "export.log"), resultStatus.Description + Environment.NewLine); }
                catch (IOException) { /* The original error remains in the utility result. */ }
            }
        }
        if (runFolder != null && history != null && projectKey != null)
        {
            var warning = history.RememberResult(projectKey, runFolder, resultStatus.Description);
            if (warning != null)
            {
                try { File.AppendAllText(Path.Combine(runFolder, "export.log"), warning + Environment.NewLine); }
                catch (IOException) { }
            }
        }
        if (runFolder != null)
            BatchExportDialog.ShowResult(resultStatus.Description, runFolder);
        else if (resultStatus.Code == TResultStatusCode.rsError)
            ShowStartupError(resultStatus.Description);
    }

    private sealed class ProjectNotFromPlmException : Exception { }

    private static void ShowStartupError(string message)
    {
        // The returned utility status may only appear in the application log.
        // Show errors through the application API even if the WinForms window failed to open.
        using var dialogs = UIDialogs.CreateHelper();
        dialogs.Invoke(d => d.MessageBox(message, message == OpenFromPlmMessage ? TMessageDialogType.mdtWarning : TMessageDialogType.mdtError,
            MessageBoxHelper.BuildButtons([TUIButtonType.btOk]), TUIButtonType.btOk,
            "Batch NC generation and export"));
    }
    private static void Check(TResultStatus status)
    {
        if (status.Code != TResultStatusCode.rsSuccess)
            throw new InvalidOperationException(status.Description ?? "The operation was cancelled or failed.");
    }

    private static IEnumerable<ICamApiTechOperation> Children(ICamApiTechOperation parent)
    {
        for (var child = parent.GetFirstChildOperation(TCamApiReorderingMode.rmDesigned);
             child != null; child = child.GetNextSiblingOperation(TCamApiReorderingMode.rmDesigned))
            yield return child;
    }

    private static void CollectLeaves(ICamApiTechOperation operation, List<ICamApiTechOperation> result)
    {
        if (!operation.Enabled)
            return;
        if (!operation.IsGroup)
        {
            if (operation.Calculated && !operation.IsError) result.Add(operation);
        }
        else
            foreach (var child in Children(operation))
                CollectLeaves(child, result);
    }

    private static void CollectGroups(ICamApiTechOperation operation, List<ICamApiTechOperation> result)
    {
        if (!operation.Enabled) return;
        var type = operation.OperationType ?? "";
        var structural = type.StartsWith("TSTRoot", StringComparison.OrdinalIgnoreCase) ||
            type.StartsWith("TSTStage", StringComparison.OrdinalIgnoreCase) ||
            type.StartsWith("TSTPart", StringComparison.OrdinalIgnoreCase);
        if (operation.IsGroup && structural)
        {
            foreach (var child in Children(operation)) CollectGroups(child, result);
            return;
        }
        var leaves = new List<ICamApiTechOperation>();
        CollectLeaves(operation, leaves);
        if (leaves.Count > 0) result.Add(operation);
    }
    // The iterator preserves group hierarchy and stays within its root.
    private sealed class SubtreeIterator : ICamApiTechOperationIterator
    {
        private readonly ICamApiTechOperation root;
        private ICamApiTechOperation current;
        public SubtreeIterator(ICamApiTechOperation root)
        {
            this.root = root;
            current = root;
        }
        public ICamApiTechOperationIteratorFilter? OperationsFilter { get; set; }
        public ICamApiTechOperation Current() => current;
        public void Reset() => current = root;
        public bool MoveToChild() => Move(current.GetFirstChildOperation(TCamApiReorderingMode.rmDesigned));
        public bool MoveToSibling() => current.Id != root.Id && Move(current.GetNextSiblingOperation(TCamApiReorderingMode.rmDesigned));
        public bool MoveToParent()
        {
            if (current.Id == root.Id)
                return false;
            current = current.GetParentOperation(TCamApiReorderingMode.rmDesigned);
            return true;
        }
        private bool Move(ICamApiTechOperation? candidate)
        {
            while (candidate != null)
            {
                if (candidate.Enabled && (candidate.IsGroup || (candidate.Calculated && !candidate.IsError)) && (OperationsFilter?.AllowIterateTo(candidate) ?? true))
                {
                    current = candidate;
                    return true;
                }
                candidate = candidate.GetNextSiblingOperation(TCamApiReorderingMode.rmDesigned);
            }
            return false;
        }
    }
}
