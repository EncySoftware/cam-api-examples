# ExtensionOperationFlipToolNet usage example

This example demonstrates how to create an extension that allowing you to set the position of the tool and the parameters of the machine axes in the CAM system.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionOperationFlipToolNet.settings.json**.
3. Restart ENCY.
4. In the utilities list (the **Utilities** button on the top panel or **Ctrl+Shift+E**), select the "**Tool: set the final position of the tool**" item. A window will open where you need to enter the parameters of all available axes of the machine, after which the end point of the machine will be calculated
5. In the utilities list, select the "**Tool: calculate machine`s axis parameters**" item. A window will open where you need to select the direction of the normal and flips, and after that the axis parameters will be calculated.
