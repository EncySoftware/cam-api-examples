# Reporting progress — `NotifyMacroStep`

`NotifyMacroStep` is macro-specific: a running macro tells ENCY which step it has reached so the UI can highlight the current step and honour breakpoints. It does no CAM work — it is purely a progress signal.

## How to call it

Get the macro manager from the application and notify before doing each logical step's work:

```csharp
using var applicationCom  = ComWrapper.Create(application);
using var macroManagerCom = applicationCom.MacroManager();

macroManagerCom.NotifyMacroStep(0);
// ... step 0 work ...
macroManagerCom.NotifyMacroStep(1);
// ... step 1 work ...
```

(`MacroManagerHelper.NotifyMacroStep(int)` wraps the call; the raw method is `ICamApiMacroManager.NotifyMacroStep(stepIndex, out ret)`.)

## What the step index means

- `stepIndex` is the 0-based position of the step **within this macro**, matching the order in the macro's `commands.json` and the `stepIndex` used by [`MacroParams`](macro-params.md).
- Builder-generated macros emit a `NotifyMacroStep(n)` at the start of every action step, with `n` incrementing across **all** steps (not just operation creation).

## Why it matters

- **UI feedback:** the macros window highlights the step currently executing.
- **Breakpoints:** the user can pause a macro on a step; the pause is evaluated when that step is notified.

If you hand-write a macro, calling `NotifyMacroStep` is optional but recommended — without it the UI cannot show progress or stop on a step. Keep the indices consistent with the values you pass to `MacroParams.GetXxx`.
