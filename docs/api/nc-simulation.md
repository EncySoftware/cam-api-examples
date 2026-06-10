# NC Generation & Simulation — ENCY CAM API

This document covers the interfaces that take a machining operation from geometry assignment through toolpath calculation, CLData output, NC program generation, and simulation.

> **Probing operations** (measuring cycles) have a separate job-assignment API — see [api/probing.md](probing.md).

---

## High-level flow

```
Geometry model (faces / curves / holes / levels / zones)
        |
   ICamApiModelFormer  (attached to ICamApiTechOperation)
        |
   ICamApiTechOperationSolver.MakeWorkPath()
        |
   ICamApiCLDReceiver  (toolpath command stream)
        |        \
        |    IExtensionGeomCLDataConverter  (optional intercept / transform)
        |
   ICamApiProject.SaveClData()  →  *.inpcld file
        |
   ICamApiNCMaker.Generate()   →  NC program file(s)
        |
   ICamApiSimulator             (verify with machining simulation)
```

---

## ICamApiNCMaker — NC program generation

`ICamApiNCMaker` converts a saved CLData file into one or more NC program files using a postprocessor.
Obtained from `ICamApiProject.NCMaker`.

### Postprocessor types

```csharp
public enum TCamApiNCMakerSettingsType
{
    ncsDotnet = 0,  // .NET-based postprocessor (XML settings file)
    ncsSppx   = 1   // SPPX postprocessor
}
```

### ICamApiMakeCncDotnetSettings

Settings for the .NET postprocessor.

| Member | Description |
|--------|-------------|
| `SettingsFilePath` (read/write) | Path to the XML settings file for the postprocessor |

### ICamApiMakeCncSppxSettings

Settings for the SPPX postprocessor.

| Member | Description |
|--------|-------------|
| `OutputFolder` (read/write) | Directory where the NC file is written |
| `NcFileName` (read/write) | File name of the resulting NC program |

### ICamApiNCMaker methods

| Member | Description |
|--------|-------------|
| `CreateSettings(type, out status)` | Creates a settings object of the requested type |
| `Generate(clDataFileName, postProcessorFilePath, settings, out status)` | Runs the postprocessor and returns the list of generated file names |

### Full SaveClData + NCMaker workflow

The two-step pattern: first save CLData to a temporary file, then call `Generate`.

```csharp
// Step 1 — save CLData from the current operation set
var clDataFile = Path.Combine(Path.GetTempPath(), "output.inpcld");
project.SaveClData(clDataFile, operationIterator, out var ret);
if (ret.Code == TResultStatusCode.rsError)
    throw new Exception("Error saving CLData: " + ret.Description);

// Step 2 — choose postprocessor type and create settings
var settings = ncMaker.CreateSettings(TCamApiNCMakerSettingsType.ncsSppx, out ret)
    as ICamApiMakeCncSppxSettings
    ?? throw new Exception("Failed to create SPPX settings");
settings.OutputFolder = Path.GetTempPath();
settings.NcFileName   = "part.nc";

// Step 3 — point to the postprocessor file and generate
var ppPath = Path.Combine(pathsHelper.PostprocessorsFolder, "Mill", "Fanuc (30i)_Mill.sppx");
ncMaker.Generate(clDataFile, ppPath, settings, out ret);
if (ret.Code == TResultStatusCode.rsError)
    throw new Exception("Error generating NC: " + ret.Description);
```

Helper wrapper version (from `NCMakerHelper`):

```csharp
using var settingsCom = ncMakerCom.CreateSettings(TCamApiNCMakerSettingsType.ncsSppx);
// cast to SPPX settings and configure ...
var filesCom = ncMakerCom.Generate(clDataFile, ppPath, settingsCom);
```

**Reference example:** `GCodeGeneration/ExtensionUtilityNCMakerNet/project/main/ExtensionNcMaker.cs`
**Full workflow example:** `FullWorkflow/FullWorkflow3DProject/project/main/TechnologyHelper.cs` (`GenerateGCode` method)

---

## ICamApiSimulator — machining simulation

`ICamApiSimulator` drives the built-in material-removal simulation.
Obtained from `ICamApiProject.Simulator`.

### Collision / gouge detection flags

| Property | Type | Description |
|----------|------|-------------|
| `CheckGouges` | bool | Detect tool gouging of the part |
| `CheckHolderCollisions` | bool | Detect holder collisions |
| `CheckMachineCollisions` | bool | Detect machine node collisions |

### Break conditions

| Property | Type | Description |
|----------|------|-------------|
| `BreakOnStopCommand` | bool | Pause on STOP CLData command |
| `BreakOnEndOfOperation` | bool | Pause after each operation completes |
| `BreakOnErrors` | bool | Pause when a simulation error is detected |

### Simulation speed

| Property | Type | Description |
|----------|------|-------------|
| `SimulationSpeedPercent` | int | Speed for smooth simulation, 0–100 |

### Fast (batch) simulation methods

| Method | Description |
|--------|-------------|
| `FastSimulateCurrentOperation()` | Simulate only the current operation |
| `FastSimulateUpToCurrentOperation()` | Simulate from the first operation up to and including the current one |
| `FastSimulateAllOperations()` | Simulate every operation in the project |
| `ResetSimulationResults()` | Clear all simulation results |

### Smooth (interactive) simulation methods

| Method | Description |
|--------|-------------|
| `SmoothSimulationStart()` | Start continuous smooth playback from the current position |
| `SmoothSimulationStop()` | Stop smooth playback |
| `SmoothSimulationStepForward()` | Execute the next CLData command |
| `SmoothSimulationStepBackward()` | Step back one CLData command |

### Saving the machining result

```csharp
// SaveMachiningResultToSTL(partStage, fileName, out status)
// partStage — reserved for future use, pass null
simulator.SaveMachiningResultToSTL(null, outputStlPath, out var ret);
if (ret.Code == TResultStatusCode.rsError)
    throw new Exception(ret.Description);
```

### Complete simulation example

```csharp
using var simulatorCom = activeProjectCom.InvokeAndWrap(project => project.Simulator);
simulatorCom.Invoke(simulator =>
{
    // configure checks
    simulator.CheckGouges            = true;
    simulator.CheckHolderCollisions  = true;
    simulator.CheckMachineCollisions = true;

    // configure break conditions
    simulator.BreakOnStopCommand     = false;
    simulator.BreakOnEndOfOperation  = false;
    simulator.BreakOnErrors          = false;

    // run
    simulator.ResetSimulationResults();
    simulator.FastSimulateAllOperations();

    // export result
    var stlPath = Path.Combine(Path.GetTempPath(), "result.stl");
    simulator.SaveMachiningResultToSTL(null, stlPath, out var ret);
    if (ret.Code == TResultStatusCode.rsError)
        throw new Exception(ret.Description);
});
```

Helper wrapper version (from `SimulatorHelper`):

```csharp
simulatorCom.SetCheckGouges(true);
simulatorCom.FastSimulateAllOperations();
simulatorCom.SaveMachiningResultToSTL(null, stlPath);
```

**Reference example:** `FullWorkflow/FullWorkflow3DProject/project/main/SimulationHelper.cs`

---

## ICamApiCLDReceiver — toolpath command stream

`ICamApiCLDReceiver` is the sink for all toolpath commands emitted by either a `ICamApiTechOperationSolver` (custom operation) or intercepted by an `IExtensionGeomCLDataConverter`. Every method corresponds to one CLData command.

Defined in `CAMAPI.MCDFormerTypes`.  The helper base class `CLDRecevierWrapperDefault` (in `CAMAPI.DotnetHelper`) provides pass-through implementations of every method so that custom subclasses only override what they need.

### Structure commands

| Method | Description |
|--------|-------------|
| `BeginItem(itemType, id, caption)` | Open a CLData structural block (workpass, engage, retract, transition, …) |
| `EndItem()` | Close the current structural block |
| `AddComment(comment)` | Embed a comment string in the CLData stream |
| `AddPrint(text)` | Emit a print/log message |
| `AddInsert(insString)` | Insert a literal postprocessor string |
| `AddOpStop()` | Program optional stop |

### Move commands

| Method | Description |
|--------|-------------|
| `CutTo(p)` | 3-axis linear move to point `p` |
| `CutTo5d(p, n)` | 5-axis linear move: position `p`, tool axis normal `n` |
| `CutTo6d(m)` | 6-axis linear move: full 4×3 transformation matrix `m` |
| `ArcTo2d(pe, pc, plane, rc, canBeFull)` | 2D arc: end point `pe`, centre `pc`, working plane, radius, full-circle flag |
| `ArcTo3d(pe, pc, nc, rc, canBeFull)` | 3D arc in plane defined by `nc` (arc-plane normal at centre) |
| `ArcTo5d(pe, ne, pc, nc, rc, canBeFull)` | 5-axis arc: end position+normal, centre position+normal, radius |
| `ArcTo6d(pe, pc, nc, rc, canBeFull)` | 6-axis arc: full matrix at end, centre point and normal, radius |
| `MultiArcTo6d(pm, pe)` | Multi-axis arc segment with mid-point matrix `pm` and end matrix `pe` |
| `SetGeomLCS(m)` | Set the current geometry local coordinate system |
| `SetToolContactData(isExists, cn)` | Provide tool contact point data |
| `SetCurrentPlane(plane)` | Set the active working plane (`aplXY`, `aplYZ`, `aplZX`, …) |

### Feed commands

Feed type is expressed as a flag value from `TFeedTypeFlag`:

| Flag | Value | Meaning |
|------|-------|---------|
| `affWorking` | 0 | Standard cutting feed |
| `affRapid` | 1 | Rapid traverse |
| `affFirst` | 2 | First-line feed |
| `affEngage` | 4 | Small approach feed |
| `affRetract` | 8 | Small retraction feed |
| `affPlunge` | 16 | Plunge (Z-axis) feed |
| `affFinish` | 32 | Finishing pass feed |
| `affNext` | 64 | Short transition feed (Waterline) |
| `affReturn` | 128 | Return feed (pocketing / engraving) |
| `affApproach` | 256 | Distant approach feed |
| `affRapid5D` | 512 | 5-axis rapid with axis limit control |
| `affTransitionOnSafe` | 1024 | Transition on safe surface |
| `affApproachFromSafe` | 2048 | Approach from safe surface |
| `affReturnToSafe` | 4096 | Return to safe surface |
| `affLongNext` | 8192 | Long Waterline transition |
| `affThreadPitch` | 16384 | Thread pitch feed |
| `affChipBreak` | 32768 | Chip-breaking feed |

| Method | Description |
|--------|-------------|
| `OutStandardFeed(feed)` | Switch to a standard feed type (flag value) |
| `OutPercentFeed(feed, percent)` | Switch to a feed type scaled to `percent` % of the configured value |
| `OutFeed(feed, value, mpm)` | Switch to a feed type with an explicit value; `mpm=true` for m/min, `false` for mm/rev |

### Spindle commands

| Method | Description |
|--------|-------------|
| `AddSpindleSpeedOnRPM(rpm, range, direction)` | Set spindle speed in RPM; `direction=true` for CW |
| `AddSpindleSpeedOnCSS(css, maxRpm, range, direction)` | Set constant surface speed with RPM cap |
| `AddSpindleOff()` | Stop the spindle |
| `AddSpindleOrient(orientationAngle)` | Orient spindle to angle (degrees) |

### Compensation and auxiliary commands

| Method | Description |
|--------|-------------|
| `AddRadiusCompensation(mode, corrNumber)` | Radius compensation: `arcOff`, `arcLeft`, `arcRight` |
| `AddLengthCompensation(mode, corrNumber)` | Length compensation: `alcOff`, `alcOn`, `alcOnNegative` |
| `AddDelay(value)` | Dwell for `value` seconds |
| `AddCoolant(onOff, pipeNumber)` | Coolant on/off for pipe `pipeNumber` |
| `OutPower(power, value)` | Set laser/plasma power channel |
| `OutEffector(effectorID, value)` | Set binary effector state |

### Extended cycles

```csharp
// Begin a canned cycle block
receiver.BeginExtCycle(subCommands, subType, comment);
receiver.AddExtCyclePropFlt(propCode, value);
receiver.AddExtCyclePropInt(propCode, value);
receiver.AddExtCyclePropBol(propCode, value);
receiver.AddExtCyclePropStr(propCode, value);
receiver.EndExtCycle();
```

`subCommands` is a bitmask of `TExtendedCycleSubCommand`:
`aecscCycleOn = 1`, `aecscCycleOff = 2`, `aecscCycleCall = 4`.

### Toolpath structure items (TCLDItemType)

`BeginItem` / `EndItem` mark sections of the CLData tree:

| Value | Meaning |
|-------|---------|
| `aitCLData` | Top-level CLData block |
| `aitSub` | Sub-program |
| `aitGroup` | Logical group |
| `aitWorkpass` | Cutting pass |
| `aitEngage` | Engage (entry) move |
| `aitRetract` | Retract (exit) move |
| `aitApproach` | Approach from safe position |
| `aitReturn` | Return to safe position |
| `aitTransition` | Inter-pass transition |
| `aitTransitionOnSafe` | Transition via safe surface |
| `aitApproachFromSafe` | Approach from safe surface |
| `aitReturnToSafe` | Return to safe surface |
| `aitLongNext` | Long transition (Waterline) |

### Usage in a custom operation solver

```csharp
public void MakeWorkPath(ICamApiCLDReceiver cldFormer,
    ICamApiTechOperation techOperation,
    out TResultStatus resultStatus)
{
    resultStatus = default;

    // switch to rapid feed and move to start
    cldFormer.OutStandardFeed((int)TFeedTypeFlag.affRapid);
    cldFormer.CutTo(new TST3DPoint { X = 0, Y = 0, Z = 50 });

    // plunge at plunge feed
    cldFormer.OutStandardFeed((int)TFeedTypeFlag.affPlunge);
    cldFormer.CutTo(new TST3DPoint { X = 0, Y = 0, Z = 0 });

    // cut rectangle at working feed
    cldFormer.OutStandardFeed((int)TFeedTypeFlag.affWorking);
    cldFormer.CutTo(new TST3DPoint { X = 100, Y =   0, Z = 0 });
    cldFormer.CutTo(new TST3DPoint { X = 100, Y = 100, Z = 0 });
    cldFormer.CutTo(new TST3DPoint { X =   0, Y = 100, Z = 0 });
    cldFormer.CutTo(new TST3DPoint { X =   0, Y =   0, Z = 0 });

    // add a 2D arc
    var pe = new TST3DPoint { X = 50, Y = 0, Z = 0 };
    var pc = new TST3DPoint { X = 25, Y = 0, Z = 0 };
    cldFormer.ArcTo2d(pe, pc, TCLDPlaneType.aplXY, 25.0, false);

    cldFormer.AddComment("End of pass");
}
```

**Reference examples:**
- `Operation/ExtensionOperationSimpleNet/project/main/ExtensionOperationSimpleNet.cs`
- `Operation/ExtensionOperationParamsNet/project/main/ExtensionOperationParams.cs`
- `CLData/ExtensionGeomCLDataConverterNet/project/main/CLDReceiverWrapperCustom.cs`

---

## ICamApiExportToolpathReceiver — consuming an exported toolpath

`ICamApiTechOperation.ExportToolpath(receiver)` (see [project.md](project.md#icamapitechoperation)) streams the calculated toolpath into a callback receiver that your plugin implements. Unlike `ICamApiCLDReceiver`, which carries in-progress solver commands, `ICamApiExportToolpathReceiver` exposes a commands-and-children tree that describes the already-calculated toolpath in a neutral form suitable for custom post-processors or reports.

### Receiver lifecycle

| Method | Description |
|---|---|
| `OpenCommand(commandCode, commandHandle, parentCommandHandle)` | Start a new command in the tree. Commands are identified by opaque integer codes and linked to a parent command by handle. |
| `OpenCommandData()` / `CloseCommandData()` | Delimit the payload section of a command (points, normals, typed sub-commands). |
| `BeginPoints()` / `AddPoint(point)` / `EndPoints()` | Command-local polyline; optional `SetNormal(vZ)` / `SetOrientation(vZ, vX)` before a point sets orientation for 5D/6D moves. |
| `BeginChildren()` / `EndChildren()` | Nested sub-commands (enclosed structural items). |
| `CloseCommand()` | Close the currently open command. |
| `GetCommandReceiver(commandCode)` | Returns a typed `ICamApiExportToolpathCommand*` for the payload of the currently open command — cast to one of the sub-interfaces below to write typed data. |
| `GetCaptionReceiver()` | Returns `ICamApiExportToolpathCommand_Caption*` to set a command caption. |

### Typed command sub-receivers

| Interface | Purpose | Typed methods |
|---|---|---|
| `ICamApiExportToolpathCommand_Caption` | Attach a display caption to the current command | `SetCaption(caption)` |
| `ICamApiExportToolpathCommand_Comment` | Attach a comment string | `SetComment(comment)` |
| `ICamApiExportToolpathCommand_Feedrate` | Record a feed change | `SetFeed(feedType, feedUnits, value)` — `feedUnits` is `TCamApiFeedUnits` (`fuMPM`, `fuMPR`, `fuConditionCode`) |
| `ICamApiExportToolpathCommand_MultiGoto` | Multi-axis position with optional machine-axis values | `SetEndPoint`, `SetEndOrientation(A, B, C, D)`, `SetMachineStateFlags`, `SetTime`, `BeginMachineAxes(axesCount)` / `SetMachineAxisValue(id, value)` / `EndMachineAxes()` |

### Implementation skeleton

```csharp
public class MyExportReceiver : ICamApiExportToolpathReceiver
{
    public void OpenCommand(int commandCode, ulong h, ulong parent) { /* record the node */ }
    public void OpenCommandData() { }
    public void CloseCommandData() { }
    public void BeginChildren() { }
    public void EndChildren() { }
    public void CloseCommand() { }

    public void BeginPoints() { }
    public void SetNormal(TST3DPoint vZ) { }
    public void SetOrientation(TST3DPoint vZ, TST3DPoint vX) { }
    public void AddPoint(TST3DPoint p) { /* record */ }
    public void EndPoints() { }

    public ICamApiExportToolpathCommand? GetCommandReceiver(int commandCode) => /* return typed sub-receiver based on commandCode */ null;
    public ICamApiExportToolpathCommand_Caption? GetCaptionReceiver()       => null;
}

// And from the extension:
opCom.ExportToolpath(new MyExportReceiver());
```

---

## ICamApiModelFormer family — geometry model assignment

An `ICamApiTechOperation` exposes five `ICamApiModelFormer` properties:

| Property | Role |
|----------|------|
| `ModelFormerJobAssignment` | The machining area (what to machine) |
| `ModelFormerPart` | Final part geometry |
| `ModelFormerWorkpiece` | Stock material geometry |
| `ModelFormerRestrictions` | Restricted (keep-out) areas |
| `ModelFormerFixtures` | Fixture bodies |

Each model former is a container of `ICamApiModelItem` objects. The concrete geometry is assigned by querying which specialised sub-interfaces the former supports at runtime, then calling the appropriate `Add…Selected()` method. All `Add…Selected()` methods operate on whatever geometry is currently selected in the ENCY CAM geometry tree.

### Checking supported item types

Before adding geometry, check whether the operation supports the geometry kind:

```csharp
using var mfCom = operationCom.InvokeAndWrap(op => op.ModelFormerJobAssignment);
mfCom.Invoke(mf =>
{
    // faces
    if (mf is ICamApiModelFormerWithFaces withFaces)
    {
        using var items = ComWrapper.Create(withFaces.AddFacesSelected());
    }

    // 2D curves
    if (mf is ICamApiModelFormerWithCurve2D withCurve2D)
    {
        using var items = ComWrapper.Create(withCurve2D.AddCurves2DSelected());
    }

    // 5D curves
    if (mf is ICamApiModelFormerWithCurve5D withCurve5D)
    {
        using var items = ComWrapper.Create(withCurve5D.AddCurves5DSelected());
    }

    // depth levels
    if (mf is ICamApiModelFormerWithLevels withLevels)
    {
        if (withLevels.SupportsLevel(TModelFormerLevelType.amflTopLevel))
        {
            using var items = ComWrapper.Create(
                withLevels.AddLevelSelected(TModelFormerLevelType.amflTopLevel));
        }
    }

    // machining zones
    if (mf is ICamApiModelFormerWithZones withZones)
    {
        if (withZones.SupportsJobZones())
            using var items = ComWrapper.Create(withZones.AddJobZoneSelected());

        if (withZones.SupportsRestrictedZones())
            using var items = ComWrapper.Create(withZones.AddRestrictedZoneSelected());
    }

    // holes
    if (mf is ICamApiModelFormerWithHoles withHoles)
    {
        using var items = ComWrapper.Create(withHoles.AddHolesSelected());
    }
});
```

The `ICamApiModelFormer.SupportedItems` property (`ICamApiModelFormerSupportedItems`) enumerates the item types that the operation declares it can receive, including captions, hints, and icon paths.

### ICamApiModelFormerWithFaces

Assigns selected face geometry to the model former.

```csharp
// Helper syntax
using var itemsCom = modelFormerWithFacesCom.AddFacesSelected();
```

Returns `ICamApiListModelItem` — the list of model items that were added.

### ICamApiModelFormerWithLevels

Assigns top or bottom machining levels (Z-depth planes).

```csharp
bool supportsTop = withLevels.SupportsLevel(TModelFormerLevelType.amflTopLevel);
if (supportsTop)
{
    using var items = ComWrapper.Create(
        withLevels.AddLevelSelected(TModelFormerLevelType.amflTopLevel));
}
```

Each added item can be cast to `ICamApiLevelModelItem` to read/write the `Stock` offset.

### ICamApiModelFormerWithZones

```csharp
// Job zone (area to machine)
if (withZones.SupportsJobZones())
    using var jobZoneItems = ComWrapper.Create(withZones.AddJobZoneSelected());

// Restricted zone (keep-out area)
if (withZones.SupportsRestrictedZones())
    using var restrictedItems = ComWrapper.Create(withZones.AddRestrictedZoneSelected());
```

Each added job zone can be cast to `ICamApiJobZoneModelItem` to set:
- `Stock` — machining stock offset
- `ContactMode` — `ajcmCenter`, `ajcmInside`, `ajcmOutside`
- `AlternateFromSide` — alternate machining direction

### ICamApiModelFormerWithCurve2D

Assigns 2D contour curves.

```csharp
using var itemsCom = modelFormerWithCurve2DCom.AddCurves2DSelected();
```

Each item can be cast to `ICamApiCurve2DModelItem`:

| Property | Type | Description |
|----------|------|-------------|
| `Stock` | double | Offset from curve |
| `IsLeft` | bool | Machining side (left = true) |
| `IsInverted` | bool | Invert curve direction |
| `Compensation` | bool | Enable cutter radius compensation |
| `WithReturn` | bool | Enable lead-in / lead-out moves |

### ICamApiModelFormerWithCurve5D

Assigns 5-axis drive curves.

```csharp
using var itemsCom = modelFormerWithCurve5DCom.AddCurves5DSelected();
```

Each item can be cast to `ICamApiCurve5DModelItem`:

| Property | Type | Description |
|----------|------|-------------|
| `Stock` | double | Offset from curve |
| `AlternateDirection` | bool | Reverse machining direction |
| `AlternateFrontSide` | bool | Flip tool tilt side |
| `UseCustomVectors` | bool | Enable custom tool axis vectors and feed zones |
| `InterpolationMode` | `TModelFormerInterpolationMode` | `aimLinear`, `aimSpline`, `aimSplineInterMatr` |
| `FeedPoints` | `ICamApiFeedPointList*` | Feed zone list (non-empty only when `UseCustomVectors = true`) |

#### Feed zones on a 5D curve

Feed zones segment a 5D curve into regions with different feed rates:

```csharp
// enable custom control
curveItem.UseCustomVectors = true;

using var feedPoints = curveItem.FeedPoints;
// add a zone starting at curve point 'pos', spanning 100 mm
int idx = feedPoints.AddFeedPoint(pos, 100.0, out var ret);

// configure the zone
feedPoints.FeedType[idx]           = TModelFormerFeedType.afpWorkFeed;
feedPoints.FeedRatePercentage[idx] = 50;     // 50 % of programmed feed
feedPoints.FeedRateChangeType[idx] = TModelFormerFeedRateChangeType.afrcByPercent;
```

**Reference example:** `Technologist/SetFeedZone/project/main/CurveItem.cs`

### ICamApiModelFormerWithHoles

Assigns hole features or programmatically creates new holes.

```csharp
// from current selection
using var holesFromSel = modelFormerWithHolesCom.AddHolesSelected();

// create a hole programmatically
var lcs = new TST3DMatrix { /* set axis vectors */ };
using var holeCom = modelFormerWithHolesCom.CreateNewHole(lcs, 10.0 /* diameter */);
holeCom.Invoke(h =>
{
    h.TopLevel               = 0.0;
    h.TopLevelMode           = THoleTopLevelMode.htlmManual;
    h.BottomLevel            = -25.0;
    h.BottomLevelMode        = THoleBottomLevelMode.hblmManual;
    h.DrillTipCompensation   = THoleDrillTipCompensation.hdtAuto;
});
```

### ICamApiModelFormerWithProbingItems — probing cycles and actions

Exposed on operations of the "Point Probing" family. Items form a tree (group / movement / cycle), and each cycle owns an ordered list of typed probing actions.

Obtain via `opCom.ModelFormerJobAssignment()` and cast:

```csharp
using var mfCom = opCom.ModelFormerJobAssignment();
using var probingCom = mfCom.InvokeAndWrap(mf => mf as ICamApiModelFormerWithProbingItems);
if (!probingCom.IsNull)
{
    using var surf  = probingCom.AddSurfaceCycle();
    using var boss  = probingCom.AddBossCycle();
    using var hole  = probingCom.AddHoleCycle();
    using var grp   = probingCom.AddGroup();
    using var mov   = probingCom.AddMovement();
}
```

Factory helpers on `ModelFormerWithProbingItemsHelper` (each returns the typed cycle `ComWrapper`, no cast needed):

| Method | Produced cycle interface |
|---|---|
| `AddSurfaceCycle()` | `ICamApiSurfaceProbingCycle` |
| `AddBossCycle()` | `ICamApiBossProbingCycle` |
| `AddHoleCycle()` / `AddHoleProtectedCycle()` | `ICamApiHoleProbingCycle` / `ICamApiHoleProbingProtectedCycle` |
| `AddWebCycle()` | `ICamApiWebProbingCycle` |
| `AddGrooveCycle()` / `AddGrooveProtectedCycle()` | `ICamApiGrooveProbingCycle` / `ICamApiGrooveProbingProtectedCycle` |
| `AddThreePointsWebCycle()` | `ICamApiThreePointsWebProbingCycle` |
| `AddExternalRectangleCycle()` / `AddInternalRectangleCycle()` / `AddInternalRectangleProtectedCycle()` | `ICamApiExternalRectangleProbingCycle` / `ICamApi…InternalRectangleProbingCycle` |
| `AddDoubleWallInternalCornerCycle()` / `AddDoubleWallExternalCornerCycle()` | `ICamApiDoubleWall…CornerCycle` |
| `AddTripleWallInternalCornerCycle()` / `AddTripleWallExternalCornerCycle()` | `ICamApiTripleWall…CornerCycle` |
| `AddNcActionItem()` | `ICamApiNcActionProbingCycle` |
| `AddFrameOutputCycle()` | `ICamApiFrameOutputProbingCycle` |
| `AddMovement()` / `AddGroup()` | `ICamApiProbingModelItem` (movement or group) |
| `AddCycleByTemplate(fileName)` | Cycle instantiated from a library template |
| `EnumerateProbingItems()` | `IEnumerable<ComWrapper<ICamApiProbingModelItem>>` (depth-first walk) |
| `GetSelectedItem()` | Currently selected item in the UI |
| `DeleteItem(itemCom)` / `Clear()` | Remove one item / all items |
| `GetTemplateLibraryCount()` / `GetTemplateLibrary(index)` | Loaded `ICamApiProbingTemplateLibrary` instances |

#### Cycle actions

Every probing cycle carries an ordered list of typed `ICamApiProbingAction` entries — executed right after the cycle probes its primary feature (tool offset update, WCS set, report write, etc.). Typical access pattern:

```csharp
int count = cycleCom.Invoke(c => c.CycleActionsCount);
for (int i = 0; i < count; i++)
{
    using var actionCom = cycleCom.InvokeAndWrap(c => c.GetCycleAction(i));
    // cycle exposes AddSetToolOffsetAction / AddCheckBrokenToolAction / AddSetWcsAction /
    // AddCalibrateToolProbeAction / AddCalibratePartProbeAction / AddWriteToReportAction /
    // AddCustomPropGroupAction — each returns the typed action interface directly.
}
```

Helper classes: `ProbingModelItemHelper`, `ProbingModelItemIteratorHelper`, `ProbingCycleHelper`, `ProbingActionHelper`, `ProbingTemplateHelper`, plus specialised `SurfaceProbingCycleHelper`, `BossHoleProbingCycleHelper`, `WebGrooveProbingCycleHelper`, `RectangleProbingCycleHelper`, `DoubleTripleWallProbingCycleHelper`, `NcActionFrameOutputProbingCycleHelper`, `CustomPropGroupProbingActionHelper`.

---

### Other ICamApiModelFormer variants

These are additional specialized model formers — each implemented by a subset of operation types. Query with `mf is not ICamApi… x` inside `mfCom.Invoke(mf => { … })`, then call the factory. Each `Add…()` helper returns the typed model item `ComWrapper`.

| Interface | Helper | Typical call | Model item interface |
|---|---|---|---|
| `ICamApiModelFormerWithDriveFaces` | `ModelFormerWithDriveFacesHelper` | `AddDriveFacesSelected()` | `ICamApiDriveFaceModelItem` |
| `ICamApiModelFormerWithProjectCurves` | `ModelFormerWithProjectCurvesHelper` | `AddProjectCurvesSelected()` | `ICamApiProjectCurveModelItem` |
| `ICamApiModelFormerWithPocket` | `ModelFormerWithPocketHelper` | `AddPocketSelected()` | `ICamApiPocketModelItem` |
| `ICamApiModelFormerWithTurnGeometry` | `ModelFormerWithTurnGeometryHelper` | `AddTurnGeometrySelected()` | `ICamApiTurnGeometryModelItem` |
| `ICamApiModelFormerWithTurnMachineModel` | `ModelFormerWithTurnMachineModelHelper` | `SetItemMode(...)` | `ICamApiTurnMachineModelItem` |
| `ICamApiModelFormerWithGeom25D` | `ModelFormerWithGeom25DHelper` | `AddGeom25DSelected()` | `ICamApiGeom25DModelItem` |
| `ICamApiModelFormerWithSharpEdge` | `ModelFormerWithSharpEdgeHelper` | `AddSharpEdgesSelected()` | (face / edge items) |
| `ICamApiModelFormerWithChamferFaces` | `ModelFormerWithChamferFacesHelper` | `AddChamferFacesSelected()` | (face items) |
| `ICamApiModelFormerWithAreas` | `ModelFormerWithAreasHelper` | `AddArea(...)` | `ICamApiAreaModelItem` |
| `ICamApiModelFormerWithReferenceToPrevious` | `ModelFormerWithReferenceToPrevious` | `SetReferenceToPrevious(bool)` | — |

### Primitive model formers (boxes, cylinders, casting)

These model formers create pure parametric primitives — no geometry selection needed. All three primitive families share the same pattern: a factory method on the model former returns a typed item whose parameters are tweaked directly.

| Model former | Helper | Factory | Item |
|---|---|---|---|
| `ICamApiModelFormerWithBoxPrimitives` | `ModelFormerWithBoxPrimitives` | `AddBoxPrimitive(lcs, w, h, d)` · `AddBoxLinkPrimitive(...)` · `AddGeomModelBoxPrimitive(...)` · `AddSimpleBoxPrimitive(...)` | `ICamApiBoxPrimitiveModelItem` family |
| `ICamApiModelFormerWithCylinderPrimitives` | `ModelFormerWithCylinderPrimitives` | `AddCylinderPrimitive(lcs, r, h)` · `AddHoleCylinderPrimitive(...)` · `AddSimpleCylinderPrimitive(...)` | `ICamApiCylinderPrimitiveModelItem` family |
| `ICamApiModelFormerWithCastingPrimitive` | `ModelFormerWithCastingPrimitive` | `AddCastingPrimitive(lcs, …)` | `ICamApiCastingPrimitiveModelItem` |

Each primitive carries a local coordinate system plus shape parameters; set them directly on the `ComWrapper` through `Invoke` (there are no per-field helpers unless listed in the primitive's `*Helper.cs`).

### Generic model items

These typed `ComWrapper` specialisations are produced by the factories above. Each has a dedicated helper that exposes parameters as extension methods (call them on the `ComWrapper` directly):

| Model item interface | Helper | Purpose |
|---|---|---|
| `ICamApiFaceModelItem` / `ICamApiFacesArrayModelItem` | `FaceModelItemHelper` / `FacesArrayModelItemHelper` | Single face / face array |
| `ICamApiMeshesArrayModelItem` | `MeshesArrayModelItemHelper` | Mesh array |
| `ICamApiCurveModelItem` / `ICamApiCurvesArrayModelItem` | `CurveModelItemHelper` / `CurvesArrayModelItemHelper` | Generic 3D curve / curve array |
| `ICamApiPointModelItem` / `ICamApiLineModelItem` / `ICamApiCoordinateItem` | `PointModelItemHelper` / `LineModelItemHelper` / `CoordinateItemHelper` | Points, lines, coordinate frames |
| `ICamApiGeometryNodeBasedModelItem` | `GeometryNodeBasedModelItemHelper` | Base for items that reference a geometry tree node |
| `ICamApiLinkModelItem` | `LinkModelItemHelper` | Link item between two items |
| `ICamApiDriveFaceModelItem` · `ICamApiProjectCurveModelItem` · `ICamApiPocketModelItem` · `ICamApiTurnGeometryModelItem` · `ICamApiGeom25DModelItem` · `ICamApiTurnMachineModelItem` | `…Helper` | Parameters for each specialized model item |
| `ICamApiAreaModelItem` | `AreaModelItemHelper` | Area item (perimeter + floor level) |
| `ICamApiModelItemReference` | — | Reference to another item (e.g. reuse a job zone) |

---

### ICamApiModelFormer — base operations

| Method / Property | Description |
|-------------------|-------------|
| `SupportedItems` | Enumerate item types the operation declares support for |
| `MakeSupportedItems(callback)` | Register a callback that fills supported items programmatically |
| `FillItemsBySupportedItems()` | Apply the supported items list (returns `true` if items were added) |
| `Count` / `Item[i]` | Enumerate all model items currently in the former |
| `FindItem(itemID)` | Look up an item by string ID |
| `SearchInsertItem(type, id)` | Find or create an item of a given type |
| `AddItem(id, type, typeName)` | Add a new item entry |
| `DeleteItem(item)` | Remove an item |
| `DeleteItemById(id)` | Remove an item by ID |
| `GetFaceList(lcs)` | Retrieve all faces as `ICamApiFaceList` in the given coordinate system |
| `GetBoundingBox(lcs)` | Get the bounding box of all items |
| `AddSupportedItemsSelected(type, id)` | Add currently selected geometry for a specific supported-item type |

**Reference example:** `FullWorkflow/FullWorkflow3DProject/project/main/TechnologyHelper.cs`

---

## ICamApiTechOperationSolver — custom operation implementation

Implement `ICamApiTechOperationSolver` together with `IExtension` to create a custom operation type.

```csharp
public class MyOperationSolver : IExtension, ICamApiTechOperationSolver
{
    public IExtensionInfo? Info { get; set; }
    private ComWrapper<ICamApiTechOperation>? _operationCom;

    // --- lifecycle ---

    public void InitSolver(ICamApiTechOperationSolverInitializeContext context,
        out TResultStatus resultStatus)
    {
        resultStatus = default;
        _operationCom = ComWrapper.Create(context.TechOperation);
        // context.UpdateHandler is available for progress reporting
    }

    public void FinalizeSolver()
    {
        _operationCom?.Dispose();
        _operationCom = null;
    }

    // --- optional dynamic parameters ---

    public bool GetPropIterator(string pageId,
        out IST_CustomPropIterator? iterator,
        out TResultStatus resultStatus)
    {
        resultStatus = default;
        iterator = null;
        // Return false if the operation uses no custom property pages.
        // Return true and a populated IST_SimplePropIterator to expose
        // parameters in the operation properties panel.
        return false;
    }

    public void OnPropFilterChanged(string parameterName, string value)
    {
        // Called when a parameter changes; can be used to update
        // visibility/availability of other parameters.
    }

    // --- toolpath calculation ---

    public void MakeWorkPath(ICamApiCLDReceiver cldFormer,
        ICamApiTechOperation techOperation,
        out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            // read parameters from XMLProp
            var xmlProp = techOperation.XMLProp;
            var depth   = xmlProp.Flt["MyParams.Depth"];
            var passes  = xmlProp.Int["MyParams.Passes"];

            // emit toolpath commands
            cldFormer.OutStandardFeed((int)TFeedTypeFlag.affRapid);
            cldFormer.CutTo(new TST3DPoint { X = 0, Y = 0, Z = 10 });
            // ... additional moves
        }
        catch (Exception e)
        {
            resultStatus.Code        = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
```

### Reading XMLProp parameters

Operation parameters are stored in an `IST_XMLPropPointer` tree accessible via `ICamApiTechOperation.XMLProp`. The property path uses dot notation for nesting:

```csharp
var xmlProp = techOperation.XMLProp;

// read values
double depth   = xmlProp.Flt["ToolpathParams.ZLayers.ZStart"];
int    count   = xmlProp.Int["ToolpathParams.ZLayers.Count"];
string mode    = xmlProp.Str["DrillingType"];
bool   exists  = xmlProp.PropExists["ToolpathParams.ZLayers.ZStart"];

// write values
xmlProp.Flt["ToolpathParams.ZLayers.ZStep"] = -5.0;
xmlProp.Str["DrillingType"] = "HolePocketing";
```

### Exposing custom parameters (GetPropIterator)

To add a properties page to the operation panel, create and populate an `IST_SimplePropIterator` inside `GetPropIterator`. The `ExtensionOperationParamsNet` example shows the full pattern using `OperationCustomPropsHelper` as a convenience wrapper:

```csharp
public bool GetPropIterator(string pageId,
    out IST_CustomPropIterator? iterator,
    out TResultStatus resultStatus)
{
    resultStatus = default;
    iterator = null;
    try
    {
        using var propHelpersCom =
            SystemExtensionFactory.GetSingletonExtension<IST_CustomPropHelpers>(
                "Extension.CustomPropHelpers");

        _propIteratorCom?.Dispose();
        _propIteratorCom = propHelpersCom.InvokeAndWrap(h => h.CreateSimplePropIterator());

        // add a numeric parameter
        using var doublePropCom = propHelpersCom.InvokeAndWrap(h => h.CreateDoubleProp("Depth"));
        doublePropCom.Invoke(prop =>
        {
            prop.PropID      = "CustomOperationPropertiesArray(MyParams.Depth)";
            prop.ValueGetter = new DoubleValueGetter(() =>
                _operationCom!.Invoke(op => op.XMLProp.Flt[prop.PropID]));
            prop.ValueSetter = new DoubleValueSetter(v =>
                _operationCom!.Invoke(op => op.XMLProp.Flt[prop.PropID] = v));
            _propIteratorCom!.Invoke(it => it.AddNewProp(prop, -1));
        });

        _propIteratorCom.Invoke(it => it.MoveToRoot());
        iterator = _propIteratorCom.Invoke(it => (IST_CustomPropIterator)it);
        return true;
    }
    catch (Exception e)
    {
        resultStatus.Code        = TResultStatusCode.rsError;
        resultStatus.Description = e.Message;
        return false;
    }
}
```

**Reference examples:**
- `Operation/ExtensionOperationSimpleNet/project/main/ExtensionOperationSimpleNet.cs` — minimal solver, XMLProp parameters
- `Operation/ExtensionOperationParamsNet/project/main/ExtensionOperationParams.cs` — solver with full dynamic property page

---

## IExtensionGeomCLDataConverter — intercepting the CLData stream

`IExtensionGeomCLDataConverter` allows a plugin to intercept and modify every CLData command that flows from the operation solver to the postprocessor. It is registered alongside `IExtension` in the extension factory.

The interface has two methods:

| Method | Description |
|--------|-------------|
| `GetCLDReceiverWrapper(operation, receiver, out ret)` | Return a custom `ICamApiCLDReceiver` that wraps the original `receiver`. All commands will now pass through the wrapper. |
| `FinalizeConverter()` | Release all COM objects held by the converter. Called when the operation finishes. |

### Implementation pattern

1. Subclass `CLDRecevierWrapperDefault` and override only the methods of interest.
2. In `GetCLDReceiverWrapper`, construct the subclass wrapping the provided `receiver`.
3. In `FinalizeConverter`, dispose the wrapper.

```csharp
public class ExtensionGeomClDataConverter : IExtension, IExtensionGeomCLDataConverter
{
    public IExtensionInfo? Info { get; set; }
    private ICamApiCLDReceiver? _wrapper;

    public ICamApiCLDReceiver? GetCLDReceiverWrapper(
        ICamApiTechOperation operation,
        ICamApiCLDReceiver receiver,
        out TResultStatus ret)
    {
        ret = default;
        _wrapper = new MyReceiverWrapper(receiver);
        return _wrapper;
    }

    public void FinalizeConverter()
    {
        if (_wrapper is IDisposable d)
        {
            d.Dispose();
            _wrapper = null;
        }
    }
}

// Shift all working-feed CutTo points upward by 20 mm
public class MyReceiverWrapper : CLDRecevierWrapperDefault
{
    private int _currentFeed = (int)TFeedTypeFlag.affWorking;

    public MyReceiverWrapper(ICamApiCLDReceiver receiver) : base(receiver) { }

    public override void OutStandardFeed(int feed)
    {
        _currentFeed = feed;
        base.OutStandardFeed(feed);
    }

    public override void CutTo(TST3DPoint p)
    {
        if (_currentFeed == (int)TFeedTypeFlag.affWorking)
            p.Z += 20.0;
        base.CutTo(p);
    }
}
```

**Reference example:** `CLData/ExtensionGeomCLDataConverterNet/project/main/`
(`ExtensionGeomCLDataConverter.cs` + `CLDReceiverWrapperCustom.cs`)

---

## ICAMAPIProjectEtalon — test reference data

`ICAMAPIProjectEtalon` stores a tree of typed data nodes for use in automated tests. It is not part of the production machining workflow.

| Interface | Purpose |
|-----------|---------|
| `ICAMAPIProjectEtalon` | Load/save etalon from/to file; exposes `Version` and `FileName` |
| `ICAMAPIProjectEtalonReceiver` | Sink for writing etalon nodes (`BeginEtalonNode` / `EndEtalonNode`) |
| `ICAMAPIEtalizableObject` | Any object that can serialise itself to an `ICAMAPIProjectEtalonReceiver` |
| `ICAMAPIProjectEtalonComparer` | Compare two etalons; configure ignored node types via `IgnoredNodeTypesList` |
| `ICAMAPIProjectEtalonFormer` | Factory: `BeginEtalon()` / `EndEtalon()`, `CreateEtalonComparer()`, `SetFlag` / `GetFlag` |

Typical test usage:

```csharp
// Record reference data
etalonFormer.BeginEtalon();
etalizableObject.SaveToEtalonReceiver(etalonFormer as ICAMAPIProjectEtalonReceiver, out _);
var referenceEtalon = etalonFormer.EndEtalon();
referenceEtalon.SaveToFile("reference.etalon");

// Compare in a later test run
var currentEtalon = /* produce current etalon */;
var comparer = etalonFormer.CreateEtalonComparer();
comparer.IgnoredNodeTypesList = "Timestamp,Version";
bool equal = comparer.CompareEtalons(referenceEtalon, currentEtalon);
```

---

## IDL reference

| Interface | Library | GUID |
|-----------|---------|------|
| `ICamApiNCMaker` | `CAMAPI_NCMaker` | `46c43f46-79e9-43bb-abbd-b74f8620aca6` |
| `ICamApiMakeCncDotnetSettings` | `CAMAPI_NCMaker` | `6d8c69c9-9d81-4d5b-8929-9c6b14863c7d` |
| `ICamApiMakeCncSppxSettings` | `CAMAPI_NCMaker` | `5d1ea41a-0b6e-4be3-ba5d-1352b8d0df52` |
| `ICamApiSimulator` | `CAMAPI_Simulator` | `af46b3b9-0dc7-4780-96d2-1b146e058330` |
| `ICamApiCLDReceiver` | `CAMAPI_MCDFormerTypes` | `d947e098-92b6-4bac-9ca5-e3914cbf7904` |
| `ICamApiModelFormer` | `CAMAPI_ModelFormerTypes` | `5e407027-cd32-4702-b8da-b899a8b2ee38` |
| `ICamApiTechOperation` | `CAMAPI_TechOperation` | `c0384d7a-9e7c-4867-87aa-42642af3186c` |
| `ICamApiTechOperationSolver` | `CAMAPI_TechOperation` | `4559efd1-f631-451f-b5e7-d6f6a66f7acb` |
| `IExtensionGeomCLDataConverter` | `CAMAPI_TechOperation` | `369770ef-e71b-4fc0-b3b4-a8b55035de45` |
| `ICAMAPIProjectEtalon` | `CAMAPI_EtalonProject` | `E5493D50-5FBD-4931-B46E-8310A3B4AEA1` |
