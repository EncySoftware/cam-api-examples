# ENCY CAM API Documentation

This documentation is written for Claude instances helping developers write plugins and integrations for ENCY CAM.

## Start here

**What are you building?**

| Task | Where to start |
|---|---|
| In-process plugin (DLL loaded by ENCY) | [CAMAPI overview](api/overview.md) → [Entry points guide](general/extension-entry-points.md) |
| Standalone external app communicating with ENCY | [CAMIPC overview](ipc/overview.md) → [IPC connection guide](ipc/connection.md) |
| Not sure which API to use | See comparison below |
| **ENCY shows an error or crashes when closing** after my plugin ran — undestroyed-objects dialog, `LostObjects_*.txt`, AV on exit | [COM lifetime → Symptoms](general/com-lifetime.md#symptoms-what-a-missing-using-looks-like) — almost always a missing `using` |

## CAMAPI vs CAMIPC

| | CAMAPI | CAMIPC |
|---|---|---|
| **Deployment** | DLL plugin, loaded by ENCY | Standalone .exe, connects via IPC |
| **Trigger** | User clicks button / lifecycle event | Initiated by external app |
| **Performance** | In-process, direct COM calls | IPC overhead per call |
| **Typical use** | Custom operations, UI extensions, solvers | Automation, integration, testing |
| **Entry point** | `IExtension*` interfaces | `IIpcHelper` → `ICamIpcApplication` |

## Documentation map

### Concepts (read these first)
- [Extension entry points](general/extension-entry-points.md) — all 7 plugin types, when to use each, full C# boilerplate
- [COM lifetime management](general/com-lifetime.md) — ComWrapper, Invoke vs InvokeAndWrap, ListComWrapper, MTA, and what a forgotten `using` looks like at ENCY shutdown
- [Error handling](general/error-handling.md) — TResultStatus, no-exceptions rule, IExtensionLogger
- [UI patterns](general/ui-patterns.md) — Inspector dialog, modal WPF (STA thread), non-modal WPF + IExtensionLazyUnloadable
- [Theming plugin windows](general/theming-plugin-windows.md) — applying the host palette to your own window; WinForms visual-styles pitfalls, scrollbars, title bar

### Writing macros
- [Macros](macros/README.md) — authoring a macro body (`ICamApiMacro.Run`, run context, `MacroParams`, `NotifyMacroStep`). Managing/building/running macros from a host is in [api/application.md](api/application.md#icamapimacromanager) (in-process) and [ipc/application.md](ipc/application.md#icamipcmacromanager) (IPC).

### CAMAPI domain reference
- [Application & singletons](api/application.md) — ICamApiApplication, utilities, macros, events, collections
- [Entry points](api/entry-points.md) — IExtensionManager, IExtensionStorage, IExtensionLogger
- [Project](api/project.md) — ICamApiProject, technologist, tech operations, parts, stages, snapshots
- [Geometry](api/geometry.md) — model tree, entities, faces, curves, mesh, coordinate systems, GeomPicker, sketcher, point snapper
- [Feature finder](api/feature-finder.md) — automatic recognition of holes, pockets, fillets, chamfers, planes, edges
- [Tools & machine](api/tools-machine.md) — machining tools, tool lists, machines, workpiece setup, live machine state
- [NC & simulation](api/nc-simulation.md) — NCMaker, Simulator, CLDReceiver, ModelFormer, TechOperationSolver
- [UI](api/ui.md) — main form, viewport, view cube, dialogs, SimplePropIterator, CamApiInspectorWindow

### CAMIPC domain reference
- [Connection](ipc/connection.md) — DLL loading, IIpcHelper, TExecuteContext, IAsyncResult, event listeners
- [Application](ipc/application.md) — ICamIpcApplication, paths, extension manager, logger, XmlProp
- [Project](ipc/project.md) — differences from CAMAPI project domain
- [Geometry](ipc/geometry.md) — differences from CAMAPI geometry domain
- [Feature finder](ipc/feature-finder.md) — differences from CAMAPI feature-finder domain
- [Tools & machine](ipc/tools-machine.md) — differences from CAMAPI tools domain
- [NC & simulation](ipc/nc-simulation.md) — differences from CAMAPI NC domain
- [UI](ipc/ui.md) — main form, viewport, view cube, PrimeView, PrimeViewModel
- [Extra](ipc/extra.md) — CloudsApp, FunctionalTest, Extension.PLM
- [Macros — record & save over IPC](ipc/macros.md) — MacroManager → builder → commands, recording lifecycle, command schema, guard cases

## Key patterns (quick reference)

### Minimal CAMAPI plugin
```csharp
// 1. Implement IExtension + chosen entry point interface
public class MyPlugin : IExtension, IExtensionUtility {
    public IExtensionInfo? Info { get; set; }

    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus) {
        resultStatus = default;
        try {
            using var appCom = ComWrapper.Create(context.CamApplication);
            using var projectCom = appCom.GetActiveProject();
            // your logic here
        } catch (Exception e) {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}

// 2. Register in extension.json (see general/extension-entry-points.md for full format)
```

### Minimal CAMIPC connection
```csharp
// Load helper DLL and connect (see ipc/connection.md for full details)
var helper = IpcHelperLoader.Load("CAMIPC.Helper.Cam.dll");
var context = new TExecuteContext { Timeout = 5000 };
using var appCom = helper.ConnectToRunningInstance(context);
```

### COM lifetime rule
```csharp
// Always use 'using' — never store COM objects in fields without explicit lifetime control
using var projectCom = appCom.GetActiveProject();   // correct
var project = appCom.GetActiveProject();             // WRONG — leaks COM reference
```
`ComWrapper<T>` has no finalizer: an undisposed wrapper is never released, and the host reports it (or crashes) when ENCY closes — see [Symptoms](general/com-lifetime.md#symptoms-what-a-missing-using-looks-like).
