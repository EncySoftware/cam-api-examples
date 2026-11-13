using System;
using System.Collections.Generic;
using BuildSystem;
using BuildSystem.Core.Builders.Dotnet;
using BuildSystem.Core.Cleaner;
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
                Platforms = new Dictionary<string, string> { [Variant.NodePlatform] = "AnyCPU" }
            },

            new Variant
            {
                Name = "Release",
                Configurations = new Dictionary<string, string> { [Variant.NodeConfig] = "Release" },
                Platforms = new Dictionary<string, string> { [Variant.NodePlatform] = "AnyCPU" }
            }
        ];

        ManagerProps =
        [
            new BuilderDotnetProps
            {
                Name = "BuilderDotnet"
            },
            new CleanerCommonProps
            {
                Name = "CleanerCommon"
            }
        ];

        ManagerNames.Add("builder", "Debug", "BuilderDotnet");
        ManagerNames.Add("builder", "Release", "BuilderDotnet");
        ManagerNames.Add("cleaner", "Debug", "CleanerCommon");
        ManagerNames.Add("cleaner", "Release", "CleanerCommon");
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
