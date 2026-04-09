# Connecting a .NET Application to a Running ENCY Instance

This guide covers connecting a standalone .NET process (console app, WPF app, etc.) to a live ENCY instance via IPC. For in-process plugins loaded by ENCY itself, see the extension entry points guide; the IPC layer is only needed for out-of-process automation.

Reference example: `ApplicationNet/CreateOperations/project/main/CamHelper.cs`

---

## Overview

The IPC bridge is provided by `CAMIPC.Helper.Cam.dll`, which ships in the ENCY installation folder. The DLL exposes a single C export, `CreateHelper`, that returns an `IIpcHelper` COM interface. From there you enumerate running ENCY instances, pick one, and receive an `ICamIpcApplication` that gives access to the full CAM API over a socket-based IPC channel.

Every call across the IPC channel takes a `ref TExecuteContext` argument. You must check `executeContext.ResultStatus.Code` after each call.

---

## Step 1: Load the DLL and Create IIpcHelper

```csharp
using System.IO;
using System.Runtime.InteropServices;
using CAMAPI.DotnetHelper;
using CAMIPC.Helper;
using CAMHelper.NativeLibUtils;

// Adjust path to match the installed ENCY version
const string camFolder = @"C:\Program Files\ENCY Software\ENCY 2\Bin64";
var helperPath = Path.Combine(camFolder, "CAMIPC.Helper.Cam.dll");

if (!File.Exists(helperPath))
    throw new FileNotFoundException($"CAMIPC.Helper.Cam.dll not found at: {helperPath}");

// Load the native DLL
var helperDllPtr = NativeLibLoader.LoadDll(helperPath, out var loadResult);
if (helperDllPtr == IntPtr.Zero || loadResult != 0)
    throw new Exception($"Failed to load CAMIPC.Helper.Cam.dll: error code {loadResult}");

// Bind the CreateHelper export
delegate IntPtr CreateHelperDelegate();
var createHelper = NativeLibLoader.GetProc<CreateHelperDelegate>(helperDllPtr, "CreateHelper");

// Obtain IIpcHelper — wrap immediately for deterministic release
var helperCom = new ComWrapper<IIpcHelper>(createHelper());
```

`NativeLibLoader` is a thin P/Invoke wrapper provided in the ENCY helper NuGet package. `FreeDll` must be called on the pointer when the application exits (see the `CamHelper` dispose chain below).

---

## Step 2: Enumerate Running Instances

`IIpcHelper.GetRunningCamAppList` returns an `ICamIpcListApplication`. Each element is an `ICamIpcApplication` representing one running ENCY process.

```csharp
using CAMIPC.ExecuteContext;
using CAMAPI.ResultStatus;

var executeContext = new TExecuteContext();

using var instancesCom = helperCom.InvokeAndWrap(helper =>
    helper.GetRunningCamAppList(ref executeContext));

if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
    throw new Exception(executeContext.ResultStatus.Description);

// Get the first available instance (index 0)
var applicationCom = instancesCom.InvokeAndWrap(instances =>
{
    if (instances.Count == 0)
        throw new Exception("No running ENCY instance found. Start ENCY first.");
    return instances.Get(0, executeContext);
});

if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
    throw new Exception(executeContext.ResultStatus.Description);
```

If you need a specific instance (e.g., by process ID), use `IIpcHelper.GetRunningCamApp(processId, ref executeContext)` instead.

---

## Step 3: The CamHelper Wrapper Class

In practice, wrap the entire connection lifecycle in a disposable class. The dispose order is critical: the application COM object must be released before the helper, and the helper before the DLL is unloaded.

```csharp
using System;
using System.IO;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using CAMHelper.NativeLibUtils;
using CAMIPC.Application;
using CAMIPC.ExecuteContext;
using CAMIPC.Helper;

public class CamHelper : IDisposable
{
    private readonly ComWrapper<IIpcHelper> _helper;
    private readonly ComWrapper<ICamIpcApplication>? _application;
    private readonly IntPtr _helperDllPtr;

    private delegate IntPtr CreateHelperDelegate();

    public ComWrapper<ICamIpcApplication> GetApplication() =>
        _application ?? throw new Exception("ENCY application not connected");

    public CamHelper()
    {
        const string camFolder = @"C:\Program Files\ENCY Software\ENCY 2\Bin64";
        var helperPath = Path.Combine(camFolder, "CAMIPC.Helper.Cam.dll");
        if (!File.Exists(helperPath))
            throw new FileNotFoundException($"CAMIPC.Helper.Cam.dll not found: {helperPath}");

        _helperDllPtr = NativeLibLoader.LoadDll(helperPath, out var loadResult);
        if (_helperDllPtr == IntPtr.Zero || loadResult != 0)
            throw new Exception($"Failed to load helper DLL: error code {loadResult}");

        var proc = NativeLibLoader.GetProc<CreateHelperDelegate>(_helperDllPtr, "CreateHelper");
        _helper = new ComWrapper<IIpcHelper>(proc());

        var executeContext = new TExecuteContext();

        using var instancesCom = _helper.InvokeAndWrap(h =>
            h.GetRunningCamAppList(ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);

        _application = instancesCom.InvokeAndWrap(instances =>
        {
            if (instances.Count == 0)
                throw new Exception("No running ENCY instance found.");
            return instances.Get(0, executeContext);
        });
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
    }

    public void Dispose()
    {
        // Dispose order matters: application before helper, helper before DLL
        _application?.Dispose();
        _helper.Dispose();
        NativeLibLoader.FreeDll(_helperDllPtr);
    }
}
```

Usage:

```csharp
// In a WPF application (STA), set this before constructing CamHelper
ComWrapperSettings.ApplicationApartmentState = ApartmentState.STA;

using var cam = new CamHelper();
var executeContext = new TExecuteContext();

using var projectCom = cam.GetApplication().InvokeAndWrap(app =>
    app.GetActiveProject(ref executeContext));
if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
    throw new Exception(executeContext.ResultStatus.Description);
```

---

## TExecuteContext

`TExecuteContext` is a struct that accompanies every IPC call. It carries two fields:

```
TExecuteContext
├── ExecuteSettings: TExecuteSettings
│   ├── ThreadHandle: integer   // 0 = main thread (default)
│   └── Timeout: integer        // 0 = use ENCY default timeout
└── ResultStatus: TResultStatus
    ├── Code: TResultStatusCode // rsOk | rsError
    └── Description: string     // error message when Code == rsError
```

**Always create a fresh `TExecuteContext` (or reset it) before a group of related calls, and check `ResultStatus.Code` after each call that can fail:**

```csharp
var executeContext = new TExecuteContext();
// Optionally configure: executeContext.ExecuteSettings.Timeout = 30000; // 30 s

using var projectCom = applicationCom.InvokeAndWrap(app =>
    app.GetActiveProject(ref executeContext));

if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
    throw new Exception("GetActiveProject failed: " + executeContext.ResultStatus.Description);
```

`ThreadHandle` selects which thread inside ENCY processes the call. The default (0) is the main thread. Most API calls must run on the main thread.

---

## IAsyncResult

Some long-running IPC operations return `IAsyncResult` instead of completing synchronously. This allows you to start the operation and poll or wait for completion.

```csharp
// IAsyncResult interface:
//   WaitFor(timeout: int) -> bool        — waits up to `timeout` ms; returns true if done
//   GetResult(timeout: int) -> IUnknown* — waits and returns the COM result object
//   GetResultStatus(timeout: int) -> TResultStatus — waits and returns the status
```

Typical pattern:

```csharp
using var asyncResultCom = someOperationCom.InvokeAndWrap(op =>
    op.StartLongRunningOperation(ref executeContext));

// Wait up to 60 seconds
bool completed = asyncResultCom.Invoke(ar => ar.WaitFor(60_000));
if (!completed)
    throw new TimeoutException("Operation did not complete within 60 s");

// Retrieve the status
var status = asyncResultCom.Invoke(ar => ar.GetResultStatus(0));
if (status.Code == TResultStatusCode.rsError)
    throw new Exception("Async operation failed: " + status.Description);
```

Use async calls for operations that block the ENCY main thread for extended periods (toolpath calculation, simulation, G-code generation). For quick property reads and object traversal, synchronous calls are preferred.

---

## ICamIpcEventListener — Subscribing to ENCY Events

To receive events from a running ENCY instance, register an event listener through `IIpcEventListenerManager`. ENCY sends events over a socket to the port your listener opens.

The general pattern:

1. Obtain `IIpcEventListenerManager` from the interaction layer.
2. Call `RegisterListener(listenerIdent)` — this opens a local socket and returns an `ICamIpcEventListener` whose `Port` property holds the listening port number.
3. Call `RegisterHandler(senderInstanceIdent, eventHeader, handlerIdent, handler, listener)` — `handler` is your implementation of `ICamIpcEventHandler`.
4. Implement `ICamIpcEventHandler.GetTimeOut(interfaceUid)` — return `-1` for async (fire-and-forget) or `0` for infinite wait. Positive values specify a millisecond timeout.

```csharp
// ICamIpcEventHandler implementation
public class MyEventHandler : ICamIpcEventHandler
{
    public int GetTimeOut(string interfaceUid) => -1; // async, ENCY doesn't wait for us

    public void OnEvent(string eventIdent, string paramsJson)
    {
        Console.WriteLine($"Event received: {eventIdent}, params: {paramsJson}");
    }
}
```

Listener registration and unregistration must be paired. Always call `UnregisterHandler` and `UnregisterListener` when shutting down, otherwise the ENCY process may hold a connection open.

---

## Error Handling in IPC

IPC errors surface through `TExecuteContext.ResultStatus` — the same `TResultStatus` used everywhere in the API. Check it after every call:

```csharp
private static void CheckStatus(TExecuteContext ctx, string callName)
{
    if (ctx.ResultStatus.Code == TResultStatusCode.rsError)
        throw new Exception($"{callName} failed: {ctx.ResultStatus.Description}");
}

// Usage:
var ctx = new TExecuteContext();
using var projCom = applicationCom.InvokeAndWrap(app => app.GetActiveProject(ref ctx));
CheckStatus(ctx, nameof(ICamIpcApplication.GetActiveProject));
```

Common failure causes over IPC:
- ENCY process exited between `GetRunningCamAppList` and the actual call — the call returns `rsError` with a connection error description.
- Timeout exceeded — increase `ExecuteContext.ExecuteSettings.Timeout`.
- Calling on the wrong thread handle — use the default (0) unless you have a specific reason to target another thread.

---

## Launching a New ENCY Instance

Instead of connecting to an existing instance, you can launch a new one:

```csharp
// Console mode (headless, no window)
using var appCom = helperCom.InvokeAndWrap(h =>
    h.RunNewConsoleApp("", ref executeContext));

// Window mode (with UI, hosted in a Prime window)
using var appCom = helperCom.InvokeAndWrap(h =>
    h.RunNewWinApp("", ref executeContext));
```

Both methods block until the ENCY process has started and is ready to accept IPC calls. The returned `ICamIpcApplication` is ready for use immediately.

To shut down a process you started:

```csharp
int processId = appCom.Invoke(app => app.ProcessId);
helperCom.Invoke(h => h.KillCamApp(processId, ref executeContext));
```
