# COM Object Lifetime Management in .NET

This document is critical reading for all .NET developers working with the ENCY CAM API. Mishandling COM lifetime is the most common source of crashes and subtle memory corruption in plugin and application code.

---

## Why ComWrapper\<T\> Exists

The ENCY host application is written in Delphi and implements a classic COM reference-counting model. Every COM object has an internal reference count. When that count reaches zero the object is destroyed. In .NET, the garbage collector manages memory non-deterministically — it may release a Runtime Callable Wrapper (RCW) seconds or minutes after the last managed reference disappears, or never during a short-lived process. If the GC finalizes an RCW at the wrong moment, the underlying COM `Release` call arrives after the Delphi object has already been invalidated by other means, causing access violations or use-after-free crashes.

`ComWrapper<T>` solves this by:

1. Taking explicit ownership of a COM object at construction time via `Marshal.GetIUnknownForObject` (incrementing the ref count).
2. Implementing `IDisposable` — calling `Dispose()` deterministically releases all held pointers in the correct order.
3. Providing cross-thread access: ENCY's COM objects live on MTA threads. `ComWrapper<T>` marshals the pointer safely so that code on any thread (including a WPF STA thread) can call COM methods without COM apartment violations.

**The rule is simple:** every COM object you receive from the API must be wrapped in a `ComWrapper<T>` and disposed — either with `using` or an explicit `Dispose()` call — before the method that received it returns.

---

## ComWrapper\<T\> API

### Constructors and Factory Method

```csharp
// Wrap a COM object obtained directly from an API call.
// The wrapper takes ownership (increments ref count internally).
using var nodeCom = new ComWrapper<IMyInterface>(someComObject);

// Preferred factory shorthand — identical semantics, infers the type.
using var nodeCom = ComWrapper.Create(someComObject);
```

Both accept `null`/`IntPtr.Zero` gracefully and produce an "empty" wrapper whose `IsNull` property returns `true`.

### Calling Methods That Return No COM Object

Use `Invoke(Action<T>)` when the COM method has no return value, or when the return value is a plain .NET type (string, int, bool, struct):

```csharp
using var technologistCom = ComWrapper.Create(context.CamApplication.Technologist);

// No return value
technologistCom.Invoke(t => t.ResetAllOperationsToolpath());

// Scalar return value — captured via closure
string rootId = string.Empty;
technologistCom.Invoke(t => rootId = t.RootOperation.Id);
```

There is also a typed overload that returns a value directly:

```csharp
string execPath = applicationCom.Invoke(app => app.ExecutablePath);
```

If the COM method returns a `TResultStatus` alongside its result you can use the status-checking overload, which throws automatically on error:

```csharp
// Overload: Func<T, (TReturn Result, TResultStatus Status)>
// Throws if status.Code == rsError
string id = technologistCom.Invoke(t =>
    (t.CreateOperation("TSTFaceMillingOp", prevId, "", out var status), status));
```

### Calling Methods That Return Another COM Object

Use `InvokeAndWrap<TResult>` when the COM method returns another COM interface. The result is automatically wrapped in a new `ComWrapper<TResult>` that takes ownership.

```csharp
using var activeProjectCom = applicationCom.InvokeAndWrap(app =>
    app.GetActiveProject(ref executeContext));

using var technologistCom = activeProjectCom.InvokeAndWrap(project =>
    project.Technologist);

// With status checking — throws on error
using var operationCom = technologistCom.InvokeAndWrap(t =>
    (t.CreateOperation("TSTFaceMillingOp", prevId, "", out var status), status));
```

**Never** return a raw COM interface pointer out of `Invoke` and assign it to a plain variable. Always use `InvokeAndWrap` for COM-returning calls.

### Accessing the Raw COM Object

`ComWrapper<T>` exposes two deprecated raw-access properties — `Instance` (nullable) and `It` (throws on empty). **Do not use them in new code.** Both are marked `[Obsolete]` and give warning CS0618; they bypass MTA marshalling and can fail unpredictably when the RCW is accessed off its apartment thread.

Use instead:

| Need | Use |
|---|---|
| Read a property / call a method | `wrapperCom.Invoke(x => x.Property)` / `wrapperCom.Invoke(x => x.Method(...))` or the `*Helper.cs` extension method |
| Call a method that returns another COM object | `wrapperCom.InvokeAndWrap(x => x.Method(...))` |
| Check whether the wrapper is empty | `wrapperCom.IsNull` |
| Pass a wrapped COM object as argument to another COM method | Nest the calls so the argument is read inside the outer `Invoke`: `outerCom.Invoke(outer => innerCom.Invoke(inner => outer.DoWith(inner)))` |

If a scenario seems to require `.It` / `.Instance`, the fix is usually to add a missing extension method to `CAMAPI.DotnetHelper` — not to reach for the raw pointer.

### The `using` Pattern

Always use `using` for `ComWrapper<T>` instances. Treat every COM-returning call as a resource allocation:

```csharp
public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
{
    resultStatus = default;
    try
    {
        using var applicationCom = ComWrapper.Create(context.CamApplication);
        using var projectCom = applicationCom.InvokeAndWrap(app =>
            app.GetActiveProject(ref executeContext));
        using var technologistCom = projectCom.InvokeAndWrap(p => p.Technologist);

        // work with technologistCom ...
    }
    catch (Exception e)
    {
        resultStatus.Code = TResultStatusCode.rsError;
        resultStatus.Description = e.Message;
    }
}
// All COM objects released deterministically when the using blocks exit
```

For long-lived wrappers (fields), call `Dispose()` explicitly in the owning object's `Dispose()` method.

### Checking for Null

```csharp
using var nodeCom = geometryModelCom.InvokeAndWrap(m =>
    m.FindByFullName("Part\\Part1.igs\\Face11", out var status));

if (nodeCom.IsNull)
    throw new Exception("Geometry node not found");
```

### Interface Casting

To query whether a COM object also implements another interface, perform the cast **inside** `Invoke` (or `InvokeAndWrap`) — never against `.It` / `.Instance` outside. The RCW lives in the MTA context, and casting the raw instance from the caller's thread can fail unpredictably.

**When you only need the narrow interface for one block of work** — cast inside `Invoke` and use the variable locally:

```csharp
mfCom.Invoke(mf =>
{
    if (mf is not ICamApiModelFormerWithHoles mfWithHoles) return;
    mfWithHoles.AddHolesSelected();
});
```

**When you need a typed `ComWrapper<T>` to keep around (e.g. to pass to an extension method)** — wrap the cast result and check `IsNull`:

```csharp
using var mfWithHolesCom = mfCom.InvokeAndWrap(mf => mf as ICamApiModelFormerWithHoles);
if (!mfWithHolesCom.IsNull)
    mfWithHolesCom.AddHolesSelected();     // call the DotnetHelper extension method
```

> `ComWrapper<T>.AsInstanceOf<TTarget>()` performs the QI-cast through `Invoke` (MTA-safe) and
> returns an **independent** wrapper with its own ref count — disposing the original does not
> invalidate it. It is a valid alternative, but for consistency this SDK and all examples use the
> `InvokeAndWrap(x => x as IFoo)` + `IsNull` pattern shown above; prefer it in new code.

---

## ListComWrapper\<T\>

`ListComWrapper<T>` is an `IList<ComWrapper<T>>` that manages the lifetime of a collection of COM wrappers. It is used when you need to build up or hold onto a set of COM objects across multiple statements.

### Ownership Transfer on Add

`Add(item)` always calls `item.TransferOwnership()` internally. After the call, the original `item` variable is invalidated (marked disposed); ownership belongs to the list. This prevents double-release.

```csharp
using var curves = new ListComWrapper<IMyGeomCurve>();

foreach (var faceId in faceIds)
{
    // InvokeAndWrap creates a local wrapper — Add transfers ownership into the list
    var curveCom = geometryModelCom.InvokeAndWrap(m =>
        m.FindByFullName(faceId, out _));
    curves.Add(curveCom);
    // curveCom is now empty/disposed — do not use it after this line
}

// Use the list...
foreach (var curveCom in curves)
    curveCom.Invoke(c => c.Selected = true);

// Dispose releases all items
```

### Disposal

`Dispose()` iterates every item in the list and calls `Dispose()` on each, then clears the list. Always use `using` on a `ListComWrapper<T>`:

```csharp
using var items = new ListComWrapper<ISomeInterface>();
// ... fill items ...
// items.Dispose() called automatically here
```

`Clear()` also disposes all items and empties the list (useful for reuse within a `using` block).

`Remove(item)` and `RemoveAt(index)` each dispose the removed item.

---

## MTA Threading

ENCY's COM objects are created on MTA (Multi-Threaded Apartment) threads. By default, `ComWrapper<T>` assumes the host process is MTA (`ComWrapperSettings.ApplicationApartmentState == ApartmentState.MTA`) — this is the correct setting for pure console apps and for in-process plugins.

When you need to show a WPF window (which requires STA), you must set:

```csharp
ComWrapperSettings.ApplicationApartmentState = ApartmentState.STA;
```

This must be set **before** creating any `ComWrapper<T>` instance (typically at application startup, as in `MainWindow` constructor). When STA mode is active, `ComWrapper<T>` internally marshals COM pointers into an MTA-thread pool via `MtaTaskScheduler`.

### MtaTaskScheduler

`MtaTaskScheduler` is a static pool of 8 background threads, all with `ApartmentState.MTA`. It is used internally by `ComWrapper<T>` when the calling thread is STA. You rarely need to use it directly, but it is important to understand:

- All `Invoke` and `InvokeAndWrap` calls on a `ComWrapper<T>` that was created on an STA thread automatically dispatch through `MtaTaskScheduler`.
- The scheduler starts automatically on first use and shuts down cleanly on `AppDomain.ProcessExit`.

If you ever need to execute arbitrary code on an MTA thread (for example, to create a COM object that must be created on MTA), you can use it directly:

```csharp
await MtaTaskScheduler.Run(() =>
{
    // this lambda runs on an MTA thread
    var comObject = CreateSomeComObject();
    // wrap immediately
});
```

**Important:** never access raw COM interface references inside a WPF event handler or any STA-thread callback directly. Always go through `ComWrapper<T>.Invoke` or `ComWrapper<T>.InvokeAndWrap`, which will dispatch to MTA automatically.

---

## IteratorCom Helpers

ENCY iterators expose a cursor-based API: `Current()`, `MoveToSibling()`, optionally `MoveToChild()` and `MoveToParent()`. The `IteratorHelper` extension methods wrap these into standard .NET `IEnumerable<T>`.

### `AsEnumerable` — for scalar (non-COM) node values

Use when `Current()` returns a plain value (string, int, struct) — one that does not itself need disposal:

```csharp
using var opTypesCom = technologistCom.InvokeAndWrap(t => t.OperationTypes);

var ids = opTypesCom.AsEnumerable(
    current:       iter => iter.Current()?.Id,   // returns string
    moveToSibling: iter => iter.Next(),
    moveToChild:   null,
    moveToParent:  null,
    reset:         null
).ToList();
```

The iteration runs on the raw iterator instance without COM wrapping, so it is efficient but requires that the iterator not be disposed during iteration.

### `AsComEnumerable` — for COM object nodes

Use when `Current()` returns a COM interface pointer that must itself be wrapped and disposed. The constraint `where TNode : IDisposable` enforces this. Each yielded item is a `ComWrapper<TNode>` (or any `IDisposable` COM wrapper), and each is disposed after its `foreach` body executes:

```csharp
using var iterCom = technologistCom.InvokeAndWrap(t => t.GetOperations(TReorderMode.rmReordered, out _));

foreach (using var operationCom in iterCom.AsComEnumerable(
    current:       iter => ComWrapper.Create(iter.Current()),
    moveToSibling: iter => iter.Next(),
    moveToChild:   iter => iter.MoveToChild(),
    moveToParent:  iter => iter.MoveToParent(),
    reset:         null))
{
    var id = operationCom.Invoke(op => op.Id);
    Console.WriteLine(id);
    // operationCom.Dispose() called automatically here
}
```

The difference between the two helpers is ownership: `AsEnumerable` yields raw values and does no COM cleanup; `AsComEnumerable` yields `IDisposable` objects and disposes each one after the loop body.

**Tree traversal:** both helpers perform depth-first traversal when `moveToChild` and `moveToParent` are non-null. Pass `null` for both to get flat (sibling-only) iteration.
