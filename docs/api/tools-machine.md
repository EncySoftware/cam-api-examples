# Tools & Machine — CAMAPI Reference

This document covers the interfaces for working with machining tools, machines, machine configuration, workpiece setup, and coordinate systems in the ENCY CAM API (.NET helper layer).

**Primary syntax:** .NET helper extension methods (`ComWrapper<T>` pattern).
**Direct access:** Raw IDL/COM signatures are noted as "direct access" where they differ meaningfully.

---

## Table of Contents

1. [ICamApiMachiningTool](#icamapimachiningtool)
2. [ICamApiMachiningToolInfo](#icamapimachiningtoolinfo)
3. [ICamApiMachiningToolOperationsIterator](#icamapimachiningtooloperationsiterator)
4. [ICamApiMachiningToolsList](#icamapimachiningtoolslist)
5. [ICamApiMachiningToolsManager](#icamapimachiningtoolsmanager)
6. [ICamApiMachineInfo](#icamapimachineinfo)
7. [ICamApiMachine](#icamapimachine)
8. [ICamApiMachineEvaluator](#icamapimachineevaluator)
9. [ICamApiMachineConfiguration](#icamapimachineconfiguration)
10. [ICamApiMachinesLibrary](#icamapimachineslibrary)
11. [ICamApiWorkpieceSetup](#icamApiworkpiecesetup)
12. [ICamApiWorkpieceCoordinateSystem](#icamApiworkpiececoordinatesystem)
13. [ICamApiToolConnector / ICamApiWorkpieceConnector](#connectors)

---

## ICamApiMachiningTool

**Namespace:** `CAMAPI.Tools`
**Helper:** `MachiningToolHelper`

Abstract interface representing any machining tool. Cast to a more specific interface depending on the concrete tool type.

| Helper method | Returns | Description |
|---|---|---|
| `ToolName(this ComWrapper<ICamApiMachiningTool>)` | `string` | User-visible tool name |

```csharp
using var toolEntityCom = toolInfoCom.ToolEntity(); // ComWrapper<ICamApiMachiningTool>
string name = toolEntityCom.ToolName();
```

> **Direct access (IDL):** `propertyR (ToolName, string)` on `ICamApiMachiningTool` (uuid `33B1098F-FA44-4D57-B08F-506E6CA2C5C5`).

---

## ICamApiMachiningToolInfo

**Namespace:** `CAMAPI.ToolsList`
**Helper:** `MachiningToolInfoHelper`

Describes one tool entry in the project tools list. Obtained from `ICamApiMachiningToolsList.ToolInfo[index]`.

| Helper method | Returns | Writable | Description |
|---|---|---|---|
| `ToolGUID(...)` | `string` | No | Globally unique tool identifier |
| `ToolID(...)` | `string` | No | Identifier unique within the project tools list |
| `ToolCaption(...)` | `string` | No | User-friendly display name |
| `ConnectorID(...)` | `string` | Yes (`SetConnectorID`) | Machine connector that holds the tool |
| `MagazineNumber(...)` | `int` | No | Magazine slot number |
| `ToolNumber(...)` | `int` | No | CNC controller tool number (T-code) |
| `FirstCorrectorNumber(...)` | `int` | No | First corrector/offset entry index in the CNC table |
| `ToolType(...)` | `string` | No | Tool type identifier as used by CAM (e.g. `"CylindricalMill"`) |
| `ToolProperties(...)` | `ComWrapper<IST_XMLPropPointer>` | No | Full tool properties as an XML property tree |
| `ToolEntity(...)` | `ComWrapper<ICamApiMachiningTool>` | No | The abstract tool object; cast to a concrete type if needed |

```csharp
using var toolsListCom = new ComWrapper<ICamApiMachiningToolsList>(activeProject.ToolsList);

for (int i = 0; i < toolsListCom.Count(); i++)
{
    using var toolInfoCom = toolsListCom.ToolInfo(i);

    Console.WriteLine($"Caption:          {toolInfoCom.ToolCaption()}");
    Console.WriteLine($"Type:             {toolInfoCom.ToolType()}");
    Console.WriteLine($"GUID:             {toolInfoCom.ToolGUID()}");
    Console.WriteLine($"ID:               {toolInfoCom.ToolID()}");
    Console.WriteLine($"Tool number:      {toolInfoCom.ToolNumber()}");
    Console.WriteLine($"First corrector:  {toolInfoCom.FirstCorrectorNumber()}");
    Console.WriteLine($"Connector ID:     {toolInfoCom.ConnectorID()}");
    Console.WriteLine($"Magazine number:  {toolInfoCom.MagazineNumber()}");
}
```

Changing the connector:

```csharp
toolInfoCom.SetConnectorID("Spindle1");
```

> **Direct access (IDL):** `ICamApiMachiningToolInfo` (uuid `f3dee0e0-89d3-4870-9869-9b31d13a20b1`) in `CAMAPI.ToolsList`.

**Full working example:** [`ProjectToolsList/ExtensionUtilityProjectToolsListNet/project/main/ExtensionUtilityProjectToolsList.cs`](../../ProjectToolsList/ExtensionUtilityProjectToolsListNet/project/main/ExtensionUtilityProjectToolsList.cs)

---

## ICamApiMachiningToolOperationsIterator

**Namespace:** `CAMAPI.ToolsList`
**Helper:** `MachiningToolOperationsIteratorHelper`

A forward iterator over all operations that use a specific tool. Obtained from `ICamApiMachiningToolsList.GetOperationsUsingTheTool(toolId)`.

| Helper method | Returns | Description |
|---|---|---|
| `Reset(...)` | `void` | Rewinds to before the first operation |
| `MoveNext(...)` | `bool` | Advances to the next operation; returns `false` when exhausted |
| `CurrentOperationIsEmpty(...)` | `bool` | `true` when the iterator is exhausted or before `Reset()` |
| `GetCurrentOperationID(...)` | `string` | Unique ID of the current operation in the project |
| `GetCurrentOperationCaption(...)` | `string` | Display name of the current operation |
| `GetCurrentOperationToolInfo(...)` | `ComWrapper<ICamApiMachiningToolInfo>` | Tool info as used in this specific operation |
| `AsEnumerable(...)` | `IEnumerable<ICamApiMachiningToolInfo>` | Convenience LINQ-compatible enumeration |

**Usage pattern — classic loop:**

```csharp
using var iterCom = toolsListCom.GetOperationsUsingTheTool(toolId);

iterCom.Reset();
while (!iterCom.CurrentOperationIsEmpty())
{
    Console.WriteLine(iterCom.GetCurrentOperationCaption());
    iterCom.MoveNext();
}
```

**Usage pattern — LINQ:**

```csharp
foreach (var toolInfo in iterCom.AsEnumerable())
{
    Console.WriteLine(toolInfo.ToolCaption);
}
```

> **Note:** `Reset()` must be called before the first `CurrentOperationIsEmpty()` check. The iterator starts in an uninitialized state.

> **Direct access (IDL):** `ICamApiMachiningToolOperationsIterator` (uuid `9219593f-65f7-445f-bd57-f17281690479`) in `CAMAPI.ToolsList`.

---

## ICamApiMachiningToolsList

**Namespace:** `CAMAPI.ToolsList`
**Helper:** `MachiningToolsListHelper`

The ordered list of all tools assigned to the active project. Obtained from `ICamApiProject.ToolsList`.

| Helper method | Returns | Description |
|---|---|---|
| `Count(...)` | `int` | Number of tools in the list |
| `ToolInfo(this, int index)` | `ComWrapper<ICamApiMachiningToolInfo>` | Tool info at the given index (0-based) |
| `IndexOfToolID(this, string toolId)` | `int` | Index of the tool by its `ToolID`, or `-1` if not found |
| `GetOperationsUsingTheTool(this, string toolId)` | `ComWrapper<ICamApiMachiningToolOperationsIterator>` | Iterator over operations that use the given tool |

```csharp
using var toolsListCom = new ComWrapper<ICamApiMachiningToolsList>(activeProject.ToolsList);

int count = toolsListCom.Count();
int idx = toolsListCom.IndexOfToolID("myToolId");

using var iterCom = toolsListCom.GetOperationsUsingTheTool("myToolId");
```

> **Direct access (IDL):** `ICamApiMachiningToolsList` (uuid `e6bc17b8-4425-4928-9c45-59a6b8bd1c64`) in `CAMAPI.ToolsList`.

---

## ICamApiMachiningToolsManager

**Namespace:** `CAMAPI.Tools`
**Helper:** `MachiningToolsManagerHelper`

Manages tool libraries and the tool roster of the active project. Obtained from `ICamApiApplication.MachiningToolsManager`.

| Helper method | Returns | Description |
|---|---|---|
| `OpenExistingLibrary(this, string libraryPath)` | `void` | Attaches an existing tool library file so its tools become searchable |
| `AddToolToProject(this, string libraryPath, string toolId)` | `void` | Adds a tool from a library to the active project |

Both methods throw `Exception` when the underlying `TResultStatus` carries an error.

```csharp
using var managerCom = applicationCom.InvokeAndWrap(app => app.MachiningToolsManager);

// Open a library first (optional if already known to the project)
managerCom.OpenExistingLibrary(@"C:\Libraries\Tools\MyMills.stdb");

// Add a specific tool by its ID
managerCom.AddToolToProject(@"C:\Libraries\Tools\MyMills.stdb", "tool-guid-or-id");
```

**Full working example:** [`Technologist/Operation/OperationToolAddNet/project/main/SelectToolOnClicked.cs`](../../Technologist/Operation/OperationToolAddNet/project/main/SelectToolOnClicked.cs)

> **Direct access (IDL):** `ICamApiMachiningToolsManager` (uuid `1d615fa9-5e39-4275-84e8-1a8ce245946b`) in `CAMAPI.Tools`. The raw `out TResultStatus` parameter must be checked manually when calling without the helper.

---

## ICamApiMachineInfo

**Namespace:** `CAMAPI.Machine`
**Helper:** `MachineInfoHelper`

Read-only snapshot of the machine assigned to the project. Obtained from `ICamApiProject.MachineInformation`.

| Helper method | Returns | Description |
|---|---|---|
| `SchemaFilePath(...)` | `string` | Absolute path to the machine schema (`.xml` / `.spmt`) file |
| `XMLNodeName(...)` | `string` | Root XML node ID inside the schema file |
| `GUID(...)` | `string` | Globally unique identifier of the machine schema |
| `MachineCaption(...)` | `string` | Human-readable machine name |
| `MachineTypeName(...)` | `string` | Machine type identifier as used in XML |

```csharp
using var machineInfoCom = new ComWrapper<ICamApiMachineInfo>(activeProject.MachineInformation);

Console.WriteLine($"Machine: {machineInfoCom.MachineCaption()}");
Console.WriteLine($"GUID:    {machineInfoCom.GUID()}");
Console.WriteLine($"File:    {machineInfoCom.SchemaFilePath()}");
Console.WriteLine($"XML node: {machineInfoCom.XMLNodeName()}");
```

**Full working example:** [`ProjectMachine/ExtensionUtilityProjectMachineInfoNet/project/main/ExtensionProjectMachineInfo.cs`](../../ProjectMachine/ExtensionUtilityProjectMachineInfoNet/project/main/ExtensionProjectMachineInfo.cs)

> **Direct access (IDL):** `ICamApiMachineInfo` (uuid `7367e163-74b9-4715-b03e-dbad8d171a88`) in `CAMAPI.Machine`.

---

## ICamApiMachine

**Namespace:** `CAMAPI.Machine`
**Helper:** `MachineHelper`

A live machine instance attached to an operation, with access to XML properties, connectors, and kinematic evaluation. Obtained from `ICamApiProject.Machine`.

| Helper method | Returns | Description |
|---|---|---|
| `XMLNodeName(...)` | `string` | Root XML node name |
| `GUID(...)` | `string` | Machine schema GUID |
| `MachineCaption(...)` | `string` | Display name |
| `XMLProp(...)` | `ComWrapper<IST_XMLPropPointer>` | Full XML property tree (read/write) |
| `CreateEvaluator()` | `ComWrapper<ICamApiMachineEvaluator>` | Creates a kinematic evaluator instance |
| `WorkpieceConnectorsCount(...)` | `int` | Number of workpiece connectors on this machine |
| `WorkpieceConnector(this, int index)` | `ComWrapper<ICamApiWorkpieceConnector>` | Workpiece connector at index (0-based) |
| `ToolPieceConnectorsCount(...)` | `int` | Number of tool connectors |
| `ToolPieceConnector(this, int index)` | `ComWrapper<ICamApiToolConnector>` | Tool connector at index (0-based) |
| `LoadFromOperationXml(this, ComWrapper<IST_XMLPropPointer>)` | `void` | Re-initializes machine state from operation XML |

The following members have no dedicated extension method; call them through `Invoke` / `InvokeAndWrap`:

| IDL member | Returns | Description |
|---|---|---|
| `TCPMEnabled` (R/W) | `bool` | Tool Center Point Management (RTCP) mode flag |
| `GetCurrentWorkpieceCSWorldMatrix()` | `TST3DMatrix` | Current workpiece CS (G54) matrix relative to the world CS. Result depends on TCPM mode |
| `GetCurrentWorkpieceCSMatrix()` | `TST3DMatrix` | Current workpiece CS (G54) matrix relative to the workpiece connector |
| `GetCurrentWorkpieceCSID()` | `string` | Identifier of the current workpiece CS, e.g. `"G54"` |

**Tool Center Point Management and current workpiece CS:**

```csharp
// Read TCPM state
bool tcpm = machineCom.Invoke(m => m.TCPMEnabled);

// Switch it on (equivalent to enabling RTCP)
machineCom.Invoke(m => m.TCPMEnabled = true);

// Query active work-offset (G54 and siblings)
string id    = machineCom.Invoke(m => m.GetCurrentWorkpieceCSID());
var    world = machineCom.Invoke(m => m.GetCurrentWorkpieceCSWorldMatrix()); // world-relative
var    local = machineCom.Invoke(m => m.GetCurrentWorkpieceCSMatrix());      // relative to workpiece connector
```

**Reading XML properties:**

```csharp
using var machineCom = activeProjectCom.InvokeAndWrap(project => project.Machine);
using var xmlPropCom = machineCom.XMLProp();

xmlPropCom.Invoke(xmlProp =>
{
    double spindleX = xmlProp.Flt["MachineDimensions.SpindleCenter.X"];
    Console.WriteLine($"Spindle X: {spindleX}");
});
```

**Writing XML properties:**

```csharp
xmlPropCom.Invoke(xmlProp =>
{
    xmlProp.Flt["MachineDimensions.TableCenter.X"] = 1000;
    xmlProp.Flt["MachineDimensions.TableCenter.Y"] = 0;
});
```

**Iterating connectors:**

```csharp
int wpCount = machineCom.WorkpieceConnectorsCount();
for (int i = 0; i < wpCount; i++)
{
    using var connCom = machineCom.WorkpieceConnector(i);
    Console.WriteLine($"Workpiece connector [{i}]: {connCom.Invoke(c => c.Name)}");
}

int tcCount = machineCom.ToolPieceConnectorsCount();
for (int i = 0; i < tcCount; i++)
{
    using var connCom = machineCom.ToolPieceConnector(i);
    Console.WriteLine($"Tool connector [{i}] id={connCom.Id()} name={connCom.Name()}");
}
```

**Full working example:** [`ProjectMachine/MachinePropsChangeNet/project/main/ExtensionMachinePropsChange.cs`](../../ProjectMachine/MachinePropsChangeNet/project/main/ExtensionMachinePropsChange.cs)

> **Direct access (IDL):** `ICamApiMachine` (uuid `f953cd89-f706-4e6a-94a7-b2aab3b17aff`) in `CAMAPI.Machine`. `LoadFromOperationXml` has an `out TResultStatus` parameter that must be checked without the helper.

---

## ICamApiMachineEvaluator

**Namespace:** `CAMAPI.Machine`

Stateful kinematic solver. Created by calling `ICamApiMachine.CreateEvaluator()`. The evaluator holds the current machine position and can solve forward/inverse kinematics.

All methods are accessed directly on the COM interface (no dedicated helper methods beyond `CreateEvaluator`). Key methods:

| Method | Description |
|---|---|
| `CalcNextPos5D(TST5DPoint, alternate, checkAll, ignoreRails)` | Solve machine position from a 5D toolpath point (XYZ + tool axis). Result stored in internal `NextPos`. |
| `CalcNextPos(TST5DPoint, alternate, checkAll, ignoreRails)` | Same as `CalcNextPos5D` with `AngleAroundTool` also considered. |
| `CalcNextPos6D(TST3DMatrix, alternate, checkAll)` | Solve from a full 4×4 matrix (6D). |
| `SetNextPos(rotaryOnly)` | Apply the result of the last `CalcNextPos*` call to the machine state. |
| `GetAbsoluteMatrix()` | Returns the current tool coordinate system matrix relative to the workpiece coordinate system. |
| `NCToGeom(matrix)` | Convert NC-space matrix to geometry space. |
| `GeomToNC(matrix)` | Convert geometry-space matrix to NC space. |
| `GetWorldWorkpieceConnectorMatrix(index)` | World-space position of workpiece connector at `index`. |

```csharp
using var evaluatorCom = machineCom.CreateEvaluator();

evaluatorCom.Invoke(evaluator =>
{
    var point5d = new TST5DPoint { /* ... */ };
    bool ok = evaluator.CalcNextPos5D(point5d, false, true, false);
    if (ok)
    {
        evaluator.SetNextPos(rotaryOnly: false);
        var matrix = evaluator.GetAbsoluteMatrix();
    }
});
```

> **Direct access (IDL):** `ICamApiMachineEvaluator` (uuid `b14befd9-e63f-4e87-ba5b-0aec1aa97f46`) in `CAMAPI.Machine`.

---

## ICamApiMachineConfiguration

**Namespace:** `CAMAPI.MachineConfiguration`
**Helper:** `MachineConfigurationHelper`

Represents the axis and flip configuration of a machine as set on an operation. Controls which axes have explicit values and which flips are active.

### Flips

| Helper method | Returns | Writable | Description |
|---|---|---|---|
| `FlipsCount(...)` | `int` | No | Total number of flip variants |
| `FlipId(this, int index)` | `string` | No | Flip identifier at `index` |
| `FlipCaption(this, int index)` | `string` | No | Localized display name of the flip |
| `FlipEnabled(this, int index)` | `bool` | Yes (`SetFlipEnabled`) | Whether the flip is active |

### Axes

| Helper method | Returns | Writable | Description |
|---|---|---|---|
| `AxesCount(...)` | `int` | No | Total number of axes in configuration |
| `AxisId(this, int index)` | `string` | No | Axis identifier at `index` |
| `AxisAvailable(this, int index)` | `bool` | No | Whether this axis is applicable to the current machine |
| `AxisDefined(this, int index)` | `bool` | Yes (`SetAxisDefined`) | Whether the axis value is explicitly set (vs. inherited from previous operation) |
| `AxisValue(this, int index)` | `double` | Yes (`SetAxisValue`) | Numeric axis position value |
| `GetAxisIndexOf(this, string axisId)` | `int` | — | Finds axis index by ID, returns `-1` if not found |
| `SetAxesValues(this, evaluator, rotaryOnly)` | `void` | — | Copies current evaluator position into axis values |

```csharp
// List all configured axes
int axesCount = machineConfigCom.AxesCount();
for (int i = 0; i < axesCount; i++)
{
    if (!machineConfigCom.AxisAvailable(i))
        continue;

    string id = machineConfigCom.AxisId(i);
    bool defined = machineConfigCom.AxisDefined(i);
    double value = defined ? machineConfigCom.AxisValue(i) : double.NaN;
    Console.WriteLine($"  Axis {id}: defined={defined}, value={value}");
}

// Set axis value by ID
int aIdx = machineConfigCom.GetAxisIndexOf("B");
if (aIdx >= 0)
{
    machineConfigCom.SetAxisDefined(aIdx, true);
    machineConfigCom.SetAxisValue(aIdx, 45.0);
}

// Sync all axis values from a machine evaluator
machineConfigCom.SetAxesValues(evaluatorCom, rotaryAxesOnly: true);
```

> **Direct access (IDL):** `ICamApiMachineConfiguration` (uuid `feef5150-e500-4c54-9399-732ff1caf3ed`) in `CAMAPI.MachineConfiguration`. `SetAxesValues` has an `out TResultStatus` that must be checked without the helper.

---

## ICamApiMachinesLibrary

**Namespace:** `CAMAPI.MachinesLibrary`
**Helper:** `MachinesLibraryHelper`

Provides access to the machine library registered in the ENCY installation. Used to identify which machine is assigned to the project or to locate a machine by GUID.

| Helper method | Returns | Description |
|---|---|---|
| `CurrentMachine(...)` | `ComWrapper<ICamApiMachineInfo>` | Machine currently assigned to the active project |
| `FindMachine(this, Guid guid, string filePath, string typeName)` | `ComWrapper<ICamApiMachineInfo>` | Finds the best matching machine by GUID; `filePath` and `typeName` are optional hints |

```csharp
using var libraryCom = /* obtained from session/application context */;

// Get the currently assigned machine
using var currentMachineCom = libraryCom.CurrentMachine();
Console.WriteLine(currentMachineCom.MachineCaption());

// Locate a machine by GUID
var guid = new Guid("7367e163-74b9-4715-b03e-dbad8d171a88");
using var foundCom = libraryCom.FindMachine(guid, filePath: "", typeName: "");
if (foundCom != null)
    Console.WriteLine($"Found: {foundCom.MachineCaption()}");
```

> **Direct access (IDL):** `ICamApiMachinesLibrary` (uuid `43b54d38-0b3c-46d4-9129-8f3301a9db59`) in `CAMAPI.MachinesLibrary`.

---

## ICamApiWorkpieceSetup

**Namespace:** `CAMAPI.Workpiece`
**Helper:** `WorkpieceSetupHelper`

Describes how the workpiece is mounted on the machine for a given operation — which machine connector is used and where the workpiece coordinate system origin is placed.

| Helper method | Returns | Writable | Description |
|---|---|---|---|
| `MachineSideConnectorIndex(...)` | `int` | Yes (`SetMachineSideConnectorIndex`) | Index into `ICamApiMachine.WorkpieceConnector[...]` |
| `WorkpieceSideCoordinateSystemName(...)` | `string` | Yes (`SetWorkpieceSideCoordinateSystemName`) | Name of the coordinate system on the workpiece side |
| `Offset(...)` | `TST3DMatrix` | Yes (`SetOffset`) | Offset of the workpiece coordinate system relative to the global coordinate system |

```csharp
using var setupCom = /* obtained from operation context */;

int connectorIdx = setupCom.MachineSideConnectorIndex();
string csName = setupCom.WorkpieceSideCoordinateSystemName();
TST3DMatrix offset = setupCom.Offset();

// Change the mounting connector
setupCom.SetMachineSideConnectorIndex(1);
```

> **Direct access (IDL):** `ICamApiWorkpieceSetup` (uuid `3b1c2d3e-4f5a-6b7c-8d9e-a0b1c2d3e4f5`) in `CAMAPI.Workpiece`.

---

## ICamApiWorkpieceCoordinateSystem

**Namespace:** `CAMAPI.Workpiece`
**Helper:** `WorkpieceCoordinateSystemHelper`

Controls the G-code coordinate system offset (G54–G59 and sub-variants) for an operation.

### Coordinate system modes (`TCamApiWorkpieceCoordinateSystemMode`)

| Value | Constant | Description |
|---|---|---|
| `0` | `wcsGlobal` | Use the global (machine zero) coordinate system |
| `1` | `wcsOff` | No coordinate system active |
| `2` | `wcsPrevious` | Inherit from the previous part in the setup stage |
| `3` | `wcsOffset` | Global CS with a numeric offset |
| `4` | `wcsName` | Use a named coordinate system from the project |

### Helper members

| Helper method | Returns | Writable | Description |
|---|---|---|---|
| `ID(...)` | `string` | Yes (`SetID`) | G-code register value: `"54"`, `"54.1"`, `"55"`, … `"59"` (without the `G` prefix) |
| `Mode(...)` | `TCamApiWorkpieceCoordinateSystemMode` | Yes (`SetMode`) | Active mode |
| `CoordinateSystemName(...)` | `string` | Yes (`SetCoordinateSystemName`) | Name of CS when mode is `wcsName` |
| `Offset(...)` | `TST3DPoint` | Yes (`SetOffset`) | Numeric offset when mode is `wcsOffset` |

```csharp
using var wcsCom = /* obtained from operation workpiece context */;

// Select G55
wcsCom.SetID("55");
wcsCom.SetMode(TCamApiWorkpieceCoordinateSystemMode.wcsGlobal);

// Use a named coordinate system
wcsCom.SetMode(TCamApiWorkpieceCoordinateSystemMode.wcsName);
wcsCom.SetCoordinateSystemName("Op2_CS");

// Apply an offset from machine zero
wcsCom.SetMode(TCamApiWorkpieceCoordinateSystemMode.wcsOffset);
wcsCom.SetOffset(new TST3DPoint { X = 100, Y = 0, Z = 50 });
```

> **Direct access (IDL):** `ICamApiWorkpieceCoordinateSystem` (uuid `b31672f7-67fe-4608-98ea-d16769adced8`) in `CAMAPI.Workpiece`.

---

## Connectors

### ICamApiToolConnector

**Namespace:** `CAMAPI.Machine`
**Helper:** `ToolConnectorHelper`

Represents a spindle/tool holder position on the machine.

| Helper method | Returns | Description |
|---|---|---|
| `Id(...)` | `string` | Unique string identifier of this connector |
| `Name(...)` | `string` | Non-unique display name |

```csharp
int count = machineCom.ToolPieceConnectorsCount();
for (int i = 0; i < count; i++)
{
    using var tcCom = machineCom.ToolPieceConnector(i);
    Console.WriteLine($"Tool connector: id={tcCom.Id()}, name={tcCom.Name()}");
}
```

### ICamApiWorkpieceConnector

**Namespace:** `CAMAPI.Machine`

Represents a workpiece mounting point on the machine. No separate helper class — access `Name` directly.

```csharp
int count = machineCom.WorkpieceConnectorsCount();
for (int i = 0; i < count; i++)
{
    using var wcCom = machineCom.WorkpieceConnector(i);
    string name = wcCom.Invoke(c => c.Name);
    Console.WriteLine($"Workpiece connector [{i}]: {name}");
}
```

> **Direct access (IDL):** `ICamApiToolConnector` (uuid `a093352c-1600-4486-ae3c-b11bd36556aa`), `ICamApiWorkpieceConnector` (uuid `34f1950f-5bb6-478e-960a-a474c5b0726c`) in `CAMAPI.Machine`.

---

## Common Patterns

### Obtaining tools-related objects from a project

```csharp
using var applicationCom = new ComWrapper<ICamApiApplication>(context.CamApplication);
using var projectCom = applicationCom.InvokeAndWrap(app => app.GetActiveProject(out var rs));

// Tools list
using var toolsListCom = projectCom.InvokeAndWrap(p => p.ToolsList);

// Machine info (read-only snapshot)
using var machineInfoCom = projectCom.InvokeAndWrap(p => p.MachineInformation);

// Machine instance (live, with XML props)
using var machineCom = projectCom.InvokeAndWrap(p => p.Machine);

// Tools manager
using var managerCom = applicationCom.InvokeAndWrap(app => app.MachiningToolsManager);
```

### COM lifetime management

Always use `using` (or explicit `Dispose`) on every `ComWrapper<T>`. Do not rely on the garbage collector for COM object release, as ENCY CAM may reclaim server-side objects unpredictably.

```csharp
// Correct
using var toolInfoCom = toolsListCom.ToolInfo(i);
// toolInfoCom is released at end of using block

// Incorrect — do not hold COM wrappers across long-lived scopes without explicit disposal
var toolInfoCom = toolsListCom.ToolInfo(i); // leaks if not disposed
```

---

## Related Examples

| Example | Location | Topics covered |
|---|---|---|
| Project tools list | [`ProjectToolsList/ExtensionUtilityProjectToolsListNet/`](../../ProjectToolsList/ExtensionUtilityProjectToolsListNet/) | `ICamApiMachiningToolsList`, `ICamApiMachiningToolInfo`, `ICamApiMachiningToolOperationsIterator` |
| Machine info | [`ProjectMachine/ExtensionUtilityProjectMachineInfoNet/`](../../ProjectMachine/ExtensionUtilityProjectMachineInfoNet/) | `ICamApiMachineInfo` |
| Machine XML props change | [`ProjectMachine/MachinePropsChangeNet/`](../../ProjectMachine/MachinePropsChangeNet/) | `ICamApiMachine`, `XMLProp` |
| Add tool to operation | [`Technologist/Operation/OperationToolAddNet/`](../../Technologist/Operation/OperationToolAddNet/) | `ICamApiMachiningToolsManager`, `AddToolToProject` |
| DIN 4000 tool import | [`MachiningTools/DIN4000ImportPluginNet/`](../../MachiningTools/DIN4000ImportPluginNet/) | `IMTI_MachiningToolsImportLibrary`, tool storage creation |
