# ExtensionUtilityGeometryImporter usage example

This example demonstrates how to create an extension that will open geometry file in the CAM system.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab by specifying the file **bin/Debug/ExtensionUtilityGeometryImporterNet.settings.json**.
3. Restart Ency.
4. In the utilities menu, select the "**Utility to import geometry by C#**" item. A new empty extension should be created, and the user will be presented with a window to select a folder.