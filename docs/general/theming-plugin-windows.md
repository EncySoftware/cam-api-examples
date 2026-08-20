# Applying the Host Theme to a Plugin Window

The host application exposes its active UI palette read-only through `ICamApiTheme`. It does **not**
paint plugin windows — a plugin that opens its own window is responsible for matching the host look
itself.

This page is about **applying** the palette. For **reading** it — `Name` / `Kind` / `IsDark` /
`GetColor` and the `TCamApiColorKind` slot list — see
[api/ui.md § ICamApiTheme](../api/ui.md#icamapitheme).

## Rules

1. Read the palette **once**, when the window opens, and treat it as best-effort: if the host does
   not expose a theme, keep the framework defaults and never throw.
2. **WPF** — put the palette into resource brushes referenced with `DynamicResource`. That is the
   whole job.
3. **WinForms** — assigning `BackColor` is not enough. Controls painted by Windows visual styles
   silently ignore it; each one needs its own opt-out (table below).
4. Never try to strip visual styles with `SetWindowTheme(handle, "", "")`. It crashes some controls.
5. Prefer controls that honour `BackColor`. `NumericUpDown` and `TabControl` cannot be fully themed —
   pick a different control instead of fighting them.
6. Scrollbars are themed by *name*, not by colour, and often only after their handle exists.
7. The window title bar is a DWM call — cosmetic, wrap in `try`/`catch`.

---

## 1. Read the palette once, best-effort

Read every colour inside a **single** `Invoke` on the application wrapper and release the theme
object there: the host creates a fresh theme object per getter call, so releasing it on its own
apartment frees it deterministically.

```csharp
private static Palette ReadPalette(ICamApiApplication app)
{
    var theme = app.Theme;
    if (theme is null)
        return default;                 // headless / kernel-only host build

    try
    {
        return new Palette(true,
            FromTColor(theme.GetColor(TCamApiColorKind.ckColorWindowBackground)),
            FromTColor(theme.GetColor(TCamApiColorKind.ckColorPanelBackground)),
            FromTColor(theme.GetColor(TCamApiColorKind.ckColorText)),
            FromTColor(theme.GetColor(TCamApiColorKind.ckColorBorder)),
            FromTColor(theme.GetColor(TCamApiColorKind.ckColorBtnBackground)),
            FromTColor(theme.GetColor(TCamApiColorKind.ckColorAccent)));
    }
    finally
    {
        if (Marshal.IsComObject(theme))
            Marshal.ReleaseComObject(theme);
    }
}
```

The call site swallows everything — a missing theme is a normal condition, not an error:

```csharp
Palette palette;
try
{
    using var appCom = ComWrapper.Create(application);
    palette = appCom.Invoke(ReadPalette);
}
catch
{
    return;                             // keep the framework's default colours
}
if (!palette.Ok)
    return;
```

### Colour conversion

`GetColor` returns a Delphi `TColor` (`0x00BBGGRR`) — **the low byte is red**. Win32 `COLORREF`
(needed for the DWM calls below) uses the same layout, so it is rebuilt from the parsed colour:

```csharp
private static Color FromTColor(int tcolor)
    => Color.FromArgb(tcolor & 0xFF, (tcolor >> 8) & 0xFF, (tcolor >> 16) & 0xFF);

private static int ToColorRef(Color c)
    => c.R | (c.G << 8) | (c.B << 16);
```

### Dark or light

`ICamApiTheme.IsDark` answers this directly. If you already have the window background colour, a
luminance test on it avoids a second COM call and stays correct for host themes the SDK does not
know yet:

```csharp
private static bool IsDark(Color c)
    => 0.299 * c.R + 0.587 * c.G + 0.114 * c.B < 128;
```

---

## 2. WPF: brushes and `DynamicResource`

WPF has none of the problems below. Declare the palette as brushes in a resource dictionary, style
your controls against those brushes with `DynamicResource`, and at runtime replace only the brush
*values* — every reference repaints itself:

```xml
<SolidColorBrush x:Key="ThemeWindowBrush" Color="#1E1E1E"/>
<SolidColorBrush x:Key="ThemeTextBrush"   Color="#F1F1F1"/>

<Style TargetType="TextBlock">
    <Setter Property="Foreground" Value="{DynamicResource ThemeTextBrush}"/>
</Style>
```

```csharp
var res = window.Resources;
res["ThemeWindowBrush"] = Brush(palette.Window);
res["ThemeTextBrush"]   = Brush(palette.Text);
```

The dictionary values double as the fallback palette when the host exposes no theme, so one style
set covers dark, light and any future host theme.

One WPF caveat: the stock `Button` and `ListViewItem` templates paint their own chrome and ignore
`Background` in the hover/selected states. Retemplate them (a `Border` bound to `{TemplateBinding
Background}`) if you need those states themed.

In-repo reference: `UI/SelectedFeaturesViewerNet/project/main/Themes/PluginTheme.xaml` with
`Service/ThemeService.cs`, and `FullWorkflow/PartCalibrationWorkflowNet/project/main/Service/ThemeService.cs`.

Everything from here on is WinForms / native visual-styles specific.

---

## 3. WinForms: visual styles override `BackColor`

The failure mode is deceptive: the assignment succeeds and the property reads back correctly
(`BackColor` really is `#1E2028`), but the control still renders light, because it is painted by the
Windows visual-styles renderer rather than from its own colour properties. There is no exception and
no warning.

| Control | Does `BackColor` apply? | What it takes |
|---|---|---|
| `Panel`, `Label`, `GroupBox`, `TextBox`, `CheckBox`, `ListBox` | yes | set `BackColor` / `ForeColor` |
| `Button` | no | `FlatStyle = FlatStyle.Flat` + `FlatAppearance.BorderColor` |
| `ComboBox` with `DropDownStyle = DropDownList` | no | `FlatStyle = FlatStyle.Flat` |
| `DataGridView` | headers no, body yes | `EnableHeadersVisualStyles = false` + explicit cell styles |
| `NumericUpDown` | spin buttons never | use a different control (see rule 4) |
| `TabControl` | tab strip never | use a different control (see rule 4) |

Walk the tree recursively and special-case the controls that need the opt-out:

```csharp
private static void ApplyToChildren(Control parent, Palette p, bool dark)
{
    foreach (Control control in parent.Controls)
    {
        switch (control)
        {
            case Button button:
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = p.Border;
                button.BackColor = p.Button;
                button.ForeColor = p.Text;
                break;

            // a themed drop-down list ignores BackColor until its style is flat
            case ComboBox combo:
                combo.FlatStyle = FlatStyle.Flat;
                combo.BackColor = p.Panel;
                combo.ForeColor = p.Text;
                break;

            case DataGridView grid:
                ApplyToGrid(grid, p, dark);
                break;

            default:
                control.BackColor = p.Panel;
                control.ForeColor = p.Text;
                break;
        }

        ApplyToChildren(control, p, dark);
    }
}
```

`DataGridView` needs five separate assignments — the four style objects do not inherit from each
other, and `BackgroundColor` is only the empty area to the right of the last column:

```csharp
private static void ApplyToGrid(DataGridView grid, Palette p, bool dark)
{
    grid.EnableHeadersVisualStyles = false;     // otherwise headers stay system-coloured
    grid.BackgroundColor = p.Panel;
    grid.GridColor = p.Border;
    grid.ColumnHeadersDefaultCellStyle.BackColor = p.Button;
    grid.ColumnHeadersDefaultCellStyle.ForeColor = p.Text;
    grid.DefaultCellStyle.BackColor = p.Panel;
    grid.DefaultCellStyle.ForeColor = p.Text;
    grid.DefaultCellStyle.SelectionBackColor = p.Accent;
    grid.DefaultCellStyle.SelectionForeColor = p.Text;
}
```

---

## 4. Anti-pattern: do not strip visual styles

`SetWindowTheme(handle, "", "")` detaches a control from the visual-styles engine. It looks like the
universal fix for anything that ignores `BackColor`. **Do not use it.** WinForms controls that own
their painting still go through `VisualStyleRenderer`, which now has nothing to render with:

```
InvalidOperationException: Visual Style handle creation operation did not succeed
   at UpDownBase.UpDownButtons.OnPaint
```

The crash happens on the first repaint, not at the call — so it surfaces as a random failure later.

The correct fix is control selection, not force. **When designing a plugin window, pick controls that
honour `BackColor`:**

- `NumericUpDown` → a plain `TextBox`, parsing the value when the user presses the action button.
- `TabControl` → a `SplitContainer` (or your own header buttons).

The empty-string form is the problem. Passing a real theme *name* (next section) is the supported use
of the same API.

---

## 5. Scrollbars

Scrollbars are not coloured — they are re-themed by name, `"DarkMode_Explorer"` or `"Explorer"`:

```csharp
[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
private static extern int SetWindowTheme(IntPtr hwnd, string? subAppName, string? subIdList);
```

Three details:

**(a) The handle may not exist yet.** At the moment the window is shown, a grid's scrollbars usually
have no native handle (a typical run finds four scrollbars and can theme one). Subscribe for the
rest:

```csharp
foreach (var scrollBar in FindScrollBars(form))
{
    if (scrollBar.IsHandleCreated)
    {
        SetWindowTheme(scrollBar.Handle, theme, null);
        continue;
    }

    scrollBar.HandleCreated += (sender, _) => SetWindowTheme(((Control)sender!).Handle, theme, null);
}
```

**(b) `DataGridView` scrollbars are private fields** — reach them by reflection:

```csharp
foreach (var field in grid.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
{
    if (typeof(ScrollBar).IsAssignableFrom(field.FieldType) && field.GetValue(grid) is ScrollBar sb)
        yield return sb;
}
```

**(c) Theme the grid handle itself** as well — that is what removes the white square in the corner
between the horizontal and vertical scrollbars:

```csharp
SetWindowTheme(grid.Handle, dark ? "DarkMode_Explorer" : "Explorer", null);
```

Run this pass after the window is realized (`form.BeginInvoke(...)` from the theming entry point) and
wrap it in `try`/`catch` — themed scrollbars are optional chrome.

---

## 6. Window title bar (Windows)

The caption is owned by the desktop window manager, not by the framework:

```csharp
private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
private const int DWMWA_CAPTION_COLOR = 35;
private const int DWMWA_TEXT_COLOR = 36;

[DllImport("dwmapi.dll")]
private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
```

```csharp
var darkFlag = dark ? 1 : 0;
DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkFlag, sizeof(int));

var caption = ToColorRef(p.Window);
DwmSetWindowAttribute(form.Handle, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));
var text = ToColorRef(p.Text);
DwmSetWindowAttribute(form.Handle, DWMWA_TEXT_COLOR, ref text, sizeof(int));
```

`DWMWA_USE_IMMERSIVE_DARK_MODE` gives a dark caption. `DWMWA_CAPTION_COLOR` / `DWMWA_TEXT_COLOR`
paint it in the exact theme colour on Windows 11 build 22000+ and are silently ignored on Windows 10.
All three are cosmetic: wrap them so an unsupported OS build cannot take the window down.

---

## 7. Excluding controls from the walk

Some controls must keep their own colours — legend swatches, status indicators, anything whose colour
carries meaning. Mark them and skip them in the recursive walk:

```csharp
public const string SkipTag = "no-theme";

// at construction
var swatch = new Panel { BackColor = Color.OrangeRed, Tag = FormTheme.SkipTag };

// in the walk
if (control.Tag as string == SkipTag)
    continue;
```

Note that `continue` skips the subtree, not just the control — which is normally what you want for a
self-coloured container.

---

## Checklist

- [ ] Palette read once on window load, inside one `Invoke`, wrapped in `try`/`catch`
- [ ] Missing theme leaves the window on framework defaults without an error
- [ ] `TColor` decoded with the low byte as red
- [ ] WinForms: `FlatStyle.Flat` on every `Button` and `ComboBox`
- [ ] WinForms: `EnableHeadersVisualStyles = false` and all cell styles on every `DataGridView`
- [ ] No `NumericUpDown` / `TabControl` in the window
- [ ] No `SetWindowTheme(handle, "", "")` anywhere
- [ ] Scrollbars themed after realization, including the deferred `HandleCreated` ones
- [ ] Title-bar DWM calls wrapped in `try`/`catch`
