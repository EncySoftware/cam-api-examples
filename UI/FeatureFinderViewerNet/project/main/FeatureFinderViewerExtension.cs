using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace FeatureFinderViewerNet;

/// <summary>
/// Opens a non-modal Feature Finder viewer window.
/// Implements <see cref="IExtensionLazyUnloadable"/> so ENCY defers DLL unloading until the window closes.
/// </summary>
public class FeatureFinderViewerExtension : IExtension, IExtensionUtility, IExtensionLazyUnloadable
{
    private readonly ExtensionWindowLazyUnloadable _windowManager = new();

    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public bool CanUnload
    {
        get => _windowManager.CanUnload;
        set { }
    }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            using var applicationCom = ComWrapper.Create(context.CamApplication);
            _windowManager.SetOwnerHandle(applicationCom);

            var appForWindow = ComWrapper.Create(context.CamApplication);
            _windowManager.ShowWindow(() => new FeatureFinderViewerWindow(appForWindow));
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
