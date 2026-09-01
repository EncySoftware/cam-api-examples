# COM Object Lifetime Management in .NET

This document is critical reading for all .NET developers working with the ENCY CAM API. Mishandling COM lifetime is the most common source of crashes and subtle memory corruption in plugin and application code.

**The one rule:** every COM object the API hands you is a resource. Wrap it in a `ComWrapper<T>` and dispose it — `using` — before the method that received it returns. There is no garbage-collector safety net: see [Symptoms](#symptoms-what-a-missing-using-looks-like) for what happens when you skip it.

---

## Symptoms: what a missing `using` looks like

A leaked COM object almost never fails where the bug is. Your plugin runs fine, does its job, returns — and ENCY breaks minutes later, **when the user closes it**. If you arrived here from one of the symptoms below, a missing `using` is the first thing to check.

### 1. Undestroyed-objects dialog on exit

An error dialog with OK/Cancel buttons appears while ENCY shuts down. It reports:

- the name of the running executable;
- the module being unloaded at that moment, e.g. `CAMAPI.ExtensionManager.dll`;
- how many objects of that module were still alive;
- the full path of the report file — `LostObjects_<module>.txt`, described below;
- the first 1000 characters of that report.

Pressing OK opens the report in Notepad. Every Delphi module counts its own objects, so one shutdown can produce several dialogs and several report files.

**This dialog only exists in a Debug build of ENCY.** The object counter is compiled out of Release entirely — no dialog, no file, no overhead. A release ENCY does not mean your plugin is clean; it means nobody is counting. The same leak then surfaces as symptom 4, or not at all until a user hits it.

### 2. `LostObjects_<module>.txt`

- **Where:** the folder of the running `.exe` (`…\EXE\Bin64\`) — *not* your plugin folder and not `%TEMP%`.
- **Name:** one file per Delphi module — `LostObjects_SCKernel.txt`, `LostObjects_CAMAPI.ExtensionManager.txt`, …
- Written only when something leaked; the previous file is deleted first.

```
Application Name: <full path to the exe>
Instance Name:    <full path to the module that reported>
File write date: <timestamp>
Lost objects count: 5

TExtensionInfo       ID:1043    Thread:29104
TInstanceInfo        ID:1044    Thread:29104
...
```

`ID` is the object's creation sequence number, `Thread` is the thread it was created on. There are **no reference counts and no creation stacks** in the file — stack capture exists in the host but is disabled in shipped builds.

**How to read it:** the names are the Delphi classes implementing the interfaces you obtained — `TExtensionInfo` is the object behind `IExtensionInfo`, and so on. Take each class name, find every call in your plugin that returns the matching interface, and check that each one is `using`-wrapped or explicitly disposed. Repeated identical names mean a leak inside a loop.

### 3. "Destroy called while references remain" dialog

Another Debug-only dialog, raised the moment the host destroys an object whose reference count is still non-zero. It names the Delphi class of the object and the number of references left on it, and blames a wrong destruction order of related objects.

For a plugin author the meaning is narrower: the host destroyed the object while **your** code still held a reference to it. In a Release build the same situation is silent, and the next call made through that now-dangling reference is an access violation.

### 4. Access violation when closing ENCY (any build)

Three ways a lifetime bug turns into a crash instead of a message:

- **GC-timed release.** `ComWrapper<T>` has **no finalizer**. The pointer it takes with `Marshal.GetIUnknownForObject` is released by `Dispose()` and by nothing else — an undisposed wrapper is a permanent leak, not a delayed one. What the GC *does* eventually collect is the per-thread RCW the wrapper holds, and the CLR then calls `Release` on it late, from the finalizer thread. If that lands after the Delphi module has finalized, the call goes into unloaded code → AV.
- **Over-releasing a host singleton.** Disposing a wrapper around a long-lived host object (`IExtensionManager` and friends) from an assembly-unload or finalizer path can drop its last reference while ENCY is still using it; every subsequent access then AVs. Dispose where you acquired, at the end of the method — not from teardown hooks.
- **Reverse order.** The host frees an object, your code then calls through the reference it kept. In Debug you get dialog 3 first; in Release you get the AV directly.

### 5. "ENCY did not close correctly" on the next start

Unrelated mechanism, worth knowing so you do not chase it: ENCY flags an unclean shutdown from pid files it deletes on a normal exit. A leak alone does **not** set that flag — a normal exit stays a normal exit even with objects left over. If you see this report, the process actually died, i.e. you are in symptom 4.

### Checking your own plugin

Symptoms 1–3 require a Debug build of ENCY. If you have one, close it after exercising your plugin and look in the exe folder for `LostObjects_*.txt` — that is the whole test. With a release build the host reports nothing, so the check has to be the code review below.

For an unattended run, a debug ENCY started in job mode (`/JOB_MODE`) does not show the dialog: it sets process exit code 1 instead. That makes "did my plugin leak?" a usable CI check — non-zero exit plus a `LostObjects_*.txt` next to the exe.

**Leak checklist:**

| Pattern | Verdict |
|---|---|
| `using var xCom = ...InvokeAndWrap(...)` | correct |
| `var xCom = ...InvokeAndWrap(...)` without `using` | leak |
| `Invoke(x => x.SomeComProperty)` assigned to a plain variable | leak — use `InvokeAndWrap` |
| `AsInstanceOf<T>()` / `As*()` helper inside a loop without `using` | leak on every iteration |
| `break` out of a `foreach` over a COM iterator | leaks the iteration variable — `Dispose()` it before `break` |
| wrapper stored in a field | must be disposed in the owner's `Dispose()` |
| wrapper disposed from an unload hook / finalizer | over-release — dispose it where you took it |

---

## Why ComWrapper\<T\> Exists

The ENCY host application is written in Delphi and implements a classic COM reference-counting model. Every COM object has an internal reference count. When that count reaches zero the object is destroyed. In .NET, the garbage collector manages memory non-deterministically — it may release a Runtime Callable Wrapper (RCW) seconds or minutes after the last managed reference disappears, or never during a short-lived process. If the GC finalizes an RCW at the wrong moment, the underlying COM `Release` call arrives after the Delphi object has already been invalidated by other means, causing access violations or use-after-free crashes.

`ComWrapper<T>` solves this by:

1. Taking explicit ownership of a COM object at construction time via `Marshal.GetIUnknownForObject` (incrementing the ref count).
2. Implementing `IDisposable` — calling `Dispose()` deterministically releases all held pointers in the correct order.
3. Providing cross-thread access: ENCY's COM objects live on MTA threads. `ComWrapper<T>` marshals the pointer safely so that code on any thread (including a WPF STA thread) can call COM methods without COM apartment violations.

`ComWrapper<T>` deliberately has **no finalizer**. The reference it takes is released by `Dispose()` and by nothing else, so a wrapper you forget to dispose is not released late — it is never released at all. Disposal is not an optimisation you can leave to the runtime; it is the only thing that ends the object's life.

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

> **Warning — `AsInstanceOf` and `InvokeAndWrap(f => f as T)` load transitive assemblies:** the C# cast `f as ITarget` inside a generic lambda causes the .NET runtime to resolve every assembly that `ITarget` transitively depends on. If those assemblies are not in your csproj, the build fails with "assembly not found" errors. The same applies to extension helpers like `featureCom.AsHoleFeature()` that call `InvokeAndWrap(f => f as ICamApiHoleFeature)` internally.
>
> **Preferred alternative — inline `is` check inside `Invoke`:** keep the cast inside the lambda. This is resolved at the Delphi/COM level only, with no .NET type resolution beyond the immediate interface:
>
> ```csharp
> // CORRECT — cast stays inside Invoke; no transitive assembly loading
> var result = "";
> featureCom.Invoke(f =>
> {
>     if (f is ICamApiHoleFeature hole)
>     {
>         result = $"Ø{hole.Diameter:F2}";
>         return;
>     }
>     if (f is ICamApiFilletFeature fillet)
>     {
>         result = $"R{fillet.Size:F2}";
>         return;
>     }
> });
>
> // WRONG — InvokeAndWrap(f => f as T) triggers transitive assembly resolution
> using var holeCom = featureCom.AsHoleFeature(); // may fail to build
> ```
>
> Use the inline `is` pattern whenever you need to branch on a sub-interface of a COM object and do not want to pull in extra assembly references.

> **Pitfall — `AsInstanceOf` inside `foreach`:** each call to `AsInstanceOf` creates a **new** `ComWrapper`. The iterator that produced the original item does not know about this new wrapper and will never dispose it. You must use `using var` explicitly:
>
> ```csharp
> // CORRECT — AsInstanceOf result wrapped with using
> foreach (var itemCom in mf.EnumerateProbingItems())
> {
>     using var asCycle = itemCom.AsInstanceOf<ICamApiProbingCycle>();
>     using var asSurf  = itemCom.AsInstanceOf<ICamApiSurfaceProbingCycle>();
>     if (asCycle != null) { /* ... */ }
> }
>
> // WRONG — leaks two COM objects per iteration, produces LostObjects_*.txt
> foreach (var itemCom in mf.EnumerateProbingItems())
> {
>     var asCycle = itemCom.AsInstanceOf<ICamApiProbingCycle>(); // no using!
>     var asSurf  = itemCom.AsInstanceOf<ICamApiSurfaceProbingCycle>(); // no using!
> }
> ```

> **Pitfall — `break` inside `foreach` with a side-allocated wrapper:** if you call a method on the iteration variable before `break` and that method returns a new `ComWrapper` you intend to keep, make sure to `Dispose()` the iteration variable itself — the iterator will not reach the `yield` cleanup after `break`:
>
> ```csharp
> // WRONG — both mf and opCom leak when we break
> foreach (var opCom in techCom.EnumerateOperations(rmDesigned))
> {
>     var mf = opCom.ModelFormerJobAssignment(); // no using — already wrong
>     if (mf != null) { foundMf = mf; break; }  // opCom also leaks
>     mf?.Dispose();
> }
>
> // CORRECT — every COM-returning call gets using var; dispose opCom before break
> foreach (var opCom in techCom.EnumerateOperations(rmDesigned))
> {
>     using var mf = opCom.ModelFormerJobAssignment(); // always using var for COM
>     if (mf != null) { foundMf = mf.Transfer(); opCom.Dispose(); break; }
> }
> ```

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
