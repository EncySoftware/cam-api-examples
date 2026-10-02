# CAD — ENCY CAM IPC

IPC variant of the CAD modelling API, for out-of-process apps.

**Read the CAMAPI CAD reference first:** [`../api/cad.md`](../api/cad.md). Lifecycle, boolean
operations, the `Save()` rule and the pitfalls are the same. This file lists only the differences.

Interfaces: `CADIPC.Model`, `CADIPC.Feature`, `CADIPC.Body`. Helpers: `CADIPC.DotnetHelper`.

---

## 1. Getting a CAD model

```csharp
using CADIPC.DotnetHelper;
using CAMIPC.DotnetHelper;

using var cadNodeCom = geomCom.AddCadGroup("Profile", identityLcs);   // lcs is required over IPC
using var cadCom = cadNodeCom.GetCadApiModel();                       // ICadIpcModel
```

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

using var cutSketchCom = cadCom.AddSketch(3, 20);
cutSketchCom.AddSlot(20, 20, 80, 20, 6);
using var cutFaceCom = cutSketchCom.Face(0);
using var cutExtCom = cadCom.AddExtrude(cutFaceCom, -8);
cutExtCom.SetOperation(TCadIpcBooleanOperation.bo_Sub);
cutExtCom.SetBodyId(baseBodyId);
cadCom.Save();
```
