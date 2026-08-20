# Probing Operations — ENCY CAM API

Probing operations (measuring cycles) have a dedicated job-assignment tree separate from the regular model former. This document covers all interfaces for programmatic manipulation of that tree: adding cycles, reading/writing their geometry and properties, attaching post-processor actions, and loading cycles from template libraries.

> **Entry point:** obtain `ICamApiModelFormerWithProbingItems` by calling `AsInstanceOf<ICamApiModelFormerWithProbingItems>()` on the `ModelFormerJobAssignment` of a probing operation.

---

## Getting ICamApiModelFormerWithProbingItems

```csharp
using var appCom   = ComWrapper.Create(context.CamApplication);
using var projCom  = appCom.InvokeAndWrap(a => a.GetActiveProject(out _))
    ?? throw new Exception("No active project.");
using var techCom  = projCom.InvokeAndWrap(p => p.Technologist);
using var opCom    = techCom.InvokeAndWrap(t => t.CurrentOperation)
    ?? throw new Exception("No operation selected.");
using var mfCom    = opCom.InvokeAndWrap(op => op.ModelFormerJobAssignment);
using var probingMfCom = mfCom.AsInstanceOf<ICamApiModelFormerWithProbingItems>()
    ?? throw new Exception("Not a probing operation.");
```

---

## ICamApiModelFormerWithProbingItems

The root interface for the probing job assignment. Accessed via `AsInstanceOf<>()` from the operation's model former.

### Adding items

| Helper (.NET) | Adds | XML type |
|---|---|---|
| `AddMovement()` | Movement item | `TMovementProbingItem` |
| `AddGroup()` | Group container | `TGroupProbingItem` |
| `AddSurfaceCycle()` | Surface probe | `TSingleSurfProbingCycle` |
| `AddBossCycle()` | Boss (outer cylinder) | `TBossProbingCycle` |
| `AddHoleCycle()` | Hole (inner cylinder) | `THoleProbingCycle` |
| `AddHoleProtectedCycle()` | Hole protected | `THoleProbingProtectedCycle` |
| `AddWebCycle()` | Web (two-point slot width) | `TWebProbingCycle` |
| `AddGrooveCycle()` | Groove (two-point groove width) | `TGrooveProbingCycle` |
| `AddGrooveProtectedCycle()` | Groove protected | `TGrooveProbingCycleProtected` |
| `AddThreePointsWebCycle()` | Three-point web | `TThreePointsWebProbingCycle` |
| `AddExternalRectangleCycle()` | External rectangle | `TExternalRectangleCycle` |
| `AddInternalRectangleCycle()` | Internal rectangle | `TInternalRectangleCycle` |
| `AddInternalRectangleProtectedCycle()` | Internal rectangle protected | `TInternalRectangleProtectedCycle` |
| `AddDoubleWallInternalCornerCycle()` | Double wall — internal corner | `TDoubleWallInternalCornerCycle` |
| `AddDoubleWallExternalCornerCycle()` | Double wall — external corner | `TDoubleWallExternalCornerCycle` |
| `AddTripleWallInternalCornerCycle()` | Triple wall — internal corner | `TTripleWallInternalCornerCycle` |
| `AddTripleWallExternalCornerCycle()` | Triple wall — external corner | `TTripleWallExternalCornerCycle` |
| `AddNcActionItem()` | NC action (custom G-code) | `TNCActionProbingItem` |
| `AddFrameOutputCycle()` | Frame output | `TFrameOutputProbingCycle` |

> Typed `Add*` methods return the specific geometry interface directly. Call `using` on the returned `ComWrapper` immediately if you don't need to configure it further — the item is already registered in the tree.

```csharp
// Add and configure in one block
using var surfCom = probingMfCom.AddSurfaceCycle();
surfCom.SetTargetPoint(new TST3DPoint { x = 10, y = 20, z = 5 });
surfCom.SetClearance(3.0);
```

### Tree operations

```csharp
// Enumerate all items (depth-first)
// NOTE: do NOT add `using (itemCom)` — EnumerateProbingItems disposes each item automatically
foreach (var itemCom in probingMfCom.EnumerateProbingItems())
{
    var typeName = itemCom.GetXMLTypeName(); // e.g. "TSingleSurfProbingCycle"
}

// Get the item selected in the tree panel
using var selectedCom = probingMfCom.GetSelectedItem(); // null if nothing selected

// Delete an item
probingMfCom.DeleteItem(selectedCom!);

// Remove all items
probingMfCom.Clear();
```

### Template libraries

```csharp
int libCount = probingMfCom.GetTemplateLibraryCount();
for (int i = 0; i < libCount; i++)
{
    using var libCom = probingMfCom.GetTemplateLibrary(i)!;
    Console.WriteLine(libCom.GetCaption());
    int tmplCount = libCom.GetTemplateCount();
    for (int j = 0; j < tmplCount; j++)
    {
        using var tmplCom = libCom.GetTemplate(j)!;
        Console.WriteLine($"  {tmplCom.GetCaption()} → {tmplCom.GetTemplateFileName()}");
    }
}

// Create cycle from template (preferred way — matches user workflow)
using var fromTmplCom = probingMfCom.AddCycleByTemplate(templateFileName)!;
```

---

## ICamApiProbingModelItem

Base interface for every item in the probing tree. Provides raw XML property access and type identification.

| Helper | Description |
|---|---|
| `GetXMLTypeName()` | XML type name string (e.g. `"TSingleSurfProbingCycle"`, `"TGroupProbingItem"`) |
| `.Instance.XMLProp` | `IST_XMLPropPointer` — read/write any XML field directly (see XMLProp helpers) |

> **QI dispatch pattern:** all typed cycle interfaces are obtained from `ICamApiProbingModelItem` via `AsInstanceOf<T>()`. The host checks `XMLProp.IsInheritFrom('T<XMLTypeName>')` internally and returns `E_NOINTERFACE` if the type doesn't match.

```csharp
using var itemCom = probingMfCom.GetSelectedItem()!;
var typeName = itemCom.GetXMLTypeName();

// Narrow to typed cycle (only succeeds if the XML type matches)
using var surfCom = itemCom.AsInstanceOf<ICamApiSurfaceProbingCycle>();
if (surfCom != null)
{
    var pt = surfCom.GetTargetPoint();
}
```

---

## ICamApiProbingCycle — Common Cycle Properties

Available on all cycle items (not on movement/group). Obtain via `AsInstanceOf<ICamApiProbingCycle>()` from `ICamApiProbingModelItem`.

| Helper | Property | Description |
|---|---|---|
| `GetCaption()` / `SetCaption(v)` | `Caption` | Display name of the cycle |
| `GetSubCode()` / `SetSubCode(v)` | `SubCode` | Postprocessor sub-code |
| `GetTransition()` / `SetTransition(v)` | `Transition` | Transition type (`TProbingTransition`) |
| `GetFeed()` / `SetFeed(v)` | `Feed` | Feed during transition (`TProbingFeed`) |
| `GetFeedDistance()` / `SetFeedDistance(v)` | `FeedDistance` | Feed distance |
| `GetActionsCount()` | `ActionsCount` | Number of attached actions |
| `GetAction(index)` | — | Returns `ICamApiProbingAction` (base), use `AsInstanceOf` to narrow |

### TProbingTransition values

| Value | Meaning |
|---|---|
| `ptDefault` | Default transition |
| `ptShort` | Short transition |
| `ptOrtho` | Orthogonal transition |
| `ptSafeDist` | Safe distance transition |
| `ptSafeSurf` | Safe surface transition |

### TProbingFeed values

| Value | Meaning |
|---|---|
| `pfRapid` | Rapid feed |
| `pfNonProtected` | Non-protected (long transition) |
| `pfProtected` | Protected (short transition) |

```csharp
using var cycleCom = itemCom.AsInstanceOf<ICamApiProbingCycle>()!;
cycleCom.SetCaption("My Surface Probe");
cycleCom.SetTransition(TProbingTransition.ptSafeDist);
cycleCom.SetFeed(TProbingFeed.pfProtected);
```

---

## Cycle Geometry Interfaces

Each typed Add* method returns the geometry interface directly. All geometry interfaces are also obtainable via `AsInstanceOf<T>()` from `ICamApiProbingModelItem`.

### ICamApiSurfaceProbingCycle (`TSingleSurfProbingCycle`)

Single-point surface probe.

| Helper | Description |
|---|---|
| `GetTargetPoint()` / `SetTargetPoint(v)` | Target point in world coordinates (`TST3DPoint`) |
| `GetTargetVector()` / `SetTargetVector(v)` | Probe approach direction |
| `GetClearance()` / `SetClearance(v)` | Clearance distance |
| `GetStock()` / `SetStock(v)` | Material allowance |

### ICamApiBossProbingCycle / ICamApiHoleProbingCycle / ICamApiHoleProbingProtectedCycle

Circular boss or hole probe (shared property set).

| Helper | Description |
|---|---|
| `GetTargetPoint()` / `SetTargetPoint(v)` | First touch point |
| `GetCenterPoint()` / `SetCenterPoint(v)` | Center of the circle |
| `GetTargetVector()` / `SetTargetVector(v)` | Approach direction |
| `GetTopClearanceEnabled()` / `SetTopClearanceEnabled(v)` | Enable top clearance move |
| `GetTopClearance()` / `SetTopClearance(v)` | Clearance above top |
| `GetSideClearance()` / `SetSideClearance(v)` | Lateral clearance |
| `GetDepth()` / `SetDepth(v)` | Probing depth |
| `GetDiameter()` / `SetDiameter(v)` | Boss/hole diameter |
| `GetStock()` / `SetStock(v)` | Material allowance |
| `SetCycleVariantIsAngular(startAngle, stepCnt, angularStep)` | Switch to angular (multi-point) variant |
| `SetCycleVariantIsRectangular()` | Switch to rectangular (4-point) variant |
| `GetCycleVariantIsAngular()` | True if angular variant active |

### ICamApiWebProbingCycle / ICamApiGrooveProbingCycle / ICamApiGrooveProbingProtectedCycle

Two-point width measurement (web = external, groove = internal).

| Helper | Description |
|---|---|
| `GetTargetPoint1()` / `SetTargetPoint1(v)` | First touch point |
| `GetTargetVector1()` / `SetTargetVector1(v)` | First approach direction |
| `GetTargetPoint2()` / `SetTargetPoint2(v)` | Second touch point |
| `GetTargetVector2()` / `SetTargetVector2(v)` | Second approach direction |
| `GetTopClearanceEnabled()` / `SetTopClearanceEnabled(v)` | Enable top clearance |
| `GetTopClearance()` / `SetTopClearance(v)` | Clearance above top |
| `GetSideClearance()` / `SetSideClearance(v)` | Lateral clearance |
| `GetDepth()` / `SetDepth(v)` | Probing depth |
| `GetWidth()` / `SetWidth(v)` | Nominal width |
| `GetStock()` / `SetStock(v)` | Material allowance |

### ICamApiThreePointsWebProbingCycle

Three-point web probe — same as two-point web but adds a third touch point:

| Helper | Additional property |
|---|---|
| `GetTargetPoint3()` / `SetTargetPoint3(v)` | Third touch point |
| `GetTargetVector3()` / `SetTargetVector3(v)` | Third approach direction |

### ICamApiExternalRectangleProbingCycle / ICamApiInternalRectangleProbingCycle / ICamApiInternalRectangleProbingProtectedCycle

Rectangle probe (two opposite walls, two points each).

| Helper | Description |
|---|---|
| `GetGeom1TargetPoint1()` / `SetGeom1TargetPoint1(v)` | Wall 1, first touch |
| `GetGeom1TargetVector1()` / `SetGeom1TargetVector1(v)` | Wall 1, first approach |
| `GetGeom1TargetPoint2()` / `SetGeom1TargetPoint2(v)` | Wall 1, second touch |
| `GetGeom1TargetVector2()` / `SetGeom1TargetVector2(v)` | Wall 1, second approach |
| `GetGeom2TargetPoint1()` etc. | Wall 2 points (same pattern) |
| `GetFeedDistance()` / `SetFeedDistance(v)` | Feed distance |
| `GetAllDepth()` / `SetAllDepth(v)` | Probing depth |
| `GetAllStock()` / `SetAllStock(v)` | Material allowance |

### ICamApiDoubleWallInternalCornerCycle / ICamApiDoubleWallExternalCornerCycle

Two-wall corner probe.

| Helper | Description |
|---|---|
| `GetTargetPoint1()` / `SetTargetPoint1(v)` | Wall 1 touch point |
| `GetTargetVector1()` / `SetTargetVector1(v)` | Wall 1 approach |
| `GetTargetPoint2()` / `SetTargetPoint2(v)` | Wall 2 touch point |
| `GetTargetVector2()` / `SetTargetVector2(v)` | Wall 2 approach |
| `GetClearance1()` / `SetClearance1(v)` | Clearance on wall 1 |
| `GetClearance2()` / `SetClearance2(v)` | Clearance on wall 2 |
| `GetStepCnt1()` / `SetStepCnt1(v)` | Number of steps on wall 1 |
| `GetStep1()` / `SetStep1(v)` | Step size on wall 1 |
| `GetStepCnt2()` / `SetStepCnt2(v)` | Number of steps on wall 2 |
| `GetStep2()` / `SetStep2(v)` | Step size on wall 2 |
| `GetStock()` / `SetStock(v)` | Material allowance |

### ICamApiTripleWallInternalCornerCycle / ICamApiTripleWallExternalCornerCycle

Three-wall corner probe — same as double wall but adds a third wall (`TargetPoint3`, `TargetVector3`, `Clearance3`, `StepCnt3`, `Step3`). External corner additionally has `Depth`.

### ICamApiNcActionProbingCycle (`TNCActionProbingItem`)

Inserts a custom NC block.

| Helper | Description |
|---|---|
| `GetOutputMode()` / `SetOutputMode(v)` | 0 = EXTCYCLE, 1 = INSERT |
| `GetStringToInsert()` / `SetStringToInsert(v)` | The NC string to output |

### ICamApiFrameOutputProbingCycle (`TFrameOutputProbingCycle`)

Outputs a coordinate frame in a specified format.

| Helper | Description |
|---|---|
| `GetTargetFrame()` / `SetTargetFrame(v)` | Target frame (4×4 matrix, position + XYZ Euler rotation in degrees) |
| `GetParentFrame()` / `SetParentFrame(v)` | Parent frame |
| `GetFrameOutputFormat()` / `SetFrameOutputFormat(v)` | 0=Matrix, 1=Quaternion, 2=EulerXYZ, ... |

---

## Cycle Actions

Actions are post-measurement operations attached to a cycle (e.g. "write result to tool offset register"). Access the action list via `ICamApiProbingCycle`.

```csharp
using var cycleCom = itemCom.AsInstanceOf<ICamApiProbingCycle>()!;

// Add actions
using var offsetAction = cycleCom.AddSetToolOffsetAction();
offsetAction.SetToolNumber(1);
offsetAction.SetCorrector1(1);
offsetAction.SetCorrector2(101);

// Enumerate existing actions
int count = cycleCom.GetActionsCount();
for (int i = 0; i < count; i++)
{
    using var actionCom = cycleCom.GetAction(i)!;
    var actionType = actionCom.GetActionType(); // e.g. "TSetToolOffsetCycleAction"
}
```

### ICamApiSetToolOffsetProbingAction (`TSetToolOffsetCycleAction`)

Writes measured offset into tool compensation registers.

| Helper | Description |
|---|---|
| `GetToolNumber()` / `SetToolNumber(v)` | Tool number |
| `GetCorrector1()` / `SetCorrector1(v)` | Length corrector register |
| `GetCorrector2()` / `SetCorrector2(v)` | Radius corrector register |

### ICamApiCheckBrokenToolProbingAction (`TCheckBrokenToolCycleAction`)

Same properties as `SetToolOffset` — compares measured against stored offset to detect breakage.

### ICamApiSetWcsProbingAction (`TSetWCSCycleAction`)

Activates a WCS offset after probing.

| Helper | Description |
|---|---|
| `GetOffsetMode()` / `SetOffsetMode(v)` | `TProbingWcsOffsetMode` (see below) |
| `GetCSNumber()` / `SetCSNumber(v)` | Register number (e.g. 54 for G54) |

**TProbingWcsOffsetMode values:**

| Value | Meaning |
|---|---|
| `pwomGlobal` | Global WCS (no register) |
| `pwomOneWCS` | Explicit register (use CSNumber) |
| `pwomParametrical` | Parametrical reference |
| `pwomLocalCS` | Local coordinate system |

### ICamApiCalibrateToolProbeProbingAction / ICamApiCalibratePartProbeProbingAction

Calibrate tool-length or part-measurement probe.

| Helper | Description |
|---|---|
| `GetProbeNumber()` / `SetProbeNumber(v)` | Probe tool number |
| `GetCorrector1()` / `SetCorrector1(v)` | Length corrector register |
| `GetCorrector2()` / `SetCorrector2(v)` | Radius corrector register |

### ICamApiWriteToReportProbingAction (`TWriteToReportCycleAction`)

Stores the measured value in a report.

| Helper | Description |
|---|---|
| `GetComponentNumber()` / `SetComponentNumber(v)` | Component index in the report (writing switches to explicit mode) |
| `GetFeatureNumber()` / `SetFeatureNumber(v)` | Feature index in the report |

### ICamApiCustomProbingAction (`TProbingCycleCustomProp`)

A single custom property with a postprocessor code and typed value.

| Helper | Description |
|---|---|
| `GetCaption()` / `SetCaption(v)` | Display caption |
| `GetCode()` / `SetCode(v)` | Postprocessor code |
| `GetPropType()` / `SetPropType(v)` | `TProbingCustomPropType` |
| `SetDoubleValue(v)` | Set double value (also sets PropType) |
| `SetIntegerValue(v)` | Set integer value (also sets PropType) |
| `SetBooleanValue(v)` | Set boolean value (also sets PropType) |
| `SetStringValue(v)` | Set string value (also sets PropType) |

**TProbingCustomPropType values:** `pcptDouble`, `pcptInteger`, `pcptBoolean`, `pcptString`

### ICamApiCustomPropGroupProbingAction (`TProbingCycleCustomPropGroup`)

A named group containing multiple `ICamApiCustomProbingAction` items.

| Helper | Description |
|---|---|
| `GetGroupCaption()` / `SetGroupCaption(v)` | Group display name |
| `GetPropCount()` | Number of custom properties in the group |
| `GetProp(index)` | Returns `ICamApiCustomProbingAction` at index |
| `AddProp()` | Adds a new custom property to the group |
| `EnumerateCustomProbingActions()` | Iterates all props in the group; each wrapper is disposed automatically after the loop body |

```csharp
using var group = cycleCom.AddCustomPropGroupAction();
group.SetGroupCaption("Custom Data");

using var propDouble = group.AddProp();
propDouble.SetCaption("Diameter");
propDouble.SetDoubleValue(12.5);

using var propInt = group.AddProp();
propInt.SetCaption("Count");
propInt.SetIntegerValue(3);

using var propBool = group.AddProp();
propBool.SetCaption("Enabled");
propBool.SetBooleanValue(true);

using var propStr = group.AddProp();
propStr.SetCaption("Label");
propStr.SetStringValue("ABC");

// Enumerate — no Dispose() needed in the loop body
foreach (var p in group.EnumerateCustomProbingActions())
{
    var valueStr = p.GetPropType() switch
    {
        TProbingCustomPropType.pcptDouble  => p.GetDoubleValue().ToString("F3"),
        TProbingCustomPropType.pcptInteger => p.GetIntegerValue().ToString(),
        TProbingCustomPropType.pcptBoolean => p.GetBooleanValue().ToString(),
        TProbingCustomPropType.pcptString  => $"\"{p.GetStringValue()}\"",
        _                                  => "?"
    };
    Console.WriteLine($"  \"{p.GetCaption()}\" = {valueStr}");
}
```

### AddCustomPropAction — standalone

Adds a single `ICamApiCustomProbingAction` directly to the cycle (not inside a group):

```csharp
using var prop = cycleCom.AddCustomPropAction();
prop.SetCaption("Tolerance");
prop.SetCode(99);
prop.SetDoubleValue(0.05);
```

---

## Template Libraries

Template libraries (`.scpbl` files) contain pre-configured cycle setups — the primary way users add cycles in the UI.

```csharp
// List all libraries and templates
for (int i = 0; i < probingMfCom.GetTemplateLibraryCount(); i++)
{
    using var lib = probingMfCom.GetTemplateLibrary(i)!;
    string libName = lib.GetCaption();

    for (int j = 0; j < lib.GetTemplateCount(); j++)
    {
        using var tmpl = lib.GetTemplate(j)!;
        string tmplName = tmpl.GetCaption();
        string fileName = tmpl.GetTemplateFileName(); // pass to AddCycleByTemplate
    }
}

// Add a cycle from a template
using var newItemCom = probingMfCom.AddCycleByTemplate(fileName)!;
var typeName = newItemCom.GetXMLTypeName();
```

---

## XMLProp Access (Low-level)

Every `ICamApiProbingModelItem` exposes `XMLProp` (`IST_XMLPropPointer`) for direct read/write of any XML field not covered by a typed interface.

```csharp
using var itemCom = probingMfCom.GetSelectedItem()!;
using var xmlProp = itemCom.Instance.XMLProp.CreateHelper();
// Read/write via XMLPropPointerHelper methods
string caption = xmlProp.GetStringProp("CycleCaption");
xmlProp.SetStringProp("CycleCaption", "My Cycle");
```

---

## Complete Example

```csharp
// Get the model former for the active probing operation
using var appCom  = ComWrapper.Create(context.CamApplication);
using var projCom = appCom.InvokeAndWrap(a => a.GetActiveProject(out _))!;
using var techCom = projCom.InvokeAndWrap(p => p.Technologist);
using var opCom   = techCom.InvokeAndWrap(t => t.CurrentOperation)!;
using var mfCom   = opCom.InvokeAndWrap(op => op.ModelFormerJobAssignment);
using var pmfCom  = mfCom.AsInstanceOf<ICamApiModelFormerWithProbingItems>()!;

// Add a surface cycle and configure it
using var surfCom = pmfCom.AddSurfaceCycle();
surfCom.SetTargetPoint(new TST3DPoint { x = 0, y = 0, z = 10 });
surfCom.SetTargetVector(new TST3DPoint { x = 0, y = 0, z = -1 });
surfCom.SetClearance(5.0);

// Get common cycle properties
using var cyclePropsCom = pmfCom.GetSelectedItem()!.AsInstanceOf<ICamApiProbingCycle>()!;
cyclePropsCom.SetCaption("Top Surface");

// Add a report action to it
using var reportAction = cyclePropsCom.AddWriteToReportAction();
reportAction.SetComponentNumber(1);
reportAction.SetFeatureNumber(1);
```

---

## Examples

- [`FullWorkflow/PartCalibrationWorkflowNet` → `ProbingCyclesService.cs`](../../FullWorkflow/PartCalibrationWorkflowNet/project/main/Service/ProbingCyclesService.cs) — detects a probing operation by narrowing its model former to `ICamApiModelFormerWithProbingItems`, then adds and configures measuring cycles as part of a full part-calibration workflow.
