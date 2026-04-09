# ENCY CAM IPC — Project / Technologist Domain

This document covers the **CAMIPC** variants of the Project / Technologist interfaces.
CAMIPC is used when an extension communicates with the ENCY CAM process across a
process boundary (inter-process communication).

For a full description of each interface's purpose, properties, and usage patterns,
see the CAMAPI reference: [`../api/project.md`](../api/project.md).
This document focuses exclusively on **what differs** in the IPC layer.

---

## Key difference: TExecuteContext

Every IPC method that crosses the process boundary takes a `[in, out] struct TExecuteContext*`
parameter.  The context carries the call's execution state, error information, and
cancellation token.  In the .NET helper layer this is handled transparently — you
do not construct or pass `TExecuteContext` manually.

In raw IDL / COM you pass a pointer to a stack-allocated `TExecuteContext` on every
call:

```c
// IDL style (direct COM, not recommended for new code)
TExecuteContext ctx = {};
string id = project->GetId(&ctx);
```

With the .NET helper the context is managed inside `ComWrapper.Invoke` / `InvokeAndWrap`,
so the call sites look identical to CAMAPI.

---

## Interface mapping

| CAMAPI interface | CAMIPC counterpart |
|---|---|
| `ICamApiProject` | `ICamIpcProject` |
| `ICamApiTechnologist` | `ICamIpcTechnologist` |
| `ICamApiTechOperation` | `ICamIpcTechOperation` |
| `ICamApiTechOperationIterator` | `ICamIpcTechOperationIterator` |
| `ICamApiTechOperationType` | `ICamIpcTechOperationType` |
| `ICamApiTechOperationTypeIterator` | `ICamIpcTechOperationTypeIterator` |
| `ICamApiPart` | `ICamIpcPart` |
| `ICamApiSetupStage` | `ICamIpcSetupStage` |
| `ICamApiPartStage` | `ICamIpcPartStage` |
| `ICamApiPartAndStageList` | `ICamIpcPartAndStageList` |
| `ICamApiSnapshot` | `ICamIpcSnapshot` |
| `IListCamApiSnapshot` | `IListCamIpcSnapshot` |
| `ICamApiUserTechOperationInfo` | `ICamIpcUserTechOperationInfo` |
| `ICamApiUserTechOperationList` | `ICamIpcUserTechOperationList` |

---

## ICamIpcProject

### Differences from ICamApiProject

1. **All data-returning members are functions, not properties.**  In CAMAPI, `Id`,
   `FilePath`, and `Technologist` are COM properties.  In CAMIPC they are explicit
   `GetXxx` functions, each taking `TExecuteContext*`.

   | CAMAPI property | CAMIPC function |
   |---|---|
   | `Id` (property) | `GetId(ctx)` |
   | `FilePath` (property) | `GetFilePath(ctx)` |
   | `Technologist` (property) | `GetTechnologist(ctx)` |
   | `Snapshots` (property) | `GetSnapshots(ctx)` |
   | `NCMaker` (property) | `GetNCMaker(ctx)` |
   | `GeomImporter` (property) | `GetGeomImporter(ctx)` |

2. **`ToolsList`, `CoordinateSystems`, `MachineInformation`, `Machine`, `Simulator`,
   `GeomModel`** remain as read-only properties (no `ctx`) on `ICamIpcProject` — they
   return IPC-variant types (`ICamIpcMachiningToolsList`, etc.).

3. **`SaveClData`** takes `ICamIpcTechOperationIterator*` (the IPC variant) instead of
   `ICamApiTechOperationIterator*`.

4. **`RegisterHandler` / `UnregisterHandler`** accept `ICamIpcEventHandler*` and
   `ICamIpcEventListener*` (rather than `ICamApiEventHandler*` and `IListString*`).

5. **`GetInstanceId()`** — every IPC object exposes `GetInstanceId()` (no ctx needed),
   which returns the stable IPC object identifier used internally for message routing.
   CAMAPI objects have no equivalent.

### IPC-specific event handlers

Two save event handler interfaces exist in CAMIPC, mirroring CAMAPI:

- `ICamIpcHandlerProjectBeforeSave` — `ProjectBeforeSave(handlerIdent, project)`
- `ICamIpcHandlerProjectAfterSave`  — `ProjectAfterSave(handlerIdent, project)`

---

## ICamIpcTechnologist

### Differences from ICamApiTechnologist

1. **All mutating and querying methods take `TExecuteContext*`.**

   | CAMAPI | CAMIPC |
   |---|---|
   | `GetActiveReorderingModeOfTechnology(out status)` | `GetActiveReorderingModeOfTechnology(ctx)` |
   | `GetActiveReorderingModeOfSimulation(out status)` | `GetActiveReorderingModeOfSimulation(ctx)` |
   | `GetOperations(mode, out status)` | `GetOperations(mode, ctx)` |
   | `CalculateToolpath(links, out status)` | `CalculateToolpath(links, ctx)` |
   | `CalculateAllOperationsToolpath(links, out status)` | `CalculateAllOperationsToolpath(links, ctx)` |
   | `ResetToolpath()` | `ResetToolpath(ctx)` |
   | `ResetAllOperationsToolpath()` | `ResetAllOperationsToolpath(ctx)` |
   | `SwitchOperationEnability()` | `SwitchOperationEnability(ctx)` |
   | `GetAvailableOperationTypeIds(out status)` | `GetAvailableOperationTypeIds(ctx)` |
   | `CreateOperation(typeId, afterId, protoId, out status)` | `CreateOperation(typeId, afterId, protoId, ctx)` |
   | `CreatePart(externalId, out status)` | `CreatePart(externalId, ctx)` |
   | `CreateSetupStage(out status)` | `CreateSetupStage(ctx)` |
   | `DeleteOperation(id, out status)` | `DeleteOperation(id, ctx)` |
   | `DeletePart(partIdx, out status)` | `DeletePart(partIdx, ctx)` |
   | `DeletePartStage(partIdx, stageIdx, incl, out status)` | `DeletePartStage(partIdx, stageIdx, incl, ctx)` |
   | `DeleteSetupStage(stageIdx, out status)` | `DeleteSetupStage(stageIdx, ctx)` |
   | `CreateOperationFromUserTemplate(userOpId, afterId, out status)` | `CreateOperationFromUserTemplate(userOpId, afterId, ctx)` |

2. **`CurrentOperation` is read-only** in CAMIPC (`propertyR`).  To change the
   current operation use the separate procedure:
   ```
   SetCurrentOperation(operation, ctx)
   ```
   In CAMAPI, `CurrentOperation` is `propertyRW` so it can be set directly.

3. **`GetPartAndStageList`** is a function with `ctx` in CAMIPC:
   ```
   GetPartAndStageList(ctx) → ICamIpcPartAndStageList*
   ```
   In CAMAPI it is a read-only property `PartAndStageList`.

4. **`OperationTypes`** remains a read-only property on `ICamIpcTechnologist` (no ctx),
   returning `ICamIpcTechOperationTypeIterator*`.

### IPC event handler

`ICamIpcHandlerTechnologistOperationAdded` — fires after a new operation is added:

```
OperationAdded(handlerIdent, operation: ICamIpcTechOperation*)
```

The CAMAPI equivalent is `ICamApiHandlerTechnologistOperationAdded`.

---

## ICamIpcTechOperation

### Differences from ICamApiTechOperation

1. **All status-flag accessors are functions with `ctx`** (rather than properties):

   | CAMAPI property | CAMIPC function |
   |---|---|
   | `Id` | `GetId(ctx)` |
   | `Name` (RW) | `GetName(ctx)` / `SetName(name, ctx)` |
   | `Enabled` | `GetEnabled(ctx)` |
   | `Calculated` | `GetCalculated(ctx)` |
   | `Simulated` | `GetSimulated(ctx)` |
   | `IsRapidError` | `GetIsRapidError(ctx)` |
   | `IsHolderError` | `GetIsHolderError(ctx)` |
   | `IsCompensationError` | `GetIsCompensationError(ctx)` |
   | `IsPlungeError` | `GetIsPlungeError(ctx)` |
   | `IsTravelError` | `GetIsTravelError(ctx)` |
   | `IsCollisionError` | `GetIsCollisionError(ctx)` |
   | `IsGougeError` | `GetIsGougeError(ctx)` |
   | `IsToolOverloadError` | `GetIsToolOverloadError(ctx)` |
   | `IsTurnDirectionError` | `GetIsTurnDirectionError(ctx)` |
   | `IsMachiningResultCalculated` | `GetIsMachiningResultCalculated(ctx)` |
   | `IsError` | `GetIsError(ctx)` |

2. **`XMLProp`** returns `ICamIpcXmlPropPointer*` instead of `IST_XMLPropPointer*`.
   The property-accessor surface (`Bol`, `Int`, `Flt`, `Str`, `Arr`) is the same.

3. **`LoadFromXmlProp` / `SaveToXmlProp`** accept / return `ICamIpcXmlPropPointer*`
   and take `ctx`:
   ```
   LoadFromXmlProp(xmlProp: ICamIpcXmlPropPointer*, ctx)
   SaveToXmlProp(ctx) → ICamIpcXmlPropPointer*
   ```

4. **`ToolChanged` event handler** (`ICamIpcHandlerTechOperationToolChanged`) gains an
   extra parameter compared to CAMAPI:
   ```
   ToolChanged(tool: ICamIpcMachiningTool*, justPropertyChange: boolean)
   ```
   `justPropertyChange = true` means only a property on the existing tool changed (the
   tool itself was not replaced).  The CAMAPI handler does not have this parameter.

5. **Properties that remain as properties (no ctx):**
   `XMLProp`, `Machine`, `WorkpieceCoordinateSystem`, `MachineConfiguration`,
   `ApproachRule` (RW), `ReturnRule` (RW), `ModelFormerJobAssignment`,
   `ModelFormerPart`, `ModelFormerWorkpiece`, `ModelFormerRestrictions`,
   `ModelFormerFixtures`.

6. **Missing in CAMIPC vs CAMAPI:**
   - `OperationType` (type-id string) — not exposed on `ICamIpcTechOperation`
   - `IsGroup`
   - `FullName`
   - `Units`
   - `LCS`
   - `PartIndex` / `SetupStageIndex`
   - `Technologist` (back-reference)
   - `InitMachineEvaluator`
   - `GetPropIterator`
   - `OperationTag`

   These are available in-process only via `ICamApiTechOperation`.

### IPC event handlers on ICamIpcTechOperation

| Handler interface | Event | Difference from CAMAPI |
|---|---|---|
| `ICamIpcHandlerTechOperationInitModelFormers` | Model formers initialised | Parameter type is `IUnknown*` instead of `ICamApiModelFormer*` |
| `ICamIpcHandlerTechOperationLoadFromXmlProp` | Properties loaded from XML | Uses `IST_XmlPropPointer*` (same type name as CAMAPI) |
| `ICamIpcHandlerTechOperationSaveToXmlProp` | Properties saved to XML | Uses `IST_XmlPropPointer*` |
| `ICamIpcHandlerTechOperationToolChanged` | Tool changed | Extra `justPropertyChange: boolean` parameter |

---

## ICamIpcTechOperationIterator

### Differences from ICamApiTechOperationIterator

All navigation functions take `ctx`:

```
MoveToChild(ctx)   → boolean
MoveToSibling(ctx) → boolean
MoveToParent(ctx)  → boolean
Current(ctx)       → ICamIpcTechOperation*
Reset(ctx)
```

There is no `OperationsFilter` property on `ICamIpcTechOperationIterator` (the filter
mechanism is CAMAPI-only).

---

## ICamIpcSnapshot / IListCamIpcSnapshot

### Differences from ICamApiSnapshot

Both accessors take `ctx` in the IPC variant:

| CAMAPI property | CAMIPC function |
|---|---|
| `CreationTime` | `GetCreationTime(ctx)` |
| `IsAuto` | `GetIsAuto(ctx)` |

`IListCamIpcSnapshot` has the same shape as `IListCamApiSnapshot` (`Get`, `Count`,
`Add`, `Remove`, `RemoveAt`) but returns `ICamIpcSnapshot*` items.

---

## ICamIpcPart / ICamIpcSetupStage / ICamIpcPartStage / ICamIpcPartAndStageList

### Differences from CAMAPI counterparts

All mutating methods on `ICamIpcSetupStage` take `ctx`:
```
SetToolConnector(toolId, connectorId, ctx)
```

`ICamIpcPart`, `ICamIpcPartStage`, and `ICamIpcPartAndStageList` expose the same
properties as their CAMAPI equivalents but gain `GetInstanceId()`.

`ICamIpcPart` does **not** expose `PrototypePartIndex` or `IsPartCopy` (CAMAPI only).

`ICamIpcPartAndStageList.GetPartStage` has no `ctx` parameter (it is a lightweight
local look-up).

---

## ICamIpcUserTechOperationInfo / ICamIpcUserTechOperationList

### Differences from CAMAPI counterparts

`ICamIpcUserTechOperationInfo.XMLProp` returns `ICamIpcXmlPropPointer*` instead of
`IST_XMLPropPointer*`.

Mutating list operations take `ctx`:

| CAMAPI | CAMIPC |
|---|---|
| `Remove(guid, out status)` | `Remove(guid, ctx)` |
| `AddFromFile(file, out status)` | `AddFromFile(file, ctx)` |
| `AddFromOp(caption, op, out status)` | `AddFromOp(caption, op, ctx)` |

---

## Async vs sync

All CAMIPC methods are **synchronous from the caller's perspective** — the .NET helper
and the underlying IPC transport handle serialisation.  The `TExecuteContext` may carry
a cancellation signal, but the call does not return until the remote side has completed
or faulted.

There is no callback-based async pattern in the current CAMIPC surface for the
Project/Technologist domain.

---

## Concise usage example

The code pattern for IPC is identical to CAMAPI at the helper level.  The same
`ComWrapper<T>` idiom applies; just use the IPC-typed interfaces:

```csharp
// Both lines look the same — the IPC context is managed inside ComWrapper
using var projectCom      = applicationCom.GetActiveProject();          // ICamIpcProject
using var technologistCom = projectCom.InvokeAndWrap(p => p.GetTechnologist(ctx));

// Create an operation
using var opCom = technologistCom.InvokeAndWrap(tech =>
    tech.CreateOperation("TSTWaterlineOp", afterOpId, "", ctx));

// Read a property (uses ctx internally)
string name = opCom.Invoke(op => op.GetName(ctx));

// Write XML properties (XMLProp property has no ctx; accessor calls do)
using var xmlPropCom = opCom.InvokeAndWrap(op => op.XMLProp);
xmlPropCom.Invoke(xp =>
{
    xp.Bol["Roughing"] = true;
    xp.Flt["RoughingStep"] = 10.0;
});
```

For complete working examples using CAMAPI (which share the same helper pattern), see:
- [`../api/project.md`](../api/project.md) — shared concepts and full examples
- [`../../FullWorkflow/FullWorkflow3DProject/project/main/TechnologyHelper.cs`](../../FullWorkflow/FullWorkflow3DProject/project/main/TechnologyHelper.cs)
- [`../../Technologist/Operation/CreateUserOperationNet/project/main/ExtensionCreateUserOperation.cs`](../../Technologist/Operation/CreateUserOperationNet/project/main/ExtensionCreateUserOperation.cs)
