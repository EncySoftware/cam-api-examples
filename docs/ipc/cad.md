# CAD — ENCY CAM IPC

IPC variant of the CAD modelling API, for out-of-process apps.

**Read the CAMAPI CAD reference first:** [`../api/cad.md`](../api/cad.md). Lifecycle, boolean
operations, holes as cuts, the `Save()` rule and the pitfalls are the same. This file lists only
the differences.

Interfaces: `CADIPC.Model`, `CADIPC.Feature`, `CADIPC.Body`. Helpers: `CADIPC.DotnetHelper`.

---

## 1. Getting a CAD model

```csharp
using CADIPC.DotnetHelper;   // CAD helpers: AddSketch, AddExtrude, Save, …
using CADIPC.Feature;        // TCadIpcBooleanOperation, feature interfaces
using CADIPC.Model;          // ICadIpcModel, ICadIpcSketch, sketch faces
using CAMIPC.DotnetHelper;

using var cadNodeCom = geomCom.AddCadGroup("Profile", identityLcs);   // lcs is required over IPC
using var cadCom = cadNodeCom.GetCadApiModel();                       // ICadIpcModel
```

`TCadIpcBooleanOperation` lives in `CADIPC.Feature` (CADIPC.Feature.dll): without
`using CADIPC.Feature;` a cut does not compile. Add `using CADIPC.Body;` only when the code names
`ICadIpcBody` or `ICadIpcFace`.

| CAMAPI | CAMIPC |
|---|---|
| `geomCom.AddCadGroup(name, lcs?)` | `geomCom.AddCadGroup(name, lcs)` — no default for `lcs` |
| `nodeCom.AsCadModel()` → `ICadApiModel` | `nodeCom.GetCadApiModel()` → `ICadIpcModel` |

## 2. Naming

| CAMAPI | CAMIPC |
|---|---|
| `ICadApiModel` | `ICadIpcModel` |
| `ICadApiSketch` | `ICadIpcSketch` |
| `ICadApiSketchFace` / `ICadApiSketchContour` | `ICadIpcSketchFace` / `ICadIpcSketchContour` |
| `ICadApiSegment` / `ICadApiLine` / `ICadApiArc` / `ICadApiPoint` | `ICadIpcSegment` / `ICadIpcLine` / `ICadIpcArc` / `ICadIpcPoint` |
| `ICadApiBody` | `ICadIpcBody` |
| `ICadApiFace` | `ICadIpcFace` |
| `ICadApiFeature` and `…Extrude/Revolve/Fillet/Chamfer/DraftFeature` | `ICadIpcFeature` and `…Extrude/Revolve/Fillet/Chamfer/DraftFeature` |
| `TCadApiBooleanOperation` | `TCadIpcBooleanOperation` — same values `bo_New … bo_Auto` |

Every interface adds `GetInstanceId()`. Raw calls take `ref TExecuteContext` instead of
`out TResultStatus`, and properties become `GetX(ref ctx)` / `SetX(value, ref ctx)`.

## 3. Helpers

Helper names match CAMAPI (`AddSketch`, `Save`, `AddRectangle`, `Face`, `AddExtrude`, `SetOperation`,
`SetBodyId`, `Body`, `Id`, `EdgeId`, …). They create the `TExecuteContext` and throw on `rsError`.

Differences:

| CAMAPI helper | CAMIPC helper |
|---|---|
| `AddFillet / AddChamfer / AddDraft(IEnumerable<string> ids, …)` | Take `IListString ids` |
| `bodyCom.GetBoxMin()` / `GetBoxMax()` → `(X, Y, Z)` tuple | → `TST3DPoint` |
| `revCom.GetAxisPoint()` / `GetAxisDirection()` → `(X, Y, Z)` tuple | → `TST3DPoint` |

### Sketch import, faces, captions

Same helpers and behaviour as CAMAPI ([`../api/cad.md`](../api/cad.md) §3, §10, §11); raw IPC
names:

| Helper (CAMAPI = CAMIPC) | Raw CAMIPC call |
|---|---|
| `cadCom.AddSketchOnFace(faceCom)` | `ICadIpcModel.AddSketchOnFace(face, ref ctx)` |
| `cadCom.Face(faceId)` → `ICadIpcFace` | `ICadIpcModel.GetFace(faceId, ref ctx)` |
| `cadCom.GetCamFaceFullNames(faceId)` → `string[]` | `ICadIpcModel.GetCamFaceFullNames(faceId, ref ctx)` → `IListString` |
| `sketchCom.Load(fileName, layers?)` | `ICadIpcSketch.Load(fileName, IListString layers, ref ctx)` |
| `sketchCom.DxfLayerNames(fileName)` → `string[]` | `ICadIpcSketch.DxfLayerNames(fileName, ref ctx)` → `IListString` |
| `sketchCom.AddRoundedRectangle(x, y, width, height, r)` | `ICadIpcSketch.AddRoundedRectangle(…, ref ctx)` |
| `sketchCom.Feature()` → `ICadIpcFeature` | `ICadIpcSketch.GetFeature(ref ctx)` |
| `featCom.FaceCount()` / `featCom.Face(i)` | `ICadIpcFeature.GetFaceCount` / `GetFace(i, ref ctx)` |
| `featCom.SetCaption(name)` | `ICadIpcFeature.SetCaption(name, ref ctx)` |
| `featCom.IsValid()` | `ICadIpcFeature.GetIsValid(ref ctx)` |
| `extCom.AsFeature()` (all typed features) | cast to `ICadIpcFeature` |
| `faceCom.Id()` | `ICadIpcFace.GetId(ref ctx)` |

`Load` and `DxfLayerNames` take a path that the ENCY process opens, not the client: over IPC the
file must be reachable from the ENCY side.

## 4. Cut example

```csharp
using var baseSketchCom = cadCom.AddSketch(3, 0);
baseSketchCom.AddRectangle(0, 0, 100, 40);
using var baseFaceCom = baseSketchCom.Face(0);
using var baseExtCom = cadCom.AddExtrude(baseFaceCom, 20);
cadCom.Save();

string baseBodyId;
using (var bodyCom = cadCom.Body(0))
    baseBodyId = bodyCom.Id();

using var holeSketchCom = cadCom.AddSketch(3, 20);
holeSketchCom.AddCircle(50, 20, 4);
using var holeFaceCom = holeSketchCom.Face(0);
using var holeExtCom = cadCom.AddExtrude(holeFaceCom, -20);
holeExtCom.SetOperation(TCadIpcBooleanOperation.bo_Sub);
holeExtCom.SetBodyId(baseBodyId);
cadCom.Save();                                          // one body, 7 faces
```
