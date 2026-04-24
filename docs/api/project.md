# ENCY CAM API — Project / Technologist Domain

This document covers the CAMAPI interfaces that model an open CAM project and its
manufacturing technology tree.  All code snippets use the .NET helper layer
(`CAMAPI.DotnetHelper`).  Raw COM / IDL access is noted as **Direct access** where
relevant.

---

## Table of contents

1. [ICamApiProject](#icamapiproject)
2. [ICamApiTechnologist](#icamapitechnologist)
3. [ICamApiTechOperation](#icamapitechoperation)
4. [ICamApiTechOperationIterator](#icamapitechoperationiterator)
5. [ICamApiTechOperationType / ICamApiTechOperationTypeIterator](#icamapitechoperationtype--icamapitechoperationtypeiterator)
6. [ICamApiPart](#icamapipart)
7. [ICamApiSetupStage](#icamapisetupstage)
8. [ICamApiPartStage](#icamapipartstage)
9. [ICamApiPartAndStageList](#icamapipartandstagelist)
10. [ICamApiSnapshot / IListCamApiSnapshot](#icamapiartsnapshot--ilistcamapiartsnapshot)
11. [ICamApiUserTechOperationInfo / ICamApiUserTechOperationList](#icamapiusertechoperationinfo--icamapiusertechoperationlist)

---

## ICamApiProject

**Purpose.** Represents one open CAM project. It is the root object from which the
technology tree, NC maker, geometry model, tools list, snapshots, and coordinate
systems are all reached.

### How to obtain

`ICamApiProject` is obtained from `ICamApiApplication.GetActiveProject()`.  With the
helper:

```csharp
using var applicationCom = ComWrapper.Create(context.CamApplication);
using var projectCom = applicationCom.GetActiveProject();   // extension method
```

Without the helper (direct access):

```csharp
var project = application.GetActiveProject(out var status);
```

### Key methods — .NET helper syntax

| Helper method | Returns | Description |
|---|---|---|
| `projectCom.Id()` | `string` | Unique GUID-string identifier of the project |
| `projectCom.FilePath()` | `string` | Absolute path to the `.cam` project file |
| `projectCom.Technologist()` | `ComWrapper<ICamApiTechnologist>` | Technology tree root |
| `projectCom.NCMaker()` | `ComWrapper<ICamApiNCMaker>` | NC / G-code generator |
| `projectCom.GeomImporter()` | `ComWrapper<ICAMAPIGeometryImporter>` | Geometry file importer |
| `projectCom.CAMAPIGeomModel()` | `ComWrapper<ICAMAPIGeometryModel>` | Geometry model |
| `projectCom.MachineInformation()` | `ComWrapper<ICamApiMachineInfo>` | Project-level machine information |
| `projectCom.ToolsList()` | `ComWrapper<ICamApiMachiningToolsList>` | Tools used in the project |
| `projectCom.Snapshots()` | `ComWrapper<IListCamApiSnapshot>` | Snapshot list |
| `projectCom.CoordinateSystems()` | `ComWrapper<ICamApiListCoordinateSystem>` | Coordinate systems |
| `projectCom.Machine()` | `ComWrapper<ICamApiMachine>` | Machine instance |
| `projectCom.Simulator()` | `ComWrapper<ICamApiSimulator>` | Simulation manager |
| `projectCom.SetOperationTool(opId, toolId)` | `void` | Assign a tool to an operation by their string IDs |
| `projectCom.SaveClData(path, iterator?)` | `void` | Write CLData file for the given iterator (or all operations if `null`) |

### Event handlers

Register for project save events by calling `RegisterHandler` on the raw COM
object and passing an object that implements `ICamApiHandlerProjectBeforeSave` or
`ICamApiHandlerProjectAfterSave`.

### Code example — enumerate tools and assign one to an operation

```csharp
using var applicationCom = ComWrapper.Create(context.CamApplication);
using var projectCom = applicationCom.GetActiveProject();

// Read project metadata
Console.WriteLine("Project file: " + projectCom.FilePath());
Console.WriteLine("Project ID  : " + projectCom.Id());

// Iterate tools
using var toolsListCom = projectCom.ToolsList();
int count = toolsListCom.Invoke(tl => tl.Count);
for (int i = 0; i < count; i++)
{
    using var toolInfoCom = toolsListCom.InvokeAndWrap(tl => tl.ToolInfo[i]);
    string caption = toolInfoCom.Invoke(ti => ti.ToolCaption);
    string toolId  = toolInfoCom.Invoke(ti => ti.ToolID);
    Console.WriteLine($"  [{toolId}] {caption}");
}

// Assign tool "12" to a known operation
projectCom.SetOperationTool(someOperationId, "12");
```

**Reference example:** [`ProjectToolsList/ExtensionUtilityProjectToolsListNet/project/main/ExtensionUtilityProjectToolsList.cs`](../../ProjectToolsList/ExtensionUtilityProjectToolsListNet/project/main/ExtensionUtilityProjectToolsList.cs)

---

## ICamApiTechnologist

**Purpose.** Represents the technology (manufacturing plan) of a project.  Provides
access to the operation tree, parts, setup stages, operation types, and toolpath
calculation.

### How to obtain

```csharp
using var technologistCom = projectCom.Technologist();
```

**Direct access:** `project.Technologist` (read-only property).

### Key methods — .NET helper syntax

| Helper method | Returns | Description |
|---|---|---|
| `technologistCom.RootOperation()` | `ComWrapper<ICamApiTechOperation>` | Root node of the operation tree (represents the machine / NC program) |
| `technologistCom.CurrentOperation()` | `ComWrapper<ICamApiTechOperation>` | Currently selected operation in the UI |
| `technologistCom.SetCurrentOperation(opCom)` | `void` | Change the selected operation |
| `technologistCom.GetOperations(mode)` | `ComWrapper<ICamApiTechOperationIterator>` | Iterator over the full tree in the given reordering mode |
| `technologistCom.EnumerateOperations(mode)` | `IEnumerable<ComWrapper<ICamApiTechOperation>>` | Convenience LINQ-compatible flat enumeration |
| `technologistCom.OperationTypes()` | `ComWrapper<ICamApiTechOperationTypeIterator>` | Iterator over registered operation types |
| `technologistCom.EnumerateOperationTypes()` | `IEnumerable<ComWrapper<ICamApiTechOperationType>>` | Convenience enumeration of operation types |
| `technologistCom.GetAvailableOperationTypeIds()` | `ComWrapper<IListString>` | IDs of types that can be created right now |
| `technologistCom.PartAndStageList()` | `ComWrapper<ICamApiPartAndStageList>` | Parts and setup stages |
| `technologistCom.CreateOperation(typeId, afterId, protoId)` | `ComWrapper<ICamApiTechOperation>` | Create a new operation of the given type. Pass `""` for `afterId` to append at the end, `""` for `protoId` to use no prototype. |
| `technologistCom.CreatePart(externalId)` | `ComWrapper<ICamApiPart>` | Add a new part |
| `technologistCom.CreateSetupStage()` | `ComWrapper<ICamApiSetupStage>` | Add a new setup stage |
| `technologistCom.CreateOperationFromUserTemplate(userOpId, afterId)` | `ComWrapper<ICamApiTechOperation>` | Instantiate a user-defined operation template |
| `technologistCom.Invoke(t => t.GetOperationById(id, out var status))` | `ICamApiTechOperation*` | Find an operation by its GUID-string id |
| `technologistCom.DeleteOperation(id)` | `void` | Remove an operation |
| `technologistCom.DeletePart(partIndex)` | `void` | Remove a part from all stages |
| `technologistCom.DeletePartStage(partIdx, stageIdx, includingNext)` | `void` | Remove a part from one (or more) stages |
| `technologistCom.DeleteSetupStage(stageIndex)` | `void` | Remove a setup stage (must be empty) |
| `technologistCom.CalculateToolpath(calcLinks)` | `void` | Calculate toolpath for the current operation |
| `technologistCom.CalculateAllOperationsToolpath(calcLinks)` | `void` | Calculate toolpaths for all operations |
| `technologistCom.ResetToolpath()` | `void` | Clear toolpath of the current operation |
| `technologistCom.ResetAllOperationsToolpath()` | `void` | Clear toolpaths of all operations |
| `technologistCom.SwitchOperationEnability()` | `void` | Toggle the `Enabled` state of the current operation |
| `technologistCom.GetActiveReorderingModeOfTechnology()` | `TCamApiReorderingMode` | Active mode for technology design |
| `technologistCom.GetActiveReorderingModeOfSimulation()` | `TCamApiReorderingMode` | Active mode for simulation |

`TCamApiReorderingMode` values: `rmDesigned` (0) — original design order; `rmReordered` (1) — optimised order.

### Code example — create an operation and calculate all toolpaths

```csharp
using var technologistCom = projectCom.Technologist();

// Find the root operation id to place the new operation after it
using var rootCom = technologistCom.RootOperation();
string rootId = rootCom.Id();

// Create a waterline roughing operation after the root
using var newOpCom = technologistCom.CreateOperation("TSTRoughingWaterlineOp", rootId, "");
Console.WriteLine("Created operation: " + newOpCom.Id());

// Assign a tool
projectCom.SetOperationTool(newOpCom.Id(), "11");

// Calculate all toolpaths
technologistCom.ResetAllOperationsToolpath();
technologistCom.CalculateAllOperationsToolpath(calcLinksBetweenOperations: true);
```

**Reference examples:**
- [`Technologist/Operation/OperationNet/project/main/ExtensionCreateOperation.cs`](../../Technologist/Operation/OperationNet/project/main/ExtensionCreateOperation.cs)
- [`FullWorkflow/FullWorkflow3DProject/project/main/TechnologyHelper.cs`](../../FullWorkflow/FullWorkflow3DProject/project/main/TechnologyHelper.cs)

---

## ICamApiTechOperation

**Purpose.** Represents one node in the technology tree — either a leaf machining
operation or a group.  Provides read/write access to its name, XML properties, tool,
status flags, model formers, approach/retract rules, and more.

### How to obtain

- From `ICamApiTechnologist`: `technologistCom.CurrentOperation()`, `technologistCom.RootOperation()`
- From an iterator: `iteratorCom.InvokeAndWrap(it => it.Current())`

### Key methods — .NET helper syntax

| Helper method | Returns | Description |
|---|---|---|
| `operationCom.Id()` | `string` | Unique identifier (GUID-string) |
| `operationCom.OperationType()` | `string` | Type identifier string, e.g. `"TSTWaterlineOp"` |
| `operationCom.IsGroup()` | `bool` | `true` if this node is a group/container |
| `operationCom.Name()` | `string` | Display name |
| `operationCom.SetName(value)` | `void` | Rename the operation |
| `operationCom.FullName()` | `string` | Slash-separated full path including parent groups |
| `operationCom.Units()` | `TSTSystemUnits` | Measurement units |
| `operationCom.LCS()` | `TST3DMatrix` | Local coordinate system |
| `operationCom.PartIndex()` | `int` | Index of the associated part |
| `operationCom.SetupStageIndex()` | `int` | Index of the associated setup stage |
| `operationCom.Enabled()` | `bool` | `false` means the operation is blocked from calculation |
| `operationCom.Calculated()` | `bool` | Toolpath has been calculated |
| `operationCom.Simulated()` | `bool` | Simulation has been run |
| `operationCom.IsError()` | `bool` | Any error is present |
| `operationCom.IsRapidError()` | `bool` | Rapid-feed contact with workpiece |
| `operationCom.IsHolderError()` | `bool` | Holder collision |
| `operationCom.IsCompensationError()` | `bool` | Compensation value too large |
| `operationCom.IsPlungeError()` | `bool` | Plunge angle too large |
| `operationCom.IsTravelError()` | `bool` | Axis travel exceeded |
| `operationCom.IsCollisionError()` | `bool` | Machine node collision |
| `operationCom.IsGougeError()` | `bool` | Part gouge detected |
| `operationCom.IsToolOverloadError()` | `bool` | Tool overload |
| `operationCom.IsTurnDirectionError()` | `bool` | Wrong spindle turn direction |
| `operationCom.IsMachiningResultCalculated()` | `bool` | Machining result (stock removal) is computed |
| `operationCom.NeedDeleteChips()` | `bool` | `true` if chips need to be deleted before this operation |
| `operationCom.OperationTag()` / `SetOperationTag(value)` | `string` | User-defined tag; not used by ENCY |
| `operationCom.HasToolpath()` | `bool` | `true` if the operation has a calculated toolpath that can be exported (groups return `false`; uncalculated return `false`) |
| `operationCom.ExportToolpath(receiver)` | `void` | Stream the toolpath to an `ICamApiExportToolpathReceiver` implementation |
| `operationCom.GetParentOperation(mode)` | `ComWrapper<ICamApiTechOperation>` | Parent node in the tree for the given `TCamApiReorderingMode` |
| `operationCom.GetFirstChildOperation(mode)` | `ComWrapper<ICamApiTechOperation>` | First child for the given mode (or null) |
| `operationCom.GetNextSiblingOperation(mode)` | `ComWrapper<ICamApiTechOperation>` | Next sibling for the given mode (or null) |
| `operationCom.GetTimeStatistics()` | `TCamApiTechOperationTimeStatistics` | Rapid / idle / work / auxiliary time in seconds |
| `operationCom.GetBlocksStatistics()` | `TCamApiTechOperationBlocksStatistics` | Count of lines, arcs, multi-gotos, feed blocks, total blocks |
| `operationCom.GetLengthStatistics()` | `TCamApiTechOperationLengthStatistics` | Lengths of work/rapid/return/engage/retract/plunge segments |
| `operationCom.ToolEntity()` | `ComWrapper<ICamApiMachiningTool>` | Tool assigned to this operation |
| `operationCom.XMLProp()` | `ComWrapper<IST_XMLPropPointer>` | Live XML property bag (read/write) |
| `operationCom.SaveToXmlProp()` | `ComWrapper<IST_XMLPropPointer>` | Snapshot of current properties into a new XML structure |
| `operationCom.LoadFromXmlProp(xmlProp)` | `void` | Apply an XML property bag to the operation |
| `operationCom.GetPropIterator(pageId)` | `ComWrapper<IST_CustomPropIterator>` | Iterate typed properties on a given page |
| `operationCom.ApproachRule()` | `string` | Approach move rule (see format below) |
| `operationCom.SetApproachRule(value)` | `void` | Set approach move rule |
| `operationCom.ReturnRule()` | `string` | Retract move rule |
| `operationCom.SetReturnRule(value)` | `void` | Set retract move rule |
| `operationCom.Machine()` | `ComWrapper<ICamApiMachine>` | Machine assigned to this operation |
| `operationCom.MachineConfiguration()` | `ComWrapper<ICamApiMachineConfiguration>` | Machine configuration |
| `operationCom.WorkpieceCoordinateSystem()` | `ComWrapper<ICamApiWorkpieceCoordinateSystem>` | Workpiece CS |
| `operationCom.ModelFormerJobAssignment()` | `ComWrapper<ICamApiModelFormer>` | Job assignment geometry |
| `operationCom.ModelFormerPart()` | `ComWrapper<ICamApiModelFormer>` | Part geometry |
| `operationCom.ModelFormerWorkpiece()` | `ComWrapper<ICamApiModelFormer>` | Workpiece/stock geometry |
| `operationCom.ModelFormerRestrictions()` | `ComWrapper<ICamApiModelFormer>` | Restriction surfaces |
| `operationCom.ModelFormerFixtures()` | `ComWrapper<ICamApiModelFormer>` | Fixture geometry |
| `operationCom.Technologist()` | `ComWrapper<ICamApiTechOperationOwner>` | Back-reference to the owning technologist (cast to `ICamApiTechnologist` if needed) |

**Approach/retract rule format.** The string value follows CLData notation:

| Value | Meaning |
|---|---|
| `[AUTO]` | Avoid collisions automatically |
| `[FromPrev]` | Approach from / retract to previous position |
| `[FromRoot]` | Approach from / retract to root (safe) position |
| `[SHORT]` | Short approach/retract |
| `[Default]` | Use operation default |
| `G53 A1(v) A2(v) ...` | Custom absolute axis positions in machine CS |

### Reading and writing XML properties

The `XMLProp` property returns a live `IST_XMLPropPointer` (wrapped in `ComWrapper`).
Changes made inside an `Invoke` call are applied directly to the operation — no
`LoadFromXmlProp` call is needed for the *live* prop.

```csharp
using var xmlPropCom = operationCom.XMLProp();
xmlPropCom.Invoke(xmlProp =>
{
    // Read
    bool isRoughing = xmlProp.Bol["Roughing"];
    string strategy  = xmlProp.Str["Strategy"];
    int    stepCount = xmlProp.Int["StepCount"];
    double stepSize  = xmlProp.Flt["RoughingStep"];

    // Write
    xmlProp.Bol["Roughing"]      = true;
    xmlProp.Str["Strategy"]      = "Equidistant";
    xmlProp.Int["StepCount"]     = 8;
    xmlProp.Str["UniteType"]     = "ByLevel";
    xmlProp.Flt["RoughingStep"]  = 10.0;
});
```

Accessors on `IST_XMLPropPointer`:

| Accessor | .NET type | Description |
|---|---|---|
| `xmlProp.Bol[key]` | `bool` | Boolean value |
| `xmlProp.Int[key]` | `int` | Integer value |
| `xmlProp.Flt[key]` | `double` | Floating-point value |
| `xmlProp.Str[key]` | `string` | String value |
| `xmlProp.Arr[key]` | sub-prop | Sub-array / nested node |

To copy properties from one operation to another:

```csharp
// Save from source
using var savedXml = sourceOpCom.SaveToXmlProp();

// Apply to destination
destOpCom.LoadFromXmlProp(savedXml);
```

**Direct access (IDL):** `ICamApiTechOperation.XMLProp` (property), `LoadFromXmlProp(xmlProp, out status)`, `SaveToXmlProp(out xmlProp, out status)`.

### Feeds

Per-operation feed values for each `TFeedTypeFlag` (working, rapid, plunge, engage, retract, return, approach, …) can be read and written with a measurement-aware API.

```csharp
// Read the working feed value
var info = opCom.Invoke(op => op.GetFeedValue(TFeedTypeFlag.fftWorking, out var status));
Console.WriteLine($"{info.Measurement}: perMin={info.ValuePerMinute}, perRev={info.ValuePerRevolution}");

// Set working feed to 250 mm/min, letting ENCY recalculate other measurements
opCom.Invoke(op => op.SetFeedValue(
    TFeedTypeFlag.fftWorking,
    TCamApiFeedMeasurement.cfmPerMinute,
    250.0,
    out var status));

// Change the units the feed is output in CLData (no recalculation)
opCom.Invoke(op => op.SetFeedOutputMeasurement(
    TFeedTypeFlag.fftWorking,
    TCamApiFeedMeasurement.cfmPerRevolution,
    out var status));
```

**`TCamApiFeedMeasurement`:** `cfmPerMinute` (mm/min or in/min, G94) · `cfmPerRevolution` (G95) · `cfmPerTooth` · `cfmPercentage` · `cfmRapid`.

**`TCamApiFeedInfo` fields:** `Measurement`, `ValuePerMinute`, `ValuePerRevolution`, `ValuePerTooth`, `ValuePercent`.

### Coolant tubes

Coolant configuration is per-machine (the machine declares up to 20 coolant tubes); the operation stores on/off state per tube.

```csharp
int tubeCount = opCom.Invoke(op => op.GetCoolantTubesCount());
for (int i = 0; i < tubeCount; i++)
{
    var info = opCom.Invoke(op => op.GetCoolantTubeInfo(i, out var st)); // Name, Available
    bool on  = opCom.Invoke(op => op.GetCoolantTubeState(i, out var st));
    Console.WriteLine($"[{i}] {info.Name} available={info.Available} on={on}");
}

// Turn on tube 0 for the operation
opCom.Invoke(op => op.SetCoolantTubeState(0, true, out var status));
```

### Spindle state

```csharp
var state = opCom.Invoke(op => op.GetSpindleState());
// state.RotationMode: ssrmOff | ssrmRPM | ssrmCSS
// state.RPMValue, SurfaceSpeedValue, MaxRPMValue
// state.RotationDirection: srdForward | srdReverse
// state.Range (0 = automatic gearbox)

opCom.Invoke(op =>
{
    op.SetSpindleRotationsToRPM(12000);          // constant RPM (G97)
    // or op.SetSpindleRotationsToCSS(300);      // constant surface speed (G96)
    // or op.SetSpindleRotationsToOff();
    op.SetSpindleRotationDirection(TCamApiSpindleRotationDirection.srdForward);
    op.SetSpindleMaxRPMValue(15000);             // upper clamp for CSS mode
    op.SetSpindleGearBoxRange(0);                // 0 = auto
    // op.SetSpindleRotationByDefault();         // reset to tool/machine default
});
```

### Toolpath export and navigation

```csharp
if (opCom.HasToolpath())
    opCom.ExportToolpath(new MyCLDReceiver());   // implements ICamApiExportToolpathReceiver

// Tree navigation without going through the technologist iterator
using var parentCom  = opCom.GetParentOperation(TCamApiReorderingMode.rmDesigned);
using var childCom   = opCom.GetFirstChildOperation(TCamApiReorderingMode.rmDesigned);
using var siblingCom = opCom.GetNextSiblingOperation(TCamApiReorderingMode.rmDesigned);
```

### Toolpath statistics

`GetTimeStatistics`, `GetBlocksStatistics`, `GetLengthStatistics` return zero values if the operation is not calculated.

```csharp
var t = opCom.GetTimeStatistics();   // RapidTime, IdleWorkTime, EffectiveWorkTime, AuxiliaryTime (seconds)
var b = opCom.GetBlocksStatistics(); // Lines, Arcs, MultiGoTo, Feeds, TotalBlocks
var l = opCom.GetLengthStatistics(); // WorkLength, RapidLength, EngageLength, RetractLength, PlungeLength, …
```

### Code example — create, configure, and check an operation

```csharp
// Create
using var opCom = technologistCom.CreateOperation("TSTWaterlineOp", afterId, "");

// Set name
opCom.SetName("Roughing - Waterline");

// Configure XML properties
using var xmlPropCom = opCom.XMLProp();
xmlPropCom.Invoke(x =>
{
    x.Bol["Roughing"]     = true;
    x.Str["Strategy"]     = "Equidistant";
    x.Int["StepCount"]    = 8;
    x.Flt["RoughingStep"] = 10.0;
});

// Assign tool
projectCom.SetOperationTool(opCom.Id(), "11");

// Set approach/retract
opCom.SetApproachRule("[AUTO]");
opCom.SetReturnRule("[FromRoot]");

// After calculation — check for errors
technologistCom.CalculateAllOperationsToolpath(true);
if (opCom.IsError())
    Console.WriteLine("Errors detected on: " + opCom.FullName());
```

**Reference examples:**
- [`Technologist/Operation/OperationNet/project/main/ExtensionCreateOperation.cs`](../../Technologist/Operation/OperationNet/project/main/ExtensionCreateOperation.cs)
- [`FullWorkflow/FullWorkflow3DProject/project/main/TechnologyHelper.cs`](../../FullWorkflow/FullWorkflow3DProject/project/main/TechnologyHelper.cs)

---

## ICamApiTechOperationIterator

**Purpose.** Provides tree-navigation over the technology operation tree.  The tree
can be traversed depth-first (child → sibling) or breadth-first; parent navigation is
also available.

### How to obtain

```csharp
using var iteratorCom = technologistCom.GetOperations(TCamApiReorderingMode.rmDesigned);
```

### Navigation methods

| Helper method / IDL method | Returns | Description |
|---|---|---|
| `iterator.MoveToChild()` | `bool` | Descend into the first child; `false` if none |
| `iterator.MoveToSibling()` | `bool` | Move to the next sibling; `false` if none |
| `iterator.MoveToParent()` | `bool` | Ascend to the parent; `false` if already at root |
| `iterator.Current()` | `ICamApiTechOperation*` | Current node |
| `iterator.Reset()` | `void` | Reset to the root |

The helper `AsEnumerable()` on `ComWrapper<ICamApiTechOperationIterator>` returns a flat
`IEnumerable<ComWrapper<ICamApiTechOperation>>` that performs a full depth-first
traversal.  `TechnologistHelper.EnumerateOperations()` uses this internally.

### Code example — flat enumeration

```csharp
using var iteratorCom = technologistCom.GetOperations(TCamApiReorderingMode.rmDesigned);
foreach (var opCom in iteratorCom.AsEnumerable())
{
    using (opCom)
        Console.WriteLine(opCom.FullName());
}
```

### Code example — manual recursive rename

```csharp
// Move past root into first real child
iteratorCom.Invoke(it => { it.Reset(); it.MoveToChild(); });
RenameRecursive(iteratorCom, "", 0);

void RenameRecursive(ComWrapper<ICamApiTechOperationIterator> it, string prefix, int n)
{
    n++;
    using var opCom = it.InvokeAndWrap(i => i.Current());
    opCom.Invoke(op => op.Name = $"{prefix}{n} {op.Name}");

    if (it.Invoke(i => i.MoveToChild()))
    {
        RenameRecursive(it, $"{prefix}{n}.", 0);
        it.Invoke(i => i.MoveToParent());
    }
    if (it.Invoke(i => i.MoveToSibling()))
        RenameRecursive(it, prefix, n);
}
```

**Reference example:** [`Technologist/Operation/RenameOperationsNet/project/main/ExtensionRenameOperations.cs`](../../Technologist/Operation/RenameOperationsNet/project/main/ExtensionRenameOperations.cs)

---

## ICamApiTechOperationType / ICamApiTechOperationTypeIterator

**Purpose.** `ICamApiTechOperationType` describes one registered operation type —
its stable GUID identifier, localized display name, and the path to its XML
declaration file.  `ICamApiTechOperationTypeIterator` iterates them.

### How to obtain

```csharp
// Iterator
using var typeIterCom = technologistCom.OperationTypes();

// Or flat enumeration
foreach (var typeCom in technologistCom.EnumerateOperationTypes())
{
    using (typeCom)
    {
        string id      = typeCom.Id();
        string caption = typeCom.Caption();
        string xmlPath = typeCom.DeclarationXmlFilePath();
    }
}
```

### ICamApiTechOperationType members

| Helper method | Returns | Description |
|---|---|---|
| `typeCom.Id()` | `string` | Stable GUID string, use as `operationTypeId` in `CreateOperation` |
| `typeCom.Caption()` | `string` | Localized display name shown in the UI |
| `typeCom.DeclarationXmlFilePath()` | `string` | Path to the XML file that declares this type's properties |

### ICamApiTechOperationTypeIterator members (direct access)

| IDL method | Description |
|---|---|
| `Next()` | Advance; returns `false` when exhausted |
| `Current()` | Returns `ICamApiTechOperationType*` for the current item |
| `Reset()` | Rewind to the beginning |

**Reference example:** [`Technologist/Operation/OperationNet/project/main/ExtensionCreateOperation.cs`](../../Technologist/Operation/OperationNet/project/main/ExtensionCreateOperation.cs)

---

## ICamApiPart

**Purpose.** Represents a workpiece part entity inside the technologist.  A project
may have multiple parts (e.g. for multi-part machining).

### How to obtain

```csharp
// Create new
using var partCom = technologistCom.CreatePart(externalId: 100);

// Access existing via PartAndStageList
using var listCom = technologistCom.PartAndStageList();
using var partCom = listCom.Part(index: 0);
```

### Members

| Helper method | R/W | Description |
|---|---|---|
| `partCom.PartIndex()` | R | Internal auto-generated index; use for other API calls |
| `partCom.ExternalID()` | R | User-editable external system identifier |
| `partCom.SetExternalID(value)` | W | Set the external ID |
| `partCom.PrototypePartIndex()` | R | Index of the original part if this is a copy; `-1` otherwise |
| `partCom.IsPartCopy()` | R | Whether this part is a copy of another part |

### Code example

```csharp
// Add a part, then inspect it
using var partCom = technologistCom.CreatePart(externalId: 42);
Console.WriteLine($"Part index: {partCom.PartIndex()}, ExternalID: {partCom.ExternalID()}");
```

**Reference example:** [`Technologist/Operation/OperationNet/project/main/ExtensionCreatePart.cs`](../../Technologist/Operation/OperationNet/project/main/ExtensionCreatePart.cs)

---

## ICamApiSetupStage

**Purpose.** Represents one setup stage (clamping/setup) in the technologist.
Each setup stage has its own machine instance and can have tool connectors
assigned.

### How to obtain

```csharp
// Create new
using var stageCom = technologistCom.CreateSetupStage();

// Access existing
using var listCom  = technologistCom.PartAndStageList();
using var stageCom = listCom.SetupStage(index: 0);
```

### Members

| Helper method | Returns | Description |
|---|---|---|
| `stageCom.SetupStageIndex()` | `int` | Internal index; use for other API calls |
| `stageCom.Machine()` | `ComWrapper<ICamApiMachine>` | Machine assigned to this stage |
| `stageCom.SetToolConnector(toolId, connectorId)` | `void` | Assign a tool to a specific spindle connector |

**Reference example:** [`Technologist/Operation/OperationNet/project/main/ExtensionCreateSetupStage.cs`](../../Technologist/Operation/OperationNet/project/main/ExtensionCreateSetupStage.cs)

---

## ICamApiPartStage

**Purpose.** Represents the combination of a specific part placed in a specific
setup stage.  Provides access to the workpiece coordinate system and workpiece
setup for that particular part/stage combination.

### How to obtain

```csharp
using var listCom      = technologistCom.PartAndStageList();
using var partStageCom = listCom.GetPartStage(partIndex: 0, setupStageIndex: 0);
```

### Members

| Helper method | Returns | Description |
|---|---|---|
| `partStageCom.PartIndex()` | `int` | Part index |
| `partStageCom.SetupStageIndex()` | `int` | Setup stage index |
| `partStageCom.WorkpieceCoordinateSystem()` | `ComWrapper<ICamApiWorkpieceCoordinateSystem>` | Workpiece coordinate system for this combination |
| `partStageCom.WorkpieceSetup()` | `ComWrapper<ICamApiWorkpieceSetup>` | Workpiece setup (offset, orientation) |
| `partStageCom.Machine()` | `ComWrapper<ICamApiMachine>` | Machine for this stage |

### Code example — configure workpiece position

```csharp
using var listCom      = technologistCom.PartAndStageList();
using var partStageCom = listCom.GetPartStage(0, 0);

// Set workpiece offset
using var wcsCom = partStageCom.WorkpieceCoordinateSystem();
wcsCom.Invoke(wcs => wcs.Offset = new TST3DPoint { X = -100, Y = -60, Z = 0 });

// Set stock offset
using var wpSetupCom = partStageCom.WorkpieceSetup();
wpSetupCom.Invoke(wps =>
{
    wps.Offset = new TST3DMatrix
    {
        vX = new TST3DPoint { X = 1, Y = 0, Z = 0 },
        vY = new TST3DPoint { X = 0, Y = 1, Z = 0 },
        vZ = new TST3DPoint { X = 0, Y = 0, Z = 1 },
        vT = new TST3DPoint { X = 0, Y = 0, Z = 100 }
    };
});
```

**Reference example:** [`FullWorkflow/FullWorkflow3DProject/project/main/TechnologyHelper.cs`](../../FullWorkflow/FullWorkflow3DProject/project/main/TechnologyHelper.cs)

---

## ICamApiPartAndStageList

**Purpose.** Container for all parts and setup stages in the technologist.  Use it
to enumerate, access by index, and retrieve part/stage combinations.

### How to obtain

```csharp
using var listCom = technologistCom.PartAndStageList();
```

**Direct access:** `technologist.PartAndStageList` (read-only property).

### Members

| Helper method | Returns | Description |
|---|---|---|
| `listCom.SetupStagesCount()` | `int` | Number of setup stages |
| `listCom.SetupStage(index)` | `ComWrapper<ICamApiSetupStage>` | Setup stage at `index` (0-based) |
| `listCom.PartsCount()` | `int` | Number of parts |
| `listCom.Part(index)` | `ComWrapper<ICamApiPart>` | Part at `index` (0-based) |
| `listCom.GetPartStage(partIndex, stageIndex)` | `ComWrapper<ICamApiPartStage>` | Part/stage combination |

### Code example — enumerate all part/stage combinations

```csharp
using var listCom = technologistCom.PartAndStageList();
int stageCount = listCom.SetupStagesCount();
int partCount  = listCom.PartsCount();

for (int s = 0; s < stageCount; s++)
for (int p = 0; p < partCount; p++)
{
    using var psCom = listCom.GetPartStage(p, s);
    if (psCom.IsNull) continue;
    Console.WriteLine($"Part {p} in Stage {s}");
}
```

---

## ICamApiSnapshot / IListCamApiSnapshot

**Purpose.** A snapshot is a saved state of the project at a point in time.  The
list of snapshots is accessible from `ICamApiProject`.

### How to obtain

```csharp
using var snapshotListCom = projectCom.Snapshots();
```

**Direct access:** `project.Snapshots` (read-only property, returns `IListCamApiSnapshot*`).

### ICamApiSnapshot members

| Helper method | Returns | Description |
|---|---|---|
| `snapshotCom.CreationTime()` | `_FILETIME` | Windows `FILETIME` of creation |
| `snapshotCom.IsAuto()` | `bool` | `true` = created automatically by the system; `false` = manually by user |

### IListCamApiSnapshot members (direct access)

| IDL method | Description |
|---|---|
| `Get(index)` | Return `ICamApiSnapshot*` at `index` |
| `Count()` | Number of snapshots |
| `Add(snapshot)` | Add a snapshot |
| `Remove(snapshot)` | Remove a snapshot |
| `RemoveAt(index)` | Remove by index |

### Code example

```csharp
using var snapshotListCom = projectCom.Snapshots();
int count = snapshotListCom.Invoke(sl => sl.Count());
for (int i = 0; i < count; i++)
{
    using var snapCom = snapshotListCom.InvokeAndWrap(sl => sl.Get(i));
    bool isAuto = snapCom.IsAuto();
    Console.WriteLine($"Snapshot {i}: auto={isAuto}");
}
```

---

## ICamApiUserTechOperationInfo / ICamApiUserTechOperationList

**Purpose.** User tech operations (also called user templates) are reusable operation
configurations saved by the user.  `ICamApiUserTechOperationInfo` describes one
template; `ICamApiUserTechOperationList` manages the collection.

### How to obtain

`ICamApiUserTechOperationList` is accessed through `ICamApiApplication` (not through
`ICamApiProject`):

```csharp
using var applicationCom     = ComWrapper.Create(context.CamApplication);
using var userOperationsCom  = applicationCom.UserTechOperationList();
```

### ICamApiUserTechOperationList members

| Helper / IDL method | Returns | Description |
|---|---|---|
| `userOpListCom.Invoke(l => l.Item[guid])` | `ICamApiUserTechOperationInfo*` | Fetch by GUID string |
| `userOpListCom.Invoke(l => l.ItemIds)` | `IListString*` | All GUIDs in the list |
| `userOpListCom.Invoke(l => l.CreateInstance())` | `ICamApiUserTechOperationInfo*` | Create a new empty template |
| `userOpListCom.AddFromOp(caption, opCom)` | `ComWrapper<ICamApiUserTechOperationInfo>` | Save an existing operation as a template |
| `userOpListCom.Invoke(l => l.AddFromFile(path, out status))` | `ICamApiUserTechOperationInfo*` | Load a template from a file |
| `userOpListCom.Invoke(l => l.Remove(guid, out status))` | `void` | Remove by GUID |

### ICamApiUserTechOperationInfo members

| Helper method | R/W | Description |
|---|---|---|
| `infoCom.GUID()` | R | Stable GUID of the template |
| `infoCom.Caption()` | R | Display name |
| `infoCom.SetCaption(value)` | W | Change display name |
| `infoCom.XMLProp()` | R | XML property bag; `Arr['RefOperations'][0]` references the base operation |
| `infoCom.SetXMLProp(xmlPropCom)` | W | Replace the XML property bag |

### Code example — save current operation as a template, then instantiate it

```csharp
using var applicationCom    = ComWrapper.Create(context.CamApplication);
using var projectCom        = applicationCom.GetActiveProject();
using var technologistCom   = projectCom.Technologist();
using var userOperationsCom = applicationCom.UserTechOperationList();
using var currentOpCom      = technologistCom.CurrentOperation();

string currentOpId = currentOpCom.Id();

// Save as template
using var infoCom = userOperationsCom.AddFromOp(currentOpCom.Name() + " Template", currentOpCom);
string templateGuid = infoCom.GUID();

// Instantiate the template after the current operation
using var newOpCom = technologistCom.CreateOperationFromUserTemplate(templateGuid, currentOpId);
Console.WriteLine("Instantiated: " + newOpCom.Name());
```

**Reference example:** [`Technologist/Operation/CreateUserOperationNet/project/main/ExtensionCreateUserOperation.cs`](../../Technologist/Operation/CreateUserOperationNet/project/main/ExtensionCreateUserOperation.cs)
