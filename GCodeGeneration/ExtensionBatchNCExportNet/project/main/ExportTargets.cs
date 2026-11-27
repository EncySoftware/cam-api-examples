using CAMAPI.Extensions;
using CAMAPI.NCMaker;
using CAMAPI.ResultStatus;

namespace ExtensionBatchNCExportNet;

internal sealed record ExportTarget(string ExporterId, int TargetIndex, string Caption)
{
    public override string ToString() => Caption;
}

internal sealed record ExportTargetList(List<ExportTarget> Targets, string Message);

internal static class ExportTargets
{
    internal static ExportTargetList Read(IExtensionManager manager)
    {
        var targets = new List<ExportTarget>();
        var unavailable = new List<string>();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var libraries = manager.GetLibrariesInfo();
            for (var l = 0; l < libraries.Count(); l++)
            {
                var library = libraries.Get(l);
                // The API looks up a library by DLL path, not by its identifier.
                var types = manager.GetExtensionTypeInfosFromLibrary(library.ModulePath, out var status);
                if (status.Code != TResultStatusCode.rsSuccess || types == null)
                {
                    unavailable.Add(library.Name + ": could not read the extension list");
                    continue;
                }
                for (var t = 0; t < types.Count(); t++)
                {
                    var type = types.Get(t);
                    if (type is not IExtensionTypeInfoNCFilesExporter || !seen.Add(type.Id)) continue;
                    var name = string.IsNullOrWhiteSpace(type.Name) ? library.Name : type.Name;
                    try
                    {
                        var disabled = manager.GetExtensionDisabledFinal(type.Id, out status);
                        Check(status);
                        if (disabled)
                        {
                            unavailable.Add(name + ": extension disabled");
                            continue;
                        }
                        var extension = manager.GetSingletonExtension(type.Id, out status);
                        Check(status);
                        if (extension is not IExtensionNCFilesExporter exporter)
                            throw new InvalidOperationException("NC export is not supported");
                        var found = new List<ExportTarget>();
                        for (var index = 0; index < exporter.TargetsCount; index++)
                        {
                            var caption = exporter.GetTargetCaption(index);
                            if (string.IsNullOrWhiteSpace(caption)) caption = "NC export " + (index + 1);
                            found.Add(new(type.Id, index, caption + " — " + library.Name));
                        }
                        if (found.Count == 0) unavailable.Add(name + ": no NC export targets");
                        targets.AddRange(found);
                    }
                    catch (Exception)
                    {
                        unavailable.Add(name + ": could not load the integration; check its installation and connection");
                    }
                }
            }
        }
        catch (Exception)
        {
            unavailable.Add("Could not retrieve integrations. Check the application extension settings.");
        }
        var message = targets.Count == 0
            ? "No NC exporters are available. Install and enable an integration extension, then run the utility again."
            : string.Empty;
        if (unavailable.Count > 0) message += Environment.NewLine + string.Join(Environment.NewLine, unavailable);
        return new(targets, message);
    }

    internal static IExtensionNCFilesExporter Require(IExtensionManager manager, ExportTarget target)
    {
        try
        {
            var disabled = manager.GetExtensionDisabledFinal(target.ExporterId, out var status);
            Check(status);
            if (disabled) throw new InvalidOperationException("The integration is disabled.");
            var extension = manager.GetSingletonExtension(target.ExporterId, out status);
            Check(status);
            if (extension is not IExtensionNCFilesExporter exporter || target.TargetIndex < 0 || target.TargetIndex >= exporter.TargetsCount)
                throw new InvalidOperationException("The export target is no longer available.");
            return exporter;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not start {target.Caption}. Check the integration and PLM connection, then reopen the utility.", ex);
        }
    }

    private static void Check(TResultStatus status)
    {
        if (status.Code != TResultStatusCode.rsSuccess) throw new InvalidOperationException(status.Description);
    }
}
