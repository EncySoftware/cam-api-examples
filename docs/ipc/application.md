# ENCY CAM IPC API — Application Domain

This document covers the out-of-process (IPC) Application interfaces used when controlling a ENCY CAM instance from an external process (e.g., a standalone executable, a Prime shell, or another tool).

IPC interfaces mirror the in-process `CAMAPI` interfaces but add `TExecuteContext` parameters to every call that crosses the process boundary.

---

## Table of Contents

1. [ICamIpcApplication — remote application instance](#icamipcapplication)
2. [ICamIpcListApplication — managing multiple instances](#icamipclistapplication)
3. [IPC application event handlers](#ipc-application-event-handlers)
4. [ICamIpcPaths — system path properties](#icamiipcpaths)
5. [IIpcExtensionManager — managing extensions over IPC](#iipcextensionmanager)
6. [ICamIpcUtilityManager / ICamIpcUtilityInfo](#icamipcutilitymanager--icamipcutilityinfo)
7. [IIpcLogger — logging from an IPC context](#iipclogger)
8. [ICamIpcXmlPropPointer / ICamIpcXmlPropArray — XML property tree](#icamipcxmlproppointer--icamipcxmlproparray)

---

## ICamIpcApplication

The remote proxy for a single running ENCY CAM process. All methods that read from or write to the remote application take a `TExecuteContext` so the IPC layer can route the call and track the result.

### Connection management properties

| Property | Type | Description |
|---|---|---|
| `ProcessId` | `integer` | OS process ID of the ENCY instance |
| `IsAlive` | `boolean` | Whether the instance is running and responding |
| `IsBusy` | `boolean` | Whether the instance is currently processing an IPC request |
| `IsWindowed` | `boolean` | Whether the instance has a visible window |
| `ExecutablePath` | `string` | Path to the ENCY executable |
| `MainForm` | `ICamIpcApplicationMainForm*` | Proxy for the main window (available immediately without context) |
| `MachinesLibrary` | `ICamIpcMachinesLibrary*` | Machine library manager |
| `MacroManager` | `ICamIpcMacroManager*` | Macro system manager |
| `Paths` | `ICamIpcPaths*` | System path information |
| `UserTechOperationList` | `ICamIpcUserTechOperationList*` | User tech operation list |

### Connection lifecycle methods

| Method | Description |
|---|---|
| `WaitForStarted(timeout, out ResultStatus)` | Blocks until the ENCY instance is ready or the timeout (ms) elapses |
| `Close(asyncMode, out ResultStatus) → IListString*` | Sends a close request; returns any error strings |
| `StartAsyncWork(out ResultStatus)` | Starts the local IPC server to receive async callbacks from ENCY |
| `StopAsyncWork(out ResultStatus)` | Stops the local IPC server |
| `GetPort(out ResultStatus) → integer` | Returns the TCP/named-pipe port the IPC server listens on |
| `RegisterWorkingThread(threadHandle, out ResultStatus)` | Registers a thread to process async IPC commands |
| `UnregisterWorkingThread(threadHandle, out ResultStatus)` | Unregisters a working thread |

### Remote application methods (require TExecuteContext)

| Method | Description |
|---|---|
| `GetLogFilePath(ctx) → string` | Path to the ENCY log file |
| `GetActiveProject(ctx) → ICamIpcProject*` | Returns the active project proxy |
| `GetExtensionManager(ctx) → IIpcExtensionManager*` | Returns the extension manager proxy |
| `GetUtilityManager(ctx) → ICamIpcUtilityManager*` | Returns the utility manager proxy |
| `GetMachiningToolsManager(ctx) → ICamIpcMachiningToolsManager*` | Returns the tools manager proxy |
| `GetPLMManager(ctx) → IIpcPLMManager*` | Returns the PLM manager proxy |
| `GetMainFormTimeOut(timeout) → ICamIpcApplicationMainForm*` | Returns the main form proxy, waiting up to `timeout` ms |
| `OpenProject(fileName, addToReOpen, ctx)` | Opens a project file |
| `SaveCurrentProject(fileName, ctx)` | Saves the current project |
| `ExportCurrentProject(targetFileName, overwrite, ctx)` | Exports the project with all snapshots |
| `GetMainWorkMode(ctx) → TMainWorkMode` | Returns the current work mode |
| `SetMainWorkMode(workMode, ctx)` | Sets the work mode |
| `SetActiveProjectMachine(machineInfo, ctx)` | Assigns a machine to the current project |
| `OpenProjectFromPLM(plmItemId, connectionId, ctx)` | Opens a PLM project |

### Event handling over IPC

IPC events require a `listener` object in addition to the handler:

```
CreateListener(ctx) → ICamIpcEventListener*
GetDefaultListener() → ICamIpcEventListener*
RegisterHandler(handlerIdent, handler, listener, ctx)
UnregisterHandler(handlerIdent, ctx)
```

A listener encapsulates the transport channel that delivers events from ENCY back to the external process. Typically you use `GetDefaultListener()` for the default channel and `CreateListener(ctx)` when you need an isolated channel per module.

### Usage pattern

```csharp
// Obtain an ICamIpcApplication proxy (from ICamIpcSingletons or Prime):
var ctx = new TExecuteContext();
app.WaitForStarted(10_000, out var status);
if (status.Code == TResultStatusCode.rsError)
    throw new Exception(status.Description);

// Open a project:
app.OpenProject(@"C:\projects\part.encam", addToReOpen: false, ref ctx);

// Read the work mode:
var mode = app.GetMainWorkMode(ref ctx);

// Get active project:
var project = app.GetActiveProject(ref ctx);
```

---

## ICamIpcListApplication

A collection of `ICamIpcApplication` instances. Returned by the singletons or Prime to enumerate all running ENCY processes.

```
Count: integer
Get(index, ctx) → ICamIpcApplication*
GetByProcessId(processId, ctx) → ICamIpcApplication*
Add(item)
RemoveByProcessId(processId)
RemoveAt(index)
Clear()
```

---

## IPC Application Event Handlers

These are the IPC equivalents of the in-process application event handlers. They are registered via `ICamIpcApplication.RegisterHandler`.

| Interface | Method | Fires when |
|---|---|---|
| `ICamIpcHandlerApplicationAfterLoad` | `ApplicationAfterLoad(handlerIdent, application)` | ENCY has finished loading |
| `ICamIpcHandlerApplicationBeforeClose` | `ApplicationBeforeClose(handlerIdent, application)` | ENCY is about to close |
| `ICamIpcHandlerApplicationAfterClose` | `ApplicationAfterClose(handlerIdent)` | ENCY has closed |
| `ICamIpcHandlerApplicationNewProject` | `ApplicationNewProject(handlerIdent)` | A new empty project is being created |
| `ICamIpcHandlerApplicationActiveProjectChanged` | `ApplicationActiveProjectChanged(handlerIdent, newProject)` | The active project reference changed |
| `ICamIpcHandlerApplicationBeforeLoadProject` | `ApplicationBeforeLoadProject(handlerIdent, project)` | A project is about to be loaded |
| `ICamIpcHandlerApplicationAfterLoadProject` | `ApplicationAfterLoadProject(handlerIdent, project)` | A project has finished loading |
| `ICamIpcHandlerApplicationBeforeSaveProject` | `ApplicationBeforeSaveProject(handlerIdent, project)` | The current project is about to be saved |
| `ICamIpcHandlerApplicationAfterSaveProject` | `ApplicationAfterSaveProject(handlerIdent, project)` | Any project has been saved |
| `ICamIpcHandlerApplicationUpdateStartProgress` | `ApplicationUpdateStartProgress(handlerIdent, caption, percent)` | Startup progress updated |
| `ICamIpcHandlerApplicationUpdateProcessState` | `ApplicationUpdateProcessState(handlerIdent, caption, percent)` | Running process progress updated |

---

## ICamIpcPaths

Exposed directly on `ICamIpcApplication.Paths`. Contains the same path properties as the in-process `ICamApiPaths`, plus a unique instance identifier.

### Methods and properties

| Member | Description |
|---|---|
| `GetInstanceId() → string` | Unique identifier of this ENCY instance in IPC messages |
| `MainProgramFolder` | Root installation folder |
| `InterpretersFolder` | NC interpreter files |
| `ModelsFolder` | Default models folder |
| `NCProgramsFolder` | Default NC programs folder |
| `ExamplesFolder` | Example projects |
| `PostprocessorsFolder` | Postprocessor files |
| `OperationsFolder` | XML files with additional operation info |
| `MachinesFolder` | Machine definition files |
| `LibrariesFolder` | Help library files |
| `ConfigFolder` | Configuration files |
| `UserExtensionsFolder` | User-installed extensions |
| `TryReducePath(fullPath) → string` | Converts an absolute path to a portable CAM-variable form |
| `TryUnfoldPath(reducedPath) → string` | Expands a portable CAM-variable path to an absolute path |

---

## IIpcExtensionManager

The IPC proxy for the ENCY extension manager. All methods take a `TExecuteContext`.

### Library and storage management

| Method | Description |
|---|---|
| `RegisterLibrary(storageType, descFilePath, ctx) → IIpcExtensionLibraryInfo*` | Registers an extension library from a JSON description file |
| `UnRegisterLibrary(libraryPath, ctx)` | Removes a library registration |
| `ReloadStorage(storageType, ctx)` | Re-reads a storage layer |
| `GetLibrariesInfo() → IIpcListExtensionLibraryInfo*` | Lists all registered libraries |
| `GetLibraryInfo(extensionTypeId, ctx) → IIpcExtensionLibraryInfo*` | Gets library info by extension type ID |
| `FreeLibrary(libraryPath, ctx)` | Unloads all extensions from a library |

### Extension enable/disable

| Method | Description |
|---|---|
| `GetLibraryDisabled(storageType, libraryPath, ctx) → boolean` | Whether the library is disabled for a storage layer |
| `GetExtensionDisabled(storageType, extensionIdent, ctx) → boolean` | Whether a specific extension is disabled |
| `SetLibraryDisabled(storageType, libraryPath, disabled, ctx)` | Enables or disables a library for a storage layer |
| `SetExtensionDisabled(storageType, extensionIdent, disabled, ctx)` | Enables or disables an extension |
| `GetFieldInherited(storageType, entityIdent, field, ctx) → boolean` | Checks if a field value is inherited |
| `SetFieldInherited(storageType, entityIdent, field, ctx)` | Resets a field to use the inherited value |
| `GetLibraryLoaded(libraryPath, ctx) → boolean` | Whether a library is currently loaded |

### Extension discovery and instantiation

| Method | Description |
|---|---|
| `GetExtensionTypeGroups(ctx) → IListString*` | Lists all available extension type group names |
| `GetExtensionTypeInfo(extensionTypeId, ctx) → IIpcExtensionTypeInfo*` | Gets type metadata for one extension |
| `GetExtensionTypeInfos(extensionTypeGroup, ctx) → IIpcListExtensionTypeInfo*` | Lists all extension types in a group |
| `GetExtensionTypeInfosFromLibrary(libraryPath, ctx) → IIpcListExtensionTypeInfo*` | Lists all extensions declared in a library DLL |
| `CreateExtension(extensionTypeId, ctx) → IIpcExtension*` | Creates (loads) a new extension instance |
| `CreateExtensions(extensionTypeIds, ctx) → IIpcListExtension*` | Creates multiple extensions (semicolon-separated IDs) |
| `GetExtension(extensionInstanceId, ctx) → IIpcExtension*` | Returns an existing extension instance by ID |
| `FreeExtension(extensionInstanceId, ctx)` | Unloads an extension instance |

### Storage type enum — TStorageType

Values: `stSystem`, `stUser` (and potentially project-level). Defines which configuration layer a library or enable/disable flag belongs to.

### Macro build support

| Method | Description |
|---|---|
| `GetApiVersion(ctx) → string` | Current CAM Open API version — use as the macro .NET SDK version |
| `GetApiDependencies(ctx) → IListString*` | Reference assemblies a built macro must compile against — use as the macro .NET References |

Helper (`ExtensionManagerHelper`): `emCom.GetApiVersion()`, `emCom.GetApiDependencies()`. These feed the macro builder's `.NET` language settings (see `ICamIpcMacroManager` below) — without them the generated macro project cannot resolve CAMAPI types.

---

## ICamIpcMacroManager

IPC mirror of `ICamApiMacroManager` — list and run existing macros, and create new ones. See [docs/api/application.md](../api/application.md#icamapimacomanager) for the full conceptual model; this section gives the IPC helper surface. (Writing the body of a macro itself is covered in [docs/macros](../macros/README.md).)

```csharp
using var macroMgrCom = appCom.MacroManager();   // ComWrapper<ICamIpcMacroManager>
```

### Listing and running

```csharp
int count = macroMgrCom.Count();
using var info = macroMgrCom.GetMacroById("MyMacro");   // or macroMgrCom.GetMacro(index)
macroMgrCom.Execute("MyMacro", paramsJson);             // paramsJson "" = recorded defaults
```

`paramsJson` is a per-step override dictionary: `{"stepIndex": {"key": "value"}}`.

**Helpers (`MacroManagerHelper`):**

| Helper | Description |
|---|---|
| `Count()` | Number of macros |
| `GetMacro(i)` / `GetMacroById(id)` → `ComWrapper<ICamIpcMacroInfo>` | Macro metadata |
| `Execute(id, paramsJson = "")` | Run a macro |
| `CreateMacroInstance()` / `AddMacro(info)` / `RemoveMacro(id, deleteSources)` | Manage macros |
| `GetMacroBuilder()` → `ComWrapper<ICamIpcMacroBuilder>` | Build macros (below) |
| `NotifyMacroStep(i)` / `OpenInEditor(info)` | Step notify / open source |

### ICamIpcMacroInfo

`MacroInfoHelper`: getters `Id()`, `Caption()`, `Description()`, `MacroPath()`, `ExecutablePath()`, `LanguageId()`, `ExecuteExtensionId()`, `StepCount()`, `GetStep(i)`; setters `SetId(v)` … `SetExecuteExtensionId(v)` (for the `CreateMacroInstance` → fill → `AddMacro` flow).

### Discovering overridable step parameters

`MacroStepInfoHelper`: `DisplayText()`, `ParamCount()`, `GetParam(i)`, `GroupCaption()`.
`MacroStepParamHelper`: `Key()`, `LabelText()`, `ParamType()` (`TMacroCommandParamType`), `Required()`, `DefaultValue()`, `ValuesString()`.

### Creating a macro programmatically

```csharp
using var builderCom = macroMgrCom.GetMacroBuilder();
using var cmdsCom    = builderCom.GetCommandsManager();

cmdsCom.Start();
using (var cd = cmdsCom.CreateCommandData(TMacroCommandType.mctCreateOperation))
{
    cd.SetStr("OperationType", "HoleMachiningOp");
    cd.SetStr("TypeCaption",   "Hole machining");
    cd.SetStr("Name",          "Op1");
    cmdsCom.RegisterCommand(cd);
}
cmdsCom.Stop();

using var main = builderCom.CreateMainSettings();
main.SetId("MyMacro");
main.SetCaption("My macro");
main.SetCreateOperations(true);
main.SetOutputFolder(outputFolder);
main.SetExecuteExtensionId("Extension.MacroRunner.Dotnet");

using var lang = builderCom.CreateLanguageSettings("dotnet");
lang.SetTargetFramework("net8.0-windows");
// CRITICAL: SDKVersion + References — without them the generated .csproj cannot resolve
// CAMAPI types (CS0246). Source both from the extension manager.
using var emCom = appCom.ExtensionManager();
lang.SetSDKVersion(emCom.GetApiVersion());
lang.SetReferences(emCom.GetApiDependencies());

string sourcePath = builderCom.CreateMacro(main, lang);
string builtPath  = builderCom.Save();
```

**Alternative — capture the current project** instead of authoring commands:

```csharp
cmdsCom.CaptureProjectState(machine: false, workpiece: false, strategy: true);
```

### Command field-key schema (authoritative, at runtime)

Get the schema provider (`ICamIpcMacroCommandSchema`) from the commands manager; `GetFlatCommand` returns an `ICamIpcMacroCommandFieldList` (indexed list of field descriptors). The `Fields()` helper reads it in one pass:

```csharp
using var schemaCom = cmdsCom.GetCommandSchema();
using var fieldsCom = schemaCom.GetFlatCommand(TMacroCommandType.mctCreateOperation);
foreach (var f in fieldsCom.Fields())
{
    // f.Key (string), f.FieldType (TMacroCommandParamType), f.Required (bool)
}
```

**Variant (model-item) commands** — some commands accept different key sets depending on a *discriminator* field (e.g. `mctSetWorkpiecePrimitive`, whose keys depend on the item class). `GetFlatCommand` returns the common keys including the discriminator; pass a discriminator value to `GetClassItemCommand` for the variant keys:

```csharp
using var boxCom = schemaCom.GetClassItemCommand(
    TMacroCommandType.mctSetWorkpiecePrimitive, "TBoxLinkItem");
// discriminators: "TBoxLinkItem", "TCylLinkItem", "TStockLinkItem", "TAutoLinkItem"
```

An empty list (`GetCount() == 0`) means the command type / discriminator has no documented schema yet.

### Command bag and helpers

- `MacroCommandSchemaHelper`: `GetFlatCommand(type)`, `GetClassItemCommand(type, discriminator)` → `ComWrapper<ICamIpcMacroCommandFieldList>` (on the provider from `GetCommandSchema`).
- `MacroCommandFieldListHelper`: `GetCount()`, `GetKey(i)`, `GetFieldType(i)`, `GetRequired(i)`, and `Fields()` to read the whole list at once.
- `MacroCommandDataHelper`: `CommandType()`, `GetStr/GetInt/GetFloat/GetBool(key)`, `SetStr/SetInt/SetFloat/SetBool(key, value)` (setters work only on a bag from `CreateCommandData`).
- Language `"dotnet"` → .NET macro (`TargetFramework`/`SDKVersion`/`References` via `MacroBuilderLanguageSettingsHelper`); `"script"` → script macro.
- List-bearing commands (which the scalar bag cannot hold) have dedicated typed methods on the commands manager: `RegisterSetDriveFaceItemProperties`, `RegisterAddFixture`, `RegisterSetFixtureItemStock`/`Color`/`Caption`.

---

## ICamIpcUtilityManager / ICamIpcUtilityInfo

### ICamIpcUtilityManager

```
GetListInfo(ctx) → ICamIpcListUtilityInfo*
Execute(uid, ctx)
Reload()
GetUtilsUiInfoJson(ctx) → string
```

### ICamIpcUtilityInfo — read-only utility descriptor

```
GetUid(ctx) → string
GetUiVisible(ctx) → boolean
GetName(ctx) → string
GetHint(ctx) → string
GetIconPath(ctx) → string
```

### Usage example

```csharp
var utilMgr = app.GetUtilityManager(ref ctx);
var list    = utilMgr.GetListInfo(ref ctx);
int count   = list.Count();
for (int i = 0; i < count; i++)
{
    var info = list.Get(i);
    string uid  = info.GetUid(ref ctx);
    string name = info.GetName(ref ctx);
    Console.WriteLine($"{uid}: {name}");
}

// Execute by UID:
utilMgr.Execute("MyUtility.UID", ref ctx);
```

---

## IIpcLogger

Writes log entries into the ENCY log stream from an external process.

### Methods

| Method | Description |
|---|---|
| `log(event: LogItem)` | Writes a fully-populated log entry |
| `debug(message)` | Writes a `leDebug` entry |
| `verbose(message)` | Writes a `leVerbose` entry |
| `info(message)` | Writes a `leInfo` entry |
| `head(message)` | Writes a `leHead` entry |
| `warning(message)` | Writes a `leWarning` entry |
| `error(message)` | Writes a `leError` entry |

`LogItem` is the same struct used in the in-process API — see [api/application.md](../api/application.md#tlogeventtype--logitem).

```csharp
// Obtain via ICamIpcSingletons:
IIpcLogger logger = /* from singletons */;

logger.info("IPC session started");
logger.warning("Connection latency is high");
logger.error("Failed to load project");

logger.log(new LogItem {
    EventType = TLogEventType.leInfo,
    message   = "structured entry",
    srcModule = "MyTool"
});
```

---

## ICamIpcXmlPropPointer / ICamIpcXmlPropArray

IPC proxies for navigating the ENCY XML-property tree (the internal data model). These are the IPC equivalents of `IST_XMLPropPointer` / `IST_XMLPropArray`.

### ICamIpcXmlPropPointer

| Member | Type | Description |
|---|---|---|
| `GetInstanceId() → string` | — | IPC instance identifier |
| `Flt[name]` | `double` RW | Read/write a float child property by name |
| `Int[name]` | `integer` RW | Read/write an integer child property by name |
| `Bol[name]` | `boolean` RW | Read/write a boolean child property by name |
| `Str[name]` | `WideString` RW | Read/write a string child property by name |
| `Ptr[name]` | `ICamIpcXmlPropPointer*` R | Navigate to a child pointer property |
| `Arr[name]` | `ICamIpcXmlPropArray*` R | Navigate to a child array property |
| `CStr[name]` | `WideString` R | Calculated (formula-expanded) string value |

```csharp
// Example: read cutting speed from a tech-operation property tree
double speed = op.Ptr["CuttingConditions"].Flt["CuttingSpeed"];

// Write a parameter:
op.Ptr["CuttingConditions"].Flt["FeedRate"] = 0.15;
```

### ICamIpcXmlPropArray

| Member / Method | Description |
|---|---|
| `TopItem` | RW — index of the last item (count − 1) |
| `Itm[index]` | R — get item by index |
| `AddItem(item) → integer` | Appends an item; returns the new index |
| `Insert(item, index)` | Inserts before `index` |
| `Delete(index)` | Removes item at `index` |
| `ExtractItem(index) → ICamIpcXmlPropPointer*` | Removes and returns item at `index` |
| `Clear()` | Removes all items |
| `IndexOf(item) → integer` | Returns the index of an item, or −1 |
| `CreateNewItem(id) → ICamIpcXmlPropPointer*` | Creates a new typed item (if allowed by schema) |

```csharp
// Iterate over an array property:
var arr = root.Arr["Operations"];
for (int i = 0; i <= arr.TopItem; i++)
{
    var item = arr.Itm[i];
    string name = item.Str["Name"];
    Console.WriteLine(name);
}
```
