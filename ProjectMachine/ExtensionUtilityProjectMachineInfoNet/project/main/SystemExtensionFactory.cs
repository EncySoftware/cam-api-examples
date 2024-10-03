using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace ExtensionUtilityProjectMachineInfoNet;

/// <summary>
/// Factory for creating system extensions and releasing used objects after creation
/// </summary>
public static class SystemExtensionFactory
{
    /// <summary>
    /// Create instance of extension by type id. All objects are released after creation,
    /// except the extension itself. Created object will be disposable, so it will be released
    /// after using
    /// </summary>
    /// <param name="extensionTypeId">Unique identifier of extension</param>
    /// <param name="info">Object to get extension manager reference</param>
    /// <typeparam name="T">Type, which extension should be casted to</typeparam>
    /// <returns>Instance of extension</returns>
    public static ApiComObject<T> CreateExtension<T>(string extensionTypeId, IExtensionInfo? info)
    {
        using var instanceInfo = new ApiComObject<IExtensionInstanceInfo>(info?.InstanceInfo);
        using var extensionManager = new ApiComObject<IExtensionManager>(instanceInfo.Instance.ExtensionManager);
        var extension = extensionManager.Instance.CreateExtension(extensionTypeId, out var resultStatus)
                        ?? throw new Exception($"Extension {extensionTypeId} not found");
        if (resultStatus.Code == TResultStatusCode.rsError)
            throw new Exception($"Extension {extensionTypeId} not created: {resultStatus.Description}");
        if (extension is not T typedExtension)
            throw new Exception($"Extension {extensionTypeId} is not of type {typeof(T).Name}");
        return new ApiComObject<T>(typedExtension);
    }
    
    /// <summary>
    /// Create instance of extension by type id. All objects are released after creation,
    /// except the extension itself.
    /// </summary>
    /// <param name="extensionTypeId">Unique identifier of extension</param>
    /// <param name="info">Object to get extension manager reference</param>
    /// <typeparam name="T">Type, which extension should be casted to</typeparam>
    /// <returns>Instance of extension</returns>
    public static ApiComObject<T> GetSingletonExtension<T>(string extensionTypeId, IExtensionInfo? info)
    {
        using var instanceInfo = new ApiComObject<IExtensionInstanceInfo>(info?.InstanceInfo);
        using var extensionManager = new ApiComObject<IExtensionManager>(instanceInfo.Instance.ExtensionManager);
        var extension = extensionManager.Instance.GetSingletonExtension(extensionTypeId, out var resultStatus)
                        ?? throw new Exception($"Extension {extensionTypeId} not found");
        if (resultStatus.Code == TResultStatusCode.rsError)
            throw new Exception($"Extension {extensionTypeId} not created: {resultStatus.Description}");
        if (extension is not T typedExtension)
            throw new Exception($"Extension {extensionTypeId} is not of type {typeof(T).Name}");
        return new ApiComObject<T>(typedExtension);
    }
}