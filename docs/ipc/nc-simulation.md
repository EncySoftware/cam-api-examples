# NC Generation & Simulation — ENCY CAM IPC

This document covers the IPC (inter-process communication) equivalents of the NC generation and simulation interfaces. IPC interfaces are used when calling ENCY CAM from an external process. Each IPC interface mirrors the corresponding CAMAPI interface but routes calls through the IPC transport layer via a `TExecuteContext` argument.

For full conceptual documentation of what each interface does, see [../api/nc-simulation.md](../api/nc-simulation.md). This document focuses on the IPC-specific differences.

---

## General IPC conventions

- Every mutating method and every method that returns a new object takes a `[in, out] TExecuteContext*` parameter instead of the `out TResultStatus` parameter used in the direct API.
- Every IPC object exposes `GetInstanceId()` which returns the string identifier used in the IPC messaging layer.
- IPC interfaces are defined in the `CAMIPC_*` libraries and mirror their `CAMAPI_*` counterparts.

---

## ICamIpcNCMaker — NC program generation (IPC)

Mirrors `ICamApiNCMaker`. Obtained via the IPC project object.

### Postprocessor settings types

The `TCamApiNCMakerSettingsType` enumeration is shared with the direct API:

```
ncsDotnet = 0   // .NET postprocessor
ncsSppx   = 1   // SPPX postprocessor
```

### ICamIpcMakeCncSettings

Base settings interface. Provides `GetInstanceId()` for IPC routing.

### ICamIpcMakeCncDotnetSettings

| Method | Description |
|--------|-------------|
| `GetSettingsFilePath(ctx)` | Returns the path to the XML settings file |
| `SetSettingsFilePath(path, ctx)` | Sets the path to the XML settings file |

### ICamIpcMakeCncSppxSettings

| Method | Description |
|--------|-------------|
| `GetOutputFolder(ctx)` | Returns the output folder |
| `SetOutputFolder(folder, ctx)` | Sets the output folder |
| `GetNcFileName(ctx)` | Returns the NC file name |
| `SetNcFileName(name, ctx)` | Sets the NC file name |

### ICamIpcNCMaker methods

| Method | Description |
|--------|-------------|
| `GetInstanceId()` | Returns the IPC instance identifier |
| `CreateSettings(type, ctx)` | Creates settings of the given type; returns `ICamIpcMakeCncSettings*` |
| `Generate(clDataFile, ppFile, settings, ctx)` | Runs the postprocessor; returns `IListString*` of generated file names |

### IPC workflow example

```csharp
// Create SPPX settings
var settingsBase = ipcNCMaker.CreateSettings(
    TCamApiNCMakerSettingsType.ncsSppx, ref ctx);
var settings = settingsBase as ICamIpcMakeCncSppxSettings
    ?? throw new Exception("Unexpected settings type");

settings.SetOutputFolder(outputDir, ref ctx);
settings.SetNcFileName("part.nc", ref ctx);

// Generate NC program
var fileList = ipcNCMaker.Generate(clDataFile, ppPath, settingsBase, ref ctx);
```

---

## ICamIpcSimulator — machining simulation (IPC)

Mirrors `ICamApiSimulator`. Obtained via the IPC project object.

### Properties (direct, no context required)

All boolean flags and `SimulationSpeedPercent` are read/write properties with no context parameter, identical to the direct API:

| Property | Type | Description |
|----------|------|-------------|
| `CheckGouges` | bool | Detect gouges |
| `CheckHolderCollisions` | bool | Detect holder collisions |
| `CheckMachineCollisions` | bool | Detect machine collisions |
| `BreakOnStopCommand` | bool | Pause on STOP command |
| `BreakOnEndOfOperation` | bool | Pause after each operation |
| `BreakOnErrors` | bool | Pause on simulation errors |
| `SimulationSpeedPercent` | int | Smooth simulation speed 0–100 |

### Methods (no context)

Fast simulation methods and smooth simulation control methods have no context parameter:

| Method | Description |
|--------|-------------|
| `FastSimulateCurrentOperation()` | Simulate current operation only |
| `FastSimulateUpToCurrentOperation()` | Simulate from start up to current |
| `FastSimulateAllOperations()` | Simulate all operations |
| `ResetSimulationResults()` | Clear all results |
| `SmoothSimulationStart()` | Begin continuous playback |
| `SmoothSimulationStop()` | Stop playback |
| `SmoothSimulationStepForward()` | Step one command forward |
| `SmoothSimulationStepBackward()` | Step one command backward |

### SaveMachiningResultToSTL

Takes a `TExecuteContext` in the IPC version:

```
procedure SaveMachiningResultToSTL(
    in(PartStage, ICamIpcPartStage*),
    in(FileName, string),
    [in, out] struct TExecuteContext* ExecuteContext
);
```

```csharp
ipcSimulator.CheckGouges           = true;
ipcSimulator.CheckHolderCollisions = true;
ipcSimulator.ResetSimulationResults();
ipcSimulator.FastSimulateAllOperations();
ipcSimulator.SaveMachiningResultToSTL(null, stlPath, ref ctx);
```

---

## ICamIpcCLDReceiver — toolpath command stream (IPC)

Mirrors `ICamApiCLDReceiver` exactly. The method signatures are identical — no `TExecuteContext` is required on individual toolpath commands. The full command set is documented in [../api/nc-simulation.md#icamapicldreceiver](../api/nc-simulation.md#icamapicldreceiver).

Key methods:

| Method | Description |
|--------|-------------|
| `CutTo(p)` | 3-axis linear move |
| `CutTo5d(p, n)` | 5-axis linear move |
| `ArcTo2d(pe, pc, plane, rc, canBeFull)` | 2D arc move |
| `OutStandardFeed(feed)` | Set feed type by `TFeedTypeFlag` value |
| `AddSpindleSpeedOnRPM(rpm, range, direction)` | Set spindle RPM |
| `AddSpindleOff()` | Stop spindle |
| `AddComment(comment)` | Embed comment |
| `AddCoolant(onOff, pipeNumber)` | Coolant control |

---

## ICamIpcModelFormer family — geometry model assignment (IPC)

Mirrors the `ICamApiModelFormer` family. All `Add…Selected()` methods return IPC list wrappers and operate on the current geometry selection. All mutating methods take a `TExecuteContext`.

### ICamIpcModelFormer

| Method / Property | Description |
|-------------------|-------------|
| `GetInstanceId()` | IPC instance identifier |
| `SupportedItems` | `ICamIpcModelFormerSupportedItems*` |
| `MakeSupportedItems(callback, ctx)` | Register supported-items callback |
| `FillItemsBySupportedItems()` | Apply supported items list |
| `Count` / `Item[i]` | Enumerate model items |
| `AddItem(id, type, typeName)` | Add a model item |
| `FindItem(id)` | Look up an item |
| `SearchInsertItem(type, id)` | Find or create an item |
| `DeleteItem(item)` | Remove an item |
| `DeleteItemById(id, ctx)` | Remove by ID |
| `AddSupportedItemsSelected(type, id)` | Add selected geometry for a supported type |

### Specialised model former interfaces

All mirror their `ICamApi*` counterparts. The `GetInstanceId()` method is added to each for IPC routing:

| Interface | Key method | IPC difference |
|-----------|-----------|----------------|
| `ICamIpcModelFormerWithFaces` | `AddFacesSelected()` | `GetInstanceId()` added |
| `ICamIpcModelFormerWithLevels` | `SupportsLevel(type)`, `AddLevelSelected(type)` | `GetInstanceId()` added |
| `ICamIpcModelFormerWithZones` | `SupportsJobZones()`, `AddJobZoneSelected()`, `SupportsRestrictedZones()`, `AddRestrictedZoneSelected()` | `GetInstanceId()` added |
| `ICamIpcModelFormerWithCurve2D` | `AddCurves2DSelected()` | `GetInstanceId()` added |
| `ICamIpcModelFormerWithCurve5D` | `AddCurves5DSelected()` | `GetInstanceId()` added |
| `ICamIpcModelFormerWithHoles` | `AddHolesSelected()`, `CreateNewHole(lcs, diameter)` | `GetInstanceId()` added |
| `ICamIpcModelFormerWithAreas` | `AddAreasSelected(areaMode)` | `GetInstanceId()` added |

### ICamIpcFeedPointList

Mirrors `ICamApiFeedPointList`. Feed-zone mutation methods take a `TExecuteContext`:

```
AddFeedPoint(position, length, ctx) → index
RemoveFeedPoint(index, ctx)
Clear(ctx)
```

Read-only indexed properties (`TStart`, `TEnd`, `Position`, `FeedType`, `FeedRatePercentage`, `FeedRateChangeType`) require no context.

---

## IDL reference

| Interface | Library | GUID |
|-----------|---------|------|
| `ICamIpcNCMaker` | `CAMIPC_NCMaker` | `8f7d03a1-bee1-4f44-8798-deea797ba41f` |
| `ICamIpcMakeCncDotnetSettings` | `CAMIPC_NCMaker` | `82cf324d-0b79-4657-8b51-1d939951deb3` |
| `ICamIpcMakeCncSppxSettings` | `CAMIPC_NCMaker` | `aceb72e3-f0a3-4ddb-a9c6-1bec6b8fbe73` |
| `ICamIpcSimulator` | `CAMIPC_Simulator` | `abb53f60-55a5-4f65-a133-fd32e37bc775` |
| `ICamIpcCLDReceiver` | `CAMIPC_MCDFormerTypes` | `ea1f4c02-0292-4a97-9c6f-719ffc088f23` |
| `ICamIpcModelFormer` | `CAMIPC_ModelFormerTypes` | `fb0ad4d1-87fc-4a44-9b34-cdb8f8199d9b` |
| `ICamIpcModelFormerWithFaces` | `CAMIPC_ModelFormerTypes` | `450bc7d7-f75f-4eea-bbd1-816717bbe6c8` |
| `ICamIpcModelFormerWithLevels` | `CAMIPC_ModelFormerTypes` | `fe8a4a8a-8d0b-4de6-afcc-a6e1043ec4e6` |
| `ICamIpcModelFormerWithZones` | `CAMIPC_ModelFormerTypes` | `6cf8367e-feb3-4066-ba73-df66f60add3e` |
| `ICamIpcModelFormerWithCurve2D` | `CAMIPC_ModelFormerTypes` | `7dd5100d-cf82-4acf-af31-dc2b89e39b49` |
| `ICamIpcModelFormerWithCurve5D` | `CAMIPC_ModelFormerTypes` | `40d47f5a-45e8-43a2-bcde-86c1fda8f842` |
| `ICamIpcModelFormerWithHoles` | `CAMIPC_ModelFormerTypes` | `c2e303e5-d44f-434b-bbd4-bea00ce12c2e` |
| `ICamIpcModelFormerWithAreas` | `CAMIPC_ModelFormerTypes` | `e1f2a3b4-c5d6-4e7f-8a9b-0c1d2e3f4a5b` |
| `ICamIpcFeedPointList` | `CAMIPC_ModelFormerTypes` | `958a4b7e-e9f7-4e09-9e22-b4ae4ecde9f7` |
