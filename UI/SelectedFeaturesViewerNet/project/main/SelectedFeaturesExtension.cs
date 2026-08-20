using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace SelectedFeaturesViewerNet;

/// <summary>
/// Toolbar utility that opens a non-modal window listing the features recognized on the
/// geometry nodes currently selected in the viewport.
/// Implements <see cref="IExtensionLazyUnloadable"/> so ENCY defers DLL unloading until the
/// window closes — the window keeps living after <see cref="Run"/> returns.
/// </summary>
public class SelectedFeaturesExtension : IExtension, IExtensionUtility, IExtensionLazyUnloadable
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

            // Second wrapper — no 'using' — ownership transferred to the window,
            // which disposes it in SelectedFeaturesWindow.Dispose().
            var appForWindow = ComWrapper.Create(context.CamApplication);
            _windowManager.ShowWindow(() => new SelectedFeaturesWindow(appForWindow));
            // Run returns immediately; the window lives on its own STA thread.
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
