using System;
using System.Collections.Generic;
using BuildSystem;
using BuildSystem.Core.Builders.Dotnet;
using BuildSystem.Core.Builders.MsCpp;
using BuildSystem.Core.Builders.MsDelphi;
using BuildSystem.Core.Cleaner;
using BuildSystem.Core.HashGenerator;
using BuildSystem.Core.ProjectCache;
using BuildSystem.ManagerObject.Interfaces;
using BuildSystem.ManagerObject.Interfaces.Package;
using BuildSystem.ManagerObject.Interfaces.Variants;
using BuildSystem.ProjectList;
using Logging;

/// <inheritdoc />
internal class BuildSpaceSettings : BuildSpaceSettingsCommon
{
    /// <inheritdoc />
    public BuildSpaceSettings(ILogger logger, string rootDirectory)
        : base(logger, rootDirectory)
    {
        ProjectListProps = new ProjectListCommonProps(logger)
        {
            SetStorageInfo = SetStorageInfoFunc
        };

        Variants =
        [
            new Variant
            {
                Name = "Debug",
                Configurations = new Dictionary<string, string> { [Variant.NodeConfig] = "Debug" },
                Platforms = new Dictionary<string, string>
                {
                    [Variant.NodePlatform] = "Win64",
                    [Variant.NodePlatform + "_csharp"] = "x64",
                    [Variant.NodePlatform + "_cpp"] = "x64"
                }
            },

            new Variant
            {
                Name = "Release",
                Configurations = new Dictionary<string, string> { [Variant.NodeConfig] = "Release" },
                Platforms = new Dictionary<string, string>
                {
                    [Variant.NodePlatform] = "Win64",
                    [Variant.NodePlatform + "_csharp"] = "x64",
                    [Variant.NodePlatform + "_cpp"] = "x64"
                }
            }
        ];

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
                RsVarsPath = "c:/program files (x86)/embarcadero/studio/23.0/bin/rsvars.bat"
            },
            new BuilderMsCppProps
            {
                Name = "BuilderCpp",
                MsBuilderPath = "c:/Program Files/Microsoft Visual Studio/2022/Community/Msbuild/Current/Bin/MSBuild.exe"
            },
            new CleanerCommonProps
            {
                Name = "CleanerCommon"
            },
            new ProjectCacheCommonProps
            {
                Name = "ProjectCacheCommon",
                TempDir = System.IO.Path.Combine(rootDirectory, ".stbuild", "cache")
            },
            new HashGeneratorCommonProps
            {
                Name = "HashGeneratorCommon",
                HashAlgorithmType = HashAlgorithmType.Sha256
            }
        ];

        ManagerNames.Add("builder_delphi", "Debug", "BuilderDelphi");
        ManagerNames.Add("builder_delphi", "Release", "BuilderDelphi");
        ManagerNames.Add("builder_csharp", "Debug", "BuilderDotnet");
        ManagerNames.Add("builder_csharp", "Release", "BuilderDotnet");
        ManagerNames.Add("builder_cpp", "Debug", "BuilderCpp");
        ManagerNames.Add("builder_cpp", "Release", "BuilderCpp");
        ManagerNames.Add("cleaner", "Debug", "CleanerCommon");
        ManagerNames.Add("cleaner", "Release", "CleanerCommon");
        ManagerNames.Add("project_cache", "Debug", "ProjectCacheCommon");
        ManagerNames.Add("project_cache", "Release", "ProjectCacheCommon");
        ManagerNames.Add("hash_generator", "Debug", "HashGeneratorCommon");
        ManagerNames.Add("hash_generator", "Release", "HashGeneratorCommon");
    }

    private static List<StorageInfo> SetStorageInfoFunc(PackageAction packageAction, string packageId, VersionProp? packageVersion)
    {
        return
        [
            new StorageInfo
            {
                Url = Environment.GetEnvironmentVariable("NUGET_FEED_URL") ?? "https://api.nuget.org/v3/index.json",
                ApiKey = Environment.GetEnvironmentVariable("NUGET_AUTH_TOKEN") ?? ""
            }
        ];
    }
}
