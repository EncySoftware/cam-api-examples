# ExtensionUtilityProjectToolsList usage example

This example demonstrates how to create an extension that will show information about the current project`s tools in the CAM system.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionUtilityProjectToolsListNet.settings.json**.
3. Restart ENCY.
4. Go to the Machining Tab, in the tools panel (at the top of the screen in the middle) find Machining Report button, hover it, select the "**Show project tools list**" item. The information of the current project tools will be opened in Windows Notepad.