# Align tool to face example

This example demonstrates how to set the **tool orientation** of technology operations from the
normals of the planar faces selected in the geometry model. It is the programmatic equivalent of
picking a face in **Setup tab / Tool Orientation** for every operation by hand.

The library registers a single utility. When it runs, it reads the normal of every planar face
currently selected in the geometry model and applies them to the operations of the technology tree
in design order: the first selected face orients the first operation, the second face the second
operation and so on. Operations beyond the number of selected faces are left untouched. At the end
a message box lists the axis values and the inverse-kinematics branches of every processed
operation.

The tool tip stays where the operation left it: the solver is fed the current absolute position of
the tool and only its direction is replaced.

## API used

- `ICamApiGeomModel.GetFaceListOfSelected` (helper `geomModelCom.GetFaceListOfSelected()`) — the
  faces selected in the geometry model; `faceCom.GetPlane(out _, out var normal)` returns `false`
  for a non-planar face, which carries no single approach direction.
- `ICamApiMachine.CreateEvaluator` — one kinematic solver serves the whole loop.
- `ICamApiTechOperation.InitMachineEvaluator` — seeds the evaluator with the state of the operation:
  its flips, robot mode, setup coordinate system, external axes.
- `ICamApiMachineEvaluator.GetAbsoluteMatrix`, `CalcNextPos5D`, `SetNextPos` — solve the machine
  position for the requested tool direction. `SetNextPos` has to be called before the result is
  stored, otherwise the previous pose is written.
- `ICamApiMachineEvaluator.NextPosOutOfLimits` — whether the pose just solved is one the machine can
  actually hold. This is the check that paints an axis value red in the machine control panel.
- `ICamApiMachineConfiguration.SetToolOrientationFromEvaluator` — stores the pose the evaluator holds
  into the operation, indexing the axes that carry the tool orientation.
- `ICamApiMachineConfiguration` flips (`FlipsCount`, `FlipId`, `FlipEnabled`, `SetFlipEnabled`) —
  the inverse-kinematics branches of the operation.

## Reaching the direction is not the same as being able to hold it

A robot reaches the same tool direction with several joint solutions (elbow up or down, wrist
flipped or not), selected by the **flips** of the operation. The branch the current flips happen to
select may be unreachable — and `CalcNextPos5D` still returns `true` for it, because reaching the
point and staying inside the limits are two different questions.

`SolveReachable` therefore sweeps the flip combinations and keeps the first pose that is both solved
and inside the limits, re-seeding the evaluator on every iteration so the flips just written reach
the solver. On a robot the flip set also covers the **external axes** of a positioner, so letting the
sweep turn one on is often what makes a pose reachable at all.

Measured on a KUKA KR150 with a positioner, over three faces: without the sweep one face failed to
solve outright and the other two ended up 13° and 16° away from their normal; with it all three land
exactly on their normal.

> **Note on the 6th axis.** `CalcNextPos5D` covers the **Vector** and **Point** modes. The
> **ToolPath** mode additionally needs a lead direction, which a face normal does not carry — it is
> solved with `CalcNextPos6D`, taking the lead direction from the `vX` column of the matrix.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab, by specifying the file
   **bin/Debug/AlignToolToFaceNet.settings.json**.
3. Restart ENCY.
4. Select one or more planar faces in the geometry model, then run the
   **Tool: align tool axis to selected faces** utility.
