# ExtensionOperationPopupNet usage example

This example demonstrates how to create an extension that will go to the entry point when opening the **Operation pop-up** menu in the CAM system.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab by specifying the file **bin/Debug/ExtensionOperationPopupNet.settings.json**.
3. Restart Ency.
4. Add a new operation or load an existing project with operation, open the **Operation pop-up menu** of the added operation (right-click on the operation name) and you will get to the entry point.