# ExtensionPostprocessorPopupNet usage example

This example demonstrates how to create an extension for the `postprocessor_popup` entry point — the extension adds its own items to the drop-down menu of the **Postprocessor** button in the CAM main toolbar.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionPostprocessorPopupNet.settings.json**.
3. Restart ENCY.
4. Click the drop-down arrow next to the Postprocessor button on the main toolbar — select one of the "**Show postprocessor info**" items. The G code of all project operations will be generated into the temporary folder with the Sinumerik (840D), Heidenhain (iTNC530) or Fanuc (30i) milling postprocessor, one per item, and opened in Windows Notepad.
