# APT Arcs Example

Shows that the milling **APT interpreter of ENCY reads arcs**: a program whose arcs are written as
`CIRCLE` records arrives in the toolpath as arcs — radius, centre and plane kept — and not as a
chain of interpolated points.

The utility opens a non-modal window. **Load APT and calculate** adds a G-code based operation
(`TNCMillOp`) to the active project, loads the sample program into it, calculates that one
operation and lists the resulting toolpath. Arc nodes are shown in bold with their radius in
column **R**; the summary line says how many arcs of which radius came out.

Expected result for the shipped sample: **8 arcs (4 x R10, 4 x R20)**.

## The sample program

`assets/ARC_SAMPLE.APT` mills an 80x60 contour with R10 corners and then a full R20 circle built
from four 90-degree arcs — 8 arcs in all.

An arc in APT is **two lines**: `CIRCLE` gives the centre, the axis and the radius, and the `GOTO`
that follows gives the end point. The start point is wherever the tool already is:

```
CIRCLE/30.0,-20.0,-2.0, 0.0,0.0,1.0, 10.0, 0.03,0.5, 10.0,0.0
GOTO/40.0,-20.0,-2.0
```

The last two numbers are the tool diameter and corner radius.

## What the code does

```csharp
using var projectCom      = appCom.GetActiveProject();
using var technologistCom = projectCom.Technologist();
using var rootCom         = technologistCom.RootOperation();

var operationCom = technologistCom.CreateOperation("TNCMillOp", rootCom.Id(), "");

using (var gcodeOperationCom = operationCom.AsWithGCode())
    gcodeOperationCom.LoadNCProgramFile(aptFilePath);

technologistCom.SetCurrentOperation(operationCom);
technologistCom.CalculateToolpath(false);
```

`CalculateToolpath` calculates **only the current operation** — `CalculateAllOperationsToolpath`
would recalculate the operations the user already has in the project, which an example has no
business doing.

The result is read twice over, and the two readings are expected to agree:

- `operationCom.McdTree()` walked node by node — what each move actually is;
- `operationCom.GetBlocksStatistics()` — the `Arcs` / `Lines` tally ENCY itself keeps for the
  operation, with no parsing involved.

### Recognizing an arc in the MCD tree

A node caption carries neither the word `CIRCLE` nor the word `ARC`. An arc looks like this —
radius first (its sign is the direction the arc is travelled), then the end point, then the centre
as `Xc/Yc/Zc`, then the plane:

```
R-10, X40, Y-20, Z-2, Xc30, Yc-20, Zc-2, Plane XY
```

A straight move is the bare end point:

```
X-30, Y-30, Z-2
```

So the example matches `^R-?\d+.*Xc` against the caption.

## Choosing the interpreter

The operation is calculated only if ENCY knows the path to the milling APT interpreter,
`MyDocuments/Interpreters/Mill/Apt_Mill.snci`. If it does not, a modal file-choosing dialog opens —
fine in the GUI, where the user picks the file and the calculation goes on.

Two things are worth knowing about this:

- **Headless there is no dialog**, and the operation is calculated into an *empty* body with **no
  error at all**: the toolpath holds `$Header$`, `LOADTL` and `$Tail$` and not a single move. When
  running ENCY without a UI, pass the interpreter on the command line:
  `/Interpreter:<full path to the .snci>`.
- **`op.XMLProp.SetStr("SNCIFile", path)` is not enough**, convincing as it looks — the value reads
  back correctly, but the field the calculation actually uses is refreshed only inside
  `LoadFromXMLProp`, which the UI calls after the inspector is edited and the API never calls. The
  example writes the property anyway, so that a saved project says which interpreter was meant, but
  the calculation does not depend on it.

## How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab, by specifying the file
   **bin/Debug/AptArcsNet.settings.json**.
3. Restart ENCY.
4. Open a project with a milling machine, then find **APT Arcs Example** among the utilities and
   click **Load APT and calculate**.
