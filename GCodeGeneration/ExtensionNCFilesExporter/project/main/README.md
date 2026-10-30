# ExtensionNCFilesExporter usage example

This example demonstrates how to add export targets for generated NC programs. It is an NC files exporter (`IExtensionNCFilesExporter`) with three demo targets:

- **Export to PLM (demo)**
- **Export to Cloud (demo)**
- **Export to External system (demo)**

Nothing is sent anywhere. Whatever target is chosen, the extension writes what ENCY passed to it (`ICamApiNCGenerationResults`) into a temporary text file and opens it in Windows Notepad:

- the caption of the chosen target;
- the postprocessor file;
- the generated NC files;
- the messages of the postprocessor;
- the operations the program was generated from: name, type, parent operation id and operation id.

Use it as a starting point for a real exporter: replace the body of `ExportToTarget` with the upload to your system.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionNCFilesExporter.settings.json**.
3. Restart ENCY.
4. Generate an NC program. ENCY shows the targets of installed NC files exporters by their captions (`GetTargetCaption`). Choose one of the three targets above, and the report opens in Notepad.

A complete exporter built the same way is [ExtensionNCOpenInWebEditorNet](../../../ExtensionNCOpenInWebEditorNet/project/main/README.md): it sends the program to a web editor.
