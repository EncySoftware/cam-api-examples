# ENCY CAM API — Application Domain

This document covers the in-process (same-DLL) Application API used by extensions that run inside the ENCY CAM process.

---

## Table of Contents

1. [ICamApiApplication](#icamapiapplication)
2. [Application event handlers](#application-event-handlers)
3. [ICamApiApplicationSingleton — getting the app from a Global extension](#icamapiapplicationsingleton)
4. [Plugin hotkeys — global keyboard shortcuts](#plugin-hotkeys)
5. [ICamApiPaths / ICamApiConstants — system paths and constants](#icamapipaths--icamapiconstants)
6. [ICamApiUtilityManager — utilities](#icamapiutilitymanager)
7. [TResultStatus / TResultStatusCode — error handling](#tresultstatus--tresultstatuscode)
8. [ICamApiEventHandler — event registration pattern](#icamapieventhandler)
9. [IListString, IListInteger, IDictionaryStringString — collections](#iliststring-ilistinteger-idictionarystringstring)
10. [ICAMAPIFilesInStreamStorage — file-in-stream storage](#icamapifilesinstreamstorage)
11. [TLogEventType / LogItem — logging](#tlogeventtype--logitem)
12. [ICamApiMacroManager](#icamapimacromanager)

---

## ICamApiApplication

`ICamApiApplication` is the central object representing a running ENCY CAM instance. An extension receives it through the `IExtensionUtilityContext` parameter of `IExtensionUtility.Run`, or retrieves it from `ICamApiApplicationSingleton`.

### Properties

| Property | Type | Access | Description |
|---|---|---|---|
| `MainForm` | `ICamApiApplicationMainForm*` | R | Main application window |
| `ExecutablePath` | `string` | R | Path to the CAM executable |
| `LogFilePath` | `string` | R | Path to the current log file |
| `LanguageCode` | `integer` | R | Windows LCID of the selected UI language |
| `LanguageName` | `string` | R | Name of the selected UI language |
| `MainWorkMode` | `TMainWorkMode` | RW | Current work mode (Model / Machining / Simulating) |
| `MachiningToolsManager` | `ICamApiMachiningToolsManager*` | R | Tool library manager |
| `MachinesLibrary` | `ICamApiMachinesLibrary*` | R | Machine library manager |
| `UtilityManager` | `ICamApiUtilityManager*` | R | Manager for registered utilities |
| `MacroManager` | `ICamApiMacroManager*` | R | Macro system manager |
| `PLMManager` | `IPLMManager*` | R | PLM integration manager |
| `AttributesManager` | `ICamApiCustomAttributesManager*` | R | Custom attributes manager |
| `UserTechOperationList` | `ICamApiUserTechOperationList*` | R | User-defined tech operations |
| `Started` | `boolean` | R | `true` once the instance is ready to work; `false` while opening a project or shutting down |
| `Theme` | `ICamApiTheme*` | R | Snapshot of the active UI theme/palette — see [ui.md](ui.md#icamapitheme) |
| `HotkeyManager` | `ICamApiHotkeyManager*` | R | Manager for plugin-registered global keyboard shortcuts — see [Plugin hotkeys](#plugin-hotkeys). Available at application level so a global extension can register hotkeys during startup, before the main form exists |
| `Paths` | `ICamApiPaths*` | R | Standard folders used by ENCY — see [below](#icamapipaths--icamapiconstants) |
| `Constants` | `ICamApiConstants*` | R | Application constants — see [below](#icamapipaths--icamapiconstants) |
| `LastActionIdleSeconds` | `double` | R | Seconds since the last raw user input (mouse click / key / wheel) in ENCY's windows; `0` if there has been no input yet (e.g. a headless run). Native modal dialogs — message boxes, file pickers — are **not** tracked, so this keeps growing while one is open |

### Methods

| Method | Description |
|---|---|
| `GetActiveProject(out TResultStatus) → ICamApiProject*` | Returns the active project, or `null` if none is open |
| `GetExtensionManager(out TResultStatus) → IExtensionManager*` | Returns the extension manager |
| `OpenProject(fileName, addToReOpen, out TResultStatus)` | Closes the current project and opens the specified file |
| `SaveCurrentProject(fileName, out TResultStatus)` | Saves the current project; pass empty string to overwrite in-place |
| `ExportCurrentProject(targetFileName, overwrite, out TResultStatus)` | Exports the project with all snapshots |
| `CreateNewProject(out TResultStatus)` | Closes the current project and creates a new empty one |
| `SetActiveProjectMachine(machineInfo, out TResultStatus)` | Sets the machine for the active project |
| `OpenProjectFromPLM(plmItemId, connectionId, out TResultStatus)` | Opens a project from a PLM system |
| `RegisterHandler(handlerIdent, handler, events, out TResultStatus)` | Registers an event handler |
| `UnregisterHandler(handlerIdent, out TResultStatus)` | Removes an event handler |
| `AskCamAgent(message, interruptOnError, timeoutMs, out TResultStatus) → string` | Sends a free-text instruction to the paired CAM Agent and **blocks** until it finishes, returning the agent's answer — see [below](#askcamagent) |

### AskCamAgent

Sends a plain-text instruction to the paired CAM Agent (a headless ENCY started with
`--serve`) and blocks until it is done. Generated macro code calls this for the
`mctAskCamAgent` step, but any plugin may use it.

| Parameter | Meaning |
|---|---|
| `message` | The instruction text |
| `interruptOnError` | `true` → a failed or unreachable agent sets `ret.Code = rsError`; `false` → the failure is swallowed and `ret.Code` stays `rsOk` |
| `timeoutMs` | Overall wait budget; `0` keeps the client default (10 minutes) |

Returns the agent's answer, or an empty string on failure (or when the answer itself is empty).

```csharp
string answer = appCom.Invoke(a =>
{
    var text = a.AskCamAgent("Create a roughing operation for the top face", true, 0, out var status);
    if (status.Code == TResultStatusCode.rsError)
        throw new Exception(status.Description);
    return text;
});
```

> **This blocks the calling thread for as long as the agent works** — up to the 10-minute
> default. Do not call it on the UI thread without freezing the window
> ([`BeginFreeze`](ui.md#icamapiapplicationmainform)) or moving the call off-thread.

> With `interruptOnError: false` an unreachable agent is indistinguishable from an agent that
> answered with an empty string. Pass `true` when you need to tell those apart.

### TMainWorkMode enum

```
mwmModel      = 0   // Working on model geometry
mwmMachining  = 1   // Calculating toolpaths
mwmSimulating = 2   // Running simulation
```

> The `TMainWorkMode` enum now lives in `CAMAPI.ApplicationMainForm` (it is also used by
> `ICamApiVisibilityManager` — see [ui.md](ui.md#icamapivisibilitymanager)); the
> `MainWorkMode` property on the application is unchanged.

### .NET helper usage

The `ApplicationHelper` static class (namespace `CAMAPI.DotnetHelper`) provides extension methods on `ComWrapper<ICamApiApplication>`:

```csharp
// Inside IExtensionUtility.Run:
using var appCom = ComWrapper.Create(context.CamApplication);

// Get active project
using var projectCom = appCom.GetActiveProject();

// Open a project
appCom.OpenProject(@"C:\projects\part.encam", addToReOpen: false);

// Save in-place
appCom.SaveCurrentProject(string.Empty);

// Change work mode
appCom.SetMainWorkMode(TMainWorkMode.mwmMachining);

// Access sub-managers
using var utilMgrCom = appCom.Invoke(app => app.UtilityManager);

// Read the active UI theme (null in headless builds)
using var themeCom = appCom.Theme();
if (themeCom != null)
{
    bool dark = themeCom.IsDark();
    themeCom.Dispose();
}
```

---

## Application Event Handlers

All handlers are registered via `ICamApiApplication.RegisterHandler`. The `events` parameter is an `IListString` containing the interface names (GUIDs also accepted) of the handler interfaces your object implements.

Your handler class must also implement `ICamApiEventHandler` (which exposes `GetAsyncMode`). When `GetAsyncMode` returns `true` (default), the CAM application fires the event asynchronously and does not wait for your handler to finish.

### Handler interfaces

| Interface | Method | Fires when |
|---|---|---|
| `ICamApiHandlerApplicationAfterLoad` | `ApplicationAfterLoad(handlerIdent, application)` | The CAM application has finished loading |
| `ICamApiHandlerApplicationBeforeClose` | `ApplicationBeforeClose(handlerIdent, application)` | The CAM application is about to close |
| `ICamApiHandlerApplicationAfterClose` | `ApplicationAfterClose(handlerIdent)` | The CAM application has closed |
| `ICamApiHandlerApplicationNewProject` | `ApplicationNewProject(handlerIdent)` | A new (empty) project is about to be created |
| `ICamApiHandlerApplicationActiveProjectChanged` | `ApplicationActiveProjectChanged(handlerIdent, newProject)` | The active project reference has changed |
| `ICamApiHandlerApplicationBeforeLoadProject` | `ApplicationBeforeLoadProject(handlerIdent, project)` | A project file is about to be loaded |
| `ICamApiHandlerApplicationAfterLoadProject` | `ApplicationAfterLoadProject(handlerIdent, project)` | A project file has finished loading |
| `ICamApiHandlerApplicationBeforeSaveProject` | `ApplicationBeforeSaveProject(handlerIdent, project)` | The current project is about to be saved |
| `ICamApiHandlerApplicationAfterSaveProject` | `ApplicationAfterSaveProject(handlerIdent, project)` | Any project has been saved |
| `ICamApiHandlerApplicationUpdateStartProgress` | `ApplicationUpdateStartProgress(handlerIdent, caption, percent)` | Application startup progress has changed |
| `ICamApiHandlerApplicationUpdateProcessState` | `ApplicationUpdateProcessState(handlerIdent, caption, percent)` | A running process has updated its progress |

Handlers for other objects register through the same `RegisterHandler` call:

| Interface | Method | Fires when |
|---|---|---|
| `ICamApiHandlerProjectBeforeSave` | `ProjectBeforeSave(handlerIdent, project)` | That project is about to be saved |
| `ICamApiHandlerProjectAfterSave` | `ProjectAfterSave(handlerIdent, project)` | That project has been saved |
| `ICamApiHandlerTechOperationInitModelFormers` | `InitModelFormers(...)` | An operation initialises its model formers |
| `ICamApiHandlerTechOperationLoadFromXmlProp` | `LoadFromXmlProp(xmlProp)` | Operation properties were read from XML |
| `ICamApiHandlerTechOperationSaveToXmlProp` | `SaveToXmlProp(xmlProp)` | Operation properties are being written to XML |
| `ICamApiHandlerTechOperationToolChanged` | `ToolChanged(...)` | The operation's tool was replaced |
| `ICamApiHandlerTechOperationToolpathCalculated` | `ToolpathCalculated(handlerIdent, operation)` | The operation's **body** toolpath was just (re)computed |
| `ICamApiHandlerFeatureFinderUpdated` | see [feature-finder.md](feature-finder.md) | Recognition finished or the feature list changed |
| `ICamApiHandlerApplicationMainForm…` | see [ui.md](ui.md#icamapiapplicationmainform) | Main-window events (visibility, minimize, cloud buttons, UI info) |
| `ICamApiHandlerProgressIndicator` | see [ui.md](ui.md#icamapiprogressindicator) | Progress indicator shown/hidden/advanced/broken |

> `ToolpathCalculated` fires only on a real body recompute. It does **not** fire for
> link/approach recomputes, nor when a locked operation
> ([`ResetToolpathLocked`](project.md#icamapitechoperation)) keeps its frozen toolpath.

### Registration example

```csharp
// Handler class must implement ICamApiEventHandler and one or more handler interfaces:
public class MyHandler : ICamApiEventHandler, ICamApiHandlerApplicationAfterLoadProject
{
    // ICamApiEventHandler
    public bool GetAsyncMode(string interfaceUid) => false; // synchronous

    // ICamApiHandlerApplicationAfterLoadProject
    public void ApplicationAfterLoadProject(string handlerIdent, ICamApiProject project)
    {
        // react to project load
    }
}

// Registration (e.g. inside IExtensionGlobal.OnSCInitializing):
using var appCom = ComWrapper.Create(applicationSingleton.GetApplication(out _));
var handler = new MyHandler();
var events = new ListString();
events.Add(typeof(ICamApiHandlerApplicationAfterLoadProject).GUID.ToString("B"));
appCom.Invoke(app => app.RegisterHandler("my-handler-id", handler, events, out _));
```

---

## ICamApiApplicationSingleton

Used to obtain the `ICamApiApplication` instance from within a **Global extension** (`IExtensionGlobal`), which does not receive the application via context.

```
ICamApiApplicationSingleton
  GetApplication(out TResultStatus) → ICamApiApplication*
  GetCurrentOperation(out TResultStatus) → ICamApiTechOperation*
```

The singleton is obtained by casting the `IExtensionManager` (retrieved via `ExtensionManagerHelper.GetInstance()`):

```csharp
// In a Global extension entry point:
public TResultStatus OnSCInitializing()
{
    using var managerCom = ExtensionManagerHelper.GetInstance();
    // Cast to singleton interface:
    var singleton = managerCom.Invoke(mgr => mgr as ICamApiApplicationSingleton);
    // or retrieve from your extension context depending on SDK version
    return default;
}
```

> See: [`ExtensionGlobal\ExtensionGlobalNet\project\main\ExtensionGlobal.cs`](../../ExtensionGlobal/ExtensionGlobalNet/project/main/ExtensionGlobal.cs)

**Shortcut helper:** `SystemExtensionFactory.GetApplication()` wraps the singleton lookup — call it from `OnSCInitializing` (where no context is provided) to reach the application in one line:

```csharp
using var appCom = SystemExtensionFactory.GetApplication();   // throws on error
```

---

## Plugin hotkeys

Plugins can register **global keyboard shortcuts** through `ICamApiHotkeyManager`, reached from `application.HotkeyManager` (or `mainForm.HotkeyManager`). Because it lives at the application level, a **Global extension** can register hotkeys during startup — before the main form exists.

Plugin hotkeys are dispatched after the viewport but before classic menu/action shortcuts, and only while the main window is active and focus is not in a text editor. The host's own native shortcuts are pre-registered as **reserved** entries, so `FindByShortcut` reports a conflict against them (they cannot be removed).

### Interfaces

| Interface | Purpose |
|---|---|
| `ICamApiHotkeyManager` | Create / add / remove / look up hotkeys; enumerate registered ones |
| `ICamApiHotkey` | One shortcut binding — `Shortcut` (R, identity), `Caption` (RW), `Enabled` (RW), `OnExecute` (RW), `IsReserved` (R) |
| `ICamApiHotkeyOnExecute` | Callback fired on the UI thread when the shortcut is pressed |

The `Shortcut` is fixed at creation (it identifies the binding); to re-bind, create a new hotkey carrying the same handler and replace the old one.

### .NET helpers

`HotkeyManagerHelper` — `CreateHotkey`, `AddShortcut` (throws if the shortcut is taken), `RemoveShortcut` (throws for a reserved one), `FindByShortcut`, `Count`, `GetHotkey`, `EnumerateShortcuts`.
`HotkeyHelper` — `Shortcut`, `GetCaption`/`SetCaption`, `GetEnabled`/`SetEnabled`, `SetOnExecute`, `IsReserved`.

Instead of implementing `ICamApiHotkeyOnExecute` by hand, wrap a delegate with the `HotkeyOnExecute` adapter — it casts the raw application pointer to `ICamApiApplication` for you.

```csharp
using var appCom = SystemExtensionFactory.GetApplication();
using var managerCom = appCom.HotkeyManager();   // null in headless / kernel-only builds
if (managerCom == null)
    return;

// Refuse to clobber an existing (plugin or reserved native) binding
using (var existing = managerCom.FindByShortcut("Ctrl+Shift+K"))
{
    if (!existing.IsNull)
        return;
}

var handler = new HotkeyOnExecute(app =>
{
    // fires on the UI thread; 'app' is a ComWrapper<ICamApiApplication>
    using var projectCom = app.GetActiveProject();
    // ... do work
});

using var hotkeyCom = managerCom.CreateHotkey("Ctrl+Shift+K");
hotkeyCom.SetCaption("My plugin action");
hotkeyCom.SetOnExecute(handler);
managerCom.AddShortcut(hotkeyCom);
```

> `HotkeyManager()` returns `ComWrapper<ICamApiHotkeyManager>?` — it is `null` in headless / kernel-only builds where no GUI hotkey registry exists. Always null-check.

---

## ICamApiPaths / ICamApiConstants

### ICamApiPaths

Provides read-only paths to the standard folders of the installed ENCY application. Available as a singleton and also exposed on the IPC application object.

| Property | Description |
|---|---|
| `MainProgramFolder` | Root installation folder (no platform subfolder) |
| `InterpretersFolder` | NC interpreter files |
| `ModelsFolder` | Default models folder |
| `NCProgramsFolder` | Default NC programs folder |
| `ExamplesFolder` | Example projects |
| `PostprocessorsFolder` | Postprocessor files |
| `OperationsFolder` | XML files with additional operation info |
| `MachinesFolder` | Machine definition files |
| `LibrariesFolder` | Help library files |
| `ConfigFolder` | All configuration files |
| `UserExtensionsFolder` | User-installed extensions |

Path substitution methods:

```csharp
// Fold an absolute path into a portable CAM-variable form:
string reduced = pathsCom.TryReducePath(@"C:\CAM\machines\my.xml");
// => "%MACHINES%\my.xml"

// Expand a portable path back to absolute:
string full = pathsCom.TryUnfoldPath("%MACHINES%\\my.xml");
```

The `PathsHelper` extension class (`CAMAPI.DotnetHelper`) exposes all properties as strongly-typed methods on `ComWrapper<ICamApiPaths>`.

### ICamApiConstants

| Property | Description |
|---|---|
| `Version` | CAM API version string |
| `ExeName` | Executable file name |
| `ExeVersion` | Application version string |
| `Language` | Selected UI language |
| `OrgName` | Name of the head organization |

```csharp
using var constantsCom = /* obtain from singleton */;
string apiVer = constantsCom.Version();
string appVer = constantsCom.ExeVersion();
```

---

## ICamApiUtilityManager

Manages the registered utility buttons visible in the ENCY toolbar.

```
GetListInfo(out TResultStatus) → ICamApiListUtilityInfo*
Execute(uid: string, out TResultStatus)
Reload()
GetUtilsUiInfoJson() → string
```

### The three sources of utilities

Utilities come from three places, exposed as separate read-only lists plus a combined one:

| Property | Type | Contents |
|---|---|---|
| `EmbeddedSystemUtils` | `ICamApiUtilitiesList*` | Utilities built into the CAM core (hardcoded) |
| `ExtensionUtils` | `ICamApiUtilitiesList*` | Utilities implemented as extensions (read from the extension manager) |
| `UserUtils` | `ICamApiListUtilityInfo*` | Utilities added by the user — the only **editable** list |
| `SummaryUtilsList` | `ICamApiUtilitiesList*` | All three combined |

> **`GetListInfo` returns only `UserUtils`**, not everything. For the full set — including
> built-in and extension-provided utilities — walk `SummaryUtilsList`.

`ICamApiUtilitiesList` is a plain indexed list of `IUtilButtonContext`:

| Method | Description |
|---|---|
| `Count()` → `integer` | Number of utilities |
| `Get(index)` → `IUtilButtonContext*` | Utility at index |
| `GetByUid(uid)` → `IUtilButtonContext*` | Utility by its UID |
| `GetIndex(uid)` → `integer` | Index of a utility by UID |

`ICamApiListUtilityInfo` gives indexed access to `IUtilButtonContext` items, each describing a single utility:

| Property | Description |
|---|---|
| `Uid` | Unique identifier |
| `Name` | Display name |
| `Description` | Short description |
| `ModulePath` | Path to the DLL |
| `Enabled` | Whether the button is enabled |
| `Hint`, `HintTitle`, `HintText` | Tooltip content |
| `IconPath` / `IconId` | Icon file path or internal icon ID |
| `VisibleOnlyInExpertMode` | Restricts visibility to expert mode |
| `UiVisible` | Whether the utility appears in the UI |
| `Visible` | Whether the button is visible (currently mirrors `Enabled`) |

> `VisibleOnlyInExpertMode` and `UiVisible` have no helper wrappers — read them via
> `Invoke`.

### .NET helper usage

```csharp
using var managerCom = ComWrapper.Create(app.UtilityManager);
using var listCom = managerCom.GetListInfo();
int count = listCom.Invoke(l => l.Count());
for (int i = 0; i < count; i++)
{
    var item = listCom.Invoke(l => l.Get(i));
    Console.WriteLine(item.Uid + ": " + item.Name);
}

// Execute by UID:
managerCom.Execute("MyUtility.UID");

// Every utility, not just the user-defined ones:
using var allCom = managerCom.InvokeAndWrap(m => m.SummaryUtilsList);
int total = allCom.Invoke(l => l.Count());
using var oneCom = allCom.InvokeAndWrap(l => l.GetByUid("MyUtility.UID"));
```

---

## TResultStatus / TResultStatusCode

Every procedure in the ENCY API that can fail returns a `TResultStatus` as an `out` parameter (or as a function return value in some cases).

```csharp
// IDL definition:
struct TResultStatus {
    TResultStatusCode Code;   // rsSuccess = 0, rsError = 1
    string Description;       // human-readable message when Code == rsError
}
```

The .NET helpers all follow this pattern and convert errors into exceptions automatically:

```csharp
// Raw COM call:
app.OpenProject(fileName, false, out TResultStatus status);
if (status.Code == TResultStatusCode.rsError)
    throw new Exception(status.Description);

// Helper call (equivalent, exception thrown automatically):
appCom.OpenProject(fileName, addToReOpen: false);
```

When writing your own `IExtensionUtility.Run` or `IExtensionGlobal.OnSCInitializing`, set the `out TResultStatus` to communicate failure back to the host:

```csharp
public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
{
    resultStatus = default; // Code = rsSuccess by default
    try
    {
        // your code
    }
    catch (Exception e)
    {
        resultStatus.Code = TResultStatusCode.rsError;
        resultStatus.Description = e.Message;
    }
}
```

---

## ICamApiEventHandler

`ICamApiEventHandler` is the base interface that all handler objects must implement alongside the specific handler interfaces they handle.

```
GetAsyncMode(interfaceUid: string) → boolean
```

- Return `true` (default) — ENCY fires the event and does not wait for your handler. Use this for lightweight notifications.
- Return `false` — ENCY blocks until your handler method returns. Use this when you need to modify shared state before the application proceeds.

The `interfaceUid` parameter is the GUID string of the specific handler interface being queried.

---

## IListString, IListInteger, IDictionaryStringString

These are ENCY's minimal collection interfaces for cross-boundary data exchange.

### IListString

```csharp
// Built-in .NET implementation provided by DotnetHelper:
var list = new ListString();
list.Add("event-name-1");
list.Add("event-name-2");

// Or construct from a delimited string:
var list2 = new ListString("a;b;c", ';');

// Interface methods:
int count = list.Count();
string item = list.Get(0);
bool has = list.Contains("a");
list.Remove("b");
string text = list.GetText(";"); // "a;c"
list.Read("x,y,z", ",");        // clears and repopulates
```

When you receive one of these as a COM object (rather than constructing it yourself), the
helpers convert it to a normal .NET collection in one call:

```csharp
foreach (string s in listStringCom.AsEnumerable()) { /* ... */ }
Dictionary<string, string> map = dictCom.ToDictionary();
```

### IListInteger

Same pattern as `IListString` but for `int` values. Does not have `GetText` or `Read`.
`ListIntegerHelper` wraps the members as extension methods on
`ComWrapper<IListInteger>`: `Count()`, `Get(index)`, `Add(v)`, `Remove(v)`, `Clear()`,
`Contains(v)`.

### IDictionaryStringString

```csharp
// Interface methods:
dict.Add("key", "value");
string val = dict.Get("key");
bool exists = dict.Contains("key");
dict.Remove("key");
int count = dict.Count();
IListString keys = dict.GetKeys();
IListString values = dict.GetValues();
```

---

## ICAMAPIFilesInStreamStorage

A binary container format for packing multiple files into a single stream file (used for project archives and machine setups).

### ICAMAPIFilesInStreamStorageLib — factory

```
CreateNewStorage() → ICAMAPIFilesInStreamStorage*
ExtractStorage(storageFile, destinationPath, overwrite, makeMetadataFile)
PackStorage(path, mask, storageFile, deep, compressionType)
```

**TFISStorageCompressionType**: `fsctNone`, `fsctFastest`, `fsctNormal`, `fsctMax`

### ICAMAPIFilesInStreamStorage — instance

```
Open(storageFileName, openMode) → TResultStatus
Close(squash: boolean)
ItemCount: integer                          // total items (folders + files + blocks)
ItemType[index] → TFISStorageItemType      // fsitNone / fsitBlock / fsitFile / fsitFolder
ItemParentIndex[index] → integer
ItemChildIndex[index] → integer
ItemSiblingIndex[index] → integer
ItemName[index] → string
ItemFullName[index] → string               // slash-separated full path
ItemUnCompressedSize[index] → UInt64
IndexOfFullName(fullName, parent) → integer
ExtractFile(fileIndex, destinationPath, overwrite)
GetFileReadStream(fileIndex) → IStream*
ReadAllTextOfFile(fileIndex, encodingName, out resStr) → TResultStatus
```

**TFISStorageOpenMode**: `fsomClosed`, `fsomRead`, `fsomReadWrite`, `fsomWrite`

---

## TLogEventType / LogItem

### TLogEventType enum

| Value | Numeric | Description |
|---|---|---|
| `leDebug` | 0 | Most detailed trace messages |
| `leVerbose` | 1 | Verbose diagnostic messages |
| `leInfo` | 2 | Informational messages for general users |
| `leHead` | 3 | Section headings in the log |
| `leWarning` | 4 | Warnings |
| `leError` | 5 | Errors |

### LogItem record

```
LogItem {
    EventType:  TLogEventType
    message:    string
    eventDate:  FileTime
    threadID:   Word
    srcModule:  string
    subModule:  string
}
```

### IExtensionLogger (via DotnetHelper)

The extension logger is obtained via `LoggerHelper.GetInstance()` or `ExtensionManagerHelper.Logger()`:

```csharp
using var loggerCom = LoggerHelper.GetInstance();

// Convenience methods:
loggerCom.Debug("trace message");
loggerCom.Verbose("verbose message");
loggerCom.Info("informational message");
loggerCom.Head("=== Section heading ===");
loggerCom.Warning("something unexpected");
loggerCom.Error("something failed");

// Show a toast notification in the ENCY UI:
loggerCom.Notify(TLogEventType.leInfo, "Operation complete", "My Extension");

// Check level before building expensive strings:
if (loggerCom.IsEventTypeActive(TLogEventType.leDebug))
    loggerCom.Debug($"Detail: {expensiveString}");

// Structured log item:
loggerCom.Log(new LogItem {
    EventType = TLogEventType.leInfo,
    message   = "custom log entry",
    srcModule = "MyExtension"
});
```

---

## ICamApiMacroManager

`ICamApiMacroManager` is the entry point for the macro system: list and run existing macros, and create new ones. Access it through the application helper:

```csharp
using var macroMgrCom = appCom.MacroManager();
```

> **Writing the body of a macro** (the generated `.cs` that runs inside ENCY — `ICamApiMacro.Run`, `MacroParams`, `NotifyMacroStep`) is a separate topic: see [docs/macros](../macros/README.md). This section covers managing and building macros from a host plugin.

### Listing and running macros

Helper (`MacroManagerHelper`):

```csharp
macroMgrCom.Execute(id, paramsJson);   // run a macro by id; paramsJson "" = recorded defaults
```

`paramsJson` is a JSON dictionary of per-step value overrides: `{"stepIndex": {"key": "value"}}`. Pass `""` to run with the values recorded into the macro.

**Methods (full list):**

| Method | Description |
|---|---|
| `Count` (R) | Number of macros registered in the system |
| `Macro[index]` (R) → `ICamApiMacroInfo*` | Macro metadata by index (0-based) |
| `GetMacroById(id)` → `ICamApiMacroInfo*` | Macro metadata by unique id |
| `Execute(id, params, out ret)` | Run a macro; `params` = overrides JSON (may be empty) |
| `CreateMacroInstance()` → `ICamApiMacroInfo*` | New empty macro metadata (fill its fields, then `AddMacro`) |
| `AddMacro(macro, out ret)` | Register a prebuilt macro in the system |
| `RemoveMacro(id, deleteSources, out ret)` | Remove a macro (optionally delete its source files) |
| `MacroBuilder` (R) → `ICamApiMacroBuilder*` | The builder — create macros programmatically (see below) |
| `NotifyMacroStep(stepIndex, out ret)` | Report that playback reached a step; called from **inside** a running macro for UI step highlighting (see [docs/macros](../macros/notify-step.md)) |
| `OpenInEditor(macro, out ret)` | Open the macro source in the editor for its language |
| `ExportMacro(id, targetPath, out ret)` | Export the macro into a single self-contained **`.dmcr`** archive (a ZIP of the macro folder including its `manifest.json`). Works for any macro language |
| `ImportMacro(sourcePath, out ret)` → `ICamApiMacroInfo*` | Import a macro from a `.dmcr` archive and register it, returning the registered macro |

```csharp
macroMgrCom.ExportMacro("MyMacro", @"C:\share\MyMacro.dmcr");
using var importedCom = macroMgrCom.ImportMacro(@"C:\share\OtherMacro.dmcr");
```

> **`ImportMacro` overwrites**: a macro with the same id is replaced, including its previous
> folder. Check `GetMacroById` first if that matters.

> **Direct call (no helper):** `Execute`, `NotifyMacroStep`, `ExportMacro` and `ImportMacro` have helper wrappers (the first four throw on `rsError` instead of returning a status); for the rest use `macroMgrCom.Invoke(m => m.GetMacroById(id))` / `InvokeAndWrap(...)`.

### ICamApiMacroInfo — macro metadata

`propertyRW`: `Id`, `Caption`, `Description`, `MacroPath` (source), `ExecutablePath` (built), `LanguageId`, `ExecuteExtensionId`.
`propertyR`: `StepCount`, `Step[index]` → `ICamApiMacroStepInfo*`.

### ICamApiMacroStepInfo / ICamApiMacroStepParam — discover overridable parameters

To learn which keys a macro's step accepts in `Execute`'s `params` JSON, walk the steps:

- `ICamApiMacroStepInfo`: `DisplayText` (may contain `{N}` placeholders for editable params), `ParamCount`, `Param[index]` → `ICamApiMacroStepParam*`, `GroupCaption`.
- `ICamApiMacroStepParam` (read-only): `Key` (the JSON key), `LabelText`, `ParamType` (`TMacroCommandParamType`), `Required`, `DefaultValue`, `ValuesString` (semicolon-separated allowed values, empty = free-form), `Caption` (locale-aware sub-row caption for composite rows; empty falls back to `Key`/`LabelText`).

`Required` means `default_value` was JSON `null` (or absent) at record time — a playback UI must refuse to run until the user supplies a value.

**Presentation flags on `ICamApiMacroStepInfo`** — only relevant if you render your own macro
step UI; they describe how ENCY draws the row:

| Property | Meaning |
|---|---|
| `IsComposite` | The step renders as a collapsible row (header + a sub-panel of editable params) |
| `IsModelItem` | A `selectModelItem` step — its single param is a read-only label with clear + geometry-pickup, not a free-form edit |
| `SupportsUseCurrent` | The row exposes the "use current item" (star) toggle |
| `IsUseCurrent` | Current value of that toggle |
| `OpType` | Operation type for `setOperationParam` rows (drives caption/enum resolution); empty otherwise |
| `PropPath` | Property path for `setOperationParam` rows; empty otherwise |

**`TMacroCommandParamType`** — the first four are plain value fields; the rest are fields the
UI renders as a clickable pill that opens a dedicated editor:

| Value | Meaning |
|---|---|
| `mptStr` (0) | String or enumerated value |
| `mptBol` (1) | Boolean |
| `mptInt` (2) | Integer |
| `mptFlt` (3) | Float (double) |
| `mptOffsetEditor` (4) | **Not value-bearing** — a glyph button opening a detached offset editor (workpiece CS / base-setup offset). The override travels in the step's own param keys, not this param |
| `mptCoordSystemEditor` (5) | Read-only display string; opens the 6-value coordinate-system editor (translation + rotation). The transform travels via the step's own data |
| `mptColorEditor` (6) | Colour swatch pill; opens the colour picker. The colour travels via the step's own data |
| `mptGeomPicker` (7) | Geometry reference; clicking arms viewport geometry pickup (Esc cancels) |
| `mptPointEditor` (8) | Read-only `"x; y; z"` string; opens the 3-value point editor (e.g. a fixture node's Origin). Distinct from `mptCoordSystemEditor`, which is a 6-value LCS |

> For the pill types the string you read is **display only** — the real value is carried by
> the step's own param keys. Do not try to write geometry or a transform through it.

### Creating a macro programmatically — ICamApiMacroBuilder + ICamApiMacroCommandsManager

A macro is built from a sequence of recorded commands, then compiled. This mirrors exactly what the UI recorder does. The commands manager is obtained by QI from the builder.

```csharp
using var macroMgrCom = appCom.MacroManager();
using var builderCom  = macroMgrCom.InvokeAndWrap(m => m.MacroBuilder);

// 1. Record commands.
builderCom.Invoke(b =>
{
    var cmds = (ICamApiMacroCommandsManager)b;     // same object, QI
    cmds.Start(out _);

    // Imitate a "create operation" hook: build a command payload and register it.
    var cd = cmds.CreateCommandData(TMacroCommandType.mctCreateOperation);
    cd.SetStr("OperationType", "HoleMachiningOp");
    cd.SetStr("TypeCaption",   "Hole machining");
    cd.SetStr("Name",          "Op1");
    cmds.RegisterCommand(cd);

    cmds.Stop(out _);
});

// 2. Configure build settings and build.
string sourcePath = builderCom.Invoke(b =>
{
    var main = b.CreateMainSettings(out _);
    main.Id = "MyMacro";
    main.Caption = "My macro";
    main.CreateOperations = true;
    main.OutputFolder = outputFolder;
    main.ExecuteExtensionId = "Extension.MacroRunner.Dotnet";

    var lang = b.CreateLanguageSettings("dotnet", out _);
    if (lang is ICamApiMacroBuilderDotNetSettings dn)
    {
        dn.TargetFramework = "net8.0-windows";
        // CRITICAL: without SDKVersion + References the generated .csproj cannot resolve
        // CAMAPI types (CS0246 at macro build). Source both from the extension manager.
        using var emCom = appCom.ExtensionManager();
        dn.SDKVersion  = emCom.Invoke(x => x.ApiVersion);
        dn.References   = emCom.Invoke(x => x.ApiDependencies);
    }
    return b.CreateMacro(main, lang, out _);        // generates the source project
});

builderCom.Invoke(b => b.Save(out _));              // compiles the runnable macro
```

**Alternative content source — capture the current project** instead of authoring commands by hand:

```csharp
cmds.AddStrategyState(out _);   // or AddMachineState(out _) / AddWorkpieceState(out _) / AddRecognizeFeature(out _)
```

#### Command field-key schema (authoritative, at runtime)

The keys (and their types/required flags) a command of a given `TMacroCommandType` expects are discoverable at runtime — do not hard-code them. Get the schema provider (`ICamApiMacroCommandSchema`) from the commands manager, then `GetFlatCommand` returns an `ICamApiMacroCommandFieldList` (indexed list of field descriptors):

```csharp
var schema = cmds.GetCommandSchema();
var fields = schema.GetFlatCommand(TMacroCommandType.mctCreateOperation);
for (int i = 0; i < fields.GetCount(); i++)
{
    string key      = fields.GetKey(i);
    var    type     = fields.GetFieldType(i);   // TMacroCommandParamType: mptStr/mptBol/mptInt/mptFlt
    bool   required = fields.GetRequired(i);
}
```

**Variant (model-item) commands** — some commands accept different key sets depending on a *discriminator* field (e.g. `mctSetWorkpiecePrimitive`, whose keys depend on the item class). `GetFlatCommand` returns the common keys (including the discriminator key itself); pass a discriminator value to `GetClassItemCommand` for the variant-specific keys:

```csharp
var boxFields = schema.GetClassItemCommand(
    TMacroCommandType.mctSetWorkpiecePrimitive, "TBoxLinkItem");
// discriminators: "TBoxLinkItem", "TCylLinkItem", "TStockLinkItem", "TAutoLinkItem"
```

The documented set is filled incrementally; command types / discriminators without a documented schema return an empty list (`GetCount() == 0`).

#### ICamApiMacroCommandData / ICamApiMacroCommandDataBuilder

- `ICamApiMacroCommandData` (read-only view): `CommandType` + `GetStr/GetInt/GetFloat/GetBool(key)`.
- `ICamApiMacroCommandDataBuilder` (returned by `CreateCommandData`, inherits the read interface): adds `SetStr/SetInt/SetFloat/SetBool(key, value)`. Fill it, then pass to `RegisterCommand`.

#### Builder settings interfaces

- `ICamApiMacroBuilderSettings` (from `CreateMainSettings`): `Id`, `Caption`, `Description`, `OutputFolder`, `ExecuteExtensionId`, `CreateOperations`.
- `ICamApiMacroBuilderLanguageSettings` (from `CreateLanguageSettings(languageId)`): base; QI to the concrete type:
  - `"dotnet"` → `ICamApiMacroBuilderDotNetSettings` — `TargetFramework`, `SDKVersion`, `References` (builds a .NET macro DLL).
  - `"spr"` → `ICamApiMacroBuilderSprSettings` (builds a script macro; carries no extra properties).

  Any other language id makes `CreateLanguageSettings` fail with `rsError`.

#### Recording state

`ICamApiMacroCommandsManager.IsStarted` (read-only) is `true` while a recording session is
active — i.e. between `Start` and `Stop`. Query it instead of tracking the state yourself,
which matters when the user may also be recording through the UI.

#### ICamApiMacroObject — letting a model object record itself

Model objects (e.g. job-assignment model items) may implement `ICamApiMacroObject`. The
capture side does a `QueryInterface` for it, takes the JSON and feeds it into the recording
pipeline — so there is no per-type dispatch in the macro manager.

| Method | Description |
|---|---|
| `GetRecordInfo()` → `string` | JSON array of the command bags needed to recreate this object on replay |

Each element is a flat object where `"type"` is the **`TMacroCommandType` ordinal** and the
remaining keys are the fields read back via `GetStr`/`GetInt`/`GetFloat`/`GetBool`:

```json
[
  {"type": 3, "FullName": ""},
  {"type": 3, "FullName": "Group1\\Face1"},
  {"type": 1, "ItemType": 0},
  {"type": 20, "ItemCaption": "Group1\\Face1", "Stock": 0.1}
]
```

#### Environment

Before building a .NET macro the builder may need its toolchain checked/prepared: `CheckEnvironment(languageSettings, out ret)` and `SetupEnvironment(languageSettings, out ret)`.

---

## Complete example: Utility entry point

> See: [`ExtensionGlobal/ExtensionGlobalNet/project/main/ExtensionGlobal.cs`](../../ExtensionGlobal/ExtensionGlobalNet/project/main/ExtensionGlobal.cs)
> See: [`UI/ExtensionUtilityMessageBoxNet/project/main/ExtensionUtilityMessageBox.cs`](../../UI/ExtensionUtilityMessageBoxNet/project/main/ExtensionUtilityMessageBox.cs)

```csharp
// The factory class must be in the CAMAPI namespace with the name ExtensionFactory.
namespace CAMAPI;

public class ExtensionFactory : IExtensionFactory
{
    public void OnLibraryRegistered(IExtensionFactoryContext context, out TResultStatus ret)
        => ret = default;

    public void OnLibraryUnRegistered(IExtensionFactoryContext context, out TResultStatus ret)
        => ret = default;

    public IExtension? Create(string extensionIdent, out TResultStatus ret)
    {
        ret = default;
        return extensionIdent switch
        {
            "My.Extension.Id" => new MyUtility(),
            _ => throw new Exception("Unknown extension: " + extensionIdent)
        };
    }
}

public class MyUtility : IExtension, IExtensionUtility
{
    public IExtensionInfo? Info { get; set; }

    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            using var appCom = ComWrapper.Create(context.CamApplication);
            string logPath = appCom.LogFilePath();
            using var loggerCom = LoggerHelper.GetInstance();
            loggerCom.Info($"Log file is at: {logPath}");
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
```
