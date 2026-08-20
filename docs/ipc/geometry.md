# Geometry — ENCY CAM IPC

This document describes the IPC (inter-process communication) variants of the geometry interfaces. The IPC layer mirrors the CAMAPI geometry domain but is designed for use in out-of-process extensions that communicate with the ENCY CAM host over an IPC channel.

**Read the CAMAPI geometry reference first:** [`../api/geometry.md`](../api/geometry.md). This document focuses exclusively on the differences between the IPC and API layers.

---

## Table of contents

1. [Overview of IPC differences](#1-overview-of-ipc-differences)
2. [ICamIpcGeomLibrary — factory](#2-icamipcgeomlibrary--factory)
3. [ICamIpcGeometryModel — model root](#3-icamipcgeometrymodel--model-root)
4. [ICamIpcGeometryTreeNode and iterator](#4-icamipcgeometrytreenode-and-iterator)
5. [ICamIpcGeometryEntity and subtypes](#5-icamipcgeometryentity-and-subtypes)
6. [ICamIpcFace, ICamIpcLoop, ICamIpcCoEdge](#6-icamipcface-icamipcloop-icamipccoedge)
7. [ICamIpcSurface and ICamIpcNurbsSurface](#7-icamipcsurface-and-icamipcnurbssurface)
8. [ICamIpcCurve and related interfaces](#8-icamipccurve-and-related-interfaces)
9. [ICamIpcMesh](#9-icamipcmesh)
10. [ICamIpcCoordinateSystem](#10-icamipccoordinatesystem)
11. [ICamIpcGeometryImporter](#11-icamipcgeometryimporter)
12. [ICamIpcTurnGeneratrixExtractor — lathe-specific](#12-icamipcturngeneratrixextractor--lathe-specific)

---

## 1. Overview of IPC differences

Every IPC interface has two systematic additions compared to its CAMAPI counterpart:

### GetInstanceId()

All IPC interfaces expose a `GetInstanceId(): string` method. This returns a unique runtime identifier for the COM proxy object. It is used internally by the IPC transport to route calls to the correct host-side object. You do not normally need to call this from extension code, but it appears on every interface.

### TExecuteContext parameter

Any method that performs a blocking or stateful operation in the host process carries an extra `[in, out] TExecuteContext*` parameter. In the CAMAPI (in-process) equivalents this parameter is absent.

**CAMAPI (in-process):**
```csharp
var iterator = model.GetNodes(out var status);
```

**CAMIPC (out-of-process):**
```csharp
var iterator = model.GetNodes(ref executeContext);
```

`TExecuteContext` is passed by reference. It carries execution state and error information for the IPC round-trip. Always obtain it from your extension execution context and pass it through.

### Naming convention

| CAMAPI interface | CAMIPC interface |
|---|---|
| `ICAMAPIGeometryModel` | `ICamIpcGeometryModel` |
| `ICAMAPIGeometryTreeNode` | `ICamIpcGeometryTreeNode` |
| `ICAMAPIGeometryTreeNodeIterator` | `ICamIpcGeometryTreeNodeIterator` |
| `ICAMAPIGeometryEntity` | `ICamIpcGeometryEntity` |
| `ICAMAPIGeomLibrary` | `ICamIpcGeomLibrary` |
| `ICAMAPIGeometryImporter` | `ICamIpcGeometryImporter` |
| `ICamApiFace` | `ICamIpcFace` |
| `ICamApiLoop` | `ICamIpcLoop` |
| `ICamApiCoEdge` | `ICamIpcCoEdge` |
| `ICamApiCoEdgeIterator` | `ICamIpcCoEdgeIterator` |
| `ICamApiSurface` | `ICamIpcSurface` |
| `ICamApiNurbsSurface` | `ICamIpcNurbsSurface` |
| `ICamApiCurve` | `ICamIpcCurve` |
| `ICamApiCurve5D` | `ICamIpcCurve5D` |
| `ICamApiAbstractCurve` | `ICamIpcAbstractCurve` |
| `ICamApiAbstractNurbsCurve` | `ICamIpcAbstractNurbsCurve` |
| `ICamApiAbstractCurveReceiver` | `ICamIpcAbstractCurveReceiver` |
| `ICamApiCurveArcsReceiver` | `ICamIpcCurveArcsReceiver` |
| `ICamApiMesh` | `ICamIpcMesh` |
| `ICamApiMeshList` | `ICamIpcMeshList` |
| `ICamApiCoordinateSystem` | `ICamIpcCoordinateSystem` |
| `ICamApiListCoordinateSystem` | `ICamIpcListCoordinateSystem` |
| `ICMAPITurnGeneratrixExtractor` | `ICamIpcTurnGeneratrixExtractor` |
| `ICamApiFaceList` | `ICamIpcFaceList` |
| `ICamApiSurfaceCurve` | `ICamIpcSurfaceCurve` |
| `ICamApiAbstractCurveList` | `ICamIpcAbstractCurveList` |

---

## 2. ICamIpcGeomLibrary — factory

Mirrors `ICAMAPIGeomLibrary`. The same properties (`HideUserMessages`, `VisTolerancePercent`, `SearchFontFolder`) are present with identical semantics.

Key differences:

| CAMAPI method | CAMIPC equivalent | Change |
|---|---|---|
| `CreateGeometryModel(out status)` | `CreateGeometryModel(ref ctx)` | `TResultStatus` replaced by `TExecuteContext` |
| `CreateGeometryImporter(out status)` | `CreateGeometryImporter(ref ctx)` | Same |
| `CreateProjectEtalonFormer(out status)` | *(not present)* | Etalon support is CAMAPI-only |
| `CreateTurnGeneratrixExtractor()` | `CreateTurnGeneratrixExtractor()` | Unchanged — no context needed |

---

## 3. ICamIpcGeometryModel — model root

Mirrors `ICAMAPIGeometryModel`. All tree-mutation and export methods that were synchronous in CAMAPI now carry a `TExecuteContext` parameter.

### Method signature differences

| CAMAPI | CAMIPC |
|---|---|
| `GetNodes(out status)` | `GetNodes(ref ctx)` |
| `FindByFullName(name, out status)` | `FindByFullName(name, ref ctx)` |
| `Transform(node, matrix)` | `Transform(node, matrix, ref ctx)` |
| `ExportSelectedToSTL(fileName, out status)` | `ExportSelectedToSTL(fileName, ref ctx)` |
| `ExportSelectedToOSD(fileName, out status)` | `ExportSelectedToOSD(fileName, ref ctx)` |
| `ExportSelectedToDXF(fileName, out status)` | `ExportSelectedToDXF(fileName, ref ctx)` |
| `DeleteNode(node, out status)` | `DeleteNode(node, ref ctx)` |
| `AddGroup(node, out status)` | `AddGroup(node, ref ctx)` |

Methods that are **identical** between CAMAPI and CAMIPC: `DeselectAll()`.

Properties `RootNode` and `ActiveNode` have the same semantics. `GetFaceListOfSelected` and `SaveToEtalonReceiver` are **not present** on `ICamIpcGeometryModel`.

### Observing selection changes

`ICamIpcGeometryModel` adds `RegisterHandler(handlerIdent, handler, listener, ref ctx)` / `UnregisterHandler(handlerIdent, ref ctx)`. The handler implements `ICamIpcHandlerSelectionChanged` — `SelectionChanged(handlerIdent, addedNodes, removedNodes)` with the added/removed node full names as `IListString`. This mirrors the CAMAPI selection-change subscription (see [`../api/geometry.md`](../api/geometry.md#observing-selection-changes)); over IPC the callback is delivered through the standard event-listener mechanism (`ICamIpcEventListener`), like other IPC events.

---

## 4. ICamIpcGeometryTreeNode and iterator

### ICamIpcGeometryTreeNode

Identical properties and navigation links (`Name`, `FullName`, `Selected`, `Visible`, `Parent`, `Child`, `Sibling`, `GeometryEntity`) as `ICAMAPIGeometryTreeNode`, plus `GetInstanceId()`.

### ICamIpcGeometryTreeNodeIterator

Identical navigation methods (`MoveToChild`, `MoveToSibling`, `MoveToParent`, `Current`, `Reset`) as `ICAMAPIGeometryTreeNodeIterator`, plus `GetInstanceId()`.

The iterator pattern and depth-first traversal described in [`../api/geometry.md §5`](../api/geometry.md#5-traversing-the-geometry-tree) applies without change. The only difference is that the iterator is obtained from `ICamIpcGeometryModel.GetNodes(ref ctx)`.

---

## 5. ICamIpcGeometryEntity and subtypes

### ICamIpcGeometryEntity

Same properties as `ICAMAPIGeometryEntity` (`EntityClass`, `EntityType`, `Color`, `ParentColor`, `UpdateStamp`), plus `GetInstanceId()`.

Difference in `GetBoundBox`:

| CAMAPI | CAMIPC |
|---|---|
| `GetBoundBox(lcs, out status): TST3DBox` | `GetBoundBox(lcs, ref ctx): TST3DBox` |

### Specialised entity subtypes

All three specialised entity interfaces exist in IPC form with the same typed properties, plus `GetInstanceId()`:

| Interface | Property | Type |
|---|---|---|
| `ICamIpcCSGeometryEntity` | `Matrix` | `TST3DMatrix` |
| `ICamIpcFaceGeometryEntity` | `Face` | `ICamIpcFace` |
| `ICamIpcCurveGeometryEntity` | `Curve` | `ICamIpcCurve` |

---

## 6. ICamIpcFace, ICamIpcLoop, ICamIpcCoEdge

All members are identical to the CAMAPI equivalents described in [`../api/geometry.md §7`](../api/geometry.md#7-b-rep-structure-face-loop-coedge). All interfaces add `GetInstanceId()`.

`ICamIpcSurfaceCurve.SaveToReceiver` takes an `ICamIpcAbstractCurveReceiver` instead of `ICamApiAbstractCurveReceiver`, but the method signature is otherwise the same (no `TExecuteContext`).

`ICamIpcFaceList.GetMesh` has the same four parameters (`i`, `tol`, `holeCappingSize`, `fromThread`) as `ICamApiFaceList.GetMesh`.

### Analytic surface recognition and edges

Mirrors the CAMAPI additions in [`../api/geometry.md`](../api/geometry.md#analytic-surface-recognition):

- `ICamIpcFace` gains `SurfaceKind` (`TCamIpcSurfaceKind`: `skIpcOther`, `skIpcPlanar`, `skIpcCylindrical`, `skIpcConical`, `skIpcSpherical`, `skIpcToroidal`), plus `GetMinCurvatureRadius(tol)`, `GetPlane`, `GetCylinder`, `GetCone` — same out-parameters and boolean returns as CAMAPI.
- `ICamIpcCoEdge` gains `Edge` → `ICamIpcEdge` (`StartPoint`, `EndPoint`, `Length`).

### Edge adjacency — ICamIpcEdgeAnalyzer

IPC mirror of `ICamApiEdgeAnalyzer` ([`../api/geometry.md`](../api/geometry.md#edge-adjacency-and-convexity--icamapiedgeanalyzer)) — reconstructs which two faces meet at each edge and classifies it. Same singleton lookup (`GetSingletonExtension` on the IPC extension manager, then cast to `ICamIpcEdgeAnalyzer`).

- `ICamIpcFaceListBuilder` — `Add(face)`, `Build()`, `Count`, `Face[i]`.
- `ICamIpcEdgeAnalyzer` — `CreateFaceList()`, `Build(faces, tolerance)` → `ICamIpcEdgeFaceInfoList`.
- `ICamIpcEdgeFaceInfoList` — `Count`, `Item[i]`, `GetByEdge(edge)`.
- `ICamIpcEdgeFaceInfo` — `FaceA`, `FaceB` (nil for a boundary edge), `IsConvex`, `IsConcave`, `IsSmooth`, `IsBoundary`.

All add `GetInstanceId()`.

---

## 7. ICamIpcSurface and ICamIpcNurbsSurface

`ICamIpcSurface.GetNurbsForm()` returns `ICamIpcNurbsSurface` (no context parameter). `ICamIpcNurbsSurface` has the same indexed properties as `ICamApiNurbsSurface`, plus `GetInstanceId()`.

---

## 8. ICamIpcCurve and related interfaces

### ICamIpcCurve

Same properties and methods as `ICamApiCurve`, with two exceptions:

| Method | CAMAPI | CAMIPC |
|---|---|---|
| `MakeStepByLen` | `out ResultStatus` | `ref ctx` |
| `SavePartToReceiver` | takes `ICamApiAbstractCurveReceiver` | takes `ICamIpcAbstractCurveReceiver`, plus `ref ctx` |
| `FindNearestPoint` | present | **not present** in IPC |
| `Inverse()` | present | **not present** in IPC |

### ICamIpcAbstractCurveReceiver

Methods `StartCurve3D`, `StopCurve`, `CutTo3D` all take an additional `ref TExecuteContext` because the receiver implementation lives in the out-of-process extension and each call crosses the IPC boundary.

### ICamIpcCurveArcsReceiver

`ArcTo3D` and `AddCircle` both take an additional `ref TExecuteContext` for the same reason.

### ICamIpcAbstractCurve and ICamIpcAbstractNurbsCurve

`GetNurbsForm` takes `ref ctx`. `ICamIpcAbstractNurbsCurve` has the same indexed properties as `ICamApiAbstractNurbsCurve`, plus `GetInstanceId()`.

### ICamIpcCurve5D

Identical to `ICamApiCurve5D`: `Get_Point5D(t)` with no context parameter.

---

## 9. ICamIpcMesh

Same five methods as `ICamApiMesh` (`GetVertexCount`, `GetTriangleCount`, `GetVertex`, `GetTriangle`, `GetTriangleNormal`), plus `GetInstanceId()`. No context parameter on any method.

---

## 10. ICamIpcCoordinateSystem

### ICamIpcCoordinateSystem

Same four properties as `ICamApiCoordinateSystem` (`Name`, `Parent`, `Matrix`, `Color`), plus `GetInstanceId()`.

### ICamIpcListCoordinateSystem

Same management API as `ICamApiListCoordinateSystem`, but the mutating/querying methods take `ref TExecuteContext`:

| Method | CAMAPI | CAMIPC |
|---|---|---|
| `Add(name, matrix, parent, ...)` | `out ResultStatus` | `ref ctx` |
| `Remove(name, ...)` | `out ResultStatus` | `ref ctx` |
| `GetByName(name, ...)` | `out ResultStatus` | `ref ctx` |
| `SetActive(name, ...)` | `out ResultStatus` | `ref ctx` |

Property name difference: CAMAPI uses `GetActive` (property getter named `GetActive`), CAMIPC uses `Active`.

---

## 11. ICamIpcGeometryImporter

Mirrors `ICAMAPIGeometryImporter` with two differences:

1. `GeometryModel` is not a direct property. Instead, use `GetGeometryModel(ref ctx)` to retrieve the associated `ICamIpcGeometryModel`.
2. `ImportFile` does not return a `TResultStatus`. Error information is conveyed via the `TExecuteContext`:

| CAMAPI | CAMIPC |
|---|---|
| `GeometryModel` (R/W property) | `GetGeometryModel(ref ctx)` |
| `ImportFile(path, folder, dialog): TResultStatus` | `ImportFile(path, folder, dialog, ref ctx)` |

---

## 12. ICamIpcTurnGeneratrixExtractor — lathe-specific

This is an IPC-only addition (no direct equivalent in CAMAPI at the same interface level, though `ICMAPITurnGeneratrixExtractor` exists in the CAMAPI layer with identical semantics).

The IPC version differs in:

- `Receiver` property type is `ICamIpcAbstractCurveReceiver` instead of `ICamApiAbstractCurveReceiver`.
- `MakeGeneratrixForNode(node, ref ctx)` — the context parameter is required.

All other properties (`Tolerance`, `SewTolerance`, `NeedJoinCurves`, `TurnAxis`, `NeedCloseToAxis`) have identical types and semantics to the CAMAPI version described in [`../api/geometry.md §15`](../api/geometry.md#15-icmapiturngeneratrixextractor--lathe-generatrix).

Obtained from `ICamIpcGeomLibrary.CreateTurnGeneratrixExtractor()` (no context parameter on the factory method itself).

---

## 13. ICamIpcGeometryModelSketcher — creating primitives

IPC mirror of `ICamApiGeometryModelSketcher`
([`../api/geometry.md §18`](../api/geometry.md#18-icamapigeometrymodelsketcher--creating-primitives)).
Every method takes `ctx` and returns an `ICamIpcGeometryTreeNode`.

| Method | Creates |
|---|---|
| `AddPoint(x, y, z, ctx)` | A single point |
| `AddLine(p1, p2, ctx)` | A line segment |
| `AddNormalLine(origin, endPoint, ctx)` | A "normal line" |
| `StartPolyline(ctx)` | Polyline → `ICamIpcSpatialCurveBuilder` |
| `StartSpline(ctx)` | Smooth spline → `ICamIpcSpatialCurveBuilder` |

### Sketching in a local coordinate system

Coordinates are **Global** by default. Two ways to change that, same model as CAMAPI:

**Sticky** — `GetTargetCS(ctx)` / `SetTargetCS(matrix, ctx)` set the CS applied to every
subsequent `AddPoint`, `AddLine`, `AddNormalLine`, `StartPolyline` and `StartSpline`. The
matrix maps sketch-local coordinates to world, the same convention as
`ICamIpcCoordinateSystem.Matrix`; identity means Global.

**One-shot** — these take the CS explicitly and neither read nor modify `TargetCS`:

| Method | Creates |
|---|---|
| `AddPointInCS(x, y, z, lcs, ctx)` | Point in `lcs` |
| `AddLineInCS(p1, p2, lcs, ctx)` | Line in `lcs` |
| `AddNormalLineInCS(origin, endPoint, lcs, ctx)` | Normal line in `lcs` |
| `StartPolylineInCS(lcs, ctx)` | Polyline builder in `lcs` |
| `StartSplineInCS(lcs, ctx)` | Spline builder in `lcs` |

> Unlike the CAMAPI helpers — which expose these as overloads of the plain names — the IPC
> interface keeps the explicit `…InCS` method names.

> `TargetCS` is sticky server-side state on the geometry model, shared by every IPC client.
> Prefer the `…InCS` variants, or restore the previous matrix when done.
