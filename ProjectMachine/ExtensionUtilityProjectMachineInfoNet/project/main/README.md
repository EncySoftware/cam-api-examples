# ExtensionUtilityProjectMachineInfo usage example

This example demonstrates how to create an extension that will show information about the current machine in the CAM system.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionUtilityProjectMachineInfoNet.settings.json**.
3. Restart ENCY.
4. In the utilities list (the **Utilities** button on the top panel or **Ctrl+Shift+E**), select the "**Utility to get project machine info by C#**" item. The information of the current machine will be opened in Windows Notepad.