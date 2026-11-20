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

    private const string ThemeHandlerIdent = TabId + ".ThemeChanged";

    private MainTabPageHost? _host;
    private MainTabPage? _page;
    private object? _themeHandler;

    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void CreatePage(IExtensionMainTabContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            // The content factory does not call ENCY: the palette and the project name come in OnShow
            MainTabPage? createdPage = null;
            var host = _host = MainTabPageHost.Create(context, app => createdPage = new MainTabPage(app));
            var page = _page = createdPage!;

            // The handler runs on the ENCY main thread: reads the new palette there and posts it to the page
            _themeHandler = host.CamApplication.RegisterThemeChangedHandler(ThemeHandlerIdent, _ =>
            {
                var newPalette = ThemePalette.Read(host.CamApplication);
                host.Dispatcher.BeginInvoke(() => page.ApplyPalette(newPalette));
            });
        }
        catch (Exception e)
        {
            Release();
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
            var host = _host;
            var page = _page;
            if (host == null || page == null)
                return;
            var projectName = ActiveProjectName(host.CamApplication);
            var palette = ThemePalette.Read(host.CamApplication);

            // Posts to the page thread without waiting for it
            host.Dispatcher.BeginInvoke(() => page.Show(projectName, palette));
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
            Release();
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    /// <summary>
    /// Unsubscribes from the theme change and destroys the page
    /// </summary>
    private void Release()
    {
        var host = _host;
        var themeHandler = _themeHandler;
        _host = null;
        _page = null;
        _themeHandler = null;
        if (host == null)
            return;

        if (themeHandler != null)
        {
            try
            {
                host.CamApplication.UnregisterHandler(ThemeHandlerIdent);
            }
            catch
            {
                // The page is destroyed even when unsubscribing fails
            }
        }

        // Releases CamApplication and destroys the page window on the page thread without waiting; ENCY waits for the window
        host.Dispose();
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
