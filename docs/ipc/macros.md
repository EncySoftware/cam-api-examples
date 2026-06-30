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
