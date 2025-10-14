using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using BuildSystem;
using BuildSystem.Core.Builders.Dotnet;
using BuildSystem.Core.Builders.MsCpp;
using BuildSystem.Core.Builders.MsDelphi;
using BuildSystem.Core.Cleaner;
using BuildSystem.Core.HashGenerator;
using BuildSystem.Core.ProjectCache;
using Nuke.Common;
using BuildSystem.Info;
using BuildSystem.ManagerObject.Interfaces;
using BuildSystem.ManagerObject.Interfaces.Package;
using BuildSystem.ManagerObject.Interfaces.Variants;
using BuildSystem.ProjectList;
using BuildSystem.ProjectList.Restorer;
using Loggers;
using Logging;

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
        console.setMinLevel(Logging.LogLevel.info);

        // logging to file
        var file = new LoggerFile(Path.Combine(RootDirectory, "logs"), "log", 7);
        file.setMinLevel(Logging.LogLevel.debug);
        
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
            ProjectListProps = new ProjectListCommonProps(Logger)
            {
                SetStorageInfo = (_, _, _) => []
            },
            Projects =
            [
                // Path.Combine(RootDirectory.Parent, @"ExtensionEmpty\ExtensionEmptyCpp\project\main\.stbuild\ExtensionEmptyCppProject.json"),
                Path.Combine(RootDirectory.Parent, @"ExtensionEmpty\ExtensionEmptyDelphi\project\main\.stbuild\ExtensionEmptyDelphiProject.json"),
                Path.Combine(RootDirectory.Parent, @"ExtensionEmpty\ExtensionEmptyNet\project\main\.stbuild\ExtensionEmptyNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"ExtensionGlobal\ExtensionGlobalNet\project\main\.stbuild\ExtensionGlobalNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"ExtensionOperationPopup\ExtensionOperationPopupNet\project\main\.stbuild\ExtensionOperationPopupNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"ExtensionOperationPopup\ExtensionOperationPopupOnChangeNet\project\main\.stbuild\ExtensionOperationPopupOnChangeNetProject.json"),
                // Path.Combine(RootDirectory.Parent, @"ExtensionUtility\ExtensionUtilityCpp\project\main\.stbuild\ExtensionUtilityCppProject.json"),
                Path.Combine(RootDirectory.Parent, @"ExtensionUtility\ExtensionUtilityDelphi\project\main\.stbuild\ExtensionUtilityDelphiProject.json"),
                Path.Combine(RootDirectory.Parent, @"ExtensionUtility\ExtensionUtilityNet\project\main\.stbuild\ExtensionUtilityNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"GCodeGeneration\ExtensionUtilityNCMakerNet\project\main\.stbuild\ExtensionUtilityNCMakerNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"Geometry\AddinImportSvgNet\project\main\.stbuild\AddinImportSvgNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"Geometry\AddinImportObjNet\project\main\.stbuild\AddinImportObjNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"Geometry\ExtensionUtilityGeomCustomImportNet\project\main\.stbuild\ExtensionUtilityGeomCustomImportNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"Geometry\ExtensionUtilityGeometryImporterNet\project\main\.stbuild\ExtensionUtilityGeometryImporterNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"Geometry\ExtensionUtilityGeometryModelNet\project\main\.stbuild\ExtensionUtilityGeometryModelNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"Geometry\ExtensionUtilityGeometryPickerNet\project\main\.stbuild\ExtensionUtilityGeometryPickerNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"Geometry\ExtensionUtilityImportSvgNet\project\main\.stbuild\ExtensionUtilityImportSvgNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"PLMIntegration\PLMExtensionDelphi\project\main\.stbuild\PLMExtensionDelphiProject.json"),
                Path.Combine(RootDirectory.Parent, @"PLMIntegration\PLMExtensionNet\project\main\.stbuild\PLMExtensionNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"ProjectMachine\ExtensionUtilityProjectMachineInfoNet\project\main\.stbuild\ExtensionUtilityProjectMachineInfoNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"ProjectToolsList\ExtensionUtilityProjectToolsListNet\project\main\.stbuild\ExtensionUtilityProjectToolsListNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"UI\ExtensionUtilityDialogWindowNet\project\main\.stbuild\ExtensionUtilityDialogWindowNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"UI\ExtensionUtilityMessageBoxNet\project\main\.stbuild\ExtensionUtilityMessageBoxNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"UI\ExtensionUtilityNotifyNet\project\main\.stbuild\ExtensionUtilityNotifyNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"MachiningTools\MachiningToolsCreateExampleNet\.stbuild\MachiningToolsCreateExampleNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"MachiningTools\DIN4000ImportPluginNet\.stbuild\DIN4000ImportPluginNetProject.json"),
                Path.Combine(RootDirectory.Parent, @"Attributes\ExtensionAttributesManageNet\project\main\.stbuild\ExtensionAttributesManageNet.json"),
            ],
            Variants =
            [
                new Variant
                {
                    Name = "Debug",
                    Configurations = new Dictionary<string, string>
                    {
                        [BuildSystem.ManagerObject.Interfaces.Variants.Variant.NodeConfig] = "Debug"
                    },
                    Platforms = new Dictionary<string, string>
                    {
                        [BuildSystem.ManagerObject.Interfaces.Variants.Variant.NodePlatform] = "Win64",
                        [BuildSystem.ManagerObject.Interfaces.Variants.Variant.NodePlatform + "_csharp"] = "x64",
                        [BuildSystem.ManagerObject.Interfaces.Variants.Variant.NodePlatform + "_cpp"] = "x64"
                    }
                },

                new Variant
                {
                    Name = "Release",
                    Configurations = new Dictionary<string, string>
                    {
                        [BuildSystem.ManagerObject.Interfaces.Variants.Variant.NodeConfig] = "Release"
                    },
                    Platforms = new Dictionary<string, string>
                    {
                        [BuildSystem.ManagerObject.Interfaces.Variants.Variant.NodePlatform] = "Win64",
                        [BuildSystem.ManagerObject.Interfaces.Variants.Variant.NodePlatform + "_csharp"] = "x64",
                        [BuildSystem.ManagerObject.Interfaces.Variants.Variant.NodePlatform + "_cpp"] = "x64"
                    }
                }
            ],
            ManagerProps =
            [
                new BuilderDotnetProps
                {
                    Name = "BuilderDotnet"
                },
                new BuilderMsDelphiProps
                {
                    Name = "BuilderDelphi",
                    MsBuilderPath = "C:/Windows/Microsoft.NET/Framework/v4.0.30319/MSBuild.exe",
                    EnvBdsPath = "c:/program files (x86)/embarcadero/studio/23.0",
                    RsVarsPath = "c:/program files (x86)/embarcadero/studio/23.0/bin/rsvars.bat",
                },
                new BuilderMsCppProps
                {
                    Name = "BuilderCpp",
                    MsBuilderPath = "c:/Program Files/Microsoft Visual Studio/2022/Community/Msbuild/Current/Bin/MSBuild.exe"
                },
                new RestorerNugetProps
                {
                    Name = "RestorerNuget"
                },
                new CleanerCommonProps
                {
                    Name = "CleanerCommon"
                },
                new ProjectCacheCommonProps
                {
                    Name = "ProjectCacheCommon",
                    TempDir = RootDirectory / "cache"
                },
                new HashGeneratorCommonProps
                {
                    Name = "HashGeneratorCommon",
                    HashAlgorithmType = HashAlgorithmType.Sha256
                }
            ]
        };
        settings.ManagerNames.Add("builder_delphi", "Debug", "BuilderDelphi");
        settings.ManagerNames.Add("builder_delphi", "Release", "BuilderDelphi");
        settings.ManagerNames.Add("builder_csharp", "Debug", "BuilderDotnet");
        settings.ManagerNames.Add("builder_csharp", "Release", "BuilderDotnet");
        settings.ManagerNames.Add("builder_cpp", "Debug", "BuilderCpp");
        settings.ManagerNames.Add("builder_cpp", "Release", "BuilderCpp");
        settings.ManagerNames.Add("restorer", "Debug", "RestorerNuget");
        settings.ManagerNames.Add("restorer", "Release", "RestorerNuget");
        settings.ManagerNames.Add("cleaner", "Debug", "CleanerCommon");
        settings.ManagerNames.Add("cleaner", "Release", "CleanerCommon");
        settings.ManagerNames.Add("project_cache", "Debug", "ProjectCacheCommon");
        settings.ManagerNames.Add("project_cache", "Release", "ProjectCacheCommon");
        settings.ManagerNames.Add("hash_generator", "Debug", "HashGeneratorCommon");
        settings.ManagerNames.Add("hash_generator", "Release", "HashGeneratorCommon");
        settings.ReaderLocalVars["package_namespace"] = "ENCY";
        
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
            //BuildSpace.Projects.Restore(Variant);
            BuildSpace.Projects.Compile(Variant, true);

            // copy settings file, if we want to debug
            foreach (var project in BuildSpace.Projects)
            {
                var mainProjectFilePath = project.MainFilePath;
                if (mainProjectFilePath == null)
                    continue;

                var dllPath = project.GetBuildResultPath(Variant, "dll")
                              ?? throw new Exception("Build results with dll type not found");
                var jsonPath = Path.ChangeExtension(mainProjectFilePath, ".settings.json");
                if (File.Exists(jsonPath))
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
            BuildSpace.Projects.Clean("Debug");
            BuildSpace.Projects.Clean("Release");
        });

    /// <summary>
    /// Create .dext-file, which can be injected
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            foreach (var project in BuildSpace.Projects)
            {
                // path to dll (to be included in dext)
                var dllPath = project.GetBuildResultPath(Variant, "dll")
                              ?? throw new Exception("Build results with dll type not found");

                // path to JSON, describing extension (to be included in dext)
                var jsonPath = Path.ChangeExtension(dllPath, ".settings.json");
                if (!File.Exists(jsonPath)) {
                    Logger.head($"Create of dext file skipped for: {project.MainFilePath}");
                    continue;
                }
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
                Logger.head($"Created dext file: {dextPath}");
            }
        });
    
    /// <summary>
    /// Inject an early created .dext-file into the application
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private Target Inject => _ => _
        .DependsOn(Pack)
        .Executes(() =>
        {
            foreach (var project in BuildSpace.Projects)
            {
                // path to dll (to be included in dext)
                var dllPath = project.GetBuildResultPath(Variant, "dll")
                              ?? throw new Exception("Build results with dll type not found");

                // path to JSON, describing extension (to be included in dext)
                var jsonPath = Path.ChangeExtension(dllPath, ".settings.json");
                if (!File.Exists(jsonPath)) {
                    Logger.head($"Injecting of dext file skipped for: {project.MainFilePath}");
                    continue;
                }
                // path to dext
                var dextPath = Path.ChangeExtension(dllPath, ".dext");

                // execute it, because executing application will be chosen automatically
                Logger.head($"Injecting dext file: {dextPath}");
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo(dextPath)
                {
                    UseShellExecute = true
                };
                process.Start();
                process.WaitForExit();
                Logger.debug($"{dextPath} injected");
            }
        });
}