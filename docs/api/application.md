# ENCY CAM API — Application Domain

This document covers the in-process (same-DLL) Application API used by extensions that run inside the ENCY CAM process.

---

## Table of Contents

1. [ICamApiApplication](#icamapiapplication)
2. [Application event handlers](#application-event-handlers)
3. [ICamApiApplicationSingleton — getting the app from a Global extension](#icamapiapplicationsingleton)
4. [ICamApiPaths / ICamApiConstants — system paths and constants](#icamapipaths--icamapiconstants)
5. [ICamApiUtilityManager — utilities](#icamapiutilitymanager)
6. [TResultStatus / TResultStatusCode — error handling](#tresultstatus--tresultstatuscode)
7. [ICamApiEventHandler — event registration pattern](#icamapieventhandler)
8. [IListString, IListInteger, IDictionaryStringString — collections](#iliststring-ilistinteger-idictionarystringstring)
9. [ICAMAPIFilesInStreamStorage — file-in-stream storage](#icamapifilesinstreamstorage)
10. [TLogEventType / LogItem — logging](#tlogeventtype--logitem)
11. [ICamApiMacroManager](#icamapimacomanager)

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
| `AttributesManager` | `ICAMAPICustomAttributesManager*` | R | Custom attributes manager |
| `UserTechOperationList` | `ICamApiUserTechOperationList*` | R | User-defined tech operations |
| `Started` | `boolean` | R | `true` once the instance is ready to work; `false` while opening a project or shutting down |
| `Theme` | `ICamApiTheme*` | R | Snapshot of the active UI theme/palette — see [ui.md](ui.md#icamapitheme) |

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

### IListInteger

Same pattern as `IListString` but for `int` values. Does not have `GetText` or `Read`.

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

`ICamApiMacroManager` is the entry point for the macro recording and execution system. The interface is currently minimal (no public methods beyond the interface marker). Access it through:

```csharp
using var macroMgrCom = appCom.MacroManager();
```

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
