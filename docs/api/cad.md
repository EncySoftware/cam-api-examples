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
11. [Topology faces — sketch on a face, CAM face names](#11-topology-faces--sketch-on-a-face-cam-face-names)
12. [Pitfalls](#12-pitfalls)

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
- A feature the engine cannot build — e.g. a cut that removes nothing — is not applied. `Save()`
  still publishes the rest, then throws with the caption and id of each such feature.

## 2. Getting a CAD model

```csharp
using CADAPI.DotnetHelper;   // CAD helpers: AddSketch, AddExtrude, Save, …
using CADAPI.Feature;        // TCadApiBooleanOperation, feature interfaces
using CADAPI.Model;          // ICadApiModel, ICadApiSketch, sketch faces
using CAMAPI.DotnetHelper;   // ComWrapper

using var appCom = new ComWrapper<ICamApiApplication>(context.CamApplication);
using var projectCom = appCom.GetActiveProject();
using var geomCom = projectCom.CAMAPIGeomModel();
using var cadNodeCom = geomCom.AddCadGroup("Profile");   // new CAD node, identity LCS
using var cadCom = cadNodeCom.AsCadModel();              // ICadApiModel
```

`TCadApiBooleanOperation` lives in `CADAPI.Feature` (CADAPI.Feature.dll), not in `CADAPI.Model`:
without `using CADAPI.Feature;` a cut does not compile. Add `using CADAPI.Body;` only when the code
names `ICadApiBody` or `ICadApiFace`.

| Call | Returns |
|---|---|
| `geomCom.AddCadGroup(name, lcs?)` | New CAD-typed tree node; `lcs` defaults to identity |
| `nodeCom.AsCadModel()` | `ICadApiModel` of an existing CAD node; `.IsNull` for non-CAD nodes (mesh, plain folder) |

> **Raw call:** `node.AsCadModel(out var status)`.

### ICadApiModel

| Helper | Description |
|---|---|
| `AddSketch(normalAxis, offset)` | New sketch on an orthogonal plane. `normalAxis`: `1` = YZ, `2` = XZ, `3` = XY. `offset` shifts the plane along that axis |
| `AddSketchOnFace(faceCom)` | New sketch on a planar body face, see §11 |
| `Save()` | Rebuild and publish into the geometry tree |
| `SketchCount()` / `Sketch(i)` / `Sketches()` | Existing sketches |
| `BodyCount()` / `Body(i)` / `Bodies()` | Bodies (valid after `Save`) |
| `FeatureCount()` / `Feature(i)` / `Features()` | Feature tree, in tree order |
| `AddExtrude(face, depth)` | → `ICadApiExtrudeFeature` |
| `AddRevolve(face, ax, ay, az, dx, dy, dz, angle)` | → `ICadApiRevolveFeature` |
| `AddFillet(edgeIds, radius)` | → `ICadApiFilletFeature` |
| `AddChamfer(edgeIds, size)` | → `ICadApiChamferFeature` |
| `AddDraft(faceIds, angle)` | → `ICadApiDraftFeature` |
| `Face(faceId)` | `ICadApiFace` handle by a body face id, see §11 |
| `GetCamFaceFullNames(faceId)` | CAM face FullNames of a CAD face after `Save`, see §11 |

All helpers throw on `rsError`.

## 3. ICadApiSketch — 2D drawing

Coordinates are sketch-local 2D (x, y on the sketch plane). Angles in degrees.

| `normalAxis` | Plane | Sketch x | Sketch y | Positive extrude depth |
|---|---|---|---|---|
| `1` | YZ | +Y | +Z | +X |
| `2` | XZ | +X | +Z | **−Y** |
| `3` | XY | +X | +Y | +Z |

A positive depth goes along x × y of the sketch, so on XZ it points to −Y.

| Helper | Draws |
|---|---|
| `AddLine(x1, y1, x2, y2, id?)` | Line → `ICadApiLine` |
| `AddArc(x1, y1, x2, y2, cx, cy, r, clockwise?, id?)` | Arc start → end around centre; CCW by default → `ICadApiArc` |
| `AddArcFromCenter(cx, cy, r, startAngle, endAngle, clockwise?, id?)` | Arc by angles → `ICadApiArc` |
| `AddCircle(cx, cy, r, id?)` | Circle → `ICadApiArc` (`IsCircle = true`) |
| `AddPoint(x, y, id?)` | Construction point → `ICadApiPoint` |
| `AddRectangle(x, y, width, height)` | Axis-aligned, bottom-left corner at (x, y) |
| `AddRectangleFromCenter(cx, cy, width, height)` | Axis-aligned, centred |
| `AddRoundedRectangle(x, y, width, height, r)` | Axis-aligned, bottom-left corner at (x, y), all corners rounded with `r`. `0 < r ≤ min(width, height) / 2`; at exactly half it becomes a slot |
| `AddSlot(x1, y1, x2, y2, r)` | Straight slot, cap centres (x1, y1)–(x2, y2), cap radius `r` |
| `AddArcSlot(x1, y1, x2, y2, cx, cy, arcR, slotR)` | Slot along an arc; `slotR` = half width, `arcR > slotR` |
| `AddPolygon(cx, cy, r, sides)` | Regular polygon, circumradius `r`, `sides >= 3` |
| `Load(fileName, layers?)` | Replaces the sketch contents with a .dxf or .gsf file, see below |
| `DxfLayerNames(fileName)` | Layer names of a .dxf in file order → `string[]`; the sketch is not changed. Other files throw |
| `Feature()` | The sketch's own `ICadApiFeature`: `Id()` tells sketches apart, `SetCaption(name)` names it (§10) |

`id` is an optional caller tag returned in `Id`.

```csharp
using var sketchCom = cadCom.AddSketch(3, 0);           // XY plane, z = 0
sketchCom.AddRoundedRectangle(0, 0, 120, 80, 6);        // plate outline, R6 corners
using (var sketchFeatCom = sketchCom.Feature())
    sketchFeatCom.SetCaption("Plate outline");          // name in the feature tree
```

**A closed shape drawn inside another one does not make a hole.** A circle drawn inside a
100 x 40 outline leaves the plate whole: extruding the largest sketch face by 20 gives one solid
with 6 faces. Holes, pockets and slots are cuts — a second sketch on the face plane, extruded into
the body with `bo_Sub`, see §6.

### Loading a .dxf or .gsf

```csharp
using var sketchCom = cadCom.AddSketch(3, 0);
var layers = sketchCom.DxfLayerNames(dxfPath);           // check the names the file declares
sketchCom.Load(dxfPath, new[] { "OUTLINE" });           // or Load(dxfPath) for the whole file
cadCom.Save();
```

- The ENCY process reads the file: the path must be valid for ENCY.
- `layers` null or empty takes the whole file. Names match case-insensitively; a name the file
  does not have throws instead of giving an empty sketch.
- Entities without a layer belong to the layer `""` and load only without a filter.
- Each `Load` replaces the sketch contents. Pass every layer of one profile in one call, or a
  contour split across layers does not close.
- `.gsf` has no layers; `layers` is ignored for it.

**Examples:** [MountingPlateSketchExample](../../Geometry/CadSketchDrawingNet/project/main/MountingPlateSketchExample.cs),
[FlangeGasketSketchExample](../../Geometry/CadSketchDrawingNet/project/main/FlangeGasketSketchExample.cs)

## 4. Sketch faces, contours, segments

| Object | What it is | Access |
|---|---|---|
| `ICadApiSketchFace` | Closed planar region. **The input of extrude / revolve** | `sketchCom.Face(i)`, `FaceCount()`, `Faces()` |
| `ICadApiSketchContour` | One closed loop | `sketchCom.Contour(i)`, `ContourCount()`, `Contours()` |
| `ICadApiSegment` | Line / arc / circle | `Segment(i)`, `SegmentCount()`, `EnumerateSegments()` on sketch, face, contour |

- Face: `Area()` (signed), `BaseZ()` (z inside the sketch LCS, 0 for a flat face).
- Segment → concrete type: `segCom.AsLine()` (`X1`, `Y1`, `X2`, `Y2`), `segCom.AsArc()` (`CX`, `CY`, `R`,
  `StartAngle`, `EndAngle`, `IsCircle`). Null wrapper if the cast does not apply.
- `EnumerateSegments()` disposes each item after the loop step — do not keep it.
- **Face order is not drawing order.** With several shapes in a sketch, `Face(0)` is not
  necessarily the first one drawn. Take the base face by the largest `Area()`:

```csharp
var baseIndex = Enumerable.Range(0, sketchCom.FaceCount())
    .MaxBy(i => { using var f = sketchCom.Face(i); return Math.Abs(f.Area()); });
using var baseFaceCom = sketchCom.Face(baseIndex);
```

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
| `AsFeature()` | Its `ICadApiFeature`: caption, faces, `IsValid`, see §10 |

```csharp
using var faceCom = sketchCom.Face(0);
using var extCom = cadCom.AddExtrude(faceCom, 10);
cadCom.Save();
```

## 6. Boolean operations — cut, union, intersect

`TCadApiBooleanOperation` (namespace `CADAPI.Feature`, on extrude and revolve):

| Value | Effect |
|---|---|
| `bo_New` | New separate body (default) |
| `bo_Add` | Union with the target body |
| `bo_Sub` | Cut: subtract the swept volume from the target body (pocket, hole, slot) |
| `bo_Int` | Keep only the common volume |
| `bo_Auto` | Engine picks the operation |

**Set the target body explicitly.** Without `SetBodyId` the feature works on the body of the face its
sketch lies on; for a sketch on a base plane, on the body made by the last `bo_New` feature before
it. The body found is written into `BodyId`. Only the first feature that makes a body always works
as `bo_New`.
Body ids exist only after `Save()`, so the order is: build the base → `Save` → read body id →
set target → `Save`.

**Holes, pockets and slots are made only this way.** Drawing them inside the base outline does
not cut them (§3).

```csharp
// base: 100 x 40 x 20 plate
using var baseSketchCom = cadCom.AddSketch(3, 0);
baseSketchCom.AddRectangle(0, 0, 100, 40);
using var baseFaceCom = baseSketchCom.Face(0);
using var baseExtCom = cadCom.AddExtrude(baseFaceCom, 20);
cadCom.Save();                                          // one body, 6 faces

string baseBodyId;
using (var bodyCom = cadCom.Body(0))
    baseBodyId = bodyCom.Id();

// through hole: sketch on the top face plane (z = 20), cut down through the plate
using var holeSketchCom = cadCom.AddSketch(3, 20);
holeSketchCom.AddCircle(50, 20, 4);
using var holeFaceCom = holeSketchCom.Face(0);
using var holeExtCom = cadCom.AddExtrude(holeFaceCom, -20);
holeExtCom.SetOperation(TCadApiBooleanOperation.bo_Sub);
holeExtCom.SetBodyId(baseBodyId);
cadCom.Save();                                          // still one body, now 7 faces
```

- Through-cut: make `depth` at least the body thickness (here `-20` or more).
- Pocket or slot: the same cut, shallower — `AddSlot(20, 20, 80, 20, 6)` cut by `-8` gives an
  8 mm deep slot.
- Each hole adds a face: the plate has 6, one hole makes 7, two holes 8. `bodyCom.FaceCount()`
  is a quick check that a cut landed.
- The cutter sketch can also lie on a body face — `AddSketchOnFace`, §11.
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
| `AsFeature()` | Its `ICadApiFeature`, see §10 |

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

`edgeIds` / `faceIds` are `IEnumerable<string>`. Each of the three features has `AsFeature()` (§10).

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
| `FaceCount()` / `FaceId(i)` | Face ids for draft and for `cadCom.Face(faceId)` (§11) |

**The bounding box is not exact.** Once the body has round faces — a cut hole is enough — the box
can stick out of the part by up to about 0.1 mm per side, also along the hole axis. A
100 x 40 x 20 plate with one cut hole gives Z from −0.084 to 20.084; an 80 x 50 x 12 plate gives a
Z size of 12.105. Compare box sizes with a tolerance, not for equality.

## 10. ICadApiFeature — feature tree

Every node of the feature tree is an `ICadApiFeature`: `cadCom.Feature(i)`, `sketchCom.Feature()`,
or `AsFeature()` on a typed feature.

| Helper | Description |
|---|---|
| `Id()` | Stable handle in the tree, e.g. `E1`, `S1` |
| `Caption()` / `SetCaption(name)` | Name in the feature tree. Renaming keeps `Id`; `Save()` stores it in the project. An empty string clears it, and `Caption()` then returns the name made from `Id` (e.g. `#E1`), not the original one |
| `TypeId()` | e.g. `#GeCAD.Extrude`, `#GeCAD.Sketch` |
| `FaceCount()` / `Face(i)` | Topology faces this feature made → `ICadApiFace` (§11) |
| `IsValid()` | Whether the engine built the feature on the last rebuild. Extrude and revolve rebuild when a property changes; fillet, chamfer and draft only on `Save()`, so read it after `Save()` |
| `AsExtrude()`, `AsRevolve()`, `AsFillet()`, `AsChamfer()`, `AsDraft()` | Cast to the typed feature; null wrapper when the type differs |

Typed features (`ICadApiExtrudeFeature`, …) are separate interfaces, not derived from
`ICadApiFeature`: reach captions, faces and `IsValid` through `AsFeature()`.

```csharp
using (var featCom = baseExtCom.AsFeature())             // base extrude from §6
    featCom.SetCaption("Plate");
cadCom.Save();

foreach (var featCom in cadCom.Features())
    using (featCom)
    using (var extCom = featCom.AsExtrude())
        if (!extCom.IsNull)
            Console.WriteLine($"{featCom.Caption()}: depth {extCom.Depth()}, op {extCom.Operation()}, built {featCom.IsValid()}");
```

## 11. Topology faces — sketch on a face, CAM face names

`ICadApiFace` (namespace `CADAPI.Body`) is a handle to one face of a body; its `Id()` is the face id.
It holds only the id and finds the face again on every use, so it stays valid across rebuilds
(another `Save`, a new depth, a draft) while the face exists. Once a cut removes the face,
`AddSketchOnFace` with that handle throws.

| Get a face | Call |
|---|---|
| By a body face id | `cadCom.Face(bodyCom.FaceId(i))`. Edge, plane, feature and unknown ids throw |
| Faces a feature made | `featCom.FaceCount()` / `featCom.Face(i)` |

| Use it | Call |
|---|---|
| Sketch on the face | `cadCom.AddSketchOnFace(faceCom)` → `ICadApiSketch`. Planar faces only; a non-planar face or a face of another CAD model throws |
| Find it in the CAM geometry tree | `cadCom.GetCamFaceFullNames(faceId)` → `string[]` of CAM leaf-face FullNames. Call after `Save()`; a CAD face split in CAM gives several names. Unknown or unpublished ids throw. Resolve again after a rebuild or a move |

```csharp
string topFaceId;
using (var bodyCom = cadCom.Body(0))
    topFaceId = Enumerable.Range(0, bodyCom.FaceCount()).Select(i => bodyCom.FaceId(i))
        .First(id => id.Contains("TopFace"));            // the extrude's end face

using var topFaceCom = cadCom.Face(topFaceId);
using var onFaceSketchCom = cadCom.AddSketchOnFace(topFaceCom);
// draw, extrude with bo_Sub as in §6, Save

string[] camFaces = cadCom.GetCamFaceFullNames(topFaceId);
```

For a face parallel to a base plane, `AddSketch(axis, offset)` gives the same plane with the
sketch axes listed in §3.

## 12. Pitfalls

- **No `Save()` — no geometry.** `BodyCount` is 0 and the viewport is empty until `Save()`.
- **A shape drawn inside the outline is not a hole.** The extruded plate stays whole. Cut holes,
  pockets and slots with `bo_Sub` — §6.
- **`Face(0)` is not the first shape drawn.** Pick the base face by the largest `Area()` — §4.
- **`TCadApiBooleanOperation` not found.** It is in `CADAPI.Feature`: add `using CADAPI.Feature;` — §2.
- **`bo_Sub` without `SetBodyId` cuts only the last body.** With several bodies the cut lands on the
  body made by the last `bo_New` feature; the others stay whole, with no error.
- **Cut goes nowhere.** The swept volume must overlap the body: put the cutter sketch on or inside
  the body (`AddSketch(axis, offset)`) and pick the `depth` sign towards the material. A cut that
  removes nothing is not applied, and `Save()` throws naming it.
- **Bounding box is not exact.** With round faces it is up to about 0.1 mm per side larger than
  the part — compare with a tolerance, §9.
- Dispose every wrapper (`using`) — see [COM lifetime](../general/com-lifetime.md).
