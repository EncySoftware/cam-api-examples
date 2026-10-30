# ExtensionOperationToolPopupNet usage example

This example demonstrates how to create an extension for the `operation_popup` entry point — the extension adds an item to the context menu of an operation that shows the tools list of the project and adds a tool from a tool library to the project.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionOperationToolPopupNet.settings.json**.
3. Restart ENCY.
4. In the technology tree, right-click any operation and select the "**Select tool for operation**" item. The "**List of tools**" window with the tools of the current project will appear.
5. In the "**Add tool from library**" section, enter the name of a tool library from the **Tools/Examples** folder of the CAM libraries (or pick it with the "**...**" button) and the tool id, then click "**Add tool**". The tool will be added to the project, and the list will be updated.