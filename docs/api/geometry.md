# Geometry — ENCY CAM API

This document covers the full geometry domain of the ENCY CAM API: the model tree, entity inspection, B-rep traversal (Face / Loop / CoEdge / Surface / Curve), mesh access, coordinate systems, geometry import, interactive picking, and the geometry tree context-menu extension point.

All .NET examples use the `ComWrapper<T>` pattern and the extension methods from `CAMAPI.DotnetHelper`. Raw IDL access is shown as secondary "direct access" notes where it differs meaningfully.

---

## Table of contents

1. [Concepts and object model](#1-concepts-and-object-model)
2. [ICAMAPIGeomLibrary — factory](#2-icamapigeomlibrarymdash-factory)
3. [ICAMAPIGeometryModel — model root](#3-icamapigeometrymodel-model-root)
4. [ICAMAPIGeometryTreeNode — tree navigation](#4-icamapigeometrytreenode-tree-navigation)
5. [Traversing the geometry tree](#5-traversing-the-geometry-tree)
6. [ICAMAPIGeometryEntity — entity inspection](#6-icamapigeometryentity-entity-inspection)
7. [B-rep structure: Face, Loop, CoEdge](#7-b-rep-structure-face-loop-coedge)
8. [ICamApiSurface — underlying surface](#8-icamapisurface-underlying-surface)
9. [ICamApiCurve and ICamApiCurve5D](#9-icamapicurve-and-icamapicurve5d)
10. [ICamApiMesh — triangulated representation](#10-icamapimesh-triangulated-representation)
11. [ICamApiCoordinateSystem](#11-icamapicoordinatesystem)
12. [ICAMAPIGeometryImporter — importing files](#12-icamapigeometryimporter-importing-files)
13. [ICamApiGeomPicker — interactive picking](#13-icamapigeompicker-interactive-picking)
14. [IExtensionGeomModelNodePopup — context menu](#14-iextensiongeommodelnodepopup-context-menu)
15. [ICMAPITurnGeneratrixExtractor — lathe generatrix](#15-icmapiturngenatrixextractor-lathe-generatrix)
16. [Export helpers](#16-export-helpers)
17. [Entity type reference](#17-entity-type-reference)
18. [ICamApiGeometryModelSketcher — creating primitives](#18-icamapigeometrymodelsketcher-creating-primitives)
19. [ICamApiPointSnapper — snapping points to faces](#19-icamapipointsnapper-snapping-points-to-faces)

---

## 1. Concepts and object model

The geometry domain is built around a **tree of named nodes**. Each node (`ICAMAPIGeometryTreeNode`) carries one `ICAMAPIGeometryEntity` payload that describes what the node represents (group, face, curve, coordinate system, mesh, etc.). The tree is rooted at `ICAMAPIGeometryModel.RootNode`.

```
ICAMAPIGeomLibrary            — factory: creates models, importers, extractors
  └─ ICAMAPIGeometryModel     — the whole geometry model (owns the tree)
       ├─ ICAMAPIGeometryTreeNode  — one node in the tree
       │    └─ ICAMAPIGeometryEntity  — the actual geometry payload
       │         ├─ ICamApiFaceGeometryEntity  → ICamApiFace
       │         │    ├─ ICamApiLoop           → ICamApiCoEdge → ICamApiSurfaceCurve
       │         │    └─ ICamApiSurface        (NURBS form available)
       │         ├─ ICamApiCurveGeometryEntity → ICamApiCurve / ICamApiCurve5D
       │         └─ ICamApiCSGeometryEntity    (4×4 matrix)
       └─ ICAMAPIGeometryTreeNodeIterator  — depth-first tree walker
```

The library (`ICAMAPIGeomLibrary`) is accessed from the active project via `ICamApiProject.CAMAPIGeomModel` (model) and `ICamApiProject.GeomImporter` (importer), or created standalone through the library factory. It is **not** obtained through `ICAMAPIGeomLibrary` directly in most extension scenarios — instead the project exposes the model and importer directly.

---

## 2. ICAMAPIGeomLibrary — factory

`ICAMAPIGeomLibrary` is a low-level factory used when creating geometry models outside of a project context (e.g., in standalone utilities or unit tests).

### Properties

| Property | Type | Access | Description |
|---|---|---|---|
| `HideUserMessages` | `bool` | R/W | Suppress interactive dialogs during import |
| `VisTolerancePercent` | `int` | R/W | Visualization chord tolerance, 0–100 |
| `SearchFontFolder` | `string` | R/W | Priority folder for font files (useful in CI/VM environments) |

### Methods

| Method | Returns | Description |
|---|---|---|
| `CreateGeometryModel()` | `ICAMAPIGeometryModel` | Create a new empty geometry model |
| `CreateGeometryImporter()` | `ICAMAPIGeometryImporter` | Create an importer bound to no model yet |
| `CreateTurnGeneratrixExtractor()` | `ICMAPITurnGeneratrixExtractor` | Create a lathe generatrix extractor |
| `CreateFacesToTriangulatedFilesConverter()` | `ICamApiFacesToTriangulatedFilesConverter` | Create a face-to-mesh file converter |

### .NET helper usage

```csharp
// geomLibCom is a ComWrapper<ICAMAPIGeomLibrary>
geomLibCom.SetHideUserMessages(true);
using var modelCom = geomLibCom.CreateGeometryModel();
using var importerCom = geomLibCom.CreateGeometryImporter();
```

Helper class: `GeomLibraryHelper` in `CAMAPI.DotnetHelper`.

---

## 3. ICAMAPIGeometryModel — model root

Obtained in extension code via the active project:

```csharp
using var modelCom = new ComWrapper<ICAMAPIGeometryModel>(activeProject.CAMAPIGeomModel);
```

### Properties

| Property | Type | Access | Description |
|---|---|---|---|
| `RootNode` | `ICAMAPIGeometryTreeNode` | R | Root of the geometry tree |
| `ActiveNode` | `ICAMAPIGeometryTreeNode` | R/W | Node that was most recently imported or activated |

### Methods

| Method | Description |
|---|---|
| `GetNodes()` | Returns `ICAMAPIGeometryTreeNodeIterator` for a depth-first walk |
| `FindByFullName(fullName)` | Find a node by its full path name (e.g. `"Root/Body/Face_1"`) |
| `Transform(node, matrix)` | Apply a `TST3DMatrix` transformation to a node |
| `ExportSelectedToSTL(fileName)` | Export selected nodes to STL |
| `ExportSelectedToOSD(fileName)` | Export selected nodes to OSD |
| `ExportSelectedToDXF(fileName)` | Export selected nodes to DXF |
| `DeselectAll()` | Deselect all nodes |
| `DeleteNode(node)` | Remove a node from the tree |
| `GetFaceListOfSelected()` | Returns `ICAMAPIFaceList` of selected face nodes |
| `AddGroup(parentNode)` | Add an empty group node under `parentNode` |
| `AddCadGroup(name, lcs?)` | Add a CAD-typed group (a fresh GeCAD model) at the given LCS; helper defaults `lcs` to identity. The returned node hosts an editable `ICadApiModel` — reach it with `AsCadModel()` from CADAPI.DotnetHelper |
| `ExportSelectedToStep(fileName)` | Export selected nodes to a STEP file (experimental) |

Helper: `modelCom.ExportSelectedToStep(path)`, `modelCom.AddCadGroup("Fixtures")`. `AddCadGroup`
is the entry point to the CAD axis (GeCAD) that sits on top of the geometry model.

### Finding a node by name

```csharp
using var nodeCom = modelCom.InvokeAndWrap(m =>
    (m.FindByFullName("Root/Body/Face_1", out var status), status));
```

### Applying a transformation

```csharp
// Shift a node by (100, 200, 0)
var shiftMatrix = T3DMatrix.MakeShiftMatrix(new T3DPoint { X = 100, Y = 200, Z = 0 });
// Nest Invoke so the node reference is acquired inside its own wrapper's MTA context
geomNodeCom.Invoke(node =>
    modelCom.Invoke(m => m.Transform(node, shiftMatrix)));
```

See full example: `Geometry/ExtensionUtilityGeometryModelNet/project/main/GeometryNodeTransformExample.cs`

### Exporting the active node

```csharp
using var modelCom = new ComWrapper<ICAMAPIGeometryModel>(activeProject.CAMAPIGeomModel);
modelCom.Invoke(m => m.ExportSelectedToOSD(exportedFilePath, out var status));
```

See full example: `Geometry/ExtensionUtilityGeometryModelNet/project/main/GeometryModelExportExample.cs`

> **Direct access (IDL):** `ExportSelectedToOSD` takes an `[out] TResultStatus*` parameter. The .NET helper wraps this and throws on error.

---

## 4. ICAMAPIGeometryTreeNode — tree navigation

Each node in the geometry tree exposes parent/child/sibling links, forming a classical linked-tree structure.

### Properties

| Property | Type | Access | Description |
|---|---|---|---|
| `Name` | `string` | R | Short name of this node |
| `FullName` | `string` | R | Full path from root (e.g. `"Root/Body/Face_1"`) |
| `Selected` | `bool` | R/W | Selection state |
| `Visible` | `bool` | R/W | Visibility state |
| `Parent` | `ICAMAPIGeometryTreeNode` | R | Parent node (`null` for root) |
| `Child` | `ICAMAPIGeometryTreeNode` | R | First child node (`null` if none) |
| `Sibling` | `ICAMAPIGeometryTreeNode` | R | Next sibling node (`null` if none) |
| `GeometryEntity` | `ICAMAPIGeometryEntity` | R | The geometry payload |

### .NET helper

The `GeometryTreeNodeHelper` extension class provides typed wrappers for all properties, plus:

```csharp
// Enumerate only the direct children of a node
foreach (var childCom in nodeCom.EnumerateChildren())
{
    Console.WriteLine(childCom.Name());
    childCom.Dispose();
}
```

### Rendering and tessellation

| Helper method | Type | Description |
|---|---|---|
| `DoubleNormal()` / `SetDoubleNormal(v)` | `bool` | Double-sided rendering — mill both sides of surfaces (vs. only the normal side) |
| `VisTol()` / `SetVisTol(v)` | `double` | Visual tessellation tolerance (lower = finer mesh) |
| `VisMeshU()` / `SetVisMeshU(v)` | `int` | Isoparametric curves shown in the U direction |
| `VisMeshV()` / `SetVisMeshV(v)` | `int` | Isoparametric curves shown in the V direction |

### User parameters on a node

Nodes carry free-form name/value parameters typed by `TCAMAPIGeomParamType` (`aptInteger`=0,
`aptReal`=1, `aptString`=2, `aptBoolean`=3). System parameters added by the engine or at import
time cannot be overwritten or deleted (attempting to do so throws).

| Helper method | Description |
|---|---|
| `ParamsCount()` | Number of parameters on the node |
| `GetParamName(i)` / `GetParamValue(i)` / `GetParamType(i)` | Read parameter `0..ParamsCount-1` |
| `AddParam(name, type, value)` | Add or overwrite a user parameter |
| `DeleteParam(name)` | Remove a user parameter; `false` if it did not exist |

```csharp
nodeCom.AddParam("BatchNo", TCAMAPIGeomParamType.aptString, "A-17");
for (int i = 0; i < nodeCom.ParamsCount(); i++)
    Console.WriteLine($"{nodeCom.GetParamName(i)} = {nodeCom.GetParamValue(i)}");
```

### CAD content of a node

For a CAD-typed node (its geometry is a GeCAD model), `Invoke(node => node.AsCadModel(out var st))`
returns an `ICadApiModel` for inspecting or mutating the CAD content; it is `nil` for non-CAD
nodes (mesh, body, plain folders). This is the same CAD axis reached by
`ICAMAPIGeometryModel.AddCadGroup`. See `.claude/docs/cadapi-bridge.md` in the CAM repo for the
CADAPI/CADIPC layer.

---

## 5. Traversing the geometry tree

### Preferred approach: iterator + LINQ

`ICAMAPIGeometryTreeNodeIterator` performs a **depth-first** walk. The `AsEnumerable()` extension method from `GeometryTreeNodeIteratorHelper` wraps it as a standard `IEnumerable`, with correct COM disposal.

```csharp
using var iteratorCom = modelCom.InvokeAndWrap(m =>
    (m.GetNodes(out var status), status));

foreach (var nodeCom in iteratorCom.AsEnumerable())
{
    // nodeCom is a ComWrapper<ICAMAPIGeometryTreeNode>
    // It is automatically disposed after the loop body
    if (nodeCom.EntityType() == TCAMAPIGeometryEntityType.etFace)
        Console.WriteLine(nodeCom.FullName());
}
```

The iterator exposes three navigation methods:

| Method | Returns | Description |
|---|---|---|
| `MoveToChild()` | `bool` | Move down to first child; returns `false` if none |
| `MoveToSibling()` | `bool` | Move to next sibling; returns `false` if none |
| `MoveToParent()` | `bool` | Move up to parent; returns `false` at root |
| `Current()` | `ICAMAPIGeometryTreeNode` | Current node |
| `Reset()` | — | Reset to tree root |

### Manual low-level traversal (explicit MoveToChild / MoveToSibling)

When you need direct control of the iterator (instead of the `AsEnumerable()` wrapper above), use `Invoke` / `InvokeAndWrap` for every access. Navigate to the deepest first child, then walk siblings:

```csharp
using var iteratorCom = modelCom.InvokeAndWrap(m => m.GetNodes(out var status));

// descend to the leaf level
while (iteratorCom.Invoke(it => it.MoveToChild())) { }

// walk all siblings at that level
do
{
    using var nodeCom = iteratorCom.InvokeAndWrap(it => it.Current());
    if (nodeCom.EntityType() == TCAMAPIGeometryEntityType.etFace)
        Console.WriteLine(nodeCom.Name());
} while (iteratorCom.Invoke(it => it.MoveToSibling()));
```

See full example: `Geometry/ExtensionUtilityGeometryModelNet/project/main/GeometryNodesIteratorExample.cs`

### Collecting selected nodes

```csharp
var selectedNodes = new List<ComWrapper<ICAMAPIGeometryTreeNode>>();
foreach (var nodeCom in iteratorCom.AsEnumerable())
{
    if (nodeCom.Selected())
        selectedNodes.Add(nodeCom.TransferOwnership()); // take ownership away from the iterator
}
```

See full example: `Geometry/UtilityGeometryEntityReader/project/main/ExtensionGeometryEntityReader.cs`

---

## 6. ICAMAPIGeometryEntity — entity inspection

Each tree node carries one `ICAMAPIGeometryEntity`. The entity's `EntityType` property determines what additional interface is available.

### ICAMAPIGeometryEntity properties

| Property | Type | Access | Description |
|---|---|---|---|
| `EntityClass` | `string` | R | Internal class name string |
| `EntityType` | `TCAMAPIGeometryEntityType` | R | Discriminator enum (see §17) |
| `Color` | `int` | R/W | RGB color packed as integer |
| `ParentColor` | `bool` | R/W | When `true`, entity inherits color from its parent node |
| `UpdateStamp` | `long` | R | Monotonically increasing change counter |

### GetBoundBox

```csharp
// Pass an identity matrix for the world bounding box
var identity = T3DMatrix.Unit;
var box = entityCom.GetBoundBox(identity);
// box.Min and box.Max are TST3DPoint
```

> **Direct access (IDL):** `GetBoundBox(in LCS: TST3DMatrix, [out] TResultStatus*): TST3DBox`

### Specialised entity interfaces

The raw COM object also implements one of the following interfaces depending on `EntityType`. Query with a direct cast inside `nodeCom.Invoke(...)`:

#### ICamApiCSGeometryEntity (etCS)

```csharp
nodeCom.Invoke(node =>
{
    if (node.GeometryEntity is ICamApiCSGeometryEntity entityCs)
    {
        var matrix = entityCs.Matrix; // TST3DMatrix — position/orientation
        Console.WriteLine($"CS origin: {matrix.vT.X}, {matrix.vT.Y}, {matrix.vT.Z}");
    }
});
```

#### ICamApiFaceGeometryEntity (etFace)

```csharp
nodeCom.Invoke(node =>
{
    if (node.GeometryEntity is ICamApiFaceGeometryEntity entityFace)
    {
        using var faceCom = ComWrapper.Create(entityFace.Face);
        // work with ICamApiFace — see §7
    }
});
```

#### ICamApiCurveGeometryEntity (etCurve)

```csharp
nodeCom.Invoke(node =>
{
    if (node.GeometryEntity is ICamApiCurveGeometryEntity entityCurve)
    {
        using var curveCom = ComWrapper.Create(entityCurve.Curve);
        Console.WriteLine($"Length: {curveCom.Invoke(c => c.FullLen)}");
    }
});
```

Helper: `CurveGeometryEntityHelper.Curve(entityCom)` wraps the `Curve` property access so the returned `ICamApiCurve` is owned by a `ComWrapper`.

#### ICamApiImportedGeometryEntity (imported folder metadata)

A node that represents the root of an imported CAD model can be queried for the source file path and PLM metadata. This interface is implemented by the tree-node object itself (not reached through `GeometryEntity`), so the cast is performed on `ICAMAPIGeometryTreeNode`.

| Helper method | Returns | Description |
|---|---|---|
| `importedCom.CADModelLocalFileName()` | `string` | Absolute path of the source CAD file |
| `importedCom.ImportedFromPLM()` | `bool` | `true` if the model was imported from a PLM system |
| `importedCom.PLMObjectID()` | `string` | Identifier of the imported file in PLMManager (empty when `ImportedFromPLM` is false) |

```csharp
using var importedCom = nodeCom.InvokeAndWrap(node => node as ICamApiImportedGeometryEntity);
if (!importedCom.IsNull)
{
    string src = importedCom.CADModelLocalFileName();
    bool   plm = importedCom.ImportedFromPLM();
    string pId = importedCom.PLMObjectID();
}
```

Helper class: `ImportedGeometryEntityHelper` in `CAMAPI.DotnetHelper`.

See full entity-reading example: `Geometry/UtilityGeometryEntityReader/project/main/ExtensionGeometryEntityReader.cs`

---

## 7. B-rep structure: Face, Loop, CoEdge

B-rep topology follows the standard **Face → Loop → CoEdge** hierarchy.

```
ICamApiFace
  └─ ICamApiLoop (outer + inner loops via GetNext chain)
       └─ ICamApiCoEdge (via CoEdges iterator)
            └─ ICamApiSurfaceCurve (trimming curve on surface)
```

### ICamApiFace

Obtained from `ICamApiFaceGeometryEntity.Face` or from `ICamApiFaceList`.

| Member | Description |
|---|---|
| `Surface` | The underlying `ICamApiSurface` |
| `Orientation` | `true` = face normal agrees with surface normal |
| `GetFirstLoop()` | First `ICamApiLoop`; chain via `loop.GetNext()` |
| `GetMesh(tol)` | Triangulate to `ICamApiMesh` at given tolerance |
| `GetNurbsForm()` | Return face converted to NURBS representation |
| `IsCylindricalHole(tol, ...)` | Convenience test; returns center, axis, radius, ZMin, ZMax |

#### Enumerate all loops

```csharp
foreach (var loopCom in faceCom.EnumerateLoops())
{
    bool isOuter = loopCom.Invoke(l => l.IsOuter);
    // ...
    loopCom.Dispose();
}
```

#### Check for a cylindrical hole

```csharp
if (faceCom.IsCylindricalHole(0.01,
        out var center, out var axis, out var radius, out var zMin, out var zMax))
{
    Console.WriteLine($"Hole radius={radius}, axis=({axis.X},{axis.Y},{axis.Z})");
}
```

### ICamApiLoop

| Member | Description |
|---|---|
| `IsOuter` | `true` for the outer (boundary) loop |
| `CoEdges` | `ICamApiCoEdgeIterator` for the co-edges in this loop |
| `GetNext()` | Next loop on the same face (`null` after last) |
| `GetNurbsForm(needUVCurve)` | NURBS form of the loop boundary |

### ICamApiCoEdge

| Member | Description |
|---|---|
| `Geometry` | `ICamApiSurfaceCurve` — the trimming curve on the surface |
| `Orientation` | Whether the curve direction agrees with the loop traversal direction |

#### Iterate co-edges

```csharp
using var iterCom = loopCom.InvokeAndWrap(l => l.CoEdges);
foreach (var coEdgeCom in iterCom.AsEnumerable())
{
    bool orientation = coEdgeCom.Orientation();
    using var curveCom = coEdgeCom.Geometry();
    var startPt = curveCom.Invoke(c => c.StartPoint);
    coEdgeCom.Dispose();
}
```

Helper classes: `FaceHelper`, `CoEdgeHelper`, `CoEdgeIteratorHelper` in `CAMAPI.DotnetHelper`.

### ICamApiSurfaceCurve

A curve living on a surface (the 3D trim curve of a co-edge).

| Property/Method | Description |
|---|---|
| `TMin`, `TMax` | Parameter range |
| `StartPoint`, `EndPoint` | 3D start/end points |
| `StartNormal`, `EndNormal` | Surface normal at start/end |
| `StartTangent`, `EndTangent` | Curve tangent at start/end |
| `SaveToReceiver(receiver, tMin, tMax, tol)` | Discretise into polyline via `ICamApiAbstractCurveReceiver` |

### ICamApiFaceList

A flat list of faces, e.g. from `ICAMAPIGeometryModel.GetFaceListOfSelected()`.

```csharp
using var faceListCom = modelCom.InvokeAndWrap(m =>
    (m.GetFaceListOfSelected(out var status), status));

int count = faceListCom.Count();
for (int i = 0; i < count; i++)
{
    using var faceCom = faceListCom.Face(i);
    // ...
}
```

`GetMesh(index, tol, holeCappingSize, fromThread)` on `ICamApiFaceList` triangulates the i-th face with optional hole capping.

---

## 8. ICamApiSurface — underlying surface

Each `ICamApiFace` owns one `ICamApiSurface`. The primary use is to obtain the NURBS representation.

```csharp
using var surfaceCom = faceCom.Surface();
using var nurbsCom = surfaceCom.InvokeAndWrap(s => s.GetNurbsForm());
```

### ICamApiNurbsSurface

A two-dimensional B-spline surface.

| Property | Description |
|---|---|
| `Degree[i]` | Polynomial degree in direction i (0=U, 1=V) |
| `IsClosed[i]` | Closed flag in direction i |
| `IsRational` | Rational (NURBS) vs. non-rational (B-spline) |
| `KnotCount[i]` | Number of knots in direction i |
| `Knot[i][j]` | j-th knot in direction i |
| `CPCount[i]` | Control point count in direction i |
| `ControlPoint[i][j]` | Control point at (i,j) as `TST3DPoint` |
| `ControlWeight[i][j]` | Weight at (i,j) (only meaningful when `IsRational = true`) |

---

## 9. ICamApiCurve and ICamApiCurve5D

### ICamApiCurve

A parametric 3D curve with knot-point access and arc-length operations.

| Member | Description |
|---|---|
| `QntP` | Number of knot points |
| `KnotPoint[i]` | i-th knot point (`TST3DPoint`) |
| `TMin`, `TMax` | Parameter range |
| `FullLen` | Total arc length |
| `IsClosed` | Closed curve flag |
| `Box` | Bounding box (`TST3DBox`) |
| `Get_Point(t)` | Evaluate position at parameter `t` |
| `Get_UnitTangent(t, isForward)` | Normalized tangent at `t` |
| `Get_Len(t1, t2)` | Arc length between `t1` and `t2` |
| `FindNearestPoint(p, t1, t2)` | Parameter of closest point on `[t1,t2]` to `p` |
| `LenToParameter(len)` | Convert arc length to parameter |
| `MakeStepByLen(t, step, out residual)` | Walk `step` distance from `t`; returns new parameter |
| `SavePartToReceiver(receiver, t1, t2, tol)` | Discretise `[t1,t2]` into a polyline |
| `Inverse()` | Reverse curve direction in-place |

#### Sampling a curve as a polyline

```csharp
// Implement ICamApiAbstractCurveReceiver to collect points
class PolylineReceiver : ICamApiAbstractCurveReceiver
{
    public List<TST3DPoint> Points = new();
    public void StartCurve3D(TST3DPoint p) => Points.Add(p);
    public void CutTo3D(TST3DPoint p) => Points.Add(p);
    public void StopCurve(bool isClosed) { }
}

var receiver = new PolylineReceiver();
curveCom.Invoke(c => c.SavePartToReceiver(receiver, c.TMin, c.TMax, 0.01));
```

#### NURBS form of a curve

```csharp
using var nurbsCom = curveCom.InvokeAndWrap(c =>
    ((ICamApiAbstractCurve)c).GetNurbsForm());
// nurbsCom is ICamApiAbstractNurbsCurve
```

### ICamApiAbstractNurbsCurve

| Property | Description |
|---|---|
| `Degree` | Polynomial degree |
| `Is3d` | True if 3D, false if 2D (UV) |
| `IsRational` | True for NURBS, false for B-spline |
| `KnotCount` | Total knot count |
| `Knot[i]` | i-th knot value |
| `CPCount` | Control point count |
| `CP[i]` | i-th control point (`TST3DPoint`) |
| `CW[i]` | i-th weight |

### ICamApiCurve5D

An optional interface, also implemented by curve objects, that exposes 5-axis tool-axis information.

```csharp
curveCom.Invoke(c =>
{
    if (c is ICamApiCurve5D curve5D)
    {
        var pt5D = curve5D.Get_Point5D(t); // TST5DPoint — position + tool axis
    }
});
```

### ICamApiCurveArcsReceiver

An extended receiver that can accept arc segments in addition to line segments. Implement both `ICamApiAbstractCurveReceiver` and `ICamApiCurveArcsReceiver` to receive full arc-accurate output.

| Method | Description |
|---|---|
| `ArcTo3D(pc, ep, R)` | Arc from current point to `ep` with signed radius `R` (positive = CCW) |
| `AddCircle(pc, R)` | Full 360-degree circle at center `pc` |

---

## 10. ICamApiMesh — triangulated representation

A triangulated mesh obtained from `ICamApiFace.GetMesh(tol)` or `ICamApiFaceList.GetMesh(...)`.

| Method | Description |
|---|---|
| `GetVertexCount()` | Total number of vertices |
| `GetTriangleCount()` | Total number of triangles |
| `GetVertex(i)` | `TST3DPoint` for the i-th vertex |
| `GetTriangle(i)` | `TST3IPoint` — three vertex indices for the i-th triangle |
| `GetTriangleNormal(i)` | `TST3DPoint` — outward normal for the i-th triangle |

```csharp
using var meshCom = faceCom.GetMesh(0.05); // tolerance in model units

int vtxCount  = meshCom.GetVertexCount();
int triCount  = meshCom.GetTriangleCount();

for (int i = 0; i < triCount; i++)
{
    var tri = meshCom.GetTriangle(i);    // .X .Y .Z are indices into the vertex array
    var n   = meshCom.GetTriangleNormal(i);
    var v0  = meshCom.GetVertex(tri.X);
}
```

Helper class: `MeshHelper` in `CAMAPI.DotnetHelper`.

---

## 11. ICamApiCoordinateSystem

Coordinate systems appear in the geometry tree as nodes with `EntityType == etCS`. They can also be managed through a dedicated `ICamApiListCoordinateSystem`.

### ICamApiCoordinateSystem properties

| Property | Type | Access | Description |
|---|---|---|---|
| `Name` | `string` | R | Unique name |
| `Parent` | `ICamApiCoordinateSystem` | R | Parent CS (`null` = global) |
| `Matrix` | `TST3DMatrix` | R/W | 4×4 matrix relative to global CS |
| `Color` | `int` | R/W | Display color |

```csharp
using var csCom = ComWrapper.Create(csEntity.Matrix);
var origin = csCom.Matrix().vT;   // translation part
```

### ICamApiListCoordinateSystem

Manages a collection of named coordinate systems. Typically obtained from the active project.

| Method | Description |
|---|---|
| `Count` | Number of CS in the list |
| `CoordinateSystem[index]` | Access by zero-based index |
| `Add(name, matrix, parentName)` | Add a new CS |
| `Remove(name)` | Remove a CS by name |
| `GetByName(name)` | Find a CS by name |
| `GetActive` | The currently active CS |
| `SetActive(name)` | Make a CS active |

```csharp
// listCsCom is ComWrapper<ICamApiListCoordinateSystem>
listCsCom.Invoke(list =>
{
    list.Add("MyCS", matrix, "", out var status);
    list.SetActive("MyCS", out status);
});
```

Helper class: `CoordinateSystemHelper` in `CAMAPI.DotnetHelper`.

---

## 12. ICAMAPIGeometryImporter — importing files

The importer is obtained from the active project or from `ICAMAPIGeomLibrary.CreateGeometryImporter()`.

```csharp
// Obtain from project (most common in extensions)
using var importerCom = activeProjectCom.InvokeAndWrap(p => p.GeomImporter);
```

### ICAMAPIGeometryImporter members

| Member | Description |
|---|---|
| `GeometryModel` | R/W — the target `ICAMAPIGeometryModel` |
| `ImportFile(filePath, targetFolder, showDialog)` | Import; `targetFolder=""` → current folder; `showDialog=true` → shows import options UI |

### Basic import

```csharp
using var importerCom = activeProjectCom.InvokeAndWrap(p => p.GeomImporter);
importerCom.ImportFile(@"C:\models\part.igs", "", false);

// After import, the imported root node is available via ActiveNode
var importedName = modelCom.Invoke(m => m.ActiveNode.FullName);
```

### Import with dialog

Pass `showDialog: true` to let the user configure format-specific options:

```csharp
importerCom.ImportFile(filePath, "", showDialog: true);
```

### Using GeomImporterHelper

```csharp
// Set a different target model (standalone usage)
importerCom.SetGeometryModel(anotherModelCom);
importerCom.ImportFile(filePath, "SubFolder", false);
```

Helper class: `GeometryImporterHelper` in `CAMAPI.DotnetHelper`.

See full example: `Geometry/ExtensionUtilityGeometryImporterNet/project/main/ExtensionGeometryImporter.cs`

> **Direct access (IDL):** `ImportFile` returns a `TResultStatus` by value. The helper throws on `rsError`.

---

## 13. ICamApiGeomPicker — interactive picking

The geometry picker shows a modal dialog that lets the user select nodes from the geometry tree. Results are delivered asynchronously via a callback.

The picker is created as a system extension (not through `ICAMAPIGeomLibrary`):

```csharp
using var geomPickerCom = SystemExtensionFactory.CreateExtension<ICamApiGeomPicker>(
    "Extension.GeomPicker");
```

### ICamApiGeomPicker members

| Member | Description |
|---|---|
| `AvailableEntityTypes` | Bitmask of `TGeometryEntityTypeFlag` values indicating which entity types the user may select |
| `OnClose` | An `ICamApiGeomPickerOnClose` callback implementation |
| `Show()` | Display the picker dialog (non-blocking; result arrives via callback) |

### ICamApiGeomPickerOnClose

Implement this interface to receive the user's selection:

```csharp
class MyOnClose : ICamApiGeomPickerOnClose
{
    public void OnConfirm(IListString selectedItems)
    {
        // selectedItems contains the FullName strings of selected nodes
        for (int i = 0; i < selectedItems.Count(); i++)
            Console.WriteLine(selectedItems.Get(i));
    }

    public void OnCancel()
    {
        Console.WriteLine("User cancelled");
    }
}
```

### Full picker usage

```csharp
if (geomPickerCom.IsNull)
    throw new Exception("Could not create picker");

geomPickerCom.Invoke(picker =>
{
    // Allow faces and edges
    picker.AvailableEntityTypes = (ushort)(
        TGeometryEntityTypeFlag.etfFace | TGeometryEntityTypeFlag.etfEdge);

    picker.OnClose = new MyOnClose();
    picker.Show();
});

// The extension must implement IExtensionLazyUnloadable and not unload
// until OnConfirm or OnCancel has been called.
```

See full example: `Geometry/ExtensionUtilityGeometryPickerNet/project/main/ExtensionGeometryPicker.cs`

> Note: because `Show()` is non-blocking, the hosting extension must implement `IExtensionLazyUnloadable` and set `CanUnload = true` only inside the callback.

---

## 14. IExtensionGeomModelNodePopup — context menu

This extension point lets an extension add items to the right-click context menu that appears when the user right-clicks a node in the 3D model tree.

### Registration

Implement `IExtensionGeomModelNodePopup` in your extension class:

```csharp
public class MyPopupExtension : IExtension, IExtensionGeomModelNodePopup
{
    public IExtensionInfo? Info { get; set; }

    public void Build(IExtensionGeomModelNodePopupBuildContext context,
                      out TResultStatus resultStatus)
    {
        resultStatus = default;
        context.NodePopup.AddItem(
            "MyAction",          // unique identifier
            "Do something",      // visible caption (empty string = separator)
            enabled: true,
            new MyOnClicked(),
            out resultStatus);
    }
}
```

### IExtensionGeomModelNodePopupBuildContext

| Property | Description |
|---|---|
| `SelectedNode` | The node that was right-clicked |
| `ActiveProject` | The currently active project |
| `NodePopup` | The `ICamApiGeomModelNodePopup` to add items to |

### ICamApiGeomModelNodePopup

| Method | Description |
|---|---|
| `AddItem(name, caption, enabled, onClicked)` | Add a menu item |
| `GetItems()` | Returns `IListString` of item names in order |
| `GetItem(name)` | Find an existing item by name |
| `Clear()` | Remove all items |

### Handling a click

```csharp
public class MyOnClicked : ICamApiGeomModelNodePopupItemOnClicked
{
    public void OnItemClicked(
        IExtensionGeomModelNodePopupItemOnClickedContext context,
        out TResultStatus resultStatus)
    {
        resultStatus = default;
        using var nodeCom = ComWrapper.Create(context.SelectedNode);
        Console.WriteLine("Clicked: " + nodeCom.FullName());
    }
}
```

See full example: `GeometryModelNodePopup/NodeFullNameAlert/project/main/`

---

## 15. ICMAPITurnGeneratrixExtractor — lathe generatrix

Extracts the generatrix (profile) curve from a geometry node representing a turned part. Obtained from `ICAMAPIGeomLibrary.CreateTurnGeneratrixExtractor()`.

### Properties

| Property | Type | Description |
|---|---|---|
| `Receiver` | `ICamApiAbstractCurveReceiver` | Receiver that collects the output polyline |
| `Tolerance` | `double` | Chord tolerance for generatrix approximation |
| `SewTolerance` | `double` | Tolerance for joining adjacent curve segments |
| `NeedJoinCurves` | `bool` | Whether to merge adjacent segments into one curve |
| `TurnAxis` | `TST5DPoint` | Rotation axis (position + direction) |
| `NeedCloseToAxis` | `bool` | Whether to extend the profile to the rotation axis |

### Usage

```csharp
using var extractorCom = geomLibCom.CreateTurnGeneratrixExtractor();
extractorCom.Invoke(e =>
{
    e.Tolerance = 0.01;
    e.TurnAxis = myAxis;
    e.Receiver = myReceiver;
});
geomNodeCom.Invoke(node =>
    extractorCom.Invoke(e => e.MakeGeneratrixForNode(node, out var status)));
```

---

## 16. Export helpers

### ICamApiFacesToTriangulatedFilesConverter

Converts a `ICAMAPIFaceList` to STL or OSD without requiring node selection. Obtained from `ICAMAPIGeomLibrary.CreateFacesToTriangulatedFilesConverter()`.

| Member | Description |
|---|---|
| `Tolerance` | Chord tolerance for tessellation |
| `Color` | Color to embed in output file |
| `SaveFacesToSTL(faces, fileName)` | Write faces to STL |
| `SaveFacesToOSD(faces, fileName)` | Write faces to OSD |

```csharp
using var converterCom = geomLibCom.InvokeAndWrap(lib =>
    lib.CreateFacesToTriangulatedFilesConverter());

converterCom.Invoke(c => c.Tolerance = 0.05);
faceListCom.Invoke(list =>
    converterCom.Invoke(c => c.SaveFacesToSTL(list, @"C:\out\part.stl", out var status)));
```

---

## 17. Entity type reference

### TCAMAPIGeometryEntityType enum

| Value | Name | Specialised interface |
|---|---|---|
| 0 | `etGroup` | — (container node only) |
| 1 | `etPoint` | — |
| 2 | `etCurve` | `ICamApiCurveGeometryEntity` → `ICamApiCurve` |
| 3 | `etMesh` | — |
| 4 | `etFace` | `ICamApiFaceGeometryEntity` → `ICamApiFace` |
| 5 | `etVertex` | — |
| 6 | `etEdge` | — |
| 7 | `etCS` | `ICamApiCSGeometryEntity` (matrix only) |
| 8 | `etPmi` | — (PMI annotation) |
| 9 | `etView` | — (saved view) |

### TGeometryEntityTypeFlag bitmask

Used in `ICamApiGeomPicker.AvailableEntityTypes`:

| Flag | Value | Meaning |
|---|---|---|
| `etfGroup` | 1 | |
| `etfPoint` | 2 | |
| `etfCurve` | 4 | |
| `etfMesh` | 8 | |
| `etfFace` | 16 | |
| `etfVertex` | 32 | |
| `etfEdge` | 64 | |
| `etfCS` | 128 | |
| `etfPmi` | 256 | |
| `etfView` | 512 | |

---

## 18. ICamApiGeometryModelSketcher — creating primitives

The sketcher creates simple geometry (points, lines, polylines, splines) in the **Job**
geometry sub-tree — the same content a user would draw by hand. It is not a separate object:
the geometry model itself implements `ICamApiGeometryModelSketcher`, so obtain it by
QueryInterface from the model wrapper.

```csharp
using var modelCom = new ComWrapper<ICAMAPIGeometryModel>(activeProject.CAMAPIGeomModel);
using var sketcherCom = modelCom.InvokeAndWrap(m => m as ICamApiGeometryModelSketcher);
```

Helper class: `GeometryModelSketcherHelper`. Every method returns the created
`ICAMAPIGeometryTreeNode` (dispose it) and throws on engine error.

| Helper method | Creates |
|---|---|
| `AddPoint(x, y, z)` | A single point |
| `AddLine(p1, p2)` | A line segment between two `TST3DPoint` |
| `AddNormalLine(origin, endPoint)` | A "normal line" anchored at `origin`, ending at `endPoint` |
| `StartPolyline()` | Begins a polyline — returns an `ICamApiSpatialCurveBuilder` |
| `StartSpline()` | Begins a smooth spline — returns an `ICamApiSpatialCurveBuilder` |

### Multi-knot curves — ICamApiSpatialCurveBuilder

`StartPolyline` / `StartSpline` return a builder; append knots, then `Finish()` to commit the
curve into the tree and receive its node. Helper class: `SpatialCurveBuilderHelper`.

| Helper method | Description |
|---|---|
| `AddKnot(p)` | Append a knot `TST3DPoint` (throws on error) |
| `Finish()` | Commit accumulated knots; returns the new node. The builder is exhausted afterwards |
| `TryAddKnot(p, out status)` / `TryFinish(out status)` | Non-throwing variants exposing the raw `TResultStatus` |

```csharp
using var sketcherCom = modelCom.InvokeAndWrap(m => m as ICamApiGeometryModelSketcher);

// Simple primitives
using (var ptCom = sketcherCom.AddPoint(0, 0, 0)) { }
using (var lineCom = sketcherCom.AddLine(
    new TST3DPoint { X = 0, Y = 0, Z = 0 },
    new TST3DPoint { X = 50, Y = 0, Z = 0 })) { }

// A polyline built from several knots
using var builderCom = sketcherCom.StartPolyline();
builderCom.AddKnot(new TST3DPoint { X = 0,  Y = 0,  Z = 0 });
builderCom.AddKnot(new TST3DPoint { X = 50, Y = 0,  Z = 0 });
builderCom.AddKnot(new TST3DPoint { X = 50, Y = 50, Z = 0 });
using var polylineNodeCom = builderCom.Finish();
```

---

## 19. ICamApiPointSnapper — snapping points to faces

The point snapper projects measured 3D points onto the nearest position on a set of faces —
the building block for probing / measurement alignment. It is a **singleton system
extension**, obtained through its helper (no direct `CAMAPI.TechSolvers` reference needed).

```csharp
using var snapperCom = PointSnapperHelper.GetSingleton(); // ICamApiPointSnapper
```

Helper class: `PointSnapperHelper`. The snapper hands out two lightweight mutable collections:

| Helper method | Returns | Description |
|---|---|---|
| `CreateFaceList()` | `ComWrapper<ICamApiFaceListBuilder>` | An empty, mutable face list builder to fill with target faces |
| `CreatePointList()` | `ComWrapper<ICamApiPoint3DList>` | An empty point list |
| `FindNearestOnFaces(faces, points, tolerance)` | `TST3DPoint[]` | For each input point, the nearest position on any face — result `[i]` corresponds to input `[i]`. `faces` is an `ICamApiFaceList` |
| `FindNearestOnFacesRaw(...)` | `ComWrapper<ICamApiPoint3DList>` | Same, but returns the COM list (caller owns it) |

`ICamApiPoint3DList` (helper `Point3DListHelper`): `Count()`, `Item(i)`, `Add(point)`.
`ICamApiFaceListBuilder` (helper `FaceListBuilderHelper`) accumulates faces via
`AddFace(face)` / `AddRange(faceList)` and produces the immutable `ICamApiFaceList` snapshot
`FindNearestOnFaces` expects with `Build()`.

```csharp
using var snapperCom = PointSnapperHelper.GetSingleton();

using var facesBuilderCom = snapperCom.CreateFaceList();
facesBuilderCom.AddRange(selectedFacesCom);   // e.g. modelCom.GetFaceListOfSelected()
using var facesCom = facesBuilderCom.Build();  // immutable ICamApiFaceList snapshot

using var pointsCom = snapperCom.CreatePointList();
pointsCom.Add(new TST3DPoint { X = 10.2, Y = 4.8, Z = 0.1 });
pointsCom.Add(new TST3DPoint { X = 22.0, Y = 4.9, Z = 0.0 });

TST3DPoint[] snapped = snapperCom.FindNearestOnFaces(facesCom, pointsCom, tolerance: 0.5);
// snapped[i] is the closest on-surface position to the i-th measured point
```

> **Direct access (IDL):** `ICamApiPointSnapper`, `ICamApiFaceListBuilder`, and
> `ICamApiPoint3DList` live in `CAMAPI.TechSolvers`. `FindNearestOnFaces` carries an
> `out TResultStatus` that the helper checks for you.
