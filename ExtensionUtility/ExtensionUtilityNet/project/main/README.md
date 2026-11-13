# ExtensionUtility usage example

This example demonstrates how to create an extension that will create copy of current project in another folder in the CAM system.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionUtilityNet.settings.json**.
3. Restart ENCY.
4. In the utilities list (the **Utilities** button on the top panel or **Ctrl+Shift+E**), select the "**Example utility extension on C#**" item. A new empty extension should be created, and the user will be presented with a window to select a folder.