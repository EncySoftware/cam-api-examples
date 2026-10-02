# CAD — sketches, extrude, revolve, booleans, fillets

> Parametric CAD modelling inside a CAD node of the geometry tree: draw 2D sketches, turn
> them into bodies (extrude / revolve), cut or merge bodies, round or bevel edges.
> Interfaces live in `CADAPI.Model`, `CADAPI.Feature`, `CADAPI.Body`; helpers in
> `CADAPI.DotnetHelper` (NuGet package of the same name).

For out-of-process apps see [`../ipc/cad.md`](../ipc/cad.md).

---

## Table of contents

1. [Lifecycle](#1-lifecycle)
2. [Getting a CAD model](#2-getting-a-cad-model)
3. [ICadApiSketch — 2D drawing](#3-icadapisketch--2d-drawing)
4. [Sketch faces, contours, segments](#4-sketch-faces-contours-segments)
5. [ICadApiExtrudeFeature](#5-icadapiextrudefeature)
6. [Boolean operations — cut, union, intersect](#6-boolean-operations--cut-union-intersect)
7. [ICadApiRevolveFeature](#7-icadapirevolvefeature)
8. [Fillet, chamfer, draft](#8-fillet-chamfer-draft)
9. [ICadApiBody](#9-icadapibody)
10. [ICadApiFeature — feature tree](#10-icadapifeature--feature-tree)
11. [Pitfalls](#11-pitfalls)

---

## 1. Lifecycle

```
ICAMAPIGeometryModel.AddCadGroup ──► CAD tree node ──AsCadModel()──► ICadApiModel
ICadApiModel.AddSketch ──► ICadApiSketch ──draw──► Face(i) ──► AddExtrude / AddRevolve
                                                               └─► Save()  ← bodies appear here
```

- Every `Add*` only appends a feature. Geometry (bodies, edges, faces) materialises on `Save()`.
- `Save()` publishes the CAD content as child nodes of the CAD group — this is what the user sees
  and what CAM operations consume. Same as "save + exit CAD" in the UI.
- Changing a feature property after `Save()` (depth, operation, target body) needs another `Save()`.

## 2. Getting a CAD model

```csharp
using CADAPI.DotnetHelper;
using CAMAPI.DotnetHelper;

using var appCom = new ComWrapper<ICamApiApplication>(context.CamApplication);
using var projectCom = appCom.GetActiveProject();
using var geomCom = projectCom.CAMAPIGeomModel();
using var cadNodeCom = geomCom.AddCadGroup("Profile");   // new CAD node, identity LCS
using var cadCom = cadNodeCom.AsCadModel();              // ICadApiModel
```

| Call | Returns |
|---|---|
| `geomCom.AddCadGroup(name, lcs?)` | New CAD-typed tree node; `lcs` defaults to identity |
| `nodeCom.AsCadModel()` | `ICadApiModel` of an existing CAD node; `.IsNull` for non-CAD nodes (mesh, plain folder) |

> **Raw call:** `node.AsCadModel(out var status)`.

### ICadApiModel

| Helper | Description |
|---|---|
| `AddSketch(normalAxis, offset)` | New sketch on an orthogonal plane. `normalAxis`: `1` = YZ, `2` = XZ, `3` = XY. `offset` shifts the plane along that axis |
| `Save()` | Rebuild and publish into the geometry tree |
| `SketchCount()` / `Sketch(i)` / `Sketches()` | Existing sketches |
| `BodyCount()` / `Body(i)` / `Bodies()` | Bodies (valid after `Save`) |
| `FeatureCount()` / `Feature(i)` / `Features()` | Feature tree, in tree order |
| `AddExtrude(face, depth)` | → `ICadApiExtrudeFeature` |
| `AddRevolve(face, ax, ay, az, dx, dy, dz, angle)` | → `ICadApiRevolveFeature` |
| `AddFillet(edgeIds, radius)` | → `ICadApiFilletFeature` |
| `AddChamfer(edgeIds, size)` | → `ICadApiChamferFeature` |
| `AddDraft(faceIds, angle)` | → `ICadApiDraftFeature` |

All helpers throw on `rsError`.

## 3. ICadApiSketch — 2D drawing

Coordinates are sketch-local 2D (x, y on the sketch plane). Angles in degrees.

| Helper | Draws |
|---|---|
| `AddLine(x1, y1, x2, y2, id?)` | Line → `ICadApiLine` |
| `AddArc(x1, y1, x2, y2, cx, cy, r, clockwise?, id?)` | Arc start → end around centre; CCW by default → `ICadApiArc` |
| `AddArcFromCenter(cx, cy, r, startAngle, endAngle, clockwise?, id?)` | Arc by angles → `ICadApiArc` |
| `AddCircle(cx, cy, r, id?)` | Circle → `ICadApiArc` (`IsCircle = true`) |
| `AddPoint(x, y, id?)` | Construction point → `ICadApiPoint` |
| `AddRectangle(x, y, width, height)` | Axis-aligned, bottom-left corner at (x, y) |
| `AddRectangleFromCenter(cx, cy, width, height)` | Axis-aligned, centred |
| `AddSlot(x1, y1, x2, y2, r)` | Straight slot, cap centres (x1, y1)–(x2, y2), cap radius `r` |
| `AddArcSlot(x1, y1, x2, y2, cx, cy, arcR, slotR)` | Slot along an arc; `slotR` = half width, `arcR > slotR` |
| `AddPolygon(cx, cy, r, sides)` | Regular polygon, circumradius `r`, `sides >= 3` |

`id` is an optional caller tag returned in `Id`.

```csharp
using var sketchCom = cadCom.AddSketch(3, 0);           // XY plane, z = 0
sketchCom.AddRectangleFromCenter(0, 0, 120, 80);
sketchCom.AddCircle(48, 28, 4);                         // becomes a hole in the face
sketchCom.AddSlot(-25, -12, 25, -12, 5);
```

Closed shapes nested inside another closed shape become holes of the same face.

**Examples:** [MountingPlateSketchExample](../../Geometry/CadSketchDrawingNet/project/main/MountingPlateSketchExample.cs),
[FlangeGasketSketchExample](../../Geometry/CadSketchDrawingNet/project/main/FlangeGasketSketchExample.cs)

## 4. Sketch faces, contours, segments

| Object | What it is | Access |
|---|---|---|
| `ICadApiSketchFace` | Closed planar region: outer loop + holes. **The input of extrude / revolve** | `sketchCom.Face(i)`, `FaceCount()`, `Faces()` |
| `ICadApiSketchContour` | One closed loop | `sketchCom.Contour(i)`, `ContourCount()`, `Contours()` |
| `ICadApiSegment` | Line / arc / circle | `Segment(i)`, `SegmentCount()`, `EnumerateSegments()` on sketch, face, contour |

- Face: `Area()` (signed), `BaseZ()` (z inside the sketch LCS, 0 for a flat face).
- Segment → concrete type: `segCom.AsLine()` (`X1`, `Y1`, `X2`, `Y2`), `segCom.AsArc()` (`CX`, `CY`, `R`,
  `StartAngle`, `EndAngle`, `IsCircle`). Null wrapper if the cast does not apply.
- `EnumerateSegments()` disposes each item after the loop step — do not keep it.

## 5. ICadApiExtrudeFeature

`cadCom.AddExtrude(faceCom, depth)` — sweeps the face along the sketch normal. `depth` is signed:
positive along the normal, negative opposite.

| Helper (get / set) | Description |
|---|---|
| `Depth()` / `SetDepth(v)` | Signed height |
| `Operation()` / `SetOperation(op)` | Boolean operation, see §6. Default `bo_New` |
| `BodyId()` / `SetBodyId(id)` | Target body of the boolean operation |
| `UseThickness()` / `SetUseThickness(b)` | Thin-wall (shell) extrusion instead of a solid |
| `Thickness()` / `SetThickness(v)` | Wall thickness |
| `ThicknessDirection()` / `SetThicknessDirection(b)` | Side of the wall offset |

```csharp
using var faceCom = sketchCom.Face(0);
using var extCom = cadCom.AddExtrude(faceCom, 10);
cadCom.Save();
```

## 6. Boolean operations — cut, union, intersect

`TCadApiBooleanOperation` (on extrude and revolve):

| Value | Effect |
|---|---|
| `bo_New` | New separate body (default) |
| `bo_Add` | Union with the target body |
| `bo_Sub` | Cut: subtract the swept volume from the target body (pocket, hole, slot) |
| `bo_Int` | Keep only the common volume |
| `bo_Auto` | Engine picks the operation |

**A boolean feature needs a target body.** Without `SetBodyId` it behaves as `bo_New`.
Body ids exist only after `Save()`, so the order is: build the base → `Save` → read body id →
set target → `Save`.

```csharp
// base: 100 x 40 x 20 block
using var baseSketchCom = cadCom.AddSketch(3, 0);
baseSketchCom.AddRectangle(0, 0, 100, 40);
using var baseFaceCom = baseSketchCom.Face(0);
using var baseExtCom = cadCom.AddExtrude(baseFaceCom, 20);
cadCom.Save();

string baseBodyId;
using (var bodyCom = cadCom.Body(0))
    baseBodyId = bodyCom.Id();

// cut: 8 mm deep pocket from the top face (z = 20) downwards
using var cutSketchCom = cadCom.AddSketch(3, 20);
cutSketchCom.AddSlot(20, 20, 80, 20, 6);
using var cutFaceCom = cutSketchCom.Face(0);
using var cutExtCom = cadCom.AddExtrude(cutFaceCom, -8);
cutExtCom.SetOperation(TCadApiBooleanOperation.bo_Sub);
cutExtCom.SetBodyId(baseBodyId);
cadCom.Save();                                          // still one body, now with a pocket
```

- Through-cut: make `depth` at least the body thickness (here `-20` or more).
- `bo_Add` merges into one body (`BodyCount` stays 1).
- Re-targeting an existing feature (`SetBodyId` on a feature added earlier) also works — call `Save()` after.

## 7. ICadApiRevolveFeature

`cadCom.AddRevolve(faceCom, axisX, axisY, axisZ, dirX, dirY, dirZ, angle)` — rotates the face around
the axis through point (axisX, axisY, axisZ) with direction (dirX, dirY, dirZ), model coordinates.
Direction must be non-zero.

| Helper | Description |
|---|---|
| `GetAxisPoint()` / `SetAxisPoint(x, y, z)` | Point on the axis |
| `GetAxisDirection()` / `SetAxisDirection(x, y, z)` | Axis direction, need not be normalised |
| `GetAngle()` / `SetAngle(deg)` | Revolve angle, 360 = full |
| `Operation()` / `SetOperation(op)`, `BodyId()` / `SetBodyId(id)` | Same as extrude, §6 |

```csharp
// profile in the XY plane, revolved around the X axis and cut from the base body
using var profileSketchCom = cadCom.AddSketch(3, 0);
profileSketchCom.AddRectangle(0, 5, 10, 2);
cadCom.Save();
using var profileFaceCom = profileSketchCom.Face(0);
using var revCom = cadCom.AddRevolve(profileFaceCom, 0, 0, 0, 1, 0, 0, 360);
revCom.SetOperation(TCadApiBooleanOperation.bo_Sub);
revCom.SetBodyId(baseBodyId);
cadCom.Save();
```

## 8. Fillet, chamfer, draft

Work on topology ids of an existing body — take them from `ICadApiBody` after `Save()`.

| Helper | Input | Feature | Property |
|---|---|---|---|
| `cadCom.AddFillet(edgeIds, radius)` | `bodyCom.EdgeId(i)` | `ICadApiFilletFeature` | `Radius()` / `SetRadius(v)` |
| `cadCom.AddChamfer(edgeIds, size)` | `bodyCom.EdgeId(i)` | `ICadApiChamferFeature` | `Size()` / `SetSize(v)` |
| `cadCom.AddDraft(faceIds, angle)` | `bodyCom.FaceId(i)` | `ICadApiDraftFeature` | `Angle()` / `SetAngle(deg)` |

`edgeIds` / `faceIds` are `IEnumerable<string>`.

```csharp
List<string> edgeIds;
using (var bodyCom = cadCom.Body(0))
    edgeIds = Enumerable.Range(0, bodyCom.EdgeCount()).Select(i => bodyCom.EdgeId(i)).ToList();
using var filletCom = cadCom.AddFillet(edgeIds, 2);
cadCom.Save();
```

## 9. ICadApiBody

| Helper | Description |
|---|---|
| `Id()` | Stable handle, e.g. `#Body(Imp1:0)`. Use it as `BodyId` target; survives renaming |
| `Caption()` | Name in the feature tree |
| `IsSolid()` | `true` = closed solid, `false` = open shell |
| `GetBoxMin()` / `GetBoxMax()` | Axis-aligned bounding box, model coordinates, `(X, Y, Z)` tuple |
| `EdgeCount()` / `EdgeId(i)` | Edge ids for fillet / chamfer |
| `FaceCount()` / `FaceId(i)` | Face ids for draft |

## 10. ICadApiFeature — feature tree

Base of every feature. `Id()` (e.g. `E1`, `S1`), `Caption()`, `TypeId()` (e.g. `#GeCAD.Extrude`,
`#GeCAD.Sketch`). Cast with `AsExtrude()`, `AsRevolve()`, `AsFillet()`, `AsChamfer()`, `AsDraft()` —
null wrapper when the type differs.

```csharp
foreach (var featCom in cadCom.Features())
    using (featCom)
    using (var extCom = featCom.AsExtrude())
        if (!extCom.IsNull)
            Console.WriteLine($"{featCom.Caption()}: depth {extCom.Depth()}, op {extCom.Operation()}");
```

## 11. Pitfalls

- **No `Save()` — no geometry.** `BodyCount` is 0 and the viewport is empty until `Save()`.
- **`bo_Sub` without `SetBodyId` adds material.** It falls back to `bo_New`.
- **Cut goes nowhere.** The swept volume must overlap the body: put the cutter sketch on or inside
  the body (`AddSketch(axis, offset)`) and pick the `depth` sign towards the material.
- Dispose every wrapper (`using`) — see [COM lifetime](../general/com-lifetime.md).
