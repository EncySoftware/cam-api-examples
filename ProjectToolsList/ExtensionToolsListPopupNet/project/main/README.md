# ExtensionUtilityPostprocessorNet usage example

This example demonstrates how to create an extension for the `postprocessor_popup` entry point — the extension adds its own items to the drop-down menu of the **Postprocessor** button in the CAM main toolbar.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab by specifying the file **bin/Debug/ExtensionUtilityPostprocessorNet.settings.json**.
3. Restart ENCY.
4. Click the drop-down arrow next to the Postprocessor button on the main toolbar — select one of the "**Show postprocessor info**" items. A temporary notepad file with current project info will be opened.
