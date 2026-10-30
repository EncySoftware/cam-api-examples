# ExtensionManageView usage example

This example demonstrates how to create an extension that will control the main 3D view of the CAM system through the view cube.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionManageViewNet.settings.json**.
3. Restart ENCY.
4. In the utilities list (the **Utilities** button on the top panel or **Ctrl+Shift+E**), select the "**Utility to manage main view window by C#**" item. The "**View Control**" window will appear.
5. Click any button on the **Faces**, **Edges** or **Corners** tab. The main view will zoom to fit the model and turn to the selected face, edge or corner of the view cube.