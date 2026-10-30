# ExtensionUtilityLCSCreatorNet usage example

This example demonstrates how to create an extension that will create a coordinate system in the CAM system.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionUtilityLCSCreatorNet.settings.json**.
3. Restart ENCY.
4. In the utilities list (the **Utilities** button on the top panel or **Ctrl+Shift+E**), select the "**Utility to create new LCS by C#**" item. A window **"Define coordinate system"** will appear. You must select the parent coordinate system & parameters of new coordinate system. After clicking the **OK** button, the new coordinate system will created.