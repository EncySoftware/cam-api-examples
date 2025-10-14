# ExtensionUtilityGeometryPickerNet usage example

This example demonstrates how to create an extension that will pick the selected objects of the geometric model in the CAM system.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab by specifying the file **bin/Debug/ExtensionUtilityGeometryPickerNet.settings.json**.
3. Restart ENCY.
4. In the utilities menu, select the "**Utility to show selected geometry model objects by C#**" item. A window **"Geometry items model picker"** will appear. You must select the geometric model objects. After clicking the **OK** button, Windows Notepad will appear with a list of the selected objects.