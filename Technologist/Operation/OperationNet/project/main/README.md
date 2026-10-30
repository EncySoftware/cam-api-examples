# ExtensionOperationsNet usage example

This example demonstrates how to create an extension that will create setup stage / part / any operation of the user's choice in the CAM system.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionOperationsNet.settings.json**.
3. Restart ENCY.
4. In the utilities list (the **Utilities** button on the top panel or **Ctrl+Shift+E**), select the "**Operation: create setup stage**" item. The new setup stage will appear in the operations list.
5. In the utilities list, select the "**Operation: create part**" item. The new part will appear in the operations list.
6. In the utilities list, select the "**Operation: create operation**" item. A window will open with a list of all operations. After selecting an operation, it will appear in the list of operations.