using System.IO;
using System.Collections.Generic;
using Nuke.Common;
using BuildSystem.Builder.Dotnet;
using BuildSystem.Builder.MsCpp;
using BuildSystem.BuildSpace;
using BuildSystem.BuildSpace.Common;
using BuildSystem.Cleaner.Common;
using BuildSystem.Info;
using BuildSystem.Loggers;
using BuildSystem.Logging;
using BuildSystem.Restorer.Nuget;
using BuildSystem.SettingsReader;
using BuildSystem.SettingsReader.Object;
using BuildSystem.Variants;
using LoggingLevel = BuildSystem.Logging.LogLevel;

// ReSharper disable AllUnderscoreLocalParameterName

/// <inheritdoc />
// ReSharper disable once CheckNamespace
public class Build : NukeBuild
{
    /// <summary>
    /// Calling target by default
    /// </summary>
    public static int Main() => Execute<Build>(x => x.Compile);

    /// <summary>
    /// Configuration to build - 'Debug' (default) or 'Release'
    /// </summary>
    [Parameter("Settings provided for running build space")]
    public readonly string Variant = "Debug";

    /// <summary>
    /// Logging object
    /// </summary>
    private ILogger? _logger;
    private ILogger Logger => _logger ??= InitLogger();

    /// <summary>
    /// Main build space as manager over projects
    /// </summary>
    private IBuildSpace? _buildSpace;
    private IBuildSpace BuildSpace => _buildSpace ??= InitBuildSpace();
    
    private ILogger InitLogger() {
        // logging to console
        var console = new LoggerConsole();
        console.setMinLevel(LoggingLevel.info);
        
        // logging to file
        var file = new LoggerFile(Path.Combine(RootDirectory, "logs"), "log", 7);
        file.setMinLevel(LoggingLevel.debug);
        
        // singleton to transfer logs to all other loggers
        var logger = new LoggerBroadCaster();
        logger.Loggers.Add(file);
        logger.Loggers.Add(console);
        return logger;
    }

    private IBuildSpace InitBuildSpace()
    {
        BuildInfo.RunParams[RunInfo.Variant] = Variant;
        
        var settings = new SettingsObject
        {
            Projects = new HashSet<string>
            {
                Path.Combine(RootDirectory.Parent, "main", ".stbuild", "FullWorkflow3DProjectProject.json")
            },
            Variants =
            [
                new Variant
                {
                    Name = "Debug",
                    Configurations = new Dictionary<string, string>
                    {
                        [BuildSystem.Variants.Variant.NodeConfig] = "Debug"
                    },
                    Platforms = new Dictionary<string, string>
                    {
                        [BuildSystem.Variants.Variant.NodePlatform] = "x64"
                    }
                },

                new Variant
                {
                    Name = "Release",
                    Configurations = new Dictionary<string, string>
                    {
                        [BuildSystem.Variants.Variant.NodeConfig] = "Release"
                    },
                    Platforms = new Dictionary<string, string>
                    {
                        [BuildSystem.Variants.Variant.NodePlatform] = "x64"
                    }
                }
            ],
            ManagerProps =
            [
                new BuilderMsCppProps
                {
                    Name = "BuilderCpp",
                    MsBuilderPath =
                        "c:/Program Files/Microsoft Visual Studio/2022/Community/Msbuild/Current/Bin/MSBuild.exe"
                },

                new RestorerNugetProps
                {
                    Name = "RestorerNuget"
                },

                new CleanerCommonProps
                {
                    Name = "CleanerCommon"
                }
            ]
        };
        settings.ManagerNames.Add("builder", "Debug", "BuilderCpp");
        settings.ManagerNames.Add("builder", "Release", "BuilderCpp"); 
        settings.ManagerNames.Add("restorer", "Debug", "RestorerNuget");
        settings.ManagerNames.Add("restorer", "Release", "RestorerNuget");
        settings.ManagerNames.Add("cleaner", "Debug", "CleanerCommon");
        settings.ManagerNames.Add("cleaner", "Release", "CleanerCommon");
        
        var tempDir = Path.Combine(RootDirectory, "temp");
        return new BuildSpaceCommon(Logger, tempDir, SettingsReaderType.Object, settings);
    }

    /// <summary>
    /// Parameterized compile
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private Target Compile => _ => _
        .Executes(() =>
        {
            BuildSpace.Projects.Restore(Variant);
            BuildSpace.Projects.Compile(Variant, true);
        });

    /// <summary>
    /// Delete build results
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private Target Clean => _ => _
        .Executes(() =>
        {
            BuildSpace.Projects.Clean("Debug");
            BuildSpace.Projects.Clean("Release");
        });
        
    /// <summary>
    /// Removes all temporary files
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private Target CleanAll => _ => _
        .Description("Full clean - removes all temporary files")
        .DependsOn(Clean)
        .Executes(() =>
        {
            var tempDirectories = new[]
            {
                RootDirectory.Parent / "bin",
                RootDirectory.Parent / "obj",
                RootDirectory.Parent / "temp",
                RootDirectory.Parent / ".stbuild" / "temp",
                RootDirectory.Parent / ".stbuild" / ".nuke" / "temp",
                RootDirectory.Parent / ".stbuild" / "build" / "bin",
                RootDirectory.Parent / ".stbuild" / "build" / "obj",
                RootDirectory.Parent / "project" / "main" / "bin",
                RootDirectory.Parent / "project" / "main" / "obj"
            };

            foreach (var dirPath in tempDirectories)
            {
                string dir = dirPath.ToString(); 
                
                if (Directory.Exists(dir))
                {
                    try
                    {
                        Directory.Delete(dir, recursive: true);
                        Logger.head($"✅  Successfully deleted: {dir}");
                    }
                    catch (System.Exception ex)
                    {
                        Logger.head($"⚠️  Could not delete {dir}: {ex.Message}");
                    }
                }
            }
        });
}