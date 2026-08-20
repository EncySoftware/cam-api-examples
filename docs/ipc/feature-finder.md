# Feature Finder — ENCY CAM IPC

This document describes the IPC (out-of-process) variant of the feature-recognition interfaces.
The IPC layer mirrors the CAMAPI feature finder but is designed for standalone `.exe`
applications talking to a running ENCY instance over the IPC channel.

**Read the CAMAPI feature-finder reference first:** [`../api/feature-finder.md`](../api/feature-finder.md).
This document covers only the differences between the IPC and API layers.

---

## 1. Overview of IPC differences

Every IPC interface differs from its CAMAPI counterpart in the same two systematic ways
(see [`geometry.md §1`](geometry.md#1-overview-of-ipc-differences) for the full explanation):

### GetInstanceId()

Every feature interface (`ICamIpcFeatureFinder`, `ICamIpcFeatureList`, `ICamIpcFeature`, and
all specialised feature interfaces) exposes `GetInstanceId(): string`. It identifies the COM
proxy for IPC routing; extension code does not normally call it.

### TExecuteContext parameter

Methods that perform a blocking or stateful host operation carry an extra
`[in, out] TExecuteContext*` parameter in place of the CAMAPI `out TResultStatus`. Read-only
properties do not. Always pass the context obtained from your connection.

### Naming convention

| CAMAPI interface | CAMIPC interface |
|---|---|
| `ICamApiFeatureFinder` | `ICamIpcFeatureFinder` |
| `ICamApiFeatureList` | `ICamIpcFeatureList` |
| `ICamApiFeature` | `ICamIpcFeature` |
| `ICamApiHoleFeature` | `ICamIpcHoleFeature` |
| `ICamApiComplexHoleFeature` | `ICamIpcComplexHoleFeature` |
| `ICamApiHoleGrooveFeature` | `ICamIpcHoleGrooveFeature` |
| `ICamApiPocketFeature` | `ICamIpcPocketFeature` |
| `ICamApiFilletFeature` | `ICamIpcFilletFeature` |
| `ICamApiChamferFeature` | `ICamIpcChamferFeature` |
| `ICamApiPlaneFeature` | `ICamIpcPlaneFeature` |
| `ICamApiEdgeFeature` | `ICamIpcEdgeFeature` |
| `ICamApiTangentEdgeFeature` | `ICamIpcTangentEdgeFeature` |
| `ICamApiHandlerFeatureFinderUpdated` | `ICamIpcHandlerFeatureFinderUpdated` |

The enums (`TCamApiFeatureType`, `TCamApiFeatureStatus`, `TCamApiPocketType`,
`TCamApiEdgeGeomType`, `TCamApiHoleGrooveShapeType`) are shared with the CAMAPI layer — the
IPC interfaces import and reuse them unchanged. See [`../api/feature-finder.md §10`](../api/feature-finder.md#10-enum-reference).

---

## 2. Obtaining the feature finder

`ICamIpcProject.FeatureFinder` is a read-only property returning `ICamIpcFeatureFinder`
(same access path as CAMAPI, just on the IPC project object).

```csharp
using var projectCom = appCom.GetActiveProject();
using var finderCom  = projectCom.FeatureFinder();   // ComWrapper<ICamIpcFeatureFinder>
```

---

## 3. ICamIpcFeatureFinder — method signature differences

Properties `IsUpdating`, `RecognitionProgress`, `UpdateStamp` are unchanged (read-only, no
context). All retrieval and recognition methods take `ref TExecuteContext`:

| CAMAPI | CAMIPC |
|---|---|
| `GetFeatures(includeDeleted, out status)` | `GetFeatures(includeDeleted, ref ctx)` |
| `GetAllFeatures(includeDeleted, out status)` | `GetAllFeatures(includeDeleted, ref ctx)` |
| `GetSelectedFeatures(out status)` | `GetSelectedFeatures(ref ctx)` |
| `GetFeatureById(id, out status)` | `GetFeatureById(id, ref ctx)` |
| `RunRecognition(waitForCompletion, out status)` | `RunRecognition(waitForCompletion, ref ctx)` |
| `GetFeaturesForNode(name, useRef, refMatrix, out status)` | `GetFeaturesForNode(name, useRef, refMatrix, ref ctx)` |
| `GetFeaturesForSelected(out status)` | `GetFeaturesForSelected(ref ctx)` |
| `SelectFeatureByBaseEntities(names, type, out status)` | `SelectFeatureByBaseEntities(names, type, ref ctx)` |
| `CancelRecognition(out status)` | `CancelRecognition(ref ctx)` |

`CancelRecognition` aborts an in-progress background recognition (no-op if nothing is running), mirroring the CAMAPI addition.

`ICamIpcFeatureList` (`Count`, `Feature[i]`) and `ICamIpcFeature` (all base properties,
`SubFeature[i]`, `BaseEntityName[i]`) carry the same members as their CAMAPI counterparts with
no context parameter — they are pure property reads — plus `GetInstanceId()`. `ICamIpcFeature`
also adds the `Highlighted` (RW) property — a transient viewport highlight that does not change
the actual selection (see [`../api/feature-finder.md`](../api/feature-finder.md#6-icamapifeature--the-base-feature)).

The specialised feature interfaces (`ICamIpcHoleFeature`, `ICamIpcComplexHoleFeature`,
`ICamIpcHoleGrooveFeature`, `ICamIpcPocketFeature`, `ICamIpcFilletFeature`,
`ICamIpcChamferFeature`, `ICamIpcPlaneFeature`, `ICamIpcEdgeFeature`,
`ICamIpcTangentEdgeFeature`) expose the identical measurement properties documented in
[`../api/feature-finder.md §7`](../api/feature-finder.md#7-specialised-feature-interfaces-queryinterface),
each with an added `GetInstanceId()` and no context parameter.

### Typical flow

```csharp
using var finderCom = projectCom.FeatureFinder();
finderCom.RunRecognition(waitForCompletion: true, ref ctx);

using var listCom = finderCom.GetFeatures(includeDeleted: false, ref ctx);
int count = listCom.Count();
for (int i = 0; i < count; i++)
{
    using var featureCom = listCom.Feature(i);
    // read Caption / FeatureType / measurements exactly as in the CAMAPI doc
}
```

---

## 4. The FeatureFinderUpdated event

`ICamIpcHandlerFeatureFinderUpdated.FeatureFinderUpdated(handlerIdent)` mirrors the CAMAPI
handler. Register it through the IPC event mechanism (`RegisterHandler` /
`ICamIpcEventHandler`) rather than the in-process `ICamApiApplication.RegisterHandler`; see
[`connection.md`](connection.md#icamipceventlistener--subscribing-to-ency-events) and [`project.md`](project.md) for the IPC
event-listener pattern. Listener callbacks arrive on a background thread — marshal to your UI
thread if needed.
