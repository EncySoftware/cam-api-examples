# ExtensionEmbeddedInspector usage example

This example demonstrates how to show the ENCY inspector inside the extension's own WPF window, not as a separate dialog. The window edits the size, material and visibility of a stock; the line at the top follows every edit, **OK** keeps the values for the next opening, **Cancel** drops them.

**Reset**, a WPF button of the window, restores the default values. The inspector does not know the extension changed them, so the window calls `Refresh()` on the ENCY main thread: the inspector reads the properties again and keeps its scroll position.

Hints, units, read-only rows and buttons in the rows are shown in the dialog example [ExtensionUtilityDialogWindowNet](../../../ExtensionUtilityDialogWindowNet/project/main/README.md); the embedded inspector shows them the same way.

The example needs ENCY 3.0.21 or newer: older builds have no `Refresh`.

# How it works

The window and the inspector live on different threads:

- the WPF window runs on its own STA thread (`WindowHelper.ShowStaWindow`) — WPF cannot run on the ENCY main thread, it is an MTA thread;
- the inspector is a native ENCY window, so it is created, called and released only on the ENCY main thread. `EncyMainThread` is created in `Run` and moves those calls there. The inspector calls the property getters and setters and the OK / Cancel callback on the ENCY main thread too.

`InspectorHost` is the `HwndHost` that places the inspector into the WPF layout: it creates a container window, asks ENCY for `CreateEmbeddedWindow` inside it and stretches the inspector when the window is resized. Before the window closes, the inspector is released on the ENCY main thread.

Never call the inspector from the WPF thread: every call to it is posted to the ENCY main thread.

ENCY creates a new utility instance for every run, so the open window and the values kept after **OK** are static fields. `"unload_mode": "umBeforeProgramFinalization"` in the settings file keeps the library loaded until ENCY closes; with the default mode the library is unloaded after the window closes and the values are lost.

Older ENCY builds do not commit the row being edited when **OK** is pressed: confirm the value with **Enter** first.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionEmbeddedInspectorNet.settings.json**.
3. Restart ENCY.
4. In the utilities list (the **Utilities** button on the top panel or **Ctrl+Shift+E**), select the "**Utility with the ENCY inspector inside its own window by C#**" item. The "**Stock**" window will appear.
5. Change **Length** and press **Enter**. The line at the top of the window shows the new size.
6. Click **Reset** at the top. The inspector and the line show the default values.
