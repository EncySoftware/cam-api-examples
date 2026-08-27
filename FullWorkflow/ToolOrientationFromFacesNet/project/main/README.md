# Tool orientation from face normals

This example answers the question *"how do I set the tool orientation of an operation from code,
the way the Setup tab does it when I pick a face?"* — see
[issue #2](https://github.com/EncySoftware/cam-api-examples/issues/2). It builds a complete project
from scratch, so it can be run as is and reproduced end to end:

1. creates a new project and imports a part;
2. sets the machine — a **KUKA KR150 robot with an E1/E2 positioner**;
3. mounts the workpiece on the plate of that positioner;
4. creates a row of identical operations;
5. reads the normals of the planar faces of the model;
6. gives every operation the tool orientation of its own normal, choosing an inverse-kinematics
   branch the robot can actually hold;
7. **calculates the toolpath again** and reports what changed.

A robot is deliberately the machine here. On a 3- or 5-axis mill the tool orientation is nearly a
formality; on a robot the same direction is reachable by several joint solutions, most of them
outside the joint limits, and the external axes of the positioner have to be let into the solution.
That is the case the issue is about.

## What it needs from the installation

Only the part ships with the example, in `assets/`, copied next to the DLL on build and resolved
against the folder the extension is loaded from.

The robot is **not** shipped — it is one of the machines of the CAM system, and
`ICamApiMachinesLibrary.FindMachine` resolves it out of the installed library:

```csharp
machinesLibraryCom.FindMachine(MachineGuid, "", MachineTypeName);
```

The file path of `FindMachine` is optional. Given a guid it finds the machine wherever the
installation keeps it, with its 3D models, so the robot is drawn in the viewport. A path is only
needed for a schema that lives outside the machines folder — an abstract machine shipped alongside
an example, say.

The operations keep the **default tool** of the machine, so no tool library is needed either: their
toolpath calculates without one.

`ExampleSettings.cs` holds everything worth changing — the part, the machine identifiers, the
connector the part is mounted on, the operation type and the operation count.

## The call sequence

The orientation of one operation is set by five calls that have to run in this order
(`ToolOrientationAligner.AlignOperation`):

| Call | Why it is needed |
|---|---|
| `ICamApiTechOperation.InitMachineEvaluator` | Seeds the evaluator with the state of *this* operation — its flips, robot mode, setup coordinate system, external axes. Without it the inverse kinematics is solved with default flips and a disabled positioner, and the axes disagree with the UI |
| `ICamApiMachineEvaluator.GetAbsoluteMatrix` | The current tool tip, taken from the `vT` column. Only the direction is replaced, the tip stays where the operation left it |
| `ICamApiMachineEvaluator.CalcNextPos5D` | Solves the machine axes for the requested tool direction |
| `ICamApiMachineEvaluator.SetNextPos` | Moves the solution into the evaluator state. **The easiest step to miss** — without it the *previous* axes are stored |
| `ICamApiMachineConfiguration.SetToolOrientationFromEvaluator` | Stores the pose the evaluator holds into the operation, indexing the axes that carry the tool orientation |

One evaluator serves the whole loop; `InitMachineEvaluator` re-seeds it for every operation.

`SetToolOrientationFromEvaluator` supersedes `SetAxesValues` here. `SetAxesValues` updates axes the
operation **already indexes**, so on an operation created programmatically — which indexes nothing
yet — there is nothing for it to update. The newer call defines those axes from scratch.

## Reaching the direction is not the same as being able to hold it

`CalcNextPos5D` returns `true` when it has found a solution, **not** when that solution is one the
machine can hold: it happily returns a pose with a joint wound past its limit. The second question is
answered by `ICamApiMachineEvaluator.NextPosOutOfLimits`, which is the very check that paints an axis
value red in the machine control panel.

That matters because a machine with rotary axes — a robot above all — reaches the same tool direction
with several joint solutions, selected by the **flips** of the operation. The branch the current flips
happen to select may be unreachable while another one is fine. The API has no flip iterator of its
own, so `ToolOrientationAligner.SolveReachable` sweeps the combinations and keeps the first pose that
is both solved and inside the limits:

```csharp
for (var flip = 0; flip < flipsCount; flip++)
    machineConfigCom.SetFlipEnabled(flip, (mask & (1 << flip)) != 0);

operationCom.InitMachineEvaluator(evaluatorCom);      // re-seed, so the new flips reach the solver
if (!evaluatorCom.CalcNextPos5D(position, false, false, true))
    continue;
if (evaluatorCom.NextPosOutOfLimits())
    continue;
```

The re-seed inside the loop is not redundant: the flips are written into the operation, and
`InitMachineEvaluator` is what carries them over to the evaluator.

On a robot the flip set also covers the **external axes** — a `Rotate<axis>` flip per positioner axis
plus `FlipRobotTable`. Enabling one lets the positioner turn the part instead of forcing the arm to
reach around it, which is often the difference between a reachable pose and none at all. Those flips
only appear when the part is actually mounted on the positioner: see *Mounting the part* below.

## Storing the orientation costs the operation its toolpath

Writing the orientation **invalidates** the toolpath of the operation: `McdTree` reads nothing right
after the call, and nothing rebuilds it on its own. Until the toolpath is calculated again the
operation has no path at all — which is what makes the API look like it broke something rather than
like it did nothing.

Measured over the three operations of this example: the first one goes from `142` nodes to `0` the
moment its orientation is stored, and only the recalculation brings it back — `30 | 85 | 82` across
the three, three different paths, one per orientation.

So the sequence above has to be followed by:

```csharp
technologistCom.ResetAllOperationsToolpath();
technologistCom.CalculateAllOperationsToolpath(true);
```

The axes survive both the recalculation and a save + reopen of the `.stcp` project. The report the
example shows at the end prints the node count of every operation before the alignment, after the
orientation is stored and after the recalculation, so the effect is visible without opening the
simulation.

Reading that toolpath has a catch worth knowing about. Walking a tree through `McdTree` + `GetNodes`
leaves interop objects behind that are only released by their finalizer, and they keep pointing at a
toolpath that a later call destroys — storing an orientation is enough to drop it. When the finalizer
eventually runs, it releases an object that is already gone and the process dies with an access
violation, far away from the code that caused it.

`MeasureNodeCounts` therefore drains them itself, with `GC.Collect` + `GC.WaitForPendingFinalizers`,
**at the end of the read** — while the tree is still alive. Draining just before the call that
destroys the toolpath does not help: by then the objects are already stale.

## Reading the face normals

`ICAMAPIGeometryModel.GetFaceListOfSelected` builds its list out of selected **face** nodes.
Selecting the part node itself — the one `FindByFullName("Part\\<file>")` returns — yields an empty
list. `FaceNormalReader` therefore selects every node of the model with `EnumerateNodes` +
`SetSelected(true)`, then keeps the distinct normals of the faces `ICamApiFace.GetPlane` reports as
planar. A non-planar face carries no single approach direction and is skipped.

The model matters: it needs planar faces looking in **different** directions. The part shipped in
`assets/` has 36 faces, 6 distinct planes, 4 of them tilted. A flat 2.5D part is useless here — one
measured 20 faces whose 5 planes all looked along Z, so every operation would end up with the same
orientation.

The operations also need a **setup** — `WorkpieceSetup` plus `WorkpieceCoordinateSystem`. An
operation without one gets no toolpath at all (`HasToolpath` is false) and there is nothing for the
orientation to act on.

## Mounting the part

`ICamApiWorkpieceSetup.MachineSideConnectorIndex` picks which connector of the machine the part is
mounted on, out of `ICamApiMachine.WorkpieceConnectorsCount`. **Index 0 is not automatically the
table.** On a robot cell the first connector is typically the floor, which no axis moves; mounting
the part there hides the positioner from the solver, `RobotTableAxesCount` reads 0 and no
`Rotate<axis>` flip is offered. Pick the connector by `ICamApiWorkpieceConnector.Name` instead.

`ICamApiWorkpieceSetup.Offset` is the offset of the part **relative to that connector**, not a world
matrix. Feeding it the world matrix of the connector applies the transform twice and sends the part
flying off the table.

## Scope and related examples

`CalcNextPos5D` covers the **Vector** and **Point** modes of the robot 6th axis. The **ToolPath**
mode additionally needs a lead direction, which a face normal does not carry — it is solved with
`CalcNextPos6D`, taking the lead direction from the `vX` column of the matrix.

On a robot the inverse kinematics has several branches: the same tool direction is reachable with
the elbow up or down and with the wrist flipped or not. Axes that disagree with the UI *for the very
same direction* mean a different branch, not a different mode. The report prints the branch
identifiers and their state. The companion example
[AlignToolToFaceNet](../../../../Technologist/Operation/AlignToolToFaceNet/project/main/README.md)
runs the same sequence over the faces selected by hand in an already open project, instead of
building one from scratch.

The MCD tree is used for reporting only (`ToolpathReporter`). On an SDK that does not expose it, that
file can be dropped without touching the rest of the example.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab by specifying the file **bin/Debug/ToolOrientationFromFacesNet.settings.json**.
3. Restart ENCY.
4. In the utilities menu, select the "**Workflow: set tool orientation of operations from face normals**" item. The whole workflow runs and reports the result.
