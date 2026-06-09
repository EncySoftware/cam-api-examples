# Overridable parameters — `MacroParams`

A macro can expose values that the user (or a caller of `Execute`) overrides **without editing the code**. The macro reads them through the static `MacroParams` helper; if no override was supplied, the recorded default is used.

## In the macro body

Call `Initialize` once at the start of `Run`, then read values per step:

```csharp
MacroParams.Initialize(context.Params);     // context.Params is the overrides JSON

double v = MacroParams.GetFlt(stepIndex, "value", 10.0);   // default 10.0 if not overridden
string  s = MacroParams.GetStr(stepIndex, "fullName", "");
int     i = MacroParams.GetInt(stepIndex, "mode", 0);
bool    b = MacroParams.GetBol(stepIndex, "enabled", false);
```

- `stepIndex` is the 0-based index of the step within the macro (the same index used by [`NotifyMacroStep`](notify-step.md) and by the macro's `commands.json`).
- The last argument is the **default** — returned when the user did not override that key. So a macro always runs correctly even with empty params.

## The overrides format

`context.Params` is a JSON dictionary `{ "stepIndex": { "key": "value" }, ... }` — exactly what `ICamApiMacroManager.Execute(id, paramsJson)` accepts (see [../api/application.md](../api/application.md#icamapimacomanager)). Empty string = no overrides (all `GetXxx` return their defaults).

```json
{ "0": { "value": "7.5" }, "3": { "fullName": "Part\\Face1" } }
```

Values are always strings in the JSON; `GetInt`/`GetFlt`/`GetBol` parse them (invariant culture for floats).

## Making a parameter editable in the UI

The macro's `commands.json` (written next to the built macro) drives the editing UI. Each step's entry may carry `editable_params`:

```json
{
  "display_text": "Set parameter: SafeZ = {0}",
  "editable_params": [
    { "key": "value", "label": "Safe Z", "type": "flt", "default_value": "5" }
  ]
}
```

- `type` is `str` / `bol` / `int` / `flt`.
- `default_value: null` (or absent) marks the parameter **required** — the UI refuses to run until the user supplies a value.
- `display_text` uses `{N}` placeholders that the UI replaces with editable fields.

A host can discover these programmatically via `ICamApiMacroInfo.Step[i].Param[j]` (`Key` / `LabelText` / `ParamType` / `Required` / `DefaultValue` / `ValuesString`) — see [../api/application.md](../api/application.md#icamapimacomanager).
