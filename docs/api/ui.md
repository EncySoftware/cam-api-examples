# ENCY CAM API — UI Domain

This document covers the in-process UI interfaces: the main application window, the 3D viewport, the view cube, and the dialog helpers.

---

## Table of Contents

1. [ICamApiApplicationMainForm — main window](#icamapiapplicationmainform)
2. [ICamApiVisibilityManager — scene object visibility](#icamapivisibilitymanager)
3. [ICamApiTheme — active UI theme and palette](#icamapitheme)
4. [ICamApiProgressIndicator — status-bar progress](#icamapiprogressindicator)
5. [ICamApiVerifyCompareManager — deviation compare mode](#icamapiverifycomparemanager)
6. [ICamApiScriptEditor — Script IDE](#icamapiscripteditor)
7. [ICamApiViewPort — 3D viewport](#icamapiviewport)
8. [ICamApiViewCube — view orientation](#icamapiviewcube)
9. [Custom 3D rendering — visual objects, meshes, gizmos](#custom-3d-rendering)
10. [ICAMAPI_UIDialogsHelper — dialogs and message boxes](#icamapi_uidialogshelper)
11. [SimplePropIterator — inspector dialog pattern](#simplepropiterator)
12. [CamApiInspectorWindow — the easy dialog pattern](#camapiinspectorwindow)

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
| `VerifyCompareManager` | `ICamApiVerifyCompareManager*` | R | Controls the "Verify compare" mode (machining result vs part, colored by deviation). Helper: `mainFormCom.GetVerifyCompareManager()` — see [below](#icamapiverifycomparemanager) |

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
| `RunUtilitiesSetup(anchorRect)` | Opens the utilities configuration popup. `anchorRect` is the **screen** rectangle of the button that triggered it, so the popup opens directly underneath; pass an empty rect (all zeroes) to center instead. Helper: `mainFormCom.RunUtilitiesSetup()` — the parameter defaults to the empty rect |
| `CreateProcessIndicator(caption) → ICamApiProgressIndicator*` | Creates a controller for the status-bar progress indicator — see [below](#icamapiprogressindicator) |
| `OpenScriptEditor() → ICamApiScriptEditor*` | Opens the Script IDE and returns its handle, so a `.spr` file can be loaded into it. Dispatched on the main UI thread — see [below](#icamapiscripteditor) |
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
| `GetColor(colorKind)` | `int` | Palette color as a `TColor` integer in **BGR** layout (low byte = red) |

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

Reading the palette is only half the job — see
[Theming plugin windows](../general/theming-plugin-windows.md) for applying it to your own window
(WPF resource brushes, WinForms visual-styles pitfalls, scrollbars, title bar).

---

## ICamApiProgressIndicator

The native progress indicator in the status area of the main window — use it to report
progress of long plugin work in ENCY's own status bar instead of showing your own dialog.

**Access:** `mainFormCom.CreateProcessIndicator(caption)` returns a managed
`ProgressIndicator` (class, not a `ComWrapper`) that owns the COM object.

The status bar is a **single shared resource**: `Show()` fails if another process already
occupies it, and `SetPercent`/`SetCaption` fail unless this indicator currently holds it.
The managed wrapper turns those failures into exceptions, and its `Dispose()` hides the
indicator and releases the COM object — so always keep it in a `using`.

| Member | Description |
|---|---|
| `Show()` | Occupies the status bar and shows the indicator. Throws if it is already in use |
| `Hide()` | Hides the indicator and releases the status bar |
| `SetPercent(percent)` | Sets completion (0..100). Throws if not currently occupying the status bar |
| `SetCaption(caption)` | Sets the text shown next to the indicator |
| `Percent` | Current completion percent |
| `Caption` | Current caption |
| `RegisterHandler(ident, onEvent, async = true)` | Subscribes to indicator events (see below) |
| `UnregisterHandler(ident)` | Removes a previously registered handler |
| `Dispose()` | Hides the indicator and releases the COM object |

### TProgressIndicatorEventType

| Value | Fires when |
|---|---|
| `pietShow` (0) | The indicator was shown and took the status bar |
| `pietHide` (1) | The indicator was hidden and released the status bar |
| `pietBreak` (2) | The user clicked the indicator's **Break** button |
| `pietProgress` (3) | Progress changed via `SetPercent` or `SetCaption` |

`pietBreak` is the one that matters for cancellable work — it is how the user asks your
loop to stop.

```csharp
using var mainFormCom = appCom.MainForm();
using var progress = mainFormCom.CreateProcessIndicator("Processing operations");

bool cancelled = false;
progress.RegisterHandler("myPlugin.progress", eventType =>
{
    if (eventType == TProgressIndicatorEventType.pietBreak)
        cancelled = true;
});

progress.Show();
for (int i = 0; i < total && !cancelled; i++)
{
    progress.SetCaption($"Operation {i + 1} of {total}");
    progress.SetPercent(i * 100 / total);
    // ... work ...
}
// Dispose (end of using) hides the indicator
```

> **Callback thread:** by default the handler fires **asynchronously on a background
> thread** — marshal to your UI thread before touching UI, and remember the MTA rule from
> [com-lifetime.md](../general/com-lifetime.md) before touching COM. Pass `async: false`
> to be called synchronously on the thread raising the event; only do that if the handler
> does not call back into COM.

---

## ICamApiVerifyCompareManager

Controls the **"Verify compare"** mode: the machining result is compared against the part
and colored by how far it deviates. A plugin can switch the mode on, set the tolerance
band and the deviation scale, and enable click-to-measure.

**Access:** `mainFormCom.GetVerifyCompareManager()` (helper `VerifyCompareManagerHelper`).

| Helper method | Description |
|---|---|
| `GetEnabled()` / `SetEnabled(value)` | Whether the compare mode is active |
| `GetTolerance()` / `SetTolerance(value)` | Half-width of the green "in tolerance" band, in mm |
| `GetScale()` / `SetScale(scale)` | The deviation scale — see the struct below |
| `GetMeasureEnabled()` / `SetMeasureEnabled(value)` | Whether clicking a point reports its local deviation |

### TCamApiVerifyCompareCompareScale

All values are in internal model units (mm). Positive levels mean **remaining material**
(overcut); negative levels mean **gouge**.

| Field | Description |
|---|---|
| `Stock` | Nominal stock reference the bands are measured from |
| `PosInner` | Inner positive band (remaining material) |
| `PosOuter` | Outer positive band (remaining material) |
| `NegInner` | Inner negative band (gouge) |
| `NegOuter` | Outer negative band (gouge) |

```csharp
using var mainFormCom = appCom.MainForm();
using var compareCom = mainFormCom.GetVerifyCompareManager();

compareCom.SetEnabled(true);
compareCom.SetTolerance(0.05);          // ±0.05 mm reads as "in tolerance"

var scale = compareCom.GetScale();
scale.PosInner = 0.1;
scale.PosOuter = 0.5;
scale.NegInner = 0.1;
scale.NegOuter = 0.5;
compareCom.SetScale(scale);

compareCom.SetMeasureEnabled(true);     // click a point to measure its deviation
```

> **`GetScale` is lossy in both directions.** It returns only the two edge levels per side
> — inner (nearest stock) and outer (farthest); any intermediate levels the engine holds
> are dropped. `SetScale` writes two levels per side, and the engine takes `abs()` and
> sorts them, so the sign and the inner/outer order of what you pass are ignored.
> Read-modify-write of a scale that had intermediate levels will therefore flatten it.

---

## ICamApiScriptEditor

Handle to ENCY's script editor (Script IDE), used to open a script for the user to edit.

**Access:** `mainFormCom.Invoke(f => f.OpenScriptEditor())` — no helper wrapper, so wrap
the result yourself. The call is dispatched on the main UI thread.

| Method | Description |
|---|---|
| `Load(scriptPath, out TResultStatus)` | Loads a script project/file (`.spr`) into the editor and shows it for editing |

```csharp
using var editorCom = mainFormCom.InvokeAndWrap(f => f.OpenScriptEditor());
editorCom.Invoke(e =>
{
    e.Load(@"C:\scripts\my-macro.spr", out var status);
    if (status.Code == TResultStatusCode.rsError)
        throw new Exception(status.Description);
});
```

> The interface currently exposes only `Load`; more methods (goto line, close, …) may be
> appended at the end later.

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
| `ViewManager` | `ICamApiViewManager*` | R | The manager owning this viewport's scene. `mainFormCom.MainViewPort().GetViewManager()` is **the only way to reach ENCY's main scene** and draw into it — see [Custom 3D rendering](#custom-3d-rendering). Helper: `viewPortCom.GetViewManager()` |

### Methods

| Method | Description |
|---|---|
| `ZoomAll(fluently: boolean)` | Fits all geometry into view. Pass `true` for animated transition |
| `SetMatrixFluently(matrix)` | Sets the view matrix with animation |
| `SetViewBoxFluently(box)` | Sets the view box with animation |
| `SaveImage(filePath, width, height) → boolean` | Renders the current view offscreen to a PNG of the given pixel size. Does **not** require the window to be visible — intended for tests and thumbnails. Returns `false` if the render could not be produced (e.g. the viewport is not an OpenGL surface). No helper — call via `Invoke` |

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

// Render the current view to a PNG (works even when the window is hidden):
bool saved = viewPortCom.Invoke(vp => vp.SaveImage(@"C:\temp\view.png", 1920, 1080));
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

## Custom 3D rendering

Lets a plugin draw its own geometry — meshes, contours, draggable handles, transform
gizmos — either **into ENCY's own 3D scene** or into a viewport hosted in the plugin's own
window. Objects added this way are transient: they are never persisted into the project and
never appear in the geometry tree, so rebuilding a visualisation is just remove + add.

### The object model

| Concept | Interface | Role |
|---|---|---|
| Factory | `ICamApiVisualisationFactory` | Creates everything below. Resolved as a singleton |
| Scene | `ICamApiViewManager` | Owns a set of visual objects **and** the viewports that render them |
| Viewport | `ICamApiViewPort` | One rendering surface. Several viewports of one manager all draw the same scene |
| Object | `ICamApiVisualObject` | A scene node: placement + visibility |
| Mesh source | `ICamApiMeshBuilder` | Builds a triangle mesh caller-side, handed over in one `AddMesh` call |

A visual object is deliberately thin — it carries only `Transform` and `Visible`. Everything
else is a separate **aspect** fetched by `QueryInterface`, which doubles as feature
detection: a gizmo, for example, implements none of the content aspects, so those casts
return a null wrapper.

| Aspect | Helper | What it adds |
|---|---|---|
| `ICamApiRenderableVO` | `AsRenderable()` | Colour, meshes, polylines, `Clear` |
| `ICamApiInteractiveVO` | `AsInteractive()` | Selectability, draggable hotspots, interaction handler |
| `ICamApiMaterialVO` | `AsMaterial()` | `Lit` + built-in shading preset |
| `ICamApiShadedVO` | `AsShaded()` | `SpecularWeight` of lit faces |
| `ICamApiInstancedVO` | `AsInstanced()` | Draw the same mesh at many placements |
| `ICamApiCustomPassVO` | `AsCustomPass()` | `Overlay` (no depth test) and `Transparent` |
| `ICamApiTexturedVO` | `AsTextured()` | Image mapped onto faces via UV |
| `ICamApiGizmo` | `AsGizmo()` | Transform manipulator (gizmo objects only) |

> Aspect wrappers **share the source object's RCW** — do not use one past the lifetime of
> the `ComWrapper<ICamApiVisualObject>` it came from.

### Getting the factory

The factory is a singleton extension, not reachable from the application object:

```csharp
using var factoryCom = SystemExtensionFactory.GetSingletonExtension<ICamApiVisualisationFactory>(
    "Extension.Global.Singletons.ViewPort");
if (factoryCom.IsNull)
    throw new Exception("The Extension.Global.Singletons.ViewPort singleton is not available");
```

To draw into **ENCY's main scene**, take the manager from the main viewport — this is the
only route to it:

```csharp
using var mainFormCom = appCom.MainForm();
using var viewPortCom = mainFormCom.MainViewPort();
using var managerCom  = viewPortCom.GetViewManager();
```

To render in **your own window** instead, create a private scene and a viewport hosted in
your `HWND`. The kernel owns the viewport (GL context, painting, navigation); releasing the
returned interface destroys it:

```csharp
using var managerCom  = factoryCom.CreateViewManager();
using var viewPortCom = factoryCom.CreateViewPort(managerCom, myHwnd);
managerCom.Invoke(m => m.BackgroundColor = 0x00202020);   // TColor: 0x00BBGGRR
```

### ICamApiVisualisationFactory

| Helper method | Description |
|---|---|
| `CreateViewManager()` | New empty scene (with its own viewports) |
| `CreateViewPort(manager, parentWnd)` | OpenGL viewport rendering that scene, hosted in `parentWnd` |
| `CreateVisualObject(manager, parent = null)` | New renderable object in the scene (alias: `CreatePreview`) |
| `CreateGizmo(manager, parent = null)` | New move/rotate manipulator, returned as `ICamApiGizmo` |
| `CreateMeshBuilder()` | New empty mesh builder |
| `RemoveVisualObject(manager, obj)` | Removes and releases one object |
| `EnumerateVisualObjects(manager)` | Depth-first walk over the scene's objects |

> **`parent` is grouping for enumeration only.** It does **not** nest transforms,
> visibility or lifetime: a child keeps its own world-space `Transform`, is shown and hidden
> on its own, and `RemoveVisualObject` does **not** cascade to it — remove children
> explicitly. Every object is added to the scene at top level regardless.

> `EnumerateVisualObjects` follows the usual iterator contract: each yielded wrapper is
> disposed as enumeration advances, so never wrap the loop variable in a `using` — copy out
> what you need inside the loop body. See
> [com-lifetime.md](../general/com-lifetime.md).

### ICamApiMeshBuilder

Builds a mesh caller-side, then hands the whole result over in a single `AddMesh` — vertices
are added once and referenced by index from triangles.

| Helper method | Description |
|---|---|
| `AddVertex(p, normal) → int` | Adds a vertex with its shading normal, returns its index |
| `AddTriangle(i1, i2, i3)` | Adds a triangle referencing vertex indices |
| `AddVertexColored(p, normal, color) → int` | Vertex with a per-vertex `$AARRGGBB` colour, interpolated across the triangle |
| `AddVertexUV(p, normal, u, v) → int` | Vertex with a texture coordinate (0..1, origin at the image's lower-left) |
| `AddVertices(coords, normals = default)` | **Bulk** vertices from flat spans — `X,Y,Z` per vertex; empty normals means a default up-normal |
| `AddTriangles(indices)` | **Bulk** triangles from a flat index span, 3 per triangle |
| `AddVerticesColored(coords, normals, colors)` | Bulk colored variant — one packed colour per vertex |
| `AddVerticesUV(coords, normals, uvs)` | Bulk UV variant — one `U,V` pair per vertex |
| `BeginFace()` / `EndFace()` | Wraps a **planar** face; the kernel triangulates it |
| `BeginFaceLoop()` / `AddLoopPoint(p)` / `EndFaceLoop()` | One boundary loop of that face |
| `AddFaceLoop(coords)` | Bulk alternative for a whole loop |
| `GetVertexCount()` / `GetTriangleCount()` | Counts added so far |
| `Clear()` | Drops the geometry so the builder can be reused |

Prefer the **bulk** overloads for anything large: they cost one interop call instead of one
per vertex. They take `ReadOnlySpan<double>` / `ReadOnlySpan<int>`, pinned for the duration
of the call — the kernel copies the data, so the spans need not outlive it.

For a **planar** face with holes, add the outer loop first, then each hole, all between
`BeginFace`/`EndFace`. Non-planar surfaces must be supplied as triangles.

> Per-vertex colours and UVs are **all-or-nothing per builder**: once any vertex carries a
> colour, vertices added without one default to white; the same holds for UVs, defaulting to
> `(0, 0)`.

### Drawing a mesh

```csharp
using var objCom = factoryCom.CreateVisualObject(managerCom);

using (var builderCom = factoryCom.CreateMeshBuilder())
{
    builderCom.AddVertices(coords, normals);   // X,Y,Z per vertex
    builderCom.AddTriangles(indices);          // 3 indices per triangle

    using var renderableCom = objCom.AsRenderable();
    renderableCom.SetColor(255, 160, 0, 255);  // R,G,B,A — each 0..255
    renderableCom.AddMesh(builderCom);
}

// Flat, self-illuminated look — good for overlays and annotations
using (var materialCom = objCom.AsMaterial())
    materialCom.SetLit(false);

// Draw on top of the scene, blended by the colour alpha
using (var passCom = objCom.AsCustomPass())
{
    passCom.SetOverlay(true);
    passCom.SetTransparent(true);
}
```

Contours go through the same renderable aspect — `AddPolyline(coords, closed)` in bulk, or
`BeginPolyline`/`AddPolylinePoint`/`EndPolyline` point by point. Polylines are always drawn
unlit, so `Lit` and the material preset do not affect them.

`SetColor` channels are `0..255` and are masked to the low 8 bits, so `256` silently reads
as `0` — clamp before passing. `Clear()` on the renderable aspect drops all primitives, which
is how you rebuild an object's content in place.

### TCamApiMaterialPreset

Shading preset for **lit** faces (`AsMaterial().SetPreset(...)`); ignored when `Lit` is false.

| Value | Look |
|---|---|
| `ampStandard` | Default surface shading |
| `ampShiny` | Glossy — pronounced specular highlights |
| `ampMatte` | Flat, diffuse only, no highlights |
| `ampWood` | Wood-like shading |

### Picking and draggable handles

Set `Selectable` to make an object pickable by clicking its geometry, and register an
`ICamApiViewInteractionHandler` to receive events. Callbacks run on the viewport's GUI
thread, and may call back into the object (`Clear`/`AddMesh`) — those are serialized
internally.

| Helper method | Description |
|---|---|
| `SetSelectable(value)` | Object can be picked; hotspots then show only while it is selected |
| `AddHotSpot(p) → int` | Draggable handle at a world position, drawn as the default marker |
| `ClearHotSpots()` | Removes all handles |
| `SetInteractionHandler(handler)` | Attaches the callback object (`null` detaches) |

| Handler callback | Fires when |
|---|---|
| `OnHotSpotDragStart(spotId)` | A drag of that handle began |
| `OnHotSpotDrag(spotId, p, rayOrigin, rayDir)` | Continuously while dragging. `p` is the cursor projected onto the ground `Z=0` plane; `rayOrigin`/`rayDir` give the full pick ray so you can project onto your own constraint instead |
| `OnHotSpotDragEnd(spotId)` | The drag finished |
| `OnSelectedChanged(selected)` | The object was selected or deselected |

```csharp
private sealed class PickHandler(Action<string> onPicked) : ICamApiViewInteractionHandler
{
    public void OnHotSpotDragStart(int spotId) { }
    public void OnHotSpotDrag(int spotId, TST3DPoint p, TST3DPoint rayOrigin, TST3DPoint rayDir) { }
    public void OnHotSpotDragEnd(int spotId) { }

    public void OnSelectedChanged(bool selected)
    {
        if (selected)
            onPicked("object picked");
    }
}

var handler = new PickHandler(msg => log.Info(msg));   // keep it alive while attached
using (var interactiveCom = objCom.AsInteractive())
{
    interactiveCom.SetSelectable(true);
    interactiveCom.SetInteractionHandler(handler);
}
```

> **Keep the handler referenced** for as long as it is attached — it is a managed object
> held by native code, and nothing else roots it.

> **Handle ids are 1-based, assigned in call order, and reused after `ClearHotSpots()`** —
> ids cached across a rebuild must be dropped when the hotspots are cleared, or they will
> refer to different handles.

For richer handles, the raw interface also offers `AddHotSpotShaped` (a caller-supplied mesh
instead of the default marker, optionally screen-scaled, `Rigid` so it stays put while
driving an edit, or `Decorative` so it draws but cannot be grabbed) and
`UpdateHotSpotShaped` (re-place a handle without disturbing an active drag). Both are
`Invoke`-only — no helper wrapper yet.

### ICamApiGizmo

The same move/rotate manipulator ENCY uses to set the work coordinate system.
`CreateGizmo` returns `ICamApiGizmo` directly; the same object also exposes
`ICamApiVisualObject` via `QueryInterface`, so it enumerates and is removed like any other
visual object.

| Helper method | Description |
|---|---|
| `GetMatrix()` / `SetMatrix(m)` | Placement — reading it returns the current (possibly dragged) transform |
| `SetVisible(v)` | Visibility |
| `SetMoveX/Y/Z(v)`, `SetRotateX/Y/Z(v)` | Show and enable individual handles |
| `SetEnabledHandles(mx, my, mz, rx, ry, rz)` | All six flags in one call |
| `SetHandler(handler)` | Receives `OnGizmoChanged(matrix)` as the user drags |

```csharp
using var gizmoCom = factoryCom.CreateGizmo(managerCom);
gizmoCom.SetEnabledHandles(true, true, true, false, false, true);  // XYZ move + Z rotate
gizmoCom.SetMatrix(placement);
gizmoCom.SetHandler(new MyGizmoHandler());   // ICamApiGizmoHandler
```

### Cleaning up

Objects live in the scene until explicitly removed, so a plugin that draws into ENCY's main
scene **must** remove what it added — on rebuild and on unload:

```csharp
foreach (var objCom in _myObjects)
{
    factoryCom.RemoveVisualObject(managerCom, objCom);
    objCom.Dispose();
}
_myObjects.Clear();
```

---

## ICAMAPI_UIDialogsHelper

Provides modal dialogs and file-selection dialogs. Obtained via the `UIDialogs.CreateHelper()` factory method from `CAMAPI.UIDialogs.DotnetHelper`.

### Methods

| Method | Returns | Description |
|---|---|---|
| `CreateWindow(winCaption) → ICAMAPI_UIDialogWindow*` | `ICAMAPI_UIDialogWindow*` | Creates a custom property-inspector window |
| `CreateEmbeddedWindow(winCaption, parentWnd) → ICAMAPI_UIDialogWindow*` | `ICAMAPI_UIDialogWindow*` | Same, but the inspector is embedded as a **child** of `parentWnd` (HWND) instead of a top-level window — so the native property inspector can sit inside your own form. Fill `PropIterator` and call `Show()`; it stays embedded until released |
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

### Embedding the inspector in your own form

`CreateEmbeddedWindow` gives the same inspector as a child window, so you can host ENCY's
native property editor inside your own WPF/WinForms form instead of a separate dialog:

```csharp
using var helperCom = UIDialogs.CreateHelper();
using var windowCom = helperCom.InvokeAndWrap(
    helper => helper.CreateEmbeddedWindow("Parameters", myPanelHwnd));

var iter = new SimplePropIterator();
iter.AddFloatProp("feed", "Feed rate", 250.0);

windowCom.Invoke(window =>
{
    window.PropIterator = iter.Iterator;
    window.Show();          // embedded, non-blocking
});
// keep windowCom alive while the panel is on screen — releasing it destroys the child window
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
