# Tools & Machine — CAMIPC Reference

This document describes the **IPC layer** variants of the Tools & Machine interfaces. The IPC layer (`CAMIPC.*`) is used for out-of-process communication with ENCY CAM. All interfaces mirror their `CAMAPI.*` counterparts but operate over IPC transport.

For full property and method descriptions see the CAMAPI reference: [`docs/api/tools-machine.md`](../api/tools-machine.md).

---

## General IPC Differences

Every IPC interface carries an additional `GetInstanceId()` method that returns the opaque string handle used internally by the IPC message bus. You do not need to call this directly in typical extension code.

Procedures that take an `out TResultStatus` parameter in CAMAPI are replaced in CAMIPC by a `[in, out] struct TExecuteContext*` parameter. The `TExecuteContext` carries both the error status and additional execution context for the IPC transport.

---

## ICamIpcMachiningTool

**Library:** `CAMIPC.Tools` (uuid `a8203348-327f-4354-b98d-4fb7ea005329`)
**Interface uuid:** `4819106e-f393-4ad5-9af1-15e2a17b91d8`

Identical surface to `ICamApiMachiningTool`: single read-only property `ToolName`.

No .NET helper extension class exists at this layer; use `ComWrapper<ICamIpcMachiningTool>` directly.

---

## ICamIpcMachiningToolInfo

**Library:** `CAMIPC.ToolsList` (uuid `59644b7b-a5c5-4356-b8c7-a5c03db7c55a`)
**Interface uuid:** `2a27b612-a9a8-4e83-a124-438b42ce8d3e`

Exposes the same properties as `ICamApiMachiningToolInfo`:

| Property | Type | Access | Description |
|---|---|---|---|
| `ToolGUID` | `string` | R | Global unique tool identifier |
| `ToolID` | `string` | R | Project-local tool identifier |
| `ToolCaption` | `string` | R | Display name |
| `ConnectorID` | `string` | RW | Machine connector identifier |
| `MagazineNumber` | `Integer` | R | Magazine slot |
| `ToolNumber` | `Integer` | R | CNC T-code number |
| `FirstCorrectorNumber` | `Integer` | R | First CNC corrector table entry |
| `ToolType` | `string` | R | CAM tool type string |
| `ToolProperties` | `ICamIpcXmlPropPointer*` | R | XML property tree |

**Differences from CAMAPI:**

- `ToolProperties` returns `ICamIpcXmlPropPointer*` instead of `IST_XMLPropPointer*`. The IPC XML prop interface is the CAMIPC-specific counterpart and carries the same data over the IPC channel.
- `ToolEntity` (`ICamApiMachiningTool*`) is **not present** in the IPC interface. Tool geometry access is not exposed over IPC.
- `GetInstanceId()` is available for transport-level identification.

---

## ICamIpcMachiningToolsList

**Library:** `CAMIPC.ToolsList`
**Interface uuid:** `0e64e645-1d06-4155-bdfe-7343ae13fc1c`

| Member | Type | Description |
|---|---|---|
| `Count` | `Integer` (R) | Number of tools |
| `ToolInfo[Index]` | `ICamIpcMachiningToolInfo*` (IR) | Tool info at index |
| `GetInstanceId()` | `string` | IPC handle |

**Differences from CAMAPI:**

- `IndexOfToolID(toolId)` is **not present** in the IPC interface.
- `GetOperationsUsingTheTool(toolId)` is **not present** in the IPC interface.

If you need to search by ID or iterate operations via IPC, retrieve the full list and filter client-side.

---

## ICamIpcMachiningToolsManager

**Library:** `CAMIPC.Tools`
**Interface uuid:** `3bbf4605-5649-4a2b-8266-2781f054fec6`

| Method | Signature difference from CAMAPI |
|---|---|
| `OpenExistingLibrary` | Last parameter is `[in, out] TExecuteContext*` instead of `out TResultStatus` |
| `AddToolToProject` | Last parameter is `[in, out] TExecuteContext*` instead of `out TResultStatus` |
| `GetInstanceId()` | Additional IPC-only method |

Error handling: inspect `TExecuteContext.ResultStatus` after the call.

---

## ICamIpcMachineInfo

**Library:** `CAMIPC.Machine` (uuid `4f4116ff-061a-4cff-8b5f-eda2bd7ea0a4`)
**Interface uuid:** `3882595f-caca-4eeb-95df-5efd0bca9cf6`

Surface is identical to `ICamApiMachineInfo`:

| Property | Description |
|---|---|
| `SchemaFilePath` | Path to machine schema file |
| `XMLNodeName` | Root XML node name |
| `GUID` | Machine schema GUID |
| `MachineCaption` | Display name |
| `MachineTypeName` | XML type identifier |

Additional: `GetInstanceId()`.

---

## ICamIpcMachine

**Library:** `CAMIPC.Machine`
**Interface uuid:** `13b47db3-8362-4292-8310-09ccfe1bd991`

| Member | Difference from `ICamApiMachine` |
|---|---|
| `XMLProp` | Returns `ICamIpcXmlPropPointer*` |
| `WorkpieceConnector[Index]` | Returns `ICamIpcWorkpieceConnector*` |
| `LoadFromOperationXml` | Takes `ICamIpcXmlPropPointer*` and `[in, out] TExecuteContext*` |
| `ToolPieceConnectorsCount` / `ToolPieceConnector` | **Not present** in IPC interface |
| `CreateEvaluator()` | Returns `ICamIpcMachineEvaluator*` |

Tool connector enumeration is not available over IPC. If connector information is needed, use the CAMAPI layer (in-process) or obtain it via `ICamIpcMachineInfo` properties.

---

## ICamIpcMachineEvaluator

**Library:** `CAMIPC.Machine`
**Interface uuid:** `18cc27d0-9d02-4ad6-9c97-2a9a8862991a`

The IPC evaluator exposes only `GetInstanceId()`. The full kinematic API (`CalcNextPos*`, `SetNextPos`, `GetAbsoluteMatrix`, etc.) is **not available** over IPC.

To perform kinematic calculations, use `ICamApiMachineEvaluator` via the in-process CAMAPI layer.

---

## ICamIpcMachineConfiguration

**Library:** `CAMIPC.MachineConfiguration` (uuid `c34915c6-39e9-441d-a5bc-96c842c3ca51`)
**Interface uuid:** `687f5a11-67c1-4f1e-b259-26436fa703f5`

Surface matches `ICamApiMachineConfiguration` with the following differences:

- `AxisAvailable` property is **not present** in the IPC interface. Availability must be inferred by checking whether an axis ID is meaningful for the current machine.
- `SetAxesValues(evaluator, rotaryOnly, TExecuteContext*)` — the last parameter is `[in, out] TExecuteContext*` and the evaluator parameter type is `ICamIpcMachineEvaluator*`.
- `GetInstanceId()` is available.

All flip and axis read/write properties are present with the same semantics as CAMAPI.

### Per-operation axis limits

Mirrors the CAMAPI properties ([`../api/tools-machine.md`](../api/tools-machine.md#per-operation-axis-limits)).
All three are indexed by axis, read/write, and take **no** `TExecuteContext`:

| Property | Type | Description |
|---|---|---|
| `AxisLimitsEnabled[index]` | `boolean` | `true` — the limits below apply; `false` — inherited from the parent |
| `AxisLimitMin[index]` | `double` | Lower limit, degrees for rotary axes and mm for linear |
| `AxisLimitMax[index]` | `double` | Upper limit, same units |

> The bounds are only honoured while `AxisLimitsEnabled[index]` is `true`.

---

## ICamIpcMachinesLibrary

**Library:** `CAMIPC.MachinesLibrary` (uuid `10bb7587-a67a-4374-8b5f-c28dbe5a045f`)
**Interface uuid:** `90bf22a4-5253-46d1-af7f-32e92ef5319b`

| Member | Notes |
|---|---|
| `CurrentMachine` | Returns `ICamIpcMachineInfo*` |
| `FindMachine(Guid, FilePath, TypeName)` | Parameter names are capitalized vs. CAMAPI but semantics are identical |
| `GetInstanceId()` | IPC-only |

---

## ICamIpcWorkpieceCoordinateSystem

**Library:** `CAMIPC.Workpiece` (uuid `72a43f75-bda8-42c4-aac5-ff106e77c2e7`)
**Interface uuid:** `5cb4785f-ffdd-4388-b51d-58991080d88b`

| Member | Difference from `ICamApiWorkpieceCoordinateSystem` |
|---|---|
| `CoordinateSystemName` | Read-only property (`propertyR`) — **write is not available as a property** |
| `SetCoordinateSystemName(value, TExecuteContext*)` | Separate mutating procedure with IPC execute context |
| `GetInstanceId()` | IPC-only |

All other members (`ID`, `Mode`, `Offset`) are read/write properties, identical to CAMAPI.

---

## ICamIpcWorkpieceSetup

**Library:** `CAMIPC.Workpiece`
**Interface uuid:** `29dc5fee-a89a-4650-853a-b7ddb03a5347`

Surface is identical to `ICamApiWorkpieceSetup` with the addition of `GetInstanceId()`. All three properties (`MachineSideConnectorIndex`, `WorkpieceSideCoordinateSystemName`, `Offset`) are read/write.

---

## ICamIpcWorkpieceConnector

**Library:** `CAMIPC.Workpiece`
**Interface uuid:** `dd26492b-28ab-4468-b10f-e487e7151253`

Single `Name` property (read-only), plus `GetInstanceId()`. Semantics identical to `ICamApiWorkpieceConnector`.

Note: In CAMIPC, `ICamIpcWorkpieceConnector` is defined in `CAMIPC.Workpiece`, whereas in CAMAPI it is defined in `CAMAPI.Machine`. There is no IPC-layer tool connector interface — `ICamApiToolConnector` has no CAMIPC equivalent.

---

## Summary of IPC Limitations

| Feature | Available in CAMAPI | Available in CAMIPC |
|---|---|---|
| Full tool info (all properties) | Yes | Yes (except `ToolEntity`) |
| `IndexOfToolID` | Yes | No — filter client-side |
| Operations-using-tool iterator | Yes | No |
| Tool connector enumeration | Yes | No |
| Machine kinematic evaluator (full) | Yes | Stub only (`GetInstanceId`) |
| `AxisAvailable` on configuration | Yes | No |
| XML prop write on machine | Yes | Yes (via `ICamIpcXmlPropPointer`) |
| `CoordinateSystemName` as writable property | Yes | No — use `SetCoordinateSystemName` procedure |
