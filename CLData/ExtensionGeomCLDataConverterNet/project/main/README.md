# ExtensionGeomCLDataConverter usage example

This example demonstrates how to create an extension that will convert geometry CLData in the CAM system.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionGeomCLDataConverterNet.settings.json**.
3. Restart ENCY.
4. Create a 2D contouring operation (`TST2DContouringOp`) and calculate it. The extension lifts the working moves of the toolpath by 20 along the Z axis.