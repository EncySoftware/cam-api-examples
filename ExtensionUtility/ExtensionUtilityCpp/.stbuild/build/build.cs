using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using BuildSystem.Builder.MsCpp; 
using BuildSystem.Cleaner.Common;
using Nuke.Common;
using BuildSystem.BuildSpace;
using BuildSystem.BuildSpace.Common;
using BuildSystem.Info;
using BuildSystem.Loggers;
using BuildSystem.Logging;
using BuildSystem.ManagerObject;
using BuildSystem.ManagerObject.Interfaces;
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
    public static int Main() => Execute<Build>(x => x.Pack);
    
    /// <summary>
    /// Configuration to build - 'Debug' (default) or 'Release'
    /// </summary>
    [Parameter("Settings provided for running build space")]
    public readonly string Variant = "Debug";

    /// <summary>
    /// Logging object
    /// </summary>
    private readonly ILogger _logger;
    
    /// <summary>
    /// Main build space as manager over projects
    /// </summary>
    private readonly IBuildSpace _buildSpace;

    /// <summary>
    /// Build system
    /// </summary>
    public Build()
    { 
        _logger = InitLogger();
        _buildSpace = InitBuildSpace();        
    }
    
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
                Path.Combine(RootDirectory.Parent, "project", "main", ".stbuild", "ExtensionUtilityCppProject.json")
            },
            Variants = new VariantList
            {
                new()
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
                new()
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
            },
            ManagerProps = new List<IManagerProp>
            {
                new BuilderMsCppProps
                {
                    Name = "BuilderCpp",
                    MsBuilderPath = "c:/Program Files/Microsoft Visual Studio/2022/Community/Msbuild/Current/Bin/MSBuild.exe"
                },
                new CleanerCommonProps
                {
                    Name = "CleanerCommon",
                    AllBuildResults = true
                },
                new RestorerNugetProps
                {
                    Name = "RestorerNuget",
                    DepsProp = new List<RestorerDepProp>
                    {
                        new()
                        {
                            PackageId = "EncySoftware.CAMAPI.SDK.tlb",
                            Version = "1.1.3",
                            OutDir = Path.Combine(RootDirectory.Parent, "SDK")
                        }
                    }
                }
            }
        };
        settings.ManagerNames.Add("builder", "Debug", "BuilderCpp");
        settings.ManagerNames.Add("builder", "Release", "BuilderCpp");
        settings.ManagerNames.Add("cleaner", "Debug", "CleanerCommon");
        settings.ManagerNames.Add("cleaner", "Release", "CleanerCommon");
        settings.ManagerNames.Add("restorer", "Debug", "RestorerNuget");
        settings.ManagerNames.Add("restorer", "Release", "RestorerNuget");
        
        var tempDir = Path.Combine(RootDirectory, "temp");
        return new BuildSpaceCommon(_logger, tempDir, SettingsReaderType.Object, settings);
    }

    /// <summary>
    /// Parameterized compile
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private Target Compile => _ => _
        .Executes(() =>
        {
            _buildSpace.Projects.Restore(Variant);
            _buildSpace.Projects.Compile(Variant, true);

            // copy settings file, if we want to debug
            foreach (var project in _buildSpace.Projects)
            {
                var mainProjectFilePath = project.MainFilePath;
                if (mainProjectFilePath == null)
                    continue;

                var dllPath = project.GetBuildResultPath(Variant, "dll")
                              ?? throw new Exception("Build results with dll type not found");
                var jsonPath = Path.ChangeExtension(mainProjectFilePath, ".settings.json");
                if (!File.Exists(jsonPath))
                    throw new Exception("Settings file not found");

                File.Copy(jsonPath, Path.ChangeExtension(dllPath, ".settings.json"), true);
            }
        });

    /// <summary>
    /// Delete build results
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private Target Clean => _ => _
        .Executes(() =>
        {
            _buildSpace.Projects.Clean(Variant);
        });

    /// <summary>
    /// Create .dext file, which can be injected
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            foreach (var project in _buildSpace.Projects)
            {
                // path to dll (to be included into dext)
                var dllPath = project.GetBuildResultPath(Variant, "dll")
                              ?? throw new Exception("Build results with dll type not found");

                // path to json, describing extension (to be included into dext)
                var jsonPath = Path.ChangeExtension(dllPath, ".settings.json");

                // make new dext
                var outputFolder = Path.GetDirectoryName(dllPath)
                                   ?? throw new Exception("Parent folder of dll path is null");
                var dextPath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(dllPath) + ".dext");
                if (File.Exists(dextPath))
                    File.Delete(dextPath);

                using var zipToOpen = new FileStream(dextPath, FileMode.Create);
                using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update);
                archive.CreateEntryFromFile(dllPath, Path.GetFileName(dllPath));
                archive.CreateEntryFromFile(jsonPath, Path.GetFileName(jsonPath));
                _logger.head($"Created dext file: {dextPath}");
            }
        });
}