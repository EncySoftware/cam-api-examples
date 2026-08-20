# Recording and saving a macro over IPC (CAMIPC)

A macro is recorded as a sequence of commands, then generated into a runnable macro.
This is the full flow, top to bottom.

## Objects

```
application.MacroManager → GetMacroBuilder → builder
builder.GetCommandsManager → commands
```

## Lifecycle

1. **Start the recording**
   ```
   commands.Start()
   ```

2. **Record commands** — repeat for each action. A command is a bag of key/value
   fields; which keys to fill (and which are required) comes from the schema, not
   from guessing — see *Discovering a command's parameters* below.
   ```
   data := commands.CreateCommandData(commandType)   // commandType ∈ TMacroCommandType
   data.SetStr(key, value)                            // setter per field type (see below)
   data.SetInt(key, value)
   ...
   commands.RegisterCommand(data)
   ```

3. **Stop the recording**
   ```
   commands.Stop()
   ```

4. **Set the name and language**
   ```
   main := builder.CreateMainSettings()
   main.SetCaption('MyMacro')   // display name in the macro list
   main.SetId('MyMacro')        // identifier / folder name
   // optional: main.SetDescription(...) / SetOutputFolder(...) / SetCreateOperations(...)

   lang := builder.CreateLanguageSettings('dotnet')   // 'dotnet' (C#) or 'spr'
   ```
   Unknown language id fails with `rsError`. Any settings you leave unset are filled
   with host defaults in the next step (output folder, .NET references, and an
   auto-generated id when `Id` is empty).

5. **Generate the macro**
   ```
   builder.CreateMacro(main, lang)
   ```

6. **Save it**
   ```
   path := builder.Save()   // writes the macro to disk, registers it, returns the path
   ```

## Capturing current project state

Besides recording individual commands, you can snapshot the current project state into
the recording. Call these **between `Start` and `Stop`** — each one appends command(s)
to the active recording, indistinguishable from user-recorded ones (replay applies them
as-is).

| Method | Effect |
|---|---|
| `commands.AddMachineState()` | capture the current machine selection |
| `commands.AddWorkpieceState()` | capture the current workpiece primitive |
| `commands.AddStrategyState()` | capture the current operations strategy |
| `commands.AddRecognizeFeature()` | append a blank "recognize feature by type" step; the feature type and target item are chosen later in the macro row (no pick required at record time) |

Each method requires an active recording session; called before `Start` or after `Stop`
it fails with `rsError` ("Macro recording is not started").

> Replaces the former single `CaptureProjectState(captureMachine, captureWorkpiece,
> captureStrategy)` call — the three `Add*State` methods map 1:1 to its flags, and
> `AddRecognizeFeature` is new.

## Discovering a command's parameters

Do not hardcode keys. Ask the schema what a command expects.

```
schema := commands.GetCommandSchema()
fields := schema.GetFlatCommand(commandType)
```

`commandType` is a value of the `TMacroCommandType` enum — enumerate it to learn the
available command types.

`fields` describes one command:

- `GetCount()` — number of fields.
- `GetKey(i)` — the field key (the string you pass to a setter).
- `GetFieldType(i)` — value type, which dictates the setter to use:
  - `0 = Str`   → `SetStr`
  - `1 = Bool`  → `SetBool`
  - `2 = Int`   → `SetInt`
  - `3 = Float` → `SetFloat`
- `GetRequired(i)` — `true` ⇒ the field **must** be set, otherwise the macro is malformed.

Rules when filling:

- **Required** fields: always set them. Omitting one produces a macro that may fail on replay.
- **Optional** fields: set them only when you have a value. An empty value is not
  recorded and will not overwrite the target's own default.

### Variant (model-item) commands

Some commands have variant-specific keys selected by a discriminator (e.g. a model
item whose keys differ per class). For those, `GetFlatCommand` returns the **common**
keys including the discriminator key; read its value, then request the variant keys:

```
variantFields := schema.GetClassItemCommand(commandType, discriminator)
```

## Guard cases

| Action | Condition | Result |
|---|---|---|
| `Start` | a recording is already active | **error** — "Macro recording is already started" |
| `Stop` | not currently recording | **no-op** (not an error) |
| `RegisterCommand` | called before `Start` or after `Stop` | **command is silently dropped** (no error) |
| `CreateMacro` | recording not stopped yet | **error** — "Stop the recording before building the macro." |
| `CreateMacro` | nothing was recorded | **error** — "No recorded macro to build…" |
| `CreateMacro` | called again without a new `Start` | **error** — "Macro record wasn't started yet" |
| `Save` | no preceding `CreateMacro` | **error** — "Macro code file is not created. Call CreateMacro first." |
| `AddMachineState` / `AddWorkpieceState` / `AddStrategyState` / `AddRecognizeFeature` | called before `Start` or after `Stop` | **error** — "Macro recording is not started" |

Instead of tracking the session yourself, ask: `commands.GetIsStarted()` is `true` between
`Start` and `Stop`. Worth checking when the user may also be recording through the UI.

## Managing and running macros

`application.MacroManager` (`ICamIpcMacroManager`) is the entry point for everything that is
not recording:

| Method | Description |
|---|---|
| `GetCount()` | Number of registered macros |
| `GetMacro(index)` | Macro by index → `ICamIpcMacroInfo` |
| `GetMacroById(id)` | Macro by id |
| `CreateMacroInstance()` | New empty `ICamIpcMacroInfo` to fill and register |
| `AddMacro(macro)` | Register a macro |
| `RemoveMacro(id, deleteSources)` | Unregister; `deleteSources` also deletes its files |
| `GetMacroBuilder()` | The builder used for recording (above) |
| `Execute(id, params)` | Run a macro — see below |
| `NotifyMacroStep(stepIndex)` | Report replay progress for the given step |
| `OpenInEditor(macro)` | Open the macro source in the editor for its language |
| `ExportMacro(id, targetPath)` | Export to a single self-contained `.dmcr` archive |
| `ImportMacro(sourcePath)` | Import a `.dmcr` archive and register it → `ICamIpcMacroInfo` |

`Execute`'s `params` is a **JSON dictionary of user-overridden values**, keyed by step index:

```json
{"0": {"feed": "250"}, "3": {"depth": "12.5"}}
```

Pass an **empty string** to run with the recorded defaults.

> `ImportMacro` overwrites a macro with the same id, including its previous folder — check
> `GetMacroById` first if that matters.

## Inspecting a macro's steps

`ICamIpcMacroInfo` exposes the recorded steps, which is what a playback UI needs to show
editable parameters before running:

- `ICamIpcMacroInfo`: `GetStepCount()`, `GetStep(index)`, plus read/write `Id`, `Caption`,
  `Description`, `MacroPath`, `ExecutablePath`, `LanguageId`, `ExecuteExtensionId`.
- `ICamIpcMacroStepInfo` (read-only): `GetDisplayText()`, `GetParamCount()`,
  `GetParam(index)`, `GetGroupCaption()`.
- `ICamIpcMacroStepParam` (read-only): `GetKey()`, `GetLabelText()`, `GetParamType()`,
  `GetRequired()`, `GetDefaultValue()`, `GetValuesString()`.

`GetParamType()` returns the **integer ordinal** of `TMacroCommandParamType` — `0 = Str`,
`1 = Bool`, `2 = Int`, `3 = Float` — the same numbering as `GetFieldType` above.
`GetValuesString()` is a semicolon-separated list of allowed values, empty for free-form.
`GetRequired()` means the step was recorded without a default, so a playback UI must refuse
to run until the user supplies one.

The keys you collect this way are exactly the keys of the `Execute` params JSON.

> The full CAMAPI-side model — including the composite/pill parameter types and the
> presentation flags — is documented in
> [`../api/application.md`](../api/application.md#icamapimacrostepinfo--icamapimacrostepparam--discover-overridable-parameters).
> The IPC mirror exposes the read-only subset listed above.

## Language settings and .NET

`ICamIpcMacroBuilderLanguageSettings` folds the .NET-specific properties
(`TargetFramework`, `SDKVersion`, `References`) directly into the one interface — the server
narrows the wrapped object to the concrete language interface. For **SPR** settings the .NET
getters return empty strings and the setters do nothing, so there is no separate interface to
request. `References` marshals by value as a comma-separated list.
