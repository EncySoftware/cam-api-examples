using System.IO;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace ExtensionMainTabNet;

/// <summary>
/// Tab of its own in the ENCY main window. ENCY calls CreatePage, OnShow, OnHide and DestroyPage on its main thread;
/// the page lives on a thread of its own, made by <see cref="MainTabPageHost"/>
/// </summary>
public class ExtensionMainTab : IExtension, IExtensionMainTab
{
    /// <summary>
    /// Id of the extension in the settings file, and the id of the tab for SetActiveMainTab
    /// </summary>
    public const string TabId = "Extension.MainTab.Net";

    private ComWrapper<ICamApiApplication>? _applicationCom;
    private MainTabPageHost? _host;
    private MainTabPage? _page;

    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void CreatePage(IExtensionMainTabContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            var applicationCom = _applicationCom = ComWrapper.Create(context.CamApplication);

            // The content factory gets the ENCY values read here and does not call ENCY itself
            var palette = ThemePalette.Read(applicationCom);
            MainTabPage? page = null;
            _host = MainTabPageHost.Create(context.HostWindowHandle,
                () => page = new MainTabPage(applicationCom, palette));
            _page = page;
        }
        catch (Exception e)
        {
            _applicationCom?.Dispose();
            _applicationCom = null;
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    /// <inheritdoc />
    public void OnShow(IExtensionMainTabContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            var page = _page;
            if (_host == null || page == null || _applicationCom == null)
                return;
            var projectName = ActiveProjectName(_applicationCom);
            var palette = ThemePalette.Read(_applicationCom);

            // Posts to the page thread without waiting for it
            _host.Dispatcher.BeginInvoke(() => page.Show(projectName, palette));
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    /// <inheritdoc />
    public void OnHide(IExtensionMainTabContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
    }

    /// <inheritdoc />
    public void DestroyPage(IExtensionMainTabContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            var host = _host;
            var applicationCom = _applicationCom;
            _host = null;
            _page = null;
            _applicationCom = null;
            if (host == null)
            {
                applicationCom?.Dispose();
                return;
            }

            // Releases the wrapper on the page thread, after the last call of the page to ENCY
            if (applicationCom != null)
                host.Dispatcher.InvokeAsync(applicationCom.Dispose);

            // Destroys the page window on its thread without waiting; ENCY waits for the window itself
            host.Dispose();
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    /// <summary>
    /// File name of the active project without the extension
    /// </summary>
    private static string ActiveProjectName(ComWrapper<ICamApiApplication> applicationCom)
    {
        using var projectCom = applicationCom.GetActiveProject();
        if (projectCom.IsNull)
            return "No project";
        var filePath = projectCom.FilePath();
        return string.IsNullOrEmpty(filePath) ? "New project, not saved yet" : Path.GetFileNameWithoutExtension(filePath);
    }
}
