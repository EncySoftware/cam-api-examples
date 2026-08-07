# Extension System (CAMAPI)

> For a practical guide on choosing the right entry point and implementing each one, see [Extension Entry Points](../general/extension-entry-points.md).

All extension management in ENCY is performed through the interfaces defined in `CAMAPI.Extensions`. The central interface is `IExtensionManager`, which ENCY exposes via `CAMAPI.ExtensionManager.dll`.

**Obtaining the manager (.NET helper):**

```csharp
using CAMAPI.DotnetHelper;

using var managerCom = ExtensionManagerHelper.GetInstance();
// or from an application context:
using var managerCom = appCom.GetExtensionManager();
```

**Obtaining the manager (direct IDL):**

```
ICamApiApplication.GetExtensionManager(out TResultStatus, return IExtensionManager*)
```

---

## IExtensionManager

### Registration

#### `RegisterLibrary`

Registers a JSON description file into the specified storage. Returns library metadata on success.

```csharp
// .NET helper
ComWrapper<IExtensionLibraryInfo> info = managerCom.RegisterLibrary(
    TStorageType.stCurrentUser,
    @"C:\MyPlugins\MyExtension.json");
```

```
// IDL
IExtensionManager.RegisterLibrary(
    in StorageType: TStorageType,
    in JsonFilePath: string,
    out TResultStatus,
    return IExtensionLibraryInfo*)
```

#### `RegisterLibraryFromFolder`

Reads a plugin folder, finds its `settings.json`, copies the folder into the storage's own
location, then registers it. Convenient for installing a self-contained plugin folder in one
call. The `needReload` out-flag reports whether a storage reload is required to pick it up.

```csharp
// IDL
IExtensionManager.RegisterLibraryFromFolder(
    in StorageType: TStorageType,
    in FolderPath: string,
    out TResultStatus,
    out needReload: boolean,
    return IExtensionLibraryInfo*)
```

#### `UnRegisterLibrary`

Removes a library from all storages by its DLL path or JSON path.

```csharp
managerCom.UnRegisterLibrary(@"C:\MyPlugins\MyExtension.dll");
```

```
// IDL
IExtensionManager.UnRegisterLibrary(in LibraryPath: string, out TResultStatus)
```

#### `ReloadStorage`

Forces a re-read of a specific storage tier (e.g. after an external change to the JSON files).

```csharp
managerCom.ReloadStorage(TStorageType.stCurrentUser);
```

```
// IDL
IExtensionManager.ReloadStorage(in StorageType: TStorageType, out TResultStatus)
```

---

### State

#### `GetLibraryDisabled` / `SetLibraryDisabled`

Query or set the disabled flag for a library in a specific storage tier.

```csharp
bool disabled = managerCom.GetLibraryDisabled(TStorageType.stCurrentUser, libraryPath);
managerCom.SetLibraryDisabled(TStorageType.stCurrentUser, libraryPath, disabled: true);
```

#### `GetLibraryDisabledFinal`

Returns the effective disabled state after all storage tiers have been merged (highest-priority tier wins).

```csharp
bool effectivelyDisabled = managerCom.GetLibraryDisabledFinal(libraryPath);
```

#### `GetExtensionDisabled` / `SetExtensionDisabled`

Same as the library variants but per individual extension type ID.

```csharp
bool disabled = managerCom.GetExtensionDisabled(TStorageType.stCurrentUser, extensionTypeId);
managerCom.SetExtensionDisabled(TStorageType.stCurrentUser, extensionTypeId, disabled: true);
```

#### `GetExtensionDisabledFinal`

Effective disabled state for an individual extension after merging all storage tiers.

```csharp
bool effectivelyDisabled = managerCom.GetExtensionDisabledFinal(extensionTypeId);
```

#### `GetLibraryLoaded`

Returns whether a library DLL is currently loaded in memory.

```csharp
bool loaded = managerCom.GetLibraryLoaded(libraryPath);
```

#### `GetFieldInherited` / `SetFieldInherited`

Checks or marks whether a property (`TStorageField`) of a library or extension is inherited from a lower-priority storage tier rather than explicitly set in the current one.

```csharp
bool inherited = managerCom.GetFieldInherited(
    TStorageType.stCurrentUser, entityIdent, TStorageField.sfLibraryDisabled);

managerCom.SetFieldInherited(
    TStorageType.stCurrentUser, entityIdent, TStorageField.sfAll);
```

#### `GetLibraryStorages`

Returns a list of `TStorageType` values representing the storage tiers in which the library has non-inherited (explicitly set) values.

```csharp
using var storagesCom = managerCom.GetLibraryStorages(libraryPath);
```

#### `AddExtension` / `RemoveExtension`

Explicitly tracks an extension within a storage tier so its disabled flag can be managed independently of the library-level flag.

```csharp
managerCom.AddExtension(TStorageType.stCurrentUser, libraryPath, extensionTypeId);
managerCom.RemoveExtension(TStorageType.stCurrentUser, libraryPath, extensionTypeId);
```

---

### Querying Metadata

#### `GetExtensionTypeInfo`

Returns the `IExtensionTypeInfo` for a single extension by its unique type ID.

```csharp
using var typeInfoCom = managerCom.GetExtensionTypeInfo("My.Extension.Ident");
```

#### `GetExtensionTypeInfos`

Returns all `IExtensionTypeInfo` objects belonging to a group (the `Group` field in the JSON).

```csharp
using var listCom = managerCom.GetExtensionTypeInfos("Extension.Util.Common");
```

#### `GetExtensionTypeInfosFromLibrary`

Returns all extension type infos from a specific library DLL path.

```csharp
using var listCom = managerCom.GetExtensionTypeInfosFromLibrary(libraryPath);
```

#### `GetExtensionTypeGroups`

Returns all known group strings across all registered libraries.

```csharp
using var groupsCom = managerCom.GetExtensionTypeGroups();
```

#### `GetExtensionTypeGroup`

Returns the structured `IExtensionTypeGroup` for one group id (richer than the plain string
from `GetExtensionTypeGroups`).

```csharp
using var groupCom = managerCom.InvokeAndWrap(m =>
    (m.GetExtensionTypeGroup("Extension.Util.Common", out var status), status));
// IExtensionTypeGroup: Id (string), Caption (string), IsSystem (bool)
```

`IListExtensionTypeGroup` is the list form (`Get(index)`, `Count()`, `Add`, `Remove`, `RemoveAt`).

#### `GetLibraryInfo`

Returns `IExtensionLibraryInfo` for the library that contains the specified extension type ID.

```csharp
using var libInfoCom = managerCom.GetLibraryInfo("My.Extension.Ident");
```

#### `GetLibrariesInfo`

Returns metadata for all currently registered libraries.

```csharp
using var allLibsCom = managerCom.GetLibrariesInfo();
```

Each `IExtensionLibraryInfo` carries, besides its path and `UnloadMode`, the descriptive
fields taken from the library's `settings.json`: `Version`, `Id`, `Author`, `IconPath`, and
`Tags` (all read-only strings).

---

### Creation

#### `CreateExtension`

Creates and returns a new instance of an extension by its unique type ID. The extension's DLL is loaded if not already in memory.

```csharp
using var extCom    = managerCom.CreateExtension("My.Extension.Ident");
using var solverCom = extCom.InvokeAndWrap(e => e as ICamApiTechOperationSolver);
if (solverCom.IsNull)
    throw new Exception("Extension does not implement ICamApiTechOperationSolver");
```

```
// IDL
IExtensionManager.CreateExtension(in ExtensionTypeId: string, out TResultStatus, return IExtension*)
```

#### `CreateExtensionsByGroups`

Creates instances of all non-disabled extensions whose `Group` matches any of the semicolon-separated group strings.

```csharp
using var listCom = managerCom.CreateExtensionsByGroups("Extension.Util.Common;Extension.Util.Custom");
```

```
// IDL
IExtensionManager.CreateExtensionsByGroups(in ExtensionGroups: string, out TResultStatus, return IListExtension*)
```

#### `GetSingletonExtension`

Returns the single shared instance of a singleton extension (creates it on first call). Use for extensions that must exist exactly once across all callers.

```csharp
using var extCom = managerCom.GetSingletonExtension("My.Singleton.Ident");
// Static shortcut (no manager reference needed):
using var extCom2 = ExtensionManagerHelper.GetSingletonExtension("My.Singleton.Ident");
```

#### `GetExtension`

Returns an already-created extension instance by its numeric instance ID (`IExtensionInstanceInfo.Id`).

```csharp
using var extCom = managerCom.GetExtension(instanceId);
// Static shortcut:
using var extCom2 = ExtensionManagerHelper.GetExtension(instanceId);
```

#### `GetExtensionsByTypeId`

Returns all currently live instances of a specific extension type.

```csharp
using var listCom = managerCom.GetExtensionsByTypeId("My.Extension.Ident");
```

---

### Lifecycle

#### `FreeExtension`

Unloads a single extension instance by its numeric instance ID.

```csharp
managerCom.FreeExtension(instanceId);
```

```
// IDL
IExtensionManager.FreeExtension(in ExtensionInstanceId: hyper, out TResultStatus)
```

#### `FreeExtensionsByTypeId`

Unloads all live instances of a specific extension type.

```csharp
managerCom.FreeExtensionsByTypeId("My.Extension.Ident");
```

#### `FreeLibrary`

Unloads all extension instances from the specified library and then unloads the DLL.

```csharp
managerCom.FreeLibrary(libraryPath);
```

#### `FinalizeExtensions`

Unloads all extensions across all libraries. Called by ENCY during application shutdown; plugins do not normally call this directly.

```csharp
managerCom.FinalizeExtensions();
```

---

### Diagnostics

#### `Logger`

Returns the `IExtensionLogger` used by the extension system. Prefer accessing it through the static helper:

```csharp
using var loggerCom = ExtensionManagerHelper.Logger();
if (!loggerCom.IsNull)
    loggerCom.Invoke(log => log.Info("My message"));
```

#### `ApiVersion`

Returns the version string of the CAMAPI SDK against which the extension manager was built.

```csharp
string version = managerCom.ApiVersion();
// or static:
string version = ExtensionManagerHelper.ApiVersion();
```

---

## IExtensionStorage / TStorageType

`IExtensionStorage` represents a single tier of a layered configuration store. Tiers are evaluated in priority order; a value set in a higher-priority tier overrides the same value from lower tiers.

| Value | Priority | Scope | Mutable |
|---|---|---|---|
| `stSystem` | Lowest | All users, machine-wide | No |
| `stDealer` | 2 | Current user only | No |
| `stAllUsers` | 3 | All users, machine-wide | Yes |
| `stCurrentUser` | 4 | Current user only | Yes |
| `stDebugMode` | 5 | Current user, debug sessions | Yes |
| `stTestMode` | Highest | Automated test runs | Yes |

**Typical development workflow:** register your DLL with `stDebugMode` so it does not affect other users and is easy to disable.

**Key storage methods (on `IExtensionStorage` directly):**

| Method | Description |
|---|---|
| `Read` | Load current state from the backing file or database. |
| `Save` | Persist the current in-memory state. |
| `RegisterLibrary(LibraryIdent)` | Add a library entry to this tier's tracking list. |
| `UnRegisterLibrary(LibraryIdent)` | Remove a library entry from this tier. |
| `GetLibraries` | List all library paths recorded in this tier. |
| `GetExtensions(LibraryIdent)` | List extension type IDs that have explicit settings in this tier. |
| `GetLibraryDisabled` / `SetLibraryDisabled` | Read or write the disabled flag for a library. |
| `GetExtensionDisabled` / `SetExtensionDisabled` | Read or write the disabled flag for an individual extension. |
| `GetLibraryFieldInherited` / `SetLibraryFieldInherited` | Query or mark a library property as inherited from a lower tier. |
| `GetExtensionFieldInherited` / `SetExtensionFieldInherited` | Query or mark an extension property as inherited. |

`TStorageField` selects which property the inheritance methods act on:

| Value | Meaning |
|---|---|
| `sfAll` | All fields |
| `sfLibraryDisabled` | The library-level disabled flag |
| `sfExtensionDisabled` | The extension-level disabled flag |

---

## Extension type-info kinds

Every extension declares its kind through an `IExtensionTypeInfo…` marker interface (the same
mechanism behind `IExtensionTypeInfoUtility`, `IExtensionTypeInfoOperationPopup`, etc. — see
[extension entry points](../general/extension-entry-points.md)). In addition to the popup and
utility kinds documented there, the API also declares:

| Type-info interface | Injection point |
|---|---|
| `IExtensionTypeInfoProjectReportPopup` | Context menu of a project report |
| `IExtensionTypeInfoPostprocessorPopup` | Context menu of a postprocessor |
| `IExtensionTypeInfoToolsListPopup` | Context menu of the tools list |
| `IExtensionTypeInfoNCFilesExporter` | Export generated NC files to an external target (pairs with `IExtensionNCFilesExporter` — see [nc-simulation.md](nc-simulation.md#generatenc-and-result-objects)) |

---

## IExtensionLogger

`IExtensionLogger` is the unified logging sink for the extension system. Obtain it via `ExtensionManagerHelper.Logger()` or from `IExtensionManager.Logger`.

| Method | Level | Notes |
|---|---|---|
| `Debug(message)` | Debug | Detailed diagnostic output, typically suppressed in production. |
| `Verbose(message)` | Verbose | Finer-grained than Info, coarser than Debug. |
| `Info(message)` | Info | Normal operational messages. |
| `Head(message)` | Head | Section headings or major milestones in a log sequence. |
| `Warning(message)` | Warning | Recoverable anomalies. |
| `Error(message)` | Error | Non-recoverable failures. |
| `Log(event)` | Any | Low-level: supply a `LogItem` struct with event type and message directly. |
| `IsEventTypeActive(EventType)` | — | Returns `true` if the given `TLogEventType` is currently being collected. Use to skip expensive message construction. |
| `Notify(eventType, message, title)` | Any | Sends a visible notification to the user in the main application UI in addition to writing to the log. |

**Usage example:**

```csharp
using var loggerCom = ExtensionManagerHelper.Logger();
if (loggerCom.IsNull) throw new Exception("Logger unavailable");

loggerCom.Invoke(log =>
{
    if (log.IsEventTypeActive(TLogEventType.leDebug))
        log.Debug($"Processing operation: {operation.Name}");

    log.Info("Toolpath calculation started");
    log.Warning("No geometry assigned — using defaults");
    log.Error("CLD receiver is null");
    log.Notify(TLogEventType.leInfo, "Export complete", "My Extension");
});
```
