# ExtensionUtilityGeometryModelNet usage example

This example demonstrates how to create an extension that will manipulate geometry models in the CAM system.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab by specifying the file **bin/Debug/ExtensionUtilityGeometryModelNet.settings.json**.
3. Restart ENCY.
4. In the utilities menu, select the "**Select colored geometry nodes**" item. Elements of the geometry model with a specific color will be selected.
5. In the utilities menu, select the "**Transform geometry model**" item. The geometry model will be transformed: shifted, rotated and scaled.
6. In the utilities menu, select the "**Export geometry model**" item. The geometry model will be exported to a file and Explorer will open with the location of the exported file.