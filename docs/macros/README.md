# Writing a macro

A **macro** is a small extension that ENCY builds and runs on demand. The default language is **.NET** (a compiled DLL); a **script** language is also available. Either way the macro implements one method, `ICamApiMacro.Run`, and receives a live application context to do its work.

This folder is **only about authoring the macro body** — the code that runs *inside* ENCY. It does **not** cover the actual CAM operations you call from that code: for those (operations, geometry, tools, NC, …) use the regular API reference.

| If you want to… | Read |
|---|---|
| Understand the macro class and run context | this file |
| Write the `Run` body and call CAM commands | [writing-macro.md](writing-macro.md) |
| Let the user override values before running | [macro-params.md](macro-params.md) |
| Report progress / enable step highlighting | [notify-step.md](notify-step.md) |
| **Call actual CAM commands** (operations, geometry, …) | [../api/](../api/) — the regular API docs (start at [project.md](../api/project.md), [geometry.md](../api/geometry.md), …) |
| Manage / build / run macros from a host plugin or external app | [../api/application.md](../api/application.md#icamapimacomanager) (in-process) · [../ipc/application.md](../ipc/application.md#icamipcmacromanager) (IPC) |
| COM object lifetime (`ComWrapper`, MTA) | [../general/com-lifetime.md](../general/com-lifetime.md) |
| Returning errors across the COM boundary | [../general/error-handling.md](../general/error-handling.md) |

## Anatomy of a macro

A built macro is a normal extension DLL with two classes:

- **`ExtensionFactory`** — must be in the `CAMAPI` namespace and named `ExtensionFactory`; ENCY finds it by convention and asks it to create the macro instance.
- **The macro class** — implements `IExtension` + `ICamApiMacro`. Its `Run` method is the entry point.

```csharp
namespace CAMAPI;

public class ExtensionFactory : IExtensionFactory
{
    public void OnLibraryRegistered(IExtensionFactoryContext context, out TResultStatus ret) => ret = default;
    public void OnLibraryUnRegistered(IExtensionFactoryContext context, out TResultStatus ret) => ret = default;

    public IExtension? Create(string extensionIdent, out TResultStatus ret)
    {
        ret = default;
        if (extensionIdent == "ExtensionDummy")
            return new ExtensionDummy();
        throw new Exception("Unknown extension identifier: " + extensionIdent);
    }
}
```

The macro itself:

```csharp
public class ExtensionDummy : IExtension, ICamApiMacro
{
    public IExtensionInfo? Info { get; set; }

    public void Run(ICamApiMacroRunContext context, out TResultStatus ret)
    {
        ret = default;
        try
        {
            if (context.CamApplication is not ICamApiApplication application)
                throw new Exception("Invalid application");

            MacroParams.Initialize(context.Params);                 // see macro-params.md
            using var applicationCom = ComWrapper.Create(application);
            // ... your work here — call the regular API (see ../api/) ...
        }
        catch (Exception e)
        {
            ret.Code = TResultStatusCode.rsError;
            ret.Description = e.Message;
        }
    }
}
```

## The run context — `ICamApiMacroRunContext`

| Member | Description |
|---|---|
| `CamApplication` (`IUnknown`) | The live `ICamApiApplication`. Wrap it in `ComWrapper.Create(...)` — this is your gateway to everything (project, technologist, geometry, tools, NC). |
| `Params` (`string`) | JSON of user-supplied per-step value overrides. Pass it to `MacroParams.Initialize` — see [macro-params.md](macro-params.md). |

## Generated project layout (when you edit a built macro)

When a macro is created via the builder, ENCY generates a small project:

- `Extension.cs` — the macro class (`Run` body).
- `ExtensionFactory.cs` — the factory above.
- a global-usings file — brings in `CAMAPI.Extensions`, `CAMAPI.ResultStatus`, `CAMAPI.Macros`, `CAMAPI.Application`, `CAMAPI.DotnetHelper`, etc., so the body needs no explicit `using`s.
- a `.csproj` — its `TargetFramework`, `SDKVersion` and reference assemblies are filled by the builder. **Do not hand-edit the references** — if they are wrong the project will not resolve CAMAPI types. (Builders set them from the extension manager; see [../api/application.md](../api/application.md#icamapimacomanager).)

If you edit `Extension.cs` by hand, the two macro-specific things to keep in mind are **`MacroParams`** ([macro-params.md](macro-params.md)) and **`NotifyMacroStep`** ([notify-step.md](notify-step.md)); for everything else (the commands you call) consult the regular API docs in [../api/](../api/).
