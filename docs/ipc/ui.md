# ENCY CAM IPC API — UI Domain

This document covers the out-of-process UI interfaces for controlling the ENCY main window, 3D viewport, view cube, and the Prime multi-instance shell.

All IPC UI methods that communicate with a remote ENCY process take a `TExecuteContext` parameter. Read-only properties that are cached locally may not require one.

---

## Table of Contents

1. [ICamIpcApplicationMainForm — remote main window](#icamipcapplicationmainform)
2. [ICamIpcViewPort — remote 3D viewport](#icamipcviewport)
3. [ICamIpcViewCube — remote view cube](#icamipcviewcube)
4. [ICamIpcPrimeView — Prime shell UI actions](#icamipcprimeview)
5. [ICamIpcPrimeViewModel — Prime shell lifecycle and instance management](#icamipcprimeviewmodel)
6. [ICamIpcListPrimeViewModel — Prime instance list](#icamipclistprimeviewmodel)

---

## ICamIpcApplicationMainForm

The IPC proxy for the ENCY main application window. Obtained via `ICamIpcApplication.MainForm` or `ICamIpcApplication.GetMainFormTimeOut(timeout)`.

### Event registration

```
RegisterHandler(handlerIdent, handler, listener, ctx)
UnregisterHandler(handlerIdent, ctx)
```

Handler interfaces for the main form (IPC versions mirror the in-process set):

| Interface | Method | Fires when |
|---|---|---|
| `ICamIpcHandlerApplicationMainFormIsVisibleChanged` | `ApplicationMainFormIsVisibleChanged(handlerIdent, visible)` | Main window shown or hidden |
| `ICamIpcHandlerApplicationMainFormMinimizeChanged` | `ApplicationMainFormMinimizeChanged(handlerIdent, minimized)` | Window minimized or restored |
| `ICamIpcHandlerApplicationMainFormUiInfoChanged` | `ApplicationMainFormUiInfoChanged(handlerIdent, uiInfo)` | Any UI state change |
| `ICamIpcHandlerApplicationMainFormSaveInCloudClicked` | `ApplicationMainFormSaveInCloudClicked(handlerIdent)` | Cloud save button clicked |
| `ICamIpcHandlerApplicationMainFormShareClicked` | `ApplicationMainFormShareClicked(handlerIdent)` | Share button clicked |
| `ICamIpcHandlerApplicationMainFormCloudChatClicked` | `ApplicationMainFormCloudChatClicked(handlerIdent)` | Cloud chat icon clicked |
| `ICamIpcHandlerApplicationMainFormReloadCloudProjectClicked` | `ApplicationMainFormReloadCloudProjectClicked(handlerIdent)` | Reload cloud project clicked |

### State methods

| Method | Description |
|---|---|
| `GetVisible(ctx) → boolean` | Whether the main window is visible |
| `GetUiInfo(ctx) → ICamIpcMainFormUiInfo*` | Current UI state snapshot |

### Project management

| Method | Description |
|---|---|
| `OpenProject(fileName, ctx)` | Opens a project file |
| `OpenProjectSnap(fileName, snapName, ctx)` | Opens a project snapshot |
| `SaveCurrentProject(ctx)` | Saves in-place |
| `SaveCurrentProjectAs(ctx)` | Shows the Save As dialog |
| `SaveProjectAs(fileName)` | Saves to a specified path without a dialog |
| `SaveProject()` | Saves without a dialog |
| `ExportCurrentProject(ctx)` | Exports with all snapshots |
| `ExportProjectWithHistory(fileName)` | Saves with full snapshot history |
| `ExportDrillPoints(fileName)` | Exports selected drill operation points |
| `CreateNewProject(ctx)` | Creates a new empty project |
| `SaveAsMachineSetup(fileName)` | Saves as a machine setup file |
| `ImportMachineSetup(fileName)` | Imports a machine setup |
| `ExportSimulationResults(fileName)` | Exports simulation results |

### Application actions

| Method | Description |
|---|---|
| `BeginFreeze(freezeType)` | Prevents user interaction (`TFreezeInterfaceType`) |
| `EndFreeze()` | Restores user interaction |
| `RunAppSetup(out settingsChanged)` | Opens the application settings dialog |
| `RunUtilitiesSetup(anchorRect)` | Opens the utilities configuration popup. `anchorRect` (`TCamApiRect`) is the **screen** rectangle of the button that triggered it, so the popup opens directly underneath; pass an empty rect (all zeroes) to center instead |
| `OpenAiAssistant()` | Opens the AI assistant |
| `ShowHelpContents()` | Shows the help documentation |
| `SupportRequest()` | Opens the support request dialog |
| `ShowTutorialWnd()` | Shows the tutorial window |
| `ShowSnapshotManager(projectPath)` | Shows the snapshot manager |
| `CrashReport()` | Opens the crash report dialog |
| `OpenDotnetInterpreterInVSCode(fileName)` | Opens a .NET script in VS Code |
| `SetCurrentInPrime(value)` | Notifies the form that it is the active Prime instance |

### PLM and cloud

| Method | Description |
|---|---|
| `LoadProjectFromPLM(connectionId)` | Imports from PLM |
| `ExportProjectToPLM(connectionId)` | Exports to PLM |
| `ConnectPLMConnection(connectionId)` | Connects a PLM connection |
| `DisconnectPLMConnection(connectionId)` | Disconnects a PLM connection |
| `CloudsCollabShare()` | Shares in cloud collaboration |
| `CloudsCollabDownload()` | Downloads the latest cloud version |
| `CloudsCollabOpenChat()` | Opens cloud collaboration chat |

### Viewport access

```
GetMainViewPort(ctx) → ICamIpcViewPort*
```

### Main window handle

Returns the OS window handle (`HWND`, 64-bit value) of the ENCY main window. Use it as the parent window for modal dialogs opened by out-of-process tooling.

```
GetMainWindowHandle(ctx) → int64
```

### Plugin hotkeys

```
GetHotkeyManager(ctx) → ICamIpcHotkeyManager*
```

IPC mirror of the CAMAPI hotkey manager ([`../api/application.md`](../api/application.md#plugin-hotkeys)). Over IPC the manager keeps the binding table only — a hotkey is added by shortcut **and caption** in one call; the command-fire callback is **not** part of this manager (fire notifications go through the application event-listener mechanism, not a per-hotkey `OnExecute`).

### Status-bar progress — ICamIpcProgressIndicator

```
CreateProcessIndicator(caption, ctx) → ICamIpcProgressIndicator*
```

Drives ENCY's own status-bar progress indicator, so an out-of-process tool can report
progress in the host UI instead of its own window. IPC mirror of
[`ICamApiProgressIndicator`](../api/ui.md#icamapiprogressindicator).

| Method | Description |
|---|---|
| `Show()` | Occupies the status bar and shows the indicator. **Fails if another process already holds it** |
| `Hide()` | Hides the indicator and releases the status bar |
| `SetPercent(value)` | Completion 0..100. Fails unless this indicator currently holds the status bar |
| `SetCaption(value)` | Text shown next to the indicator. Same precondition |
| `GetPercent()` / `GetCaption()` | Current values |
| `RegisterHandler(ident, handler, listener, ctx)` | Subscribe an `ICamIpcHandlerProgressIndicator` to Show/Hide/Break/Progress |
| `UnregisterHandler(ident)` | Remove the subscription |

The handler receives `ProgressIndicatorEvent(handlerIdent, eventType)` where `eventType` is
`TProgressIndicatorEventType`: `pietShow` (0), `pietHide` (1), `pietBreak` (2),
`pietProgress` (3). **`pietBreak` is the user pressing Break** — that is how a cancel request
reaches your loop.

> The status bar is a single shared resource. Always `Hide()` when done (or on failure),
> otherwise it stays occupied for every other client.

### Verify compare — ICamIpcVerifyCompareManager

```
GetVerifyCompareManager(ctx) → ICamIpcVerifyCompareManager*
```

Controls the "Verify compare" mode — machining result against the part, coloured by
deviation. IPC mirror of
[`ICamApiVerifyCompareManager`](../api/ui.md#icamapiverifycomparemanager).

| Method | Description |
|---|---|
| `GetEnabled()` / `SetEnabled(value)` | Whether compare mode is active |
| `GetTolerance()` / `SetTolerance(value)` | Half-width of the green "in tolerance" band, mm |
| `GetScale()` / `SetScale(scale)` | Deviation scale as `TCamApiVerifyCompareCompareScale` (`Stock`, `PosInner`, `PosOuter`, `NegInner`, `NegOuter`) |
| `GetMeasureEnabled()` / `SetMeasureEnabled(value)` | Whether clicking a point reports its local deviation |

> `GetScale` returns only the edge levels per side and `SetScale` takes `abs()` and sorts what
> you pass — see the [CAMAPI note](../api/ui.md#icamapiverifycomparemanager) before doing a
> read-modify-write.

### Script editor — ICamIpcScriptEditor

```
OpenScriptEditor(ctx) → ICamIpcScriptEditor*
```

Opens ENCY's script editor (Script IDE) and returns its handle. The only method is
`Load(scriptPath, ctx)`, which loads a `.spr` script project/file and shows it for editing.

- `ICamIpcHotkeyManager` — `AddShortcut(shortcut, caption, ctx)` (fails if taken), `RemoveShortcut(shortcut, ctx)` (fails for reserved), `FindByShortcut(shortcut, ctx)`, `GetCount(ctx)`, `GetHotkey(index, ctx)`.
- `ICamIpcHotkey` — `GetShortcut(ctx)`, `GetCaption(ctx)`/`SetCaption(caption, ctx)`, `GetEnabled(ctx)`/`SetEnabled(enabled, ctx)`, `GetIsReserved(ctx)`. The `Shortcut` is fixed at creation.

Native application shortcuts are pre-registered as reserved entries, so `FindByShortcut` reports conflicts against them.

### ICamIpcMainFormUiInfo — UI state snapshot

Returned by `GetUiInfo(ctx)`. Equivalent to the in-process `ICamApiMainFormUiInfo` with one addition:

| Method | Returns | Description |
|---|---|---|
| `GetAppProcessId()` | `integer` | Process ID of the ENCY instance this info belongs to |
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
| `GetUiPLMInfo()` | `ICamIpcMainFormUiPLMInfo*` | PLM connection states |

### Usage example

```csharp
// Get the main form proxy (wait up to 5 seconds):
var mainForm = app.GetMainFormTimeOut(5_000);

// Open a project:
mainForm.OpenProject(@"C:\projects\part.encam", ref ctx);

// Get UI state:
var uiInfo = mainForm.GetUiInfo(ref ctx);
bool modified = uiInfo.GetProjectIsModified();
string caption = uiInfo.GetCaption();

// Freeze during a batch operation:
mainForm.BeginFreeze(TFreezeInterfaceType.afiiGeneral);
try { /* ... */ }
finally { mainForm.EndFreeze(); }

// Access viewport:
var viewPort = mainForm.GetMainViewPort(ref ctx);
viewPort.ZoomAll(fluently: true, ref ctx);
```

---

## ICamIpcViewPort

IPC proxy for the 3D viewport.

### Methods

| Method | Description |
|---|---|
| `GetScaleFactor(ctx) → double` | Current zoom factor |
| `GetViewMode(ctx) → TViewPortViewMode` | Current rendering mode |
| `ZoomAll(fluently, ctx)` | Fits all geometry into view |
| `GetMatrix(ctx) → TST3DMatrix` | Reads the view transformation matrix |
| `SetMatrix(value, ctx)` | Applies a view transformation matrix immediately |
| `SetMatrixFluently(matrix, ctx)` | Applies a matrix with animation |
| `GetViewBox(ctx) → TST2DBox` | Reads the view bounding box |
| `SetViewBox(value, ctx)` | Applies a view bounding box immediately |
| `SetViewBoxFluently(box, ctx)` | Applies a view bounding box with animation |
| `GetCube(ctx) → ICamIpcViewCube*` | Returns the view cube proxy |

**TViewPortViewMode values** — see [api/ui.md](../api/ui.md#tviewportviewmode-enum).

### Usage example

```csharp
var vp = mainForm.GetMainViewPort(ref ctx);

// Zoom fit:
vp.ZoomAll(fluently: true, ref ctx);

// Read the current matrix:
TST3DMatrix m = vp.GetMatrix(ref ctx);

// Apply a new matrix:
vp.SetMatrix(m, ref ctx);

// Snap the view cube to top:
var cube = vp.GetCube(ref ctx);
cube.Rotate(TViewCubeRotateMode.vcrmFaceTop, ref ctx);
```

---

## ICamIpcViewCube

IPC proxy for the orientation cube widget.

```
Rotate(mode: TViewCubeRotateMode, ctx)
```

`TViewCubeRotateMode` has the same 26 values as in the in-process API — all **Faces** (6), **Edges** (12), and **Corners** (8). See [api/ui.md](../api/ui.md#tviewcuberotatemode-enum--all-values) for the complete list.

```csharp
var cube = vp.GetCube(ref ctx);

cube.Rotate(TViewCubeRotateMode.vcrmFaceFront, ref ctx);
cube.Rotate(TViewCubeRotateMode.vcrmEdgeTopFront, ref ctx);
cube.Rotate(TViewCubeRotateMode.vcrmCornerTopFrontRight, ref ctx);
```

---

## ICamIpcPrimeView

The UI-side interface of the Prime shell. Represents the view layer (the actual WPF/Win32 window of the Prime process). Exposes a single action:

```
PressCreateNewApplication()
```

This programmatically triggers the "create new ENCY instance" button in the Prime window — the same as the user clicking the **+** button.

```csharp
// Obtain ICamIpcPrimeView from the Prime process proxy:
primeView.PressCreateNewApplication();
```

---

## ICamIpcPrimeViewModel

The view-model of a Prime shell instance. Manages the lifecycle of all ENCY application instances embedded in that Prime window.

### Properties

| Property | Type | Description |
|---|---|---|
| `ProcessId` | `integer` | OS process ID of the Prime process |

### Instance management

| Method | Description |
|---|---|
| `CreateApplication(args, ctx) → integer` | Launches a new ENCY instance inside this Prime window; returns the new process ID |
| `GetApplicationsList(ctx) → IListInteger*` | Lists the process IDs of all ENCY instances managed by this Prime |
| `CloseApplication(processId, ctx)` | Gracefully closes a managed ENCY instance |
| `KillApplication(processId, ctx)` | Forcibly terminates a managed ENCY instance |
| `ApplicationBelongs(processId, ctx) → boolean` | Returns true if the given process is managed by this Prime |
| `Close(ctx)` | Closes the entire Prime shell |

### Project and start-page integration

| Method | Description |
|---|---|
| `ParkStartPageMainWindow(startPageAppId, mainWindowId, ctx)` | Embeds a start-page window into the Prime shell |
| `StartPageOpenProject(projectFile, ctx)` | Instructs the start page to open a project |
| `GetRecentProjects() → string` | Returns a JSON array of recently opened project paths |

### Modal and blocking state

| Method | Description |
|---|---|
| `EnterModalState(appId)` | Called when an embedded ENCY instance enters a modal dialog |
| `LeaveModalState(appId)` | Called when the modal dialog closes |
| `EnterBlockPrjChange(appId)` | Called when a project change must be blocked |
| `LeaveBlockPrjChange(appId)` | Called when the block on project change is lifted |

### Usage example

```csharp
// Create a new ENCY instance in the Prime window:
int newPid = primeViewModel.CreateApplication("--headless", ref ctx);
Console.WriteLine($"New ENCY instance PID: {newPid}");

// List all managed instances:
var pids = primeViewModel.GetApplicationsList(ref ctx);
for (int i = 0; i < pids.Count(); i++)
    Console.WriteLine($"Instance: {pids.Get(i)}");

// Open a project in a specific instance:
var app = ipcList.GetByProcessId(newPid, ref ctx);
app.WaitForStarted(15_000, out var status);
app.OpenProject(@"C:\projects\part.encam", false, ref ctx);

// Close one instance:
primeViewModel.CloseApplication(newPid, ref ctx);

// Close the entire Prime shell:
primeViewModel.Close(ref ctx);
```

---

## ICamIpcListPrimeViewModel

A collection of `ICamIpcPrimeViewModel` instances. Used to enumerate and manage all running Prime shells.

```
Count: integer
Get(index, ctx) → ICamIpcPrimeViewModel*
GetByProcessId(processId, ctx) → ICamIpcPrimeViewModel*
Add(item)
RemoveByProcessId(processId)
RemoveAt(index)
Clear()
```

### Relationship between Prime and application instances

```
ICamIpcListPrimeViewModel          — all Prime shells
  └── ICamIpcPrimeViewModel        — one Prime shell (one process)
        └── ICamIpcListApplication — all ENCY instances in that Prime
              └── ICamIpcApplication — one ENCY instance
                    └── ICamIpcApplicationMainForm
                          └── ICamIpcViewPort
                                └── ICamIpcViewCube
```

In non-Prime deployments (ENCY launched standalone), each `ICamIpcApplication` is obtained directly from the IPC singletons without going through a Prime view model.
