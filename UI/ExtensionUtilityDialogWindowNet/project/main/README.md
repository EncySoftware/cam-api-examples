# ExtensionUtilityDialogWindow usage example

This example demonstrates how to create an extension that will show dialog window in the CAM system. On **OK** the values are written to a text file that opens in Notepad.

The rows also show what the inspector can do with a row:

- a hint, shown when the mouse rests on the row: **String value**, both enums, **Depth** and **Output folder**;
- units after the value: **Depth** in **mm**;
- a read-only row: **Output folder**;
- a button in the row: **Round** in the **Depth** row rounds the depth to whole millimetres. The inspector reads the properties again after the click, so the action only changes the value.

The example needs ENCY 3.0.21 or newer.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionUtilityDialogWindowNet.settings.json**.
3. Restart ENCY.
4. In the utilities list (the **Utilities** button on the top panel or **Ctrl+Shift+E**), select the "**Utility to show dialog window by C#**" item. The dialog window will be opened in the CAM system.
5. Rest the mouse on **Depth**: its hint appears. Type **7.4**, press **Enter**, click **Round**: the depth becomes **7**.
6. Press **OK**. Notepad shows the values, the depth among them.
