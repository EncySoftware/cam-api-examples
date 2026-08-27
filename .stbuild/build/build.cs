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
using BuildSystem.ProjectList.Model;
using System.Linq;    
using Nuke.Common.Utilities.Collections;
using Utils;
using Logging;

// ReSharper disable AllUnderscoreLocalParameterName

/// <inheritdoc />
// ReSharper disable once CheckNamespace
public class Build : NukeBuild
{
    /// <summary>
    /// Calling target by default
    /// </summary>
    public static int Main()
    {
        var parentDirectory = new DirectoryInfo(EnvironmentInfo.WorkingDirectory)
            .DescendantsAndSelf(x => x.Parent ?? throw new Exception("Parent directory is null for " + x.FullName))
            .First(x => x.GetDirectories(".stbuild").Any())
            .FullName;
        Environment.SetEnvironmentVariable("root", Path.Combine(parentDirectory, ".stbuild"));
        return Execute<Build>(x => x.Compile);
    }

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
        BuildInfo.RunParams[RunInfo.Local] = "local";
        
        var settings = new BuildSpaceSettings(Logger, RootDirectory.Parent);
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
            foreach (var project in BuildSpace.Projects.List.All())
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
            foreach (var project in BuildSpace.Projects.List.All())
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
            foreach (var project in BuildSpace.Projects.List.All())
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