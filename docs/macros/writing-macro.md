# Writing the macro body

Everything a macro does happens inside `ICamApiMacro.Run`. You get the live application; from there you call the **regular CAM API** exactly as any in-process extension would.

```csharp
public void Run(ICamApiMacroRunContext context, out TResultStatus ret)
{
    ret = default;
    try
    {
        if (context.CamApplication is not ICamApiApplication application)
            throw new Exception("Invalid application");

        MacroParams.Initialize(context.Params);                 // overridable values — macro-params.md
        using var applicationCom = ComWrapper.Create(application);

        // --- your work: call the regular API ---
        using var projectCom      = applicationCom.GetActiveProject();
        using var technologistCom = projectCom.Technologist();
        // ...
    }
    catch (Exception e)
    {
        // Never let an exception cross the COM boundary — report via TResultStatus.
        ret.Code = TResultStatusCode.rsError;
        ret.Description = e.Message;
    }
}
```

## Rules

- **Wrap the application** in `ComWrapper.Create(application)` and use `using` for every COM object you obtain. See [../general/com-lifetime.md](../general/com-lifetime.md).
- **Never throw across the COM boundary.** Catch everything and set `ret` (`TResultStatus`). See [../general/error-handling.md](../general/error-handling.md).
- **The actual commands** (create operations, edit geometry, assign tools, run NC, …) are the regular API — they are **not** documented here. Find them in [../api/](../api/): [project.md](../api/project.md), [geometry.md](../api/geometry.md), [tools-machine.md](../api/tools-machine.md), [nc-simulation.md](../api/nc-simulation.md).

## Worked example — create an operation

```csharp
public void Run(ICamApiMacroRunContext context, out TResultStatus ret)
{
    ret = default;
    try
    {
        if (context.CamApplication is not ICamApiApplication application)
            throw new Exception("Invalid application");
        MacroParams.Initialize(context.Params);

        using var applicationCom  = ComWrapper.Create(application);
        using var macroManagerCom = applicationCom.MacroManager();
        using var projectCom      = applicationCom.GetActiveProject();
        using var technologistCom = projectCom.Technologist();

        macroManagerCom.NotifyMacroStep(0);                      // report progress — notify-step.md
        using var operationCom = technologistCom.CreateOperation("HoleMachiningOp");

        // Read an overridable value (falls back to the default if the user did not change it):
        var safeZ = MacroParams.GetFlt(0, "value", 5.0);
        // ... apply safeZ to the operation via its XML properties (see ../api/project.md) ...
    }
    catch (Exception e)
    {
        ret.Code = TResultStatusCode.rsError;
        ret.Description = e.Message;
    }
}
```

> `CreateOperation`, `Technologist`, `GetActiveProject` and the operation-property APIs are documented in [../api/project.md](../api/project.md). This page only shows the macro scaffolding around them.
