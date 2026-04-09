# Error Handling in ENCY CAM API .NET Code

---

## TResultStatus

`TResultStatus` is the fundamental error type used throughout the ENCY CAM API. It is a plain struct with two fields:

```csharp
public struct TResultStatus
{
    public TResultStatusCode Code;        // rsOk (0) or rsError (1)
    public string Description;            // human-readable error message; empty on success
}

public enum TResultStatusCode
{
    rsOk    = 0,
    rsError = 1
}
```

Every method that can fail returns a `TResultStatus` either as an `out` parameter or as the return value. There are no exceptions thrown across the COM boundary — .NET exceptions cannot be marshalled to Delphi COM callers.

---

## The Fundamental Rule: No Exceptions Across COM Boundaries

ENCY calls into your plugin through COM. If your code lets an unhandled exception escape from a COM-exported method, the behaviour is undefined — at best the exception is silently swallowed, at worst it corrupts the host process.

**Every COM entry point must catch all exceptions and encode them into the `out TResultStatus` parameter.**

The standard pattern for a `Run` method:

```csharp
public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
{
    resultStatus = default; // Code = rsOk, Description = ""
    try
    {
        RunInternal(context);
    }
    catch (Exception e)
    {
        resultStatus.Code = TResultStatusCode.rsError;
        resultStatus.Description = e.Message;
    }
}

private void RunInternal(IExtensionUtilityContext context)
{
    // All real logic goes here. Exceptions are free to propagate within this scope
    // because they will be caught by the outer try/catch.
    using var applicationCom = ComWrapper.Create(context.CamApplication);
    // ...
}
```

This pattern appears in every example:
- `FullWorkflow/FullWorkflow3DProject/project/main/ExtensionFullWorkflow3DProject.cs`
- `UI/ExtensionManageViewNet/project/main/ExtensionManageView.cs`
- `UI/ExtensionUtilityNotifyNet/project/main/ExtensionUtilityNotify.cs`
- `UI/ExtensionUtilityDialogWindowNet/project/main/ExtensionUtilityDialogWindow.cs`

The same rule applies to `IExtensionFactory.Create`, `OnLibraryRegistered`, and `OnLibraryUnRegistered`:

```csharp
public IExtension? Create(string extensionIdent, out TResultStatus ret)
{
    try
    {
        ret = default;
        if (extensionIdent == "Extension.Utility.MyPlugin")
            return new MyExtension();
        return null;
    }
    catch (Exception e)
    {
        ret.Code = TResultStatusCode.rsError;
        ret.Description = e.Message;
        return null;
    }
}
```

---

## Checking Results from API Calls

When calling ENCY API methods that take `ref TExecuteContext` (IPC calls) or `out TResultStatus` parameters, always check the result immediately after the call:

### Pattern A: Inline check with exception

```csharp
var ctx = new TExecuteContext();

using var projectCom = applicationCom.InvokeAndWrap(app =>
    app.GetActiveProject(ref ctx));

if (ctx.ResultStatus.Code == TResultStatusCode.rsError)
    throw new Exception("GetActiveProject: " + ctx.ResultStatus.Description);
```

### Pattern B: out parameter with status return

Some API methods use `out TResultStatus` instead of `ref TExecuteContext`:

```csharp
projectCom.Invoke(project =>
{
    project.SetOperationTool(operationId, toolNumber, out var ret);
    if (ret.Code == TResultStatusCode.rsError)
        throw new Exception("SetOperationTool: " + ret.Description);
});
```

### Pattern C: InvokeAndWrap with status tuple

`ComWrapper<T>.InvokeAndWrap` has an overload that accepts a function returning `(TResult, TResultStatus)`. It throws automatically if the status is an error:

```csharp
// Throws if status.Code == rsError — no manual check needed
using var operationCom = technologistCom.InvokeAndWrap(t =>
    (t.CreateOperation("TSTFaceMillingOp", prevId, "", out var status), status));
```

### Pattern D: Invoke with status return

Similarly, `Invoke` has an overload for `Func<T, TResultStatus>` that throws on error:

```csharp
// Throws automatically if ret.Code == rsError
technologistCom.Invoke(t =>
    t.CalculateAllOperationsToolpath(true, out var ret));
```

---

## Throwing vs Setting the Status

Inside `RunInternal` (or any helper method called from it), use regular C# exceptions freely. The outer `catch` converts them to `TResultStatus`. The exception message becomes `Description`, so write meaningful messages:

```csharp
private static void SetupOperationTool(
    ComWrapper<ICamApiProject> projectCom,
    string operationId,
    string toolNumber)
{
    projectCom.Invoke(project =>
    {
        project.SetOperationTool(operationId, toolNumber, out var ret);
        if (ret.Code == TResultStatusCode.rsError)
            throw new Exception($"SetOperationTool({operationId}, {toolNumber}): {ret.Description}");
    });
}
```

---

## IExtensionLogger

For non-fatal conditions that do not stop execution, use `IExtensionLogger` (available through extension context) instead of throwing. This writes to the ENCY application log, which is visible in the ENCY log window.

```csharp
public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
{
    resultStatus = default;
    try
    {
        using var applicationCom = ComWrapper.Create(context.CamApplication);

        // Log informational messages
        context.Logger?.Log(TLogEventType.leInfo, "MyPlugin", "Starting workflow");

        // Log a warning without stopping
        if (someCondition)
            context.Logger?.Log(TLogEventType.leWarning, "MyPlugin", "Unexpected state detected");

        RunInternal(context);
    }
    catch (Exception e)
    {
        // Fatal error — set result status so ENCY shows it to the user
        resultStatus.Code = TResultStatusCode.rsError;
        resultStatus.Description = e.Message;
    }
}
```

`UIDialogs.Notify` is available for showing a popup notification to the user for important informational or warning messages (see `UI/ExtensionUtilityNotifyNet`):

```csharp
UIDialogs.Notify(TLogEventType.leInfo, "My Plugin", "Operation completed successfully.");
UIDialogs.Notify(TLogEventType.leWarning, "My Plugin", "No geometry selected.");
```

---

## How ENCY Handles Errors Returned by Plugins

When a plugin's `Run` method sets `resultStatus.Code = TResultStatusCode.rsError`, ENCY:

1. Displays the `Description` string in a system notification or error dialog (depending on how the extension was invoked).
2. Logs the error to the application log.
3. Does **not** crash — the error is treated as a graceful plugin failure.

This means the `Description` field is user-visible. Write it in a language your users understand and include enough context (operation name, file path, etc.) to diagnose the problem without access to source code.

---

## Helper: Centralised Status Check

For projects with many API calls, a small helper method reduces repetition:

```csharp
private static void Check(TExecuteContext ctx, string operationName)
{
    if (ctx.ResultStatus.Code == TResultStatusCode.rsError)
        throw new Exception($"{operationName} failed: {ctx.ResultStatus.Description}");
}

private static void Check(TResultStatus status, string operationName)
{
    if (status.Code == TResultStatusCode.rsError)
        throw new Exception($"{operationName} failed: {status.Description}");
}

// Usage:
var ctx = new TExecuteContext();
using var techCom = projectCom.InvokeAndWrap(p => p.GetTechnologist(ref ctx));
Check(ctx, nameof(ICamApiProject.GetTechnologist));

projectCom.Invoke(p => { p.SaveClData(path, iter, out var s); Check(s, "SaveClData"); });
```
