using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using Nuke.Common;
using BuildSystem;
using BuildSystem.Info;
using BuildSystem.ProjectList.Model;
using Logging;
using System.Linq;
using Nuke.Common.Utilities.Collections;
using Utils;
using LoggingLevel = Logging.LogLevel;

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
        return Execute<Build>(x => x.Pack);
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

    /// <summary>
    /// Create .dext file, which can be injected
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            foreach (var project in BuildSpace.Projects.List.All())
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
                Logger.head($"Created dext file: {dextPath}");
            }
        });
}