using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExtensionBatchNCExportNet;

internal sealed record PostprocessorChoice(string FilePath, string SettingsPath = "");
internal sealed record BatchExportOptions(PostprocessorChoice Postprocessor, bool ByGroups, string OutputRoot, ExportTarget Target);

internal static class BatchExportFiles
{
    internal static bool IsPostprocessor(string file) =>
        Path.GetExtension(file).Equals(".sppx", StringComparison.OrdinalIgnoreCase) ||
        Path.GetExtension(file).Equals(".dll", StringComparison.OrdinalIgnoreCase);

    internal static string SafeName(string name)
    {
        var value = new string(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
        return string.IsNullOrWhiteSpace(value) ? "project" : value[..Math.Min(value.Length, 60)];
    }

    internal static string DefaultOutputRoot(string projectName) =>
        Path.Combine(Path.GetTempPath(), "BatchNCExport", "Project_" + SafeName(projectName));

    internal static string FindSettings(string file)
    {
        // Standard postprocessors store the DLL in bin/debug and Settings.xml in the project folder.
        var folder = Path.GetDirectoryName(file);
        for (var i = 0; i < 3 && folder != null; i++, folder = Path.GetDirectoryName(folder))
        {
            var candidate = Path.Combine(folder, "Settings.xml");
            if (File.Exists(candidate))
                return candidate;
        }
        return "";
    }

    internal static XDocument ReadDotnetSettings(string file, string postprocessor = "")
    {
        var document = string.IsNullOrWhiteSpace(file)
            ? ReadEmbeddedSettings(postprocessor)
            : XDocument.Load(file, LoadOptions.PreserveWhitespace);
        if (OutputParameter(document) == null)
            throw new InvalidOperationException("The settings do not contain OutFiles.NCFileName. This example requires that output parameter for DLL postprocessors.");
        return document;
    }

    private static XDocument ReadEmbeddedSettings(string postprocessor)
    {
        // Read the resource as PE data without loading or executing the postprocessor assembly.
        using var stream = File.OpenRead(postprocessor);
        using var pe = new PEReader(stream);
        if (!pe.HasMetadata || pe.PEHeaders.CorHeader == null)
            throw new InvalidOperationException("The selected file is not a .NET postprocessor.");
        var metadata = pe.GetMetadataReader();
        var candidates = metadata.ManifestResources.Select(metadata.GetManifestResource)
            .Where(r => r.Implementation.IsNil &&
                (metadata.GetString(r.Name).EndsWith(".Settings.xml", StringComparison.OrdinalIgnoreCase) ||
                 metadata.GetString(r.Name).Equals("Settings.xml", StringComparison.OrdinalIgnoreCase))).ToArray();
        if (candidates.Length != 1)
            throw new InvalidOperationException("Could not identify embedded DLL settings. Select Settings.xml using DLL settings.");
        var block = pe.GetSectionData(pe.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress);
        var offset = checked((int)candidates[0].Offset);
        var reader = block.GetReader(offset, block.Length - offset);
        var length = reader.ReadInt32();
        using var xml = new MemoryStream(reader.ReadBytes(length));
        return XDocument.Load(xml, LoadOptions.PreserveWhitespace);
    }
    private static XElement? OutputParameter(XDocument document) => document.Root?
        .Element("UserDefinedParameters")?.Elements("Group")
        .FirstOrDefault(e => (string?)e.Attribute("ID") == "OutFiles")?
        .Elements("Parameter").FirstOrDefault(e => (string?)e.Attribute("ID") == "NCFileName");

    internal static string WriteDotnetSettings(string source, string folder, string postprocessor = "")
    {
        var document = ReadDotnetSettings(source, postprocessor);
        OutputParameter(document)!.Value = Path.Combine(folder, "program.nc");
        var destination = Path.Combine(folder, "Settings.xml");
        document.Save(destination);
        return destination;
    }

    internal static List<string> Discover(string folder)
    {
        if (!Directory.Exists(folder))
            return [];
        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint };
        return Directory.EnumerateFiles(folder, "*", options).Where(IsPostprocessor)
            .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase).ToList();
    }
}

internal sealed record LastExportResult(string Folder, string Summary);

internal sealed class PostprocessorHistory
{
    public List<PostprocessorChoice> Recent { get; set; } = [];
    public Dictionary<string, PostprocessorChoice> Projects { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, ExportTarget> Targets { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, bool> GroupModes { get; set; } = new();
    public Dictionary<string, LastExportResult> Results { get; set; } = new();

    private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BatchNCExport", "postprocessors.json");

    internal static string ProjectKey(string projectIdentity) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(projectIdentity.ToUpperInvariant())));

    internal static PostprocessorHistory Load()
    {
        try
        {
            var history = JsonSerializer.Deserialize<PostprocessorHistory>(File.ReadAllText(FilePath));
            if (history?.Recent != null && history.Projects != null)
                return history;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
        return new();
    }

    internal string? Remember(string key, PostprocessorChoice choice, ExportTarget target, bool byGroups)
    {
        GroupModes ??= new();
        GroupModes[key] = byGroups;
        Projects[key] = choice;
        Targets ??= new(StringComparer.OrdinalIgnoreCase);
        Targets[key] = target;
        Recent.RemoveAll(p => p.FilePath.Equals(choice.FilePath, StringComparison.OrdinalIgnoreCase));
        Recent.Insert(0, choice);
        Recent = Recent.Take(12).ToList();
        return Save();
    }

    internal string? RememberResult(string key, string folder, string summary)
    {
        Results ??= new();
        Results[key] = new(folder, summary);
        return Save();
    }

    private string? Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var temporary = FilePath + "." + Guid.NewGuid().ToString("N");
            File.WriteAllText(temporary, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, FilePath, true);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "Could not save utility preferences: " + ex.Message;
        }
    }
}
