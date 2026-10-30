# ExtensionToolsListPopupNet usage example

This example demonstrates how to create an extension for the `tools_list_popup` entry point — the extension adds its own items to the popup menu of the project tools list.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionToolsListPopupNet.settings.json**.
3. Restart ENCY.
4. Open the popup menu of the project tools list and select one of the "**Show tools list info**" items. A temporary file will be opened in Windows Notepad with every tool of the current project (caption, type, GUID, ID, number, first corrector number, connector ID, magazine number) and the operations that use it.
