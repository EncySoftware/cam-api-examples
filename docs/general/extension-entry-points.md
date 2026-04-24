# ENCY CAM API — Extension Entry Points

ENCY loads plugins from managed .NET DLLs. Each DLL exposes a single factory class (`CAMAPI.ExtensionFactory`) that ENCY finds by reflection. When ENCY needs an extension instance it calls `ExtensionFactory.Create(extensionIdent)`, where `extensionIdent` is the unique string registered in the extension's JSON config file.

Every plugin class must implement **`IExtension`** (to carry metadata) plus exactly one **role interface** that determines when and how ENCY invokes it. The role interface is called the *entry point*.

---

## Choosing an Entry Point

| Goal | Entry point |
|---|---|
| Run code on CAM startup / shutdown | `IExtensionGlobal` |
| Add a button to the toolbar / utilities menu | `IExtensionUtility` |
| Intercept or customise how a utility is executed | `IExtensionUtilityRunner` |
| Add items to the right-click menu on a tech operation | `IExtensionOperationPopup` |
| Add items to the right-click menu on a geometry tree node | `IExtensionGeomModelNodePopup` |
| Implement a custom toolpath calculation algorithm | `ICamApiTechOperationSolver` |
| Post-process (transform) CLD toolpath data after calculation | `IExtensionGeomCLDataConverter` |

---

## Entry Points Reference

### IExtensionTypeInfoGlobal → IExtensionGlobal

**Use when:** you need to execute code once when ENCY starts (`OnSCInitializing`) and once when it shuts down (`OnSCFinalizing`). Typical uses: register background services, subscribe to application-level events, initialise caches.

**Context received:** none passed directly. Use `ICamApiApplicationSingleton` (obtained via the singleton mechanism) to reach `ICamApiApplication` and all its children.

**What you can access:** the full application object — active project, extension manager, tools manager, machines library, utility manager, macro manager.

**Implementation:**

```csharp
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace MyPlugin;

public class MyGlobalExtension : IExtension, IExtensionGlobal
{
    public IExtensionInfo? Info { get; set; }

    public TResultStatus OnSCInitializing()
    {
        TResultStatus status = default;
        try
        {
            // Initialisation logic here.
            // Reach the application via ICamApiApplicationSingleton if needed.
        }
        catch (Exception e)
        {
            status.Code = TResultStatusCode.rsError;
            status.Description = e.Message;
        }
        return status;
    }

    public TResultStatus OnSCFinalizing()
    {
        TResultStatus status = default;
        try
        {
            // Cleanup logic here.
        }
        catch (Exception e)
        {
            status.Code = TResultStatusCode.rsError;
            status.Description = e.Message;
        }
        return status;
    }
}
```

**Example:** [`ExtensionGlobal/ExtensionGlobalNet/project/main/ExtensionGlobal.cs`](../../ExtensionGlobal/ExtensionGlobalNet/project/main/ExtensionGlobal.cs)

---

### IExtensionTypeInfoUtility → IExtensionUtility

**Use when:** you want to add a button to the ENCY utilities menu. When the user clicks the button, `Run` is called with the full application context.

**Context received:** `IExtensionUtilityContext` — contains `CamApplication` (`ICamApiApplication`).

**What you can access:** active project (`GetActiveProject`), extension manager (`GetExtensionManager`), main window handle, tools manager, machines library, utility manager.

**JSON type info fields:** `Caption` (button label), `HintText`, `IconPath` (relative to DLL).

**Implementation:**

```csharp
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.Project;
using CAMAPI.ResultStatus;

namespace MyPlugin;

public class MyUtilityExtension : IExtension, IExtensionUtility
{
    public IExtensionInfo? Info { get; set; }

    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            using var appCom = ComWrapper.Create(context.CamApplication);
            using var projectCom = appCom.GetActiveProject();
            if (projectCom.IsNull)
                throw new Exception("No active project");

            // Work with the project through helper extensions / Invoke.
            Console.WriteLine(projectCom.FilePath());
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
```

> **Direct access (IDL):** `IExtensionUtility.Run(IExtensionUtilityContext* Context, out TResultStatus)`

**Example:** [`ExtensionEmpty/ExtensionEmptyNet/project/main/ExtensionTest.cs`](../../ExtensionEmpty/ExtensionEmptyNet/project/main/ExtensionTest.cs)

---

### IExtensionTypeInfoUtilityRunner → IExtensionUtilityRunner

**Use when:** you need to intercept or wrap the execution of another utility — for example, to provide a custom file selector, add pre- or post-processing, or redirect execution to a different handler.

**Context received:** `IExtensionUtilityRunnerContext` — contains:
- `UtilButtonContext` (`IUtilButtonContext`) — properties of the utility button being run.
- `UtilityContext` (`IExtensionUtilityContext`) — the standard utility context (application instance).

**What you can access:** everything available through `IExtensionUtilityContext.CamApplication`, plus button metadata.

**JSON type info fields:** `FilterTypes` (file filter string for the file selector dialog), `VisibleInEditor` (whether the runner appears in the utility editor).

**Implementation:**

```csharp
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace MyPlugin;

public class MyUtilityRunner : IExtension, IExtensionUtilityRunner
{
    public IExtensionInfo? Info { get; set; }

    public void Execute(IExtensionUtilityRunnerContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            var app = context.UtilityContext.CamApplication;
            // Intercept or augment the utility run here.
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
```

> **Direct access (IDL):** `IExtensionUtilityRunner.Execute(IExtensionUtilityRunnerContext* Context, out TResultStatus)`

---

### IExtensionTypeInfoOperationPopup → IExtensionOperationPopup

**Use when:** you want to add custom items to the right-click context menu that appears on a technology operation in the ENCY technology tree.

**Context received:** `IExtensionOperationPopupBuildContext` — contains:
- `SelectedOperation` (`ICamApiTechOperation*`) — the operation that was right-clicked.
- `ActiveProject` (`ICamApiProject*`) — the current project.
- `OperationPopup` (`ICamApiTechnologyFormOperationPopup*`) — the popup builder; call `AddItem` to add entries.

**What you can access:** operation properties (name, type, XML params, tool, machine, LCS, flags), the active project tree, and the popup menu builder.

**Implementation:**

```csharp
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.TechnologyForm;

namespace MyPlugin;

public class MyOperationPopup : IExtension, IExtensionOperationPopup
{
    public IExtensionInfo? Info { get; set; }

    public TResultStatus Build(IExtensionOperationPopupBuildContext context)
    {
        TResultStatus status = default;
        try
        {
            var popup = context.OperationPopup;
            var op = context.SelectedOperation;

            popup.AddItem(
                name: "my_action",
                caption: "My Action",
                enabled: true,
                onClicked: new MyClickHandler(),
                resultStatus: out _);
        }
        catch (Exception e)
        {
            status.Code = TResultStatusCode.rsError;
            status.Description = e.Message;
        }
        return status;
    }
}
```

> **Direct access (IDL):** `IExtensionOperationPopup.Build(IExtensionOperationPopupBuildContext* Context, out TResultStatus)`

**Example:** [`ExtensionOperationPopup/ExtensionOperationPopupNet/project/main/ExtensionOperationPopup.cs`](../../ExtensionOperationPopup/ExtensionOperationPopupNet/project/main/ExtensionOperationPopup.cs)

---

### IExtensionTypeInfoGeomModelNodePopup → IExtensionGeomModelNodePopup

**Use when:** you want to add custom items to the right-click context menu on a node in the 3D geometry model tree.

**Context received:** `IExtensionGeomModelNodePopupBuildContext` — contains:
- `SelectedNode` (`ICAMAPIGeometryTreeNode*`) — the geometry tree node that was right-clicked.
- `ActiveProject` (`ICamApiProject*`) — the current project.
- `NodePopup` (`ICamApiGeomModelNodePopup*`) — the popup builder; call `AddItem` to add entries.

**What you can access:** geometry tree node properties, the active project, and the popup menu builder.

**Implementation:**

```csharp
using CAMAPI.Extensions;
using CAMAPI.GeometryModelForm;
using CAMAPI.ResultStatus;

namespace MyPlugin;

public class MyGeomNodePopup : IExtension, IExtensionGeomModelNodePopup
{
    public IExtensionInfo? Info { get; set; }

    public TResultStatus Build(IExtensionGeomModelNodePopupBuildContext context)
    {
        TResultStatus status = default;
        try
        {
            context.NodePopup.AddItem(
                name: "show_full_name",
                caption: "Show Full Name",
                enabled: true,
                onClicked: new MyNodeClickHandler(),
                resultStatus: out _);
        }
        catch (Exception e)
        {
            status.Code = TResultStatusCode.rsError;
            status.Description = e.Message;
        }
        return status;
    }
}
```

> **Direct access (IDL):** `IExtensionGeomModelNodePopup.Build(IExtensionGeomModelNodePopupBuildContext* Context, out TResultStatus)`

**Example:** [`GeometryModelNodePopup/NodeFullNameAlert/project/main/ExtensionGeomModelNodePopup.cs`](../../GeometryModelNodePopup/NodeFullNameAlert/project/main/ExtensionGeomModelNodePopup.cs)

---

### IExtensionTypeInfoOperationSolver → ICamApiTechOperationSolver

**Use when:** you are implementing a custom toolpath calculation algorithm for one or more operation types. ENCY calls `MakeWorkPath` to generate the toolpath and `GetPropIterator` to expose operation parameters in the UI.

**Context received (MakeWorkPath):**
- `cldFormer` (`ICamApiCLDReceiver*`) — the CLD output sink; call `CutTo`, `ArcTo2d`, `OutStandardFeed`, `AddComment`, etc. to emit toolpath commands.
- `TechOperation` (`ICamApiTechOperation*`) — the operation being calculated; read parameters from `XMLProp`.

**Context received (InitSolver):**
- `ICamApiTechOperationSolverInitializeContext` — carries the operation and a progress update handler.

**What you can access:** all operation properties via `ICamApiTechOperation`, the full XML parameter tree, the machine, tool, LCS, and model formers. Log through `ExtensionManagerHelper.GetInstance()`.

**Implementation:**

```csharp
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.MCDFormerTypes;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;
using STCustomPropTypes;

namespace MyPlugin;

public class MyOperationSolver : IExtension, ICamApiTechOperationSolver
{
    public IExtensionInfo? Info { get; set; }

    public void InitSolver(ICamApiTechOperationSolverInitializeContext context,
        out TResultStatus resultStatus)
    {
        resultStatus = default;
        // Store context.TechOperation if needed for GetPropIterator.
    }

    public void FinalizeSolver()
    {
        // Release any stored references.
    }

    public bool GetPropIterator(string pageId,
        out IST_CustomPropIterator? iterator,
        out TResultStatus resultStatus)
    {
        resultStatus = default;
        iterator = null;
        return false; // Return true and populate iterator to expose UI parameters.
    }

    public void OnPropFilterChanged(string parameterName, string value) { }

    public void MakeWorkPath(ICamApiCLDReceiver cldFormer,
        ICamApiTechOperation techOperation,
        out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            using var xmlPropCom = ComWrapper.Create(techOperation.XMLProp);
            double depth = xmlPropCom.Invoke(xp => xp.Flt["MyParams.Depth"]);

            // Emit toolpath commands:
            cldFormer.OutStandardFeed((int)TFeedTypeFlag.affRapid);
            cldFormer.CutTo(new TST3DPoint { X = 0, Y = 0, Z = 10 });
            cldFormer.OutStandardFeed((int)TFeedTypeFlag.affWorking);
            cldFormer.CutTo(new TST3DPoint { X = 0, Y = 0, Z = -depth });
            // ... more moves ...
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
```

> **Direct access (IDL):** `ICamApiTechOperationSolver.MakeWorkPath(ICamApiCLDReceiver*, ICamApiTechOperation*, out TResultStatus)`

**Example:** [`Operation/ExtensionOperationSimpleNet/project/main/ExtensionOperationSimpleNet.cs`](../../Operation/ExtensionOperationSimpleNet/project/main/ExtensionOperationSimpleNet.cs)

---

### IExtensionTypeInfoGeomCLDataConverter → IExtensionGeomCLDataConverter

**Use when:** you want to intercept and transform CLD toolpath data after it has been calculated by the solver, before it reaches the next stage of the pipeline. Common uses: add custom CLData records, filter moves, apply coordinate transformations.

**Context received (GetCLDReceiverWrapper):**
- `TechOperation` (`ICamApiTechOperation*`) — the operation whose toolpath is being converted.
- `Receiver` (`ICamApiCLDReceiver*`) — the downstream CLD receiver that your wrapper must delegate to.

**Return value:** a new `ICamApiCLDReceiver` implementation that wraps the supplied `Receiver`. ENCY feeds all CLD commands to the returned wrapper.

**JSON type info fields (IExtensionTypeInfoGeomCLDataConverter):**
- `FilterType` — `"all"` to apply to every operation, `"exact"` to apply only to operations listed in `OperationTypes`.
- `OperationTypes` — list of operation type identifiers (used when `FilterType` is `"exact"`).

**Implementation:**

```csharp
using CAMAPI.Extensions;
using CAMAPI.MCDFormerTypes;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;

namespace MyPlugin;

public class MyCLDataConverter : IExtension, IExtensionGeomCLDataConverter
{
    public IExtensionInfo? Info { get; set; }

    private ICamApiCLDReceiver? _wrapper;

    public ICamApiCLDReceiver? GetCLDReceiverWrapper(
        ICamApiTechOperation operation,
        ICamApiCLDReceiver receiver,
        out TResultStatus ret)
    {
        ret = default;
        try
        {
            if (receiver == null)
                throw new Exception("Receiver is null");
            _wrapper = new MyCLDReceiverWrapper(receiver); // your wrapper class
            return _wrapper;
        }
        catch (Exception e)
        {
            ret.Code = TResultStatusCode.rsError;
            ret.Description = e.Message;
            return null;
        }
    }

    public void FinalizeConverter()
    {
        if (_wrapper is IDisposable d)
        {
            d.Dispose();
            _wrapper = null;
        }
    }
}
```

> **Direct access (IDL):** `IExtensionGeomCLDataConverter.GetCLDReceiverWrapper(ICamApiTechOperation*, ICamApiCLDReceiver*, out TResultStatus, return ICamApiCLDReceiver*)`

**Example:** [`CLData/ExtensionGeomCLDataConverterNet/project/main/ExtensionGeomCLDataConverter.cs`](../../CLData/ExtensionGeomCLDataConverterNet/project/main/ExtensionGeomCLDataConverter.cs)

---

## IExtensionLazyUnloadable

If your extension opens a window or holds an external resource that must remain alive until explicitly closed, implement `IExtensionLazyUnloadable` in addition to the role interface.

```csharp
public class MyWindowExtension : IExtension, IExtensionUtility, IExtensionLazyUnloadable
{
    public IExtensionInfo? Info { get; set; }

    // Set to false while a window is open; ENCY will not unload the extension.
    public bool CanUnload { get; set; } = true;

    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        CanUnload = false;
        // Open window; set CanUnload = true when it closes.
    }
}
```

ENCY checks `CanUnload` before unloading the extension library. When `CanUnload` is `false`, unloading is deferred until `CanUnload` becomes `true` or the application terminates.

---

## ExtensionFactory Boilerplate

Every library must expose a class named exactly `ExtensionFactory` in namespace `CAMAPI`. This is a hard requirement — ENCY locates the factory by this fully-qualified name.

```csharp
// Namespace and class name must be exactly CAMAPI.ExtensionFactory.
namespace CAMAPI;

using Extensions;
using ResultStatus;

public class ExtensionFactory : IExtensionFactory
{
    public void OnLibraryRegistered(IExtensionFactoryContext context, out TResultStatus ret)
    {
        ret = default;
        // Optional: read context.Constants or context.Paths at registration time.
    }

    public void OnLibraryUnRegistered(IExtensionFactoryContext context, out TResultStatus ret)
    {
        ret = default;
    }

    public IExtension? Create(string extensionIdent, out TResultStatus ret)
    {
        try
        {
            ret = default;
            return extensionIdent switch
            {
                "My.Extension.Ident.One" => new MyFirstExtension(),
                "My.Extension.Ident.Two" => new MySecondExtension(),
                _ => throw new Exception("Unknown extension identifier: " + extensionIdent)
            };
        }
        catch (Exception e)
        {
            ret.Code = TResultStatusCode.rsError;
            ret.Description = e.Message;
            return null;
        }
    }
}
```

A single DLL can contain multiple extensions. Each gets its own unique `extensionIdent` matched by the `switch` above.

---

## Extension JSON Registration

Each extension library is described by a JSON config file that is registered in an ENCY storage (see `IExtensionManager.RegisterLibrary`). The JSON specifies:

- The path to the DLL (relative or absolute).
- Metadata for each extension: its unique `id` string, `name`, `version`, `group`, and entry-point-specific fields (e.g. `Caption` and `IconPath` for utilities, `FilterType` for CLData converters).

The `id` field in the JSON must match exactly the string passed to `IExtensionFactory.Create`. ENCY uses the `group` field to filter which extensions are loaded for a given context (e.g. the utility runner looks for extensions whose group is `Extension.Util.Common`).
