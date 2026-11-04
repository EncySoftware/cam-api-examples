# Extension Essentials

The rules every ENCY extension follows, one line each. Read this page before writing code; each rule links to the section that explains it — open it when your code touches that topic.

## COM objects

1. Every COM object the API gives you — a call's result, a context property, a handler argument — goes into `using var xCom = ComWrapper.Create(...)` or `using var xCom = ...InvokeAndWrap(...)` and is disposed before the method returns; the one exception is a wrapper kept in a field (rule 5). `ComWrapper` has no finalizer: an undisposed wrapper is never released and breaks ENCY when it closes. → [The using pattern](com-lifetime.md#the-using-pattern), [Symptoms](com-lifetime.md#symptoms-what-a-missing-using-looks-like)
2. A call that returns another COM object: `InvokeAndWrap`, never `Invoke` assigned to a plain variable. A plain value: `Invoke` or a `*Helper` extension method. Never `.It` or `.Instance`. → [ComWrapper API](com-lifetime.md#comwrappert-api)
3. Loops: `AsComEnumerable` disposes each item after the loop body; every `AsInstanceOf` or `As*()` inside a loop gets its own `using`; `Dispose()` the iteration variable before `break`. A set of objects kept across statements: `ListComWrapper<T>`. → [Iterators](com-lifetime.md#iteratorcom-helpers), [ListComWrapper](com-lifetime.md#listcomwrappert)
4. A sub-interface: cast inside `Invoke` (`if (x is IFoo foo) ...`), not with `As*()` helpers or `InvokeAndWrap(x => x as IFoo)`, which pull in extra assemblies. → [Interface casting](com-lifetime.md#interface-casting)
5. A wrapper kept in a field — say, one handed to a window — is not `using`-scoped and is disposed in the owner's `Dispose()`. A handler argument is wrapped for the call only and never kept; the close handler calls `UnregisterHandler` and sets a stopped flag that every handler checks first. → [Event-handler arguments](com-lifetime.md#event-handler-arguments), [Non-modal window](non-modal-window.md#what-has-to-work-correctly)

## Errors

6. No exception crosses the COM boundary: every entry point (`Run`, `Build`, `Create`, ...) catches everything and sets `rsError` with the message. Inside, throw freely, with a message that names the call and its arguments — the user sees it. → [The fundamental rule](error-handling.md#the-fundamental-rule-no-exceptions-across-com-boundaries)
7. Check the status of every call that returns one: `out TResultStatus` or `ref TExecuteContext` → throw on `rsError`, or use the `Invoke` / `InvokeAndWrap` overloads that take `(result, status)` and throw themselves. → [Checking results](error-handling.md#checking-results-from-api-calls)
8. Something worth telling that does not stop the work: `context.Logger?.Log(...)` or `UIDialogs.Notify(...)`, not an exception. → [IExtensionLogger](error-handling.md#iextensionlogger)

## Entry points and windows

9. An extension is a class implementing `IExtension` plus one role interface — utility, global, operation popup and the rest; `CAMAPI.ExtensionFactory` creates it by its id. Pick the role and open its example. → [Choosing an entry point](extension-entry-points.md#choosing-an-entry-point)
10. A window only when the user asked for one. A modal WPF window runs on its own STA thread and gets plain values read before it opens, no COM inside; a window that stays open goes through `WindowHelper.ShowStaWindow`, which switches `ComWrapperSettings` to STA itself, and needs `IExtensionLazyUnloadable`. → [UI patterns](ui-patterns.md), [Non-modal window](non-modal-window.md), [MTA threading](com-lifetime.md#mta-threading)
11. Anything the user sees passes the checklist before you publish. → [Design rules](design-rules.md#9-checklist-before-you-publish)
