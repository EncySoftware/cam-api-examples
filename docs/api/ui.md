# ENCY CAM API — UI Domain

This document covers the in-process UI interfaces: the main application window, the 3D viewport, the view cube, and the dialog helpers.

---

## Table of Contents

1. [ICamApiApplicationMainForm — main window](#icamapiapplicationmainform)
2. [ICamApiVisibilityManager — scene object visibility](#icamapivisibilitymanager)
3. [ICamApiTheme — active UI theme and palette](#icamapitheme)
4. [ICamApiViewPort — 3D viewport](#icamapiviewport)
5. [ICamApiViewCube — view orientation](#icamapiviewcube)
6. [ICAMAPI_UIDialogsHelper — dialogs and message boxes](#icamapi_uidialogshelper)
7. [SimplePropIterator — inspector dialog pattern](#simplepropiiterator)
8. [CamApiInspectorWindow — the easy dialog pattern](#camapiinspectorwindow)

---

## ICamApiApplicationMainForm

Represents the main ENCY application window. Obtained from `ICamApiApplication.MainForm`.

### Properties

| Property | Type | Access | Description |
|---|---|---|---|
| `MainViewPort` | `ICamApiViewPort*` | R | The main 3D viewport |
| `MainWindowHandle` | `int64` | R | Win32 HWND of the main window, suitable for use as a parent for child windows |
| `ActiveClientRect` | `TCamApiRect` | R | Screen rectangle of the active client area (place child windows relative to it) |
| `VisibilityManager` | `ICamApiVisibilityManager*` | R | Per-work-mode visibility of 3D scene objects (see below) |
| `FiltersManager` | `ICamApiFiltersManager*` | R | Per-work-mode geometry display filters |
| `HotkeyManager` | `ICamApiHotkeyManager*` | R | Manager for plugin-registered global keyboard shortcuts — same instance as `application.HotkeyManager`; see [application.md → Plugin hotkeys](application.md#plugin-hotkeys). Helper: `mainFormCom.GetHotkeyManager()` |

### Methods — visibility and state

| Method | Description |
|---|---|
| `GetVisible() → boolean` | Whether the main window is currently visible |
| `GetUiInfo() → ICamApiMainFormUiInfo*` | Returns a snapshot of the current UI state (see below) |
| `BeginFreeze(freezeType)` | Prevents user interaction. `TFreezeInterfaceType`: `afiiGeneral`, `afiiGraphicWindow`, `afiiAllowChangeSelection` |
| `EndFreeze()` | Restores user interaction |

### Methods — project management (UI-level)

These methods trigger the same actions as the corresponding toolbar buttons, including confirmation dialogs where applicable.

| Method | Description |
|---|---|
| `OpenProject(fileName, out TResultStatus)` | Opens a project file |
| `OpenProjectSnap(fileName, snapName, out TResultStatus)` | Opens a specific snapshot of a project |
| `SaveCurrentProject(out TResultStatus)` | Saves in-place |
| `SaveCurrentProjectAs(out TResultStatus)` | Shows Save As dialog |
| `SaveProjectAs(fileName)` | Saves to a specified path without dialog |
| `SaveProject()` | Saves without dialog |
| `ExportCurrentProject(out TResultStatus)` | Exports with all snapshots |
| `ExportProjectWithHistory(fileName)` | Saves with full snapshot history |
| `ExportDrillPoints(fileName)` | Exports selected drill operation points |
| `CreateNewProject(out TResultStatus)` | Creates a new empty project |
| `SaveAsMachineSetup(fileName)` | Saves current project as a machine setup file |
| `ImportMachineSetup(fileName)` | Imports a machine setup |
| `ExportSimulationResults(fileName)` | Exports simulation results |

### Methods — application actions

| Method | Description |
|---|---|
| `RunAppSetup(out settingsChanged)` | Opens the application settings dialog |
| `RunUtilitiesSetup()` | Opens the utilities configuration dialog |
| `OpenAiAssistant()` | Opens the AI assistant panel |
| `ShowHelpContents()` | Shows the help documentation |
| `SupportRequest()` | Opens the support request dialog |
| `ShowTutorialWnd()` | Shows the tutorial window |
| `ShowSnapshotManager(projectPath)` | Shows the snapshot manager for a project |
| `CrashReport()` | Starts the crash report dialog |
| `OpenDotnetInterpreterInVSCode(fileName)` | Opens a .NET interpreter script in VS Code |
| `SetCurrentInPrime(value)` | Notifies the form that it is the active instance in the Prime shell |
| `ProcessMessages()` | Pumps pending UI messages of the main thread (equivalent to Pascal `Application.ProcessMessages`); call between blocks of work that must let the UI repaint. Helper: `mainFormCom.ProcessMessages()` |

### Methods — PLM and cloud

| Method | Description |
|---|---|
| `LoadProjectFromPLM(connectionId)` | Imports a project from PLM |
| `ExportProjectToPLM(connectionId)` | Exports the current project to PLM |
| `ConnectPLMConnection(connectionId)` | Connects a PLM connection |
| `DisconnectPLMConnection(connectionId)` | Disconnects a PLM connection |
| `CloudsCollabShare()` | Shares the project in cloud collaboration |
| `CloudsCollabDownload()` | Downloads the latest cloud project version |
| `CloudsCollabOpenChat()` | Opens the cloud collaboration chat |

### Main form event handlers

Register these via `ICamApiApplicationMainForm.RegisterHandler` (same pattern as application events).

| Interface | Method | Fires when |
|---|---|---|
| `ICamApiHandlerApplicationMainFormIsVisibleChanged` | `ApplicationMainFormIsVisibleChanged(handlerIdent, visible)` | Main window shown or hidden |
| `ICamApiHandlerApplicationMainFormMinimizeChanged` | `ApplicationMainFormMinimizeChanged(handlerIdent, minimized)` | Main window minimized or restored |
| `ICamApiHandlerApplicationMainFormUiInfoChanged` | `ApplicationMainFormUiInfoChanged(handlerIdent, uiInfo)` | Any UI state change (title, modification flag, cloud state, etc.) |
| `ICamApiHandlerApplicationMainFormSaveInCloudClicked` | `ApplicationMainFormSaveInCloudClicked(handlerIdent)` | Cloud save button clicked |
| `ICamApiHandlerApplicationMainFormShareClicked` | `ApplicationMainFormShareClicked(handlerIdent)` | Share button clicked |
| `ICamApiHandlerApplicationMainFormCloudChatClicked` | `ApplicationMainFormCloudChatClicked(handlerIdent)` | Cloud chat icon clicked |
| `ICamApiHandlerApplicationMainFormReloadCloudProjectClicked` | `ApplicationMainFormReloadCloudProjectClicked(handlerIdent)` | Reload cloud project clicked |

### ICamApiMainFormUiInfo — UI state snapshot

Returned by `GetUiInfo()`. All members are read-only functions:

| Method | Returns | Description |
|---|---|---|
| `GetCaption()` | `string` | Current window title |
| `GetProjectFile()` | `string` | Path to the open project file |
| `GetDisplayPath()` | `string` | Path as shown in the UI |
| `GetStorageType()` | `string` | Storage type identifier |
| `GetProjectIsModified()` | `bool` | Whether the project has unsaved changes |
| `GetProjectIsNew()` | `bool` | Whether the project has never been saved |
| `GetProcessExists()` | `bool` | Whether a background process is running |
| `GetProcessStage()` | `int` | Background process completion percentage |
| `GetCloudsCollabIsEnabled()` | `bool` | Whether cloud collaboration is available |
| `GetCloudsIsShared()` | `bool` | Whether the project is shared in the cloud |
| `GetCloudsIsOutdated()` | `bool` | Whether a newer cloud version exists |
| `GetCloudsHasNewMessages()` | `bool` | Whether new cloud chat messages exist |
| `GetCloudsLastUpdateTime()` | `string` | Timestamp of the last cloud update |
| `GetUiPLMInfo()` | `ICamApiMainFormUiPLMInfo*` | PLM connection states |

### .NET helper usage

```csharp
using var appCom = ComWrapper.Create(context.CamApplication);
using var mainFormCom = appCom.MainForm();

// Get the Win32 window handle (use as WPF/WinForms parent):
long hwnd = mainFormCom.MainWindowHandle();

// Access the viewport:
using var viewPortCom = mainFormCom.MainViewPort();

// Freeze UI during a long operation:
mainFormCom.BeginFreeze(TFreezeInterfaceType.afiiGeneral);
try
{
    // ... long work ...
}
finally
{
    mainFormCom.EndFreeze();
}

// Check if project is modified:
using var uiInfo = mainFormCom.GetUiInfo();
bool modified = uiInfo.Invoke(i => i.GetProjectIsModified());
```

> See also: [`UI/ExtensionManageViewNet/project/main/ExtensionManageView.cs`](../../UI/ExtensionManageViewNet/project/main/ExtensionManageView.cs)

---

## ICamApiVisibilityManager

Controls whether individual 3D scene objects are shown, **per work mode** (the active
application tab). Obtained from `ICamApiApplicationMainForm.VisibilityManager`; helper class
`VisibilityManagerHelper`.

| Helper method | Description |
|---|---|
| `GetObjectVisibility(mode, objectType)` | Whether `objectType` is visible in the given work mode |
| `SetObjectVisibility(mode, objectType, isVisible)` | Show/hide `objectType` in a work mode. If `mode` is the active tab the scene repaints immediately; otherwise it applies on the next switch to that tab |

`TMainWorkMode` (work mode / active tab): `mwmModel` (0, geometry model) · `mwmMachining`
(1, toolpath) · `mwmSimulating` (2, simulation).

`TWorkObject` (scene object): `awoGeomModel` · `awoPart` · `awoWorkpiece` · `awoJobAssignment`
· `awoMachiningResult` · `awoFixtures` · `awoTool` · `awoHolder` · `awoToolPath` · `awoMachine`.

```csharp
using var mainFormCom = appCom.MainForm();
using var visCom = mainFormCom.InvokeAndWrap(f => f.VisibilityManager);

// Hide fixtures while working on the model
visCom.SetObjectVisibility(TMainWorkMode.mwmModel, TWorkObject.awoFixtures, false);
bool toolShown = visCom.GetObjectVisibility(TMainWorkMode.mwmMachining, TWorkObject.awoTool);
```

---

## ICamApiTheme

A read-only snapshot of the **currently active UI theme** — plugins observe the theme the user
chose (to match their own windows to ENCY's look), they do not switch it. Obtained via
`appCom.Theme()` (helper `ThemeHelper`); the helper returns `null` in headless / kernel-only
builds where no theme factory is registered.

| Helper method | Type | Description |
|---|---|---|
| `Name()` | `string` | Theme variant name (e.g. `"White"`, `"Outer_Space"`); `"Unknown"` if the host theme index is newer than this SDK |
| `Kind()` | `TCamApiThemeKind` | UI engine family — `tkTheme1` (classic), `tkTheme2` (quarter), `tkTheme3` (modern) |
| `IsDark()` | `bool` | `true` for a dark palette (orthogonal to `Kind` — a kind may have both light and dark variants) |
| `GetColor(colorKind)` | `int` | Palette color as a `TColor` integer in **BGR** layout (low byte = blue) |

`TCamApiColorKind` palette slots: `ckColorWindowBackground` (0) · `ckColorPanelBackground` (1)
· `ckColorText` (2) · `ckColorAccent` (3) · `ckColorTitleBackground` (4) ·
`ckColorTitleForeground` (5) · `ckColorBtnBackground` (6) · `ckColorBorder` (7).

```csharp
using var themeCom = appCom.Theme();   // ComWrapper<ICamApiTheme>? — null in headless builds
if (themeCom != null)
{
    using (themeCom)
    {
        bool dark = themeCom.IsDark();
        int bgrBackground = themeCom.GetColor(TCamApiColorKind.ckColorWindowBackground);
        // convert BGR int → your UI framework's color as needed
    }
}
```

> **Note the BGR layout:** ENCY returns Delphi `TColor` (`0x00BBGGRR`). To build a WPF/WinForms
> color, extract `R = c & 0xFF`, `G = (c >> 8) & 0xFF`, `B = (c >> 16) & 0xFF`.

---

## ICamApiViewPort

The main 3D viewport. Obtained via `ICamApiApplicationMainForm.MainViewPort`.

### Properties

| Property | Type | Access | Description |
|---|---|---|---|
| `ScaleFactor` | `double` | R | Current zoom factor |
| `ViewMode` | `TViewPortViewMode` | R | Current rendering mode |
| `Matrix` | `TST3DMatrix` | RW | View transformation matrix; setting it updates the viewport immediately |
| `ViewBox` | `TST2DBox` | RW | View bounding box; setting it updates the viewport immediately |
| `Cube` | `ICamApiViewCube*` | R | The view cube widget |

### Methods

| Method | Description |
|---|---|
| `ZoomAll(fluently: boolean)` | Fits all geometry into view. Pass `true` for animated transition |
| `SetMatrixFluently(matrix)` | Sets the view matrix with animation |
| `SetViewBoxFluently(box)` | Sets the view box with animation |

### TViewPortViewMode enum

| Value | Description |
|---|---|
| `avmWire` | Wireframe — all edges visible |
| `avmShade` | Shaded solid surfaces |
| `avmHide` | Hidden-line removal |
| `avmEdgeShade` | Shaded with visible edges |
| `avmBound` | Bounding-box mode |

### .NET helper usage

```csharp
using var viewPortCom = mainFormCom.MainViewPort();

// Zoom to fit:
viewPortCom.ZoomAll(fluently: true);

// Read current render mode:
TViewPortViewMode mode = viewPortCom.GetViewMode();

// Read current matrix:
TST3DMatrix matrix = viewPortCom.GetMatrix();

// Apply a new matrix immediately:
viewPortCom.SetMatrix(matrix);

// Apply with animation:
viewPortCom.SetMatrixFluently(matrix);

// Access the view cube:
using var cubeCom = viewPortCom.GetCube();
```

---

## ICamApiViewCube

The interactive orientation widget in the viewport corner.

### TViewCubeRotateMode enum — all values

**Faces (6)**
| Value | View |
|---|---|
| `vcrmFaceTop` | Top (+Z) |
| `vcrmFaceBottom` | Bottom (-Z) |
| `vcrmFaceFront` | Front (+Y or CAM-convention front) |
| `vcrmFaceBack` | Back |
| `vcrmFaceLeft` | Left |
| `vcrmFaceRight` | Right |

**Edges (12)**
| Value | View |
|---|---|
| `vcrmEdgeTopFront` | Top-front edge |
| `vcrmEdgeTopBack` | Top-back edge |
| `vcrmEdgeTopLeft` | Top-left edge |
| `vcrmEdgeTopRight` | Top-right edge |
| `vcrmEdgeBottomFront` | Bottom-front edge |
| `vcrmEdgeBottomBack` | Bottom-back edge |
| `vcrmEdgeBottomLeft` | Bottom-left edge |
| `vcrmEdgeBottomRight` | Bottom-right edge |
| `vcrmEdgeFrontLeft` | Front-left edge |
| `vcrmEdgeFrontRight` | Front-right edge |
| `vcrmEdgeBackLeft` | Back-left edge |
| `vcrmEdgeBackRight` | Back-right edge |

**Corners (8)**
| Value | View |
|---|---|
| `vcrmCornerTopFrontLeft` | Top-front-left corner |
| `vcrmCornerTopFrontRight` | Top-front-right corner |
| `vcrmCornerTopBackLeft` | Top-back-left corner |
| `vcrmCornerTopBackRight` | Top-back-right corner |
| `vcrmCornerBottomFrontLeft` | Bottom-front-left corner |
| `vcrmCornerBottomFrontRight` | Bottom-front-right corner |
| `vcrmCornerBottomBackLeft` | Bottom-back-left corner |
| `vcrmCornerBottomBackRight` | Bottom-back-right corner |

### Method

```
Rotate(mode: TViewCubeRotateMode, out ResultStatus: TResultStatus)
```

### .NET helper usage

```csharp
using var viewPortCom = mainFormCom.MainViewPort();
using var cubeCom = viewPortCom.GetCube();

// Snap to standard views:
cubeCom.Rotate(TViewCubeRotateMode.vcrmFaceTop);
cubeCom.Rotate(TViewCubeRotateMode.vcrmFaceFront);
cubeCom.Rotate(TViewCubeRotateMode.vcrmCornerTopFrontRight);
```

---

## ICAMAPI_UIDialogsHelper

Provides modal dialogs and file-selection dialogs. Obtained via the `UIDialogs.CreateHelper()` factory method from `CAMAPI.UIDialogs.DotnetHelper`.

### Methods

| Method | Returns | Description |
|---|---|---|
| `CreateWindow(winCaption) → ICAMAPI_UIDialogWindow*` | `ICAMAPI_UIDialogWindow*` | Creates a custom property-inspector window |
| `MessageBox(msg, dlgType, buttons, defaultButton, title) → TUIButtonType` | `TUIButtonType` | Shows a modal message box |
| `SelectFolderDialog(title, ref folder) → boolean` | `bool` | Opens a folder-picker dialog |
| `SelectFileDialog(title, filter, initialFolder) → string` | `string` | Opens a single-file-picker dialog; returns empty string on cancel |
| `SelectFilesDialog(title, filter, initialFolder) → IListString*` | `IListString*` | Opens a multi-file-picker dialog |
| `SaveFileDialog(title, filter, initialFolder, initialFile) → string` | `string` | Opens a save-file dialog; returns empty string on cancel |
| `ProcessMessages()` | — | Pumps the Windows message queue (use inside long loops) |

### TMessageDialogType enum

| Value | Icon |
|---|---|
| `mdtWarning` | Warning triangle |
| `mdtError` | Error X |
| `mdtInformation` | Information circle |
| `mdtConfirmation` | Question mark |
| `mdtCustom` | No icon |

### TUIButtonType / TUIButtonTypeFlags

Buttons are specified as a **set of flags** (bitmask):

```
btfOk = 1, btfCancel = 2, btfAbort = 4, btfRetry = 8, btfIgnore = 16,
btfYes = 32, btfNo = 64, btfClose = 128, btfHelp = 256,
btfTryAgain = 512, btfContinue = 1024, btfAll = 2048,
btfNoToAll = 4096, btfYesToAll = 8192
```

The `MessageBoxHelper.BuildButtons(params TUIButtonType[])` helper builds the set:

```csharp
var buttons = MessageBoxHelper.BuildButtons(TUIButtonType.btYes, TUIButtonType.btNo);
```

### Message box example

```csharp
using var helperCom = UIDialogs.CreateHelper();
if (helperCom.IsNull)
    throw new Exception("UIDialogs helper unavailable");

var buttons = MessageBoxHelper.BuildButtons(TUIButtonType.btOk, TUIButtonType.btCancel);
TUIButtonType result = helperCom.Invoke(helper => helper.MessageBox(
    "Do you want to continue?",
    TMessageDialogType.mdtConfirmation,
    buttons,
    TUIButtonType.btOk,
    "My Extension"));

if (result == TUIButtonType.btOk)
{
    // user confirmed
}
```

> See: [`UI/ExtensionUtilityMessageBoxNet/project/main/ExtensionUtilityMessageBox.cs`](../../UI/ExtensionUtilityMessageBoxNet/project/main/ExtensionUtilityMessageBox.cs)

### File dialog example

```csharp
string file = helperCom.Invoke(helper => helper.SelectFileDialog(
    "Select NC program",
    "NC files (*.nc)|*.nc|All files (*.*)|*.*",
    paths.NCProgramsFolder()));

if (!string.IsNullOrEmpty(file))
{
    // use the selected file
}
```

---

## SimplePropIterator

`SimplePropIterator` is a helper class from `CAMAPI.UIDialogs.DotnetHelper` that builds an `IST_CustomPropIterator` — the property bag used by `ICAMAPI_UIDialogWindow`. It lets you define a form by adding typed properties one by one.

### AddXxxProp methods

| Method | Description |
|---|---|
| `AddStringProp(name, caption, value)` | Adds a text-input property |
| `AddIntProp(name, caption, value)` | Adds an integer-input property |
| `AddFloatProp(name, caption, value)` | Adds a floating-point-input property |
| `AddBoolProp(name, caption, value)` | Adds a checkbox property |
| `AddEnumProp(name, caption, value, items)` | Adds a dropdown property; `items` is a `|`-delimited string |
| `AddFileProp(name, caption, value, filter)` | Adds a file-picker property |
| `AddFolderProp(name, caption, value)` | Adds a folder-picker property |
| `AddColorProp(name, caption, value)` | Adds a colour-picker property |
| `AddSeparator(caption)` | Adds a visual section separator |

After building the iterator, assign it to `ICAMAPI_UIDialogWindow.PropIterator` and call `ShowModal`.

---

## CamApiInspectorWindow

`CamApiInspectorWindow` is a high-level pattern that combines `ICAMAPI_UIDialogsHelper.CreateWindow` with `SimplePropIterator` into a single class. It wraps the modal dialog lifecycle:

1. Build the property iterator with `AddXxxProp` calls.
2. Call `ShowModal()` — the ENCY inspector window appears.
3. Read back the values after the user presses OK.

### Typical pattern

```csharp
using var helperCom = UIDialogs.CreateHelper();

// Create the window (wrapped)
using var windowCom = helperCom.InvokeAndWrap(helper => helper.CreateWindow("My Settings"));

// Build a property iterator
var iter = new SimplePropIterator();
iter.AddStringProp("outputPath", "Output folder", @"C:\output");
iter.AddIntProp("count", "Number of copies", 1);
iter.AddBoolProp("overwrite", "Overwrite existing files", false);

// Assign and show
TUIButtonType result = windowCom.Invoke(window =>
{
    window.PropIterator = iter.Iterator;
    return window.ShowModal();
});

if (result == TUIButtonType.btOk)
{
    string path   = iter.GetStringProp("outputPath");
    int    count  = iter.GetIntProp("count");
    bool   overwr = iter.GetBoolProp("overwrite");
    // use values ...
}
```

### Non-modal usage

For a floating panel that does not block the calling thread, use `Show()` instead of `ShowModal()`, and implement `ICAMAPI_UIDialogWindowOnClose` to receive the close notification asynchronously:

```csharp
public class MyCloseHandler : ICAMAPI_UIDialogWindowOnClose
{
    public void OnClose(TUIButtonType button)
    {
        if (button == TUIButtonType.btOk)
        {
            // read values back from the iterator
        }
    }
}

windowCom.Invoke(window =>
{
    window.OnClose = new MyCloseHandler();
    window.Buttons = MessageBoxHelper.BuildButtons(TUIButtonType.btOk, TUIButtonType.btCancel);
    window.Show(); // non-blocking
});
```

---

## Complete example: viewport control utility

> See: [`UI/ExtensionManageViewNet/project/main/ExtensionManageView.cs`](../../UI/ExtensionManageViewNet/project/main/ExtensionManageView.cs)

```csharp
public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
{
    resultStatus = default;
    try
    {
        using var appCom        = ComWrapper.Create(context.CamApplication);
        using var mainFormCom   = appCom.MainForm();
        long      hwnd          = mainFormCom.MainWindowHandle();
        var       viewPortCom   = mainFormCom.MainViewPort();

        // Snap to top view
        using var cubeCom = viewPortCom.GetCube();
        cubeCom.Rotate(TViewCubeRotateMode.vcrmFaceTop);

        // Zoom to fit with animation
        viewPortCom.ZoomAll(fluently: true);

        // Show a custom window parented to the CAM window
        WindowHelper.ShowStaWindow(hwnd,
            () => new ViewControlWindow(viewPortCom),
            () => { /* on window close */ });
    }
    catch (Exception e)
    {
        resultStatus.Code = TResultStatusCode.rsError;
        resultStatus.Description = e.Message;
    }
}
```
