using System.Windows;
using System.Windows.Media;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;

namespace ExtensionEmbeddedInspectorNet;

/// <summary>
/// ENCY colours for the window around the inspector
/// </summary>
public readonly record struct ThemePalette(bool Ok, Color Background, Color Text, Color Button, Color ButtonHover)
{
    /// <summary>
    /// Reads the palette of the current ENCY theme. Returns a palette with Ok = false when ENCY has no theme
    /// </summary>
    public static ThemePalette Read(ComWrapper<ICamApiApplication> applicationCom)
    {
        try
        {
            using var themeCom = applicationCom.Theme();
            if (themeCom == null)
                return default;
            return new ThemePalette(true,
                FromTColor(themeCom.GetColor(TCamApiColorKind.ckColorWindowBackground)),
                FromTColor(themeCom.GetColor(TCamApiColorKind.ckColorText)),
                FromTColor(themeCom.GetColor(TCamApiColorKind.ckColorFieldBackground)),
                FromTColor(themeCom.GetColor(TCamApiColorKind.ckColorFieldHoverBackground)));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Replaces the window brushes with the palette colours
    /// </summary>
    public void ApplyTo(ResourceDictionary resources)
    {
        if (!Ok)
            return;
        resources["WindowBackgroundBrush"] = new SolidColorBrush(Background);
        resources["TextBrush"] = new SolidColorBrush(Text);
        resources["ButtonBrush"] = new SolidColorBrush(Button);
        resources["ButtonHoverBrush"] = new SolidColorBrush(ButtonHover);
    }

    private static Color FromTColor(int tcolor)
    {
        return Color.FromRgb((byte)(tcolor & 0xFF), (byte)((tcolor >> 8) & 0xFF), (byte)((tcolor >> 16) & 0xFF));
    }
}
