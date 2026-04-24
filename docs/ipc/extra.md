# Additional IPC Interfaces

This page covers three specialised IPC interface groups that go beyond the core `ICamIpcApplication` API: cloud project integration, functional test automation, and PLM connection management.

All three follow the same general pattern as the core IPC layer: obtain the interface through `ICamIpcApplication` (or a cast thereof), pass `ref TExecuteContext` on every call, and check `ResultStatus` afterwards.

---

## CAMIPC.CloudsApp — Cloud Project Integration

Defined in: `CAMIPC.CloudsApp.idl`

These interfaces are used by the ENCY cloud service to push cloud-state changes (sharing status, chat notifications, version staleness) into a running ENCY instance over IPC. They are typically consumed by the cloud client process, not by general plugin code.

### ICloudsIpcApplication

Obtained by casting `ICamIpcApplication` to `ICloudsIpcApplication` (if the running instance supports cloud features).

```csharp
// Cast the connected application to ICloudsIpcApplication (perform the cast inside Invoke
// — never against the raw .Instance, which is not safe across the MTA boundary).
using var cloudAppCom = applicationCom.InvokeAndWrap(app => app as ICloudsIpcApplication);
if (cloudAppCom.IsNull)
    throw new Exception("This ENCY instance does not support cloud integration.");

var ctx = new TExecuteContext();
using var cloudProjectCom = cloudAppCom.InvokeAndWrap(app =>
    app.GetCloudsProject(ref ctx));
if (ctx.ResultStatus.Code == TResultStatusCode.rsError)
    throw new Exception("GetCloudsProject: " + ctx.ResultStatus.Description);
```

`GetCloudsProject` always returns an `ICloudsIpcProject` instance, even if the active project is not currently shared on the cloud.

### ICloudsIpcProject

`ICloudsIpcProject` provides write-only setters that push state from the cloud service into ENCY's UI:

| Method | Description |
|---|---|
| `SetPid(pid, ctx)` | Registers the PID of the cloud client process with ENCY |
| `SetIsShared(isShared, ctx)` | Marks the project as shared (or not) in ENCY's title bar and toolbar |
| `SetIsOutdated(isOutdated, ctx)` | Signals that a newer version is available on the cloud server |
| `SetHasNewMessages(hasNew, ctx)` | Triggers the "new chat messages" indicator in ENCY's UI |
| `SetLastUpdateTime(fileTime, ctx)` | Updates the "last synced" timestamp displayed in ENCY |

```csharp
var ctx = new TExecuteContext();

// Tell ENCY the project is now shared
cloudProjectCom.Invoke(p => p.SetIsShared(true, ref ctx));
if (ctx.ResultStatus.Code == TResultStatusCode.rsError)
    throw new Exception(ctx.ResultStatus.Description);

// Tell ENCY there is a newer version on the server
cloudProjectCom.Invoke(p => p.SetIsOutdated(true, ref ctx));
```

> Note: The IDL comment marks `ICloudsIpcApplication` as a temporary interface pending the cloud subsystem's own dedicated IPC mechanism. Treat it as internal API subject to removal in a future version.

---

## CAMIPC.FunctionalTest — Automated Test Runner

Defined in: `CAMIPC.FunctionalTest.idl`

`IIpcFunctionalTest` exposes the operations needed to drive ENCY's built-in functional test infrastructure from an external .NET test harness. It allows loading projects, running scenarios defined in `.stjob` files, running toolpath simulation, comparing output against reference ("etalon") files, and generating new reference files.

### Obtaining IIpcFunctionalTest

```csharp
// Cast the application to IIpcCamAppTests first
using var testAppCom = applicationCom.InvokeAndWrap(app => app as IIpcCamAppTests);
if (testAppCom.IsNull)
    throw new Exception("This ENCY instance does not expose the test interface.");

var ctx = new TExecuteContext();
using var testCom = testAppCom.InvokeAndWrap(a => a.CreateFunctionalTest(ref ctx));
if (ctx.ResultStatus.Code == TResultStatusCode.rsError)
    throw new Exception("CreateFunctionalTest: " + ctx.ResultStatus.Description);
```

### IIpcFunctionalTest API

```csharp
// Prepare a clean project state
testCom.Invoke(t => t.NewProject(ref ctx));

// Import a geometry or project file
testCom.Invoke(t => t.Import(@"C:\TestData\Part1.igs", ref ctx));

// Run a .stjob scenario file (contains scripted operations, toolpath generation, etc.)
testCom.Invoke(t => t.Run(@"C:\TestData\Scenarios\FaceMilling.stjob", ref ctx));
if (ctx.ResultStatus.Code == TResultStatusCode.rsError)
    throw new Exception("Scenario failed: " + ctx.ResultStatus.Description);

// Simulate the entire toolpath
testCom.Invoke(t => t.SimulateAll(ref ctx));

// Generate a new etalon (reference output) at the given path
testCom.Invoke(t => t.MakeEtalon(@"C:\TestData\Etalons\FaceMilling", ref ctx));

// Compare current output against the etalon; IgnoreItems filters paths to skip
IListString? ignoreItems = null; // pass null to compare everything
testCom.Invoke(t => t.CompareEtalon(@"C:\TestData\Etalons\FaceMilling", ignoreItems, ref ctx));
if (ctx.ResultStatus.Code == TResultStatusCode.rsError)
    throw new Exception("Etalon mismatch: " + ctx.ResultStatus.Description);
```

Additional methods:

| Method | Purpose |
|---|---|
| `SetMultiprocessorMode(bool, ctx)` | Enable/disable parallel toolpath calculation |
| `SetSimulationByGCode(bool, ctx)` | Toggle G-code-based vs. toolpath-based simulation |
| `ResetSimulation(ctx)` | Clear simulation state |
| `SimulateAllSmooth(ctx)` | Run smooth (interpolated) simulation |
| `RunScript(scriptPath, ctx)` | Execute a standalone script file inside ENCY |
| `SaveStcx(ctx)` | Save the current project as `.stcx` |
| `CompareEtalonSources(etalonPath, ignoreItems, ctx)` | Compare source files (XML/JSON operation data) only |
| `CheckActiveProjectHasNoIncorrectChars(ctx)` | Validate project data for encoding issues |

### Typical Test Harness Pattern

```csharp
public static void RunTest(ComWrapper<ICamIpcApplication> applicationCom, string stjobPath, string etalonPath)
{
    using var testAppCom = applicationCom.InvokeAndWrap(app => app as IIpcCamAppTests);
    if (testAppCom.IsNull)
        throw new Exception("Test interface not available.");

    var ctx = new TExecuteContext();
    using var testCom = testAppCom.InvokeAndWrap(a => a.CreateFunctionalTest(ref ctx));
    Check(ctx, "CreateFunctionalTest");

    testCom.Invoke(t => t.NewProject(ref ctx));
    Check(ctx, "NewProject");

    testCom.Invoke(t => t.Run(stjobPath, ref ctx));
    Check(ctx, $"Run({Path.GetFileName(stjobPath)})");

    testCom.Invoke(t => t.SimulateAll(ref ctx));
    Check(ctx, "SimulateAll");

    testCom.Invoke(t => t.CompareEtalon(etalonPath, null, ref ctx));
    Check(ctx, "CompareEtalon");
}

private static void Check(TExecuteContext ctx, string step)
{
    if (ctx.ResultStatus.Code == TResultStatusCode.rsError)
        throw new Exception($"{step} failed: {ctx.ResultStatus.Description}");
}
```

---

## CAMIPC.Extension.PLM — PLM Connection Management

Defined in: `CAMIPC.Extension.PLM.idl`

`IIpcPLMManager` allows an external process to discover PLM integration connections registered in a running ENCY instance. PLM connections link ENCY projects to external Product Lifecycle Management systems (e.g., Teamcenter, Windchill).

### Obtaining IIpcPLMManager

```csharp
using var plmManagerCom = applicationCom.InvokeAndWrap(app => app as IIpcPLMManager);
if (plmManagerCom.IsNull)
    throw new Exception("PLM manager interface not available in this ENCY instance.");
```

### IIpcPLMManager API

```csharp
// Get the IPC instance identifier of this PLM manager
string instanceId = plmManagerCom.Invoke(m => m.GetInstanceId());

var ctx = new TExecuteContext();

// Find the first registered connection for a given PLM extension type
// Returns connection ID string, or empty string if none found
string connectionId = plmManagerCom.Invoke(m =>
    m.FindFirstConnectionByExtensionType("com.example.plm.teamcenter", ref ctx));
if (ctx.ResultStatus.Code == TResultStatusCode.rsError)
    throw new Exception("FindFirstConnectionByExtensionType: " + ctx.ResultStatus.Description);

if (string.IsNullOrEmpty(connectionId))
    throw new Exception("No Teamcenter connection configured in this ENCY instance.");

// Get all connections for a given extension type as a JSON string
string connectionsJson = plmManagerCom.Invoke(m =>
    m.GetConnectionsByExtensionType("com.example.plm.teamcenter", ref ctx));
if (ctx.ResultStatus.Code == TResultStatusCode.rsError)
    throw new Exception("GetConnectionsByExtensionType: " + ctx.ResultStatus.Description);

// Parse connectionsJson with System.Text.Json as needed
```

The `ExtensionTypeId` parameter is the unique identifier string registered by the PLM extension plugin in ENCY's extension registry. It is defined by the PLM extension author and documented in that extension's specification.

`GetConnectionsByExtensionType` returns a JSON-serialised array of connection descriptors. The exact schema depends on the PLM extension type; deserialise with `System.Text.Json.JsonSerializer` or `Newtonsoft.Json` as appropriate for your integration.
