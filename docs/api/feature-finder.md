# Feature Finder — ENCY CAM API

> Automatic feature recognition: the Feature Finder scans the geometry of the current part
> and reports machinable features (holes, pockets, fillets, chamfers, planes, edges, …) with
> their measured parameters. Use it to drive automatic operation creation or to inspect what
> ENCY recognized on a model.

All .NET examples use the `ComWrapper<T>` pattern and the extension methods from
`CAMAPI.DotnetHelper`. Raw IDL access is shown as secondary "direct access" notes.

---

## Table of contents

1. [Concepts and object model](#1-concepts-and-object-model)
2. [Obtaining the feature finder](#2-obtaining-the-feature-finder)
3. [Running recognition](#3-running-recognition)
4. [Retrieving features](#4-retrieving-features)
5. [ICamApiFeatureList](#5-icamapifeaturelist)
6. [ICamApiFeature — the base feature](#6-icamapifeature--the-base-feature)
7. [Specialised feature interfaces (QueryInterface)](#7-specialised-feature-interfaces-queryinterface)
8. [Driving operation creation from a feature](#8-driving-operation-creation-from-a-feature)
9. [The FeatureFinderUpdated event](#9-the-featurefinderupdated-event)
10. [Enum reference](#10-enum-reference)

---

## 1. Concepts and object model

Feature recognition is owned by the project. Each recognized feature is an
`ICamApiFeature` that carries a common set of measured values (type, status, LCS, Z-range,
the geometry entities it was built from) plus, for most types, an **extended interface**
reached by `QueryInterface` that exposes type-specific measurements (a hole's diameter,
a pocket's depth, a chamfer's size, …).

```
ICamApiProject
  └─ ICamApiFeatureFinder            — recognition service (one per project)
       └─ ICamApiFeatureList         — a snapshot list of recognized features
            └─ ICamApiFeature        — one feature (base interface)
                 ├─ SubFeature[i]     — nested features (e.g. steps of a complex hole)
                 ├─ BaseEntityName[i] — geometry entities the feature was built from
                 └─ (QI) ICamApiHoleFeature / ICamApiPocketFeature / ICamApiFilletFeature /
                         ICamApiChamferFeature / ICamApiPlaneFeature / ICamApiEdgeFeature /
                         ICamApiTangentEdgeFeature / ICamApiComplexHoleFeature ...
```

A feature list is a **snapshot**: it reflects the recognition state at the moment you asked
for it. Re-query (or watch `UpdateStamp`) after recognition runs again.

---

## 2. Obtaining the feature finder

The finder is a property of the active project.

```csharp
using var appCom      = ComWrapper.Create(context.CamApplication);
using var projectCom  = appCom.GetActiveProject();
using var finderCom   = projectCom.FeatureFinder();   // ComWrapper<ICamApiFeatureFinder>
```

Helper class: `FeatureFinderHelper` in `CAMAPI.DotnetHelper`.

> **Direct access (IDL):** `ICamApiProject.FeatureFinder` is a read-only property returning
> `ICamApiFeatureFinder*`.

---

## 3. Running recognition

Recognition is not automatic for API callers — start it explicitly. It can run
**synchronously** (block until done) or **in the background**.

### Synchronous

```csharp
using var finderCom = projectCom.FeatureFinder();
finderCom.RunRecognition(waitForCompletion: true);   // returns after all features are found
```

### Background + polling

```csharp
finderCom.RunRecognition(waitForCompletion: false);  // returns immediately

while (finderCom.IsUpdating())
{
    double pct = finderCom.RecognitionProgress();     // 0..100
    // update UI, then yield
}
```

### Background + event

Subscribe to `FeatureFinderUpdated` (see §9) instead of polling — recommended for UI plugins.

| Member (helper) | Description |
|---|---|
| `finderCom.RunRecognition(waitForCompletion)` | Start recognition; block when `waitForCompletion` is `true` |
| `finderCom.CancelRecognition()` | Abort an in-progress background recognition; no-op if nothing is running. `IsUpdating` clears once the worker stops |
| `finderCom.IsUpdating()` | `true` while background recognition is running |
| `finderCom.RecognitionProgress()` | Current progress `0..100`, read while updating |
| `finderCom.UpdateStamp()` | Integer bumped every time the feature list changes |

> **Direct access (IDL):** `RunRecognition(in boolean WaitForCompletion, out TResultStatus)`,
> `CancelRecognition(out TResultStatus)`;
> `IsUpdating`, `RecognitionProgress`, `UpdateStamp` are read-only properties.

---

## 4. Retrieving features

All retrieval helpers return a `ComWrapper<ICamApiFeatureList>` (dispose it with `using`).

| Helper | Returns |
|---|---|
| `finderCom.GetFeatures(includeDeleted)` | Features of the **current part setup** |
| `finderCom.GetAllFeatures(includeDeleted)` | Features across **all setups** of the model |
| `finderCom.GetSelectedFeatures()` | Only the features currently selected in the UI |
| `finderCom.GetFeatureById(id)` | A single `ICamApiFeature?` by its unique id (`null` if not found) |
| `finderCom.GetFeaturesForNode(nodeName, useRefMatrix, refMatrix)` | Recognize candidates for one geometry tree node (by `FullName`) |
| `finderCom.GetFeaturesForSelected()` | Recognize candidates aggregated over the selected geometry nodes |
| `GetFeaturesByType(featureType, out ret)` | **All** recognized features of one type across the whole model — no helper, call via `Invoke` |

`includeDeleted` defaults to `false`; pass `true` to include features whose status is
`cafsDeleted`.

### GetFeaturesByType — when GetFeatures comes back under-typed

`GetFeatures` returns the **cached whole-model** recognition result, which routinely reports
generic features where a specific type exists — so a `QueryInterface` to
`ICamApiPocketFeature`, `ICamApiFilletFeature`, `ICamApiChamferFeature` or
`ICamApiPlaneFeature` yields `null` and the specific data is unreachable.

`GetFeaturesByType` avoids that: it scans **every** geometry tree node with per-node
recognition — the same path the right-click *Recognize* menu uses — and returns the
deduplicated instances of the requested type, including the ones the cached set omits.

```csharp
using var listCom = finderCom.InvokeAndWrap(f =>
{
    var list = f.GetFeaturesByType(TCamApiFeatureType.caftPocket, out var status);
    if (status.Code == TResultStatusCode.rsError)
        throw new Exception(status.Description);
    return list;
});

foreach (var featureCom in listCom.Enumerate())
{
    using var pocketCom = featureCom.AsInstanceOf<ICamApiPocketFeature>();
    if (!pocketCom.IsNull)
    {
        // typed pocket data is actually available here
    }
}
```

> It returns an empty list when the model has no feature of that type — an empty result is
> not an error.

> **It re-recognizes per node, so it is far more expensive than `GetFeatures`.** Call it for
> the one type you need, not in a loop over every `TCamApiFeatureType`.

### Typical read loop

```csharp
using var finderCom = projectCom.FeatureFinder();
finderCom.RunRecognition(waitForCompletion: true);

using var listCom = finderCom.GetFeatures(includeDeleted: false);
foreach (var featureCom in listCom.Enumerate())   // featureCom auto-disposed each iteration
{
    Console.WriteLine($"{featureCom.Caption()}  [{featureCom.FeatureType()}]  " +
                      $"Z {featureCom.ZMin():0.###}..{featureCom.ZMax():0.###}");
}
```

> **`GetFeatures` vs `GetFeaturesForSelected` / `GetFeaturesForNode`:**
> `GetFeatures`/`GetAllFeatures` return the already-recognized feature tree of the part.
> The `...ForNode` / `...ForSelected` variants run recognition against a specific node (or
> the current geometry selection) and return the candidates — use them when building an
> operation from a user pick.

---

## 5. ICamApiFeatureList

A read-only, index-addressable snapshot of features.

| Member (helper) | Description |
|---|---|
| `listCom.Count()` | Number of features |
| `listCom.GetFeature(i)` | `ComWrapper<ICamApiFeature>` at index `0..Count-1` |
| `listCom.Enumerate()` | `IEnumerable<ComWrapper<ICamApiFeature>>`; each item is disposed by the enumerator after your loop body — **do not** dispose it yourself |

Helper class: `FeatureListHelper`.

> **Direct access (IDL):** `Count` (read-only) and `Feature[Index]` (indexed read-only property).

---

## 6. ICamApiFeature — the base feature

Every feature exposes the same base measurements. Helper class: `FeatureHelper`.

| Member (helper) | Type | Description |
|---|---|---|
| `Id()` | `string` | Unique identifier (stable within a recognition run) |
| `Caption()` | `string` | Human-readable caption |
| `FeatureType()` | `TCamApiFeatureType` | Discriminator — see §10 |
| `Status()` | `TCamApiFeatureStatus` | Job / due / ready / deleted — see §10 |
| `IsValid()` | `bool` | `true` when geometrically valid |
| `IsMachined()` | `bool` | `true` when marked as already machined |
| `ZMin()`, `ZMax()` | `double` | Z-extent in the feature's own LCS |
| `Lcs()` | `TST3DMatrix` | Local coordinate system (axis + position) |
| `Selected()` / `SetSelected(bool)` | `bool` | Selection state; setting it highlights the feature and its base entities in the viewport |
| `Highlighted()` / `SetHighlighted(bool)` | `bool` | Transient viewport highlight of the base entities. Unlike `Selected`, it does **not** change the actual selection — use it for hover-preview; it is not captured as a macro selection step |
| `SubFeatureCount()` | `int` | Number of sub-features |
| `GetSubFeature(i)` | `ComWrapper<ICamApiFeature>` | Sub-feature at index `0..SubFeatureCount-1` |
| `BaseEntityCount()` | `int` | Number of base geometry entities |
| `BaseEntityName(i)` | `string` | Name (or RefID) of base entity `i` |
| `BaseEntityNames()` | `IReadOnlyList<string>` | All base entity names |

### Reading base entities and sub-features

```csharp
using var featureCom = listCom.GetFeature(0);

foreach (var name in featureCom.BaseEntityNames())
    Console.WriteLine("  base entity: " + name);

for (int i = 0; i < featureCom.SubFeatureCount(); i++)
{
    using var subCom = featureCom.GetSubFeature(i);
    Console.WriteLine("  sub-feature: " + subCom.Caption());
}
```

---

## 7. Specialised feature interfaces (QueryInterface)

Most feature types implement an extended interface with type-specific measurements. Obtain it
with the `As…Feature` helpers on `FeatureHelper`; each returns a wrapper that may be wrapping
`null` — always check `.IsNull` before use. Measurement accessors live on `FeatureSubtypesHelper`.

```csharp
using var featureCom = listCom.GetFeature(i);

using var holeCom = featureCom.AsHoleFeature();
if (!holeCom.IsNull)
{
    double d     = holeCom.Diameter();
    double depth = holeCom.Height();
    bool   blind = holeCom.IsBlind();
}
```

### ICamApiHoleFeature (simple hole)

`Diameter`, `Height` (depth), `TipAngle`, `TaperAngle`, `CircumAngle`, `IsBlind`,
`IsCrossHole`, `TopRc`, `BtmRc`, `ThreadOD`, `ThreadHeight`, `ThreadZMax`, `ThreadZMin`,
`ChamferSize`, `ChamferAngle` — all `double` except the two `bool` flags.
`ThreadOD == 0` means no thread; `TipAngle == 0` means a flat bottom.

### ICamApiComplexHoleFeature (multi-step / grooved hole)

| Member | Type | Description |
|---|---|---|
| `IsConicalHole()` | `bool` | Conical overall shape |
| `CircumAngle()` | `double` | Arc coverage (360 = full) |
| `GrooveCount()` | `int` | Number of grooves |
| `GetGroove(i)` | `ComWrapper<ICamApiFeature>` | Groove `i`; QI to `ICamApiHoleGrooveFeature` |

### ICamApiHoleGrooveFeature (undercut / step inside a complex hole)

`Index`, `ChildGrooveCount` (`int`); `ShapeType` (`TCamApiHoleGrooveShapeType`, §10);
`ShapeDiameter`, `ShapeHeight`, `ShapeDepth`, `ShapeRadius`, `ShapeTopRc`, `ShapeBtmRc`
(`double`); `ShapeIsBlind` (`bool`). `ShapeRadius` is meaningful only for round shapes;
`ShapeTopRc`/`ShapeBtmRc` only for square shapes (0 otherwise).

```csharp
using var complexCom = featureCom.AsComplexHoleFeature();
if (!complexCom.IsNull)
{
    for (int g = 0; g < complexCom.GrooveCount(); g++)
    {
        using var grooveFeatureCom = complexCom.GetGroove(g);
        using var grooveCom = grooveFeatureCom.InvokeAndWrap(f => f as ICamApiHoleGrooveFeature);
        if (!grooveCom.IsNull)
            Console.WriteLine($"groove {grooveCom.Index()}: {grooveCom.ShapeType()} " +
                              $"Ø{grooveCom.ShapeDiameter():0.###}");
    }
}
```

### ICamApiPocketFeature (pocket / boss / side)

| Member | Type | Description |
|---|---|---|
| `Height()` | `double` | Depth |
| `BottomRadius()` | `double` | Bottom corner radius |
| `MinCornerRadius()` | `double` | Smallest contour corner radius |
| `PocketType()` | `TCamApiPocketType` | pocket / boss / side — see §10 |
| `IsClosed()` | `bool` | Fully enclosed contour |
| `IsThroughFeature()` | `bool` | Goes through the part |
| `SideLength()` | `double` | Characteristic side length (rectangular pockets) |
| `Convexity()` | `int` | Contour convexity |

### ICamApiFilletFeature / ICamApiChamferFeature / ICamApiPlaneFeature / ICamApiEdgeFeature

Each exposes a single `Size()` (`double`): fillet radius, chamfer size, planar-face
characteristic size, and edge-feature characteristic size respectively. Obtain via
`AsFilletFeature()`, `AsChamferFeature()`, `AsPlaneFeature()`, `AsEdgeFeature()`.

### ICamApiTangentEdgeFeature

| Member | Type | Description |
|---|---|---|
| `Size()` | `double` | Characteristic size |
| `IsSmooth()` | `bool` | G1 (smooth) continuity |
| `IsConvex()` | `bool` | Convex edge |
| `EdgeGeomType()` | `TCamApiEdgeGeomType` | arc / line / circle / polygon — see §10 |

### Dispatch by type

```csharp
using var featureCom = listCom.GetFeature(i);
switch (featureCom.FeatureType())
{
    case TCamApiFeatureType.caftHole:
    {
        using var holeCom = featureCom.AsHoleFeature();
        if (!holeCom.IsNull)
            Report($"hole Ø{holeCom.Diameter():0.###} depth {holeCom.Height():0.###}");
        break;
    }
    case TCamApiFeatureType.caftPocket:
    case TCamApiFeatureType.caftBoss:
    case TCamApiFeatureType.caftSide:
    {
        using var pocketCom = featureCom.AsPocketFeature();
        if (!pocketCom.IsNull)
            Report($"{pocketCom.PocketType()} depth {pocketCom.Height():0.###}");
        break;
    }
    case TCamApiFeatureType.caftFillet:
    {
        using var filletCom = featureCom.AsFilletFeature();
        if (!filletCom.IsNull)
            Report($"fillet R{filletCom.Size():0.###}");
        break;
    }
}
```

---

## 8. Driving operation creation from a feature

`SelectFeatureByBaseEntities` mirrors the interactive "recognize → pick" flow: it deselects
everything in the geometry model, then selects the base entities of the recognized feature
whose base-entity names cover `baseEntityNames` (newline-separated) and whose type equals
`featureType`. A subsequent `CreateOperation` then consumes that selection into its Job
Assignment. Returns `false` if no feature matched.

```csharp
using var finderCom = projectCom.FeatureFinder();
finderCom.RunRecognition(waitForCompletion: true);

// Collect the base entities of the holes we want to machine
string entityNames;
using (var listCom = finderCom.GetFeatures(includeDeleted: false))
{
    using var holeFeatureCom = listCom.GetFeature(0);
    entityNames = string.Join("\n", holeFeatureCom.BaseEntityNames());
}

// Select them, then create a drilling operation that picks them up
bool matched = finderCom.SelectFeatureByBaseEntities(
    entityNames, (int)TCamApiFeatureType.caftHole);
if (matched)
{
    // technologistCom.CreateOperation(...) — see project.md
}
```

---

## 9. The FeatureFinderUpdated event

For background recognition, subscribe instead of polling. Register the handler on the
**project** with the event key `"FeatureFinderUpdated"`; ENCY calls it when the feature list
changes.

```csharp
// The handler must implement ICamApiEventHandler (base) plus the specific handler interface.
public class FeatureFinderUpdatedHandler : ICamApiEventHandler, ICamApiHandlerFeatureFinderUpdated
{
    // ICamApiEventHandler
    public bool GetAsyncMode(string interfaceUid) => false;   // called synchronously

    // ICamApiHandlerFeatureFinderUpdated
    public void FeatureFinderUpdated(string handlerIdent)
    {
        // recognition finished (or the list changed) — re-query GetFeatures() here
    }
}

// register (typically when opening your window)
var handler = new FeatureFinderUpdatedHandler();
var events  = new ListString();
events.Add(typeof(ICamApiHandlerFeatureFinderUpdated).GUID.ToString("B"));
projectCom.Invoke(p => p.RegisterHandler("MyPlugin.FeatureFinder", handler, events, out _));

// ... later, when tearing down:
projectCom.Invoke(p => p.UnregisterHandler("MyPlugin.FeatureFinder", out _));
```

The `events` list holds the GUIDs of the handler interfaces your object implements (an empty
`ListString` registers all of them). `handlerIdent` is the identifier you pass to
`RegisterHandler` and receive back in the callback, so one handler can disambiguate multiple
registrations. See [application.md](application.md#application-event-handlers) for the general
event registration pattern.

> **Direct access (IDL):** `ICamApiHandlerFeatureFinderUpdated.FeatureFinderUpdated(in string HandlerIdent)`.

---

## 10. Enum reference

### TCamApiFeatureType

| Value | Name | Extended interface (QI) |
|---|---|---|
| 0 | `caftUnknown` | — |
| 1 | `caftHole` | `ICamApiHoleFeature` |
| 2 | `caftComplexHole` | `ICamApiComplexHoleFeature` |
| 3 | `caftPocket` | `ICamApiPocketFeature` |
| 4 | `caftBoss` | `ICamApiPocketFeature` |
| 5 | `caftSide` | `ICamApiPocketFeature` |
| 6 | `caftFillet` | `ICamApiFilletFeature` |
| 7 | `caftChamfer` | `ICamApiChamferFeature` |
| 8 | `caftPlane` | `ICamApiPlaneFeature` |
| 9 | `caftSmooth` | — |
| 10 | `caftOpenLoopEdge` | `ICamApiEdgeFeature` |
| 11 | `caftWindowEdge` | `ICamApiEdgeFeature` |
| 12 | `caftTangentEdge` | `ICamApiTangentEdgeFeature` |
| 13 | `caftLoop1` | `ICamApiEdgeFeature` |
| 14 | `caftLoop2` | `ICamApiEdgeFeature` |
| 15 | `caftBorder` | — |
| 16 | `caftRev` | — |
| 17 | `caftRuled` | — |

### TCamApiFeatureStatus

| Value | Name | Meaning |
|---|---|---|
| 0 | `cafsJob` | Assigned to a job |
| 1 | `cafsDue` | Recognized, awaiting machining |
| 2 | `cafsReady` | Machined / done |
| 3 | `cafsDeleted` | Removed (returned only when `includeDeleted = true`) |

### TCamApiPocketType

| Value | Name |
|---|---|
| 0 | `captPocket` |
| 1 | `captBoss` |
| 2 | `captSide` |

### TCamApiEdgeGeomType

| Value | Name |
|---|---|
| 0 | `caegtArc` |
| 1 | `caegtLine` |
| 2 | `caegtCircle` |
| 3 | `caegtPolygon` |

### TCamApiHoleGrooveShapeType

| Value | Name |
|---|---|
| 0 | `cahgsGeneral` |
| 1 | `cahgsSquare` |
| 2 | `cahgsTrapezoid` |
| 3 | `cahgsRound` |
