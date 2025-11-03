# ExtensionOperationsNet usage example

This example demonstrates how to create an extension that will create setup stage / part / any operation of the user's choice in the CAM system.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab by specifying the file **bin/Debug/ExtensionOperationsNet.settings.json**.
3. Restart Ency.
4. In the utilities menu, select the "**Create a new setup stage**" item. The new setup stage will appear in the operations list.
5. In the utilities menu, select the "**Create a new part**" item. The new part will appear in the operations list.
4. In the utilities menu, select the "**Create a new operation**" item. A window will open with a list of all operations. After selecting an operation, it will appear in the list of operations.