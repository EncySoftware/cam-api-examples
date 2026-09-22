using System;
using System.IO;
using System.IO.Compression;
using BuildSystem;
using BuildSystem.Info;
using BuildSystem.ProjectList.Model;
using Utils;
using Logging;
using Nuke.Common;

/// <summary>Build and package this example using the repository build system.</summary>
public class Build : NukeBuild
{
    /// <summary>Default target.</summary>
    public static int Main() => Execute<Build>(x => x.Pack);

    /// <summary>Build configuration.</summary>
    [Parameter("Build configuration: Debug or Release")]
    public readonly string Variant = "Debug";

    private IBuildSpace? _buildSpace;
    private IBuildSpace BuildSpace => _buildSpace ??= InitBuildSpace();

    private IBuildSpace InitBuildSpace()
    {
        var logger = new LoggerConsole();
        logger.setMinLevel(Logging.LogLevel.info);
        BuildInfo.RunParams[RunInfo.Variant] = Variant;
        BuildInfo.RunParams[RunInfo.Local] = "local";
        var settings = new BuildSpaceSettings(logger, RootDirectory.Parent);
        return new BuildSpaceCommon(logger, Path.Combine(RootDirectory, "temp"), SettingsReaderType.Object, settings);
    }

    private Target Compile => _ => _.Executes(() => BuildSpace.Projects.Compile(Variant, true));

    private Target Clean => _ => _.Executes(() =>
    {
        BuildSpace.Projects.Clean("Debug");
        BuildSpace.Projects.Clean("Release");
    });

    private Target Pack => _ => _.DependsOn(Compile).Executes(() =>
    {
        foreach (var project in BuildSpace.Projects.List.All())
        {
            var dll = project.GetBuildResultPath(Variant, "dll")
                ?? throw new InvalidOperationException("No DLL build result.");
            var settings = Path.ChangeExtension(dll, ".settings.json");
            var package = Path.ChangeExtension(dll, ".dext");
            using var output = new FileStream(package, FileMode.Create);
            using var archive = new ZipArchive(output, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(dll, Path.GetFileName(dll));
            archive.CreateEntryFromFile(settings, Path.GetFileName(settings));
            Console.WriteLine($"Created {package}");
        }
    });
}
