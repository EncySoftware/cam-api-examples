# ExtensionUtilityNCMakerNet usage example

This example demonstrates how to create an extension that will generate G code of current toolpath in the CAM system.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab by specifying the file **bin/Debug/ExtensionUtilityNCMakerNet.settings.json**.
3. Restart Ency.
4. In the utilities menu, select the "**Extension on C# to generate G code**" item. A **Notepad** window will open containing the G code of the current toolpath and log information.