using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Feeds the host UI theme palette (CAMAPI commit 9c13e114) into the window's
/// style brushes. The control styles live centrally in Themes/PluginTheme.xaml
/// and reference these brushes via DynamicResource; here we only override the
/// brush VALUES from the host's <see cref="ICamApiTheme"/>, so the one style set
/// covers the dark, light and any other host theme. Silently keeps the
/// dictionary's (dark) defaults when the host does not expose ICamApiTheme.
/// </summary>
internal static class ThemeService
{
    public static void Apply(Window window, ComWrapper<ICamApiApplication> appCom)
    {
        (bool ok, int win, int panel, int text, int border, int btn, int accent) p;
        try
        {
            // Read every colour inside ONE Invoke on the worker thread and release
            // the theme object there: TCamApiTheme is created fresh per getter
            // call, so releasing it on its own apartment frees it deterministically
            // (holding/disposing a cross-apartment wrapper leaked it).
            p = appCom.Invoke(app => ReadPalette(app));
        }
        catch
        {
            // Host does not expose ICamApiTheme (older Bin64) — keep dark defaults.
            return;
        }
        if (!p.ok) return;

        // Override the style brushes at the window level (shadows the merged
        // dictionary defaults; DynamicResource references repaint automatically).
        var res = window.Resources;
        res["ThemeWindowBrush"] = Brush(p.win);
        res["ThemePanelBrush"]  = Brush(p.panel);
        res["ThemeInputBrush"]  = Brush(p.panel);
        res["ThemeTextBrush"]   = Brush(p.text);
        res["ThemeBorderBrush"] = Brush(p.border);
        res["ThemeButtonBrush"] = Brush(p.btn);
        res["ThemeAccentBrush"] = Brush(p.accent);
    }

    private static (bool, int, int, int, int, int, int) ReadPalette(ICamApiApplication app)
    {
        var theme = app.Theme;
        if (theme is null)
            return (false, 0, 0, 0, 0, 0, 0);
        try
        {
            return (true,
                theme.GetColor(TCamApiColorKind.ckColorWindowBackground),
                theme.GetColor(TCamApiColorKind.ckColorPanelBackground),
                theme.GetColor(TCamApiColorKind.ckColorText),
                theme.GetColor(TCamApiColorKind.ckColorBorder),
                theme.GetColor(TCamApiColorKind.ckColorBtnBackground),
                theme.GetColor(TCamApiColorKind.ckColorAccent));
        }
        finally
        {
            if (Marshal.IsComObject(theme))
                Marshal.ReleaseComObject(theme);
        }
    }

    private static SolidColorBrush Brush(int tcolor)
    {
        // TColor is BGR (low byte = blue).
        byte b = (byte)( tcolor        & 0xFF);
        byte g = (byte)((tcolor >> 8 ) & 0xFF);
        byte r = (byte)((tcolor >> 16) & 0xFF);
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
