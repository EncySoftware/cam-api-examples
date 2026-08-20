# CadSketchDrawingNet usage example

This example demonstrates how to build parametric 2D sketches with the **CAD API**
and publish them into the geometry model of the active project. It shows the sketch
primitives rectangle, circle, slot, polygon and arc slot, and the bridge from a
CAMAPI geometry node to a CAD model (`AddCadGroup` -> `AsCadModel` -> `AddSketch`
-> draw -> `Save`).

Two utilities are provided:

- **Draw mounting plate sketch** — a plate outline with four mounting holes, a
  cable slot and a hex access cut-out (rectangle + circle + slot + polygon).
- **Draw flange gasket sketch** — concentric outer/bore circles, a computed
  six-hole bolt pattern and a curved relief slot (circle + arc slot).

# How to build

1. Compile this project with **"dotnet build"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab by specifying the file **bin/Debug/CadSketchDrawingNet.settings.json**.
3. Open or create a project, then in the utilities menu select **"Draw mounting plate sketch"** or **"Draw flange gasket sketch"**. A new CAD group with the sketch appears in the geometry model.
