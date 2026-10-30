# ExtensionNCOpenInWebEditor usage example

This example demonstrates how to send a generated NC program to a web editor. It is an NC files exporter (`IExtensionNCFilesExporter`) with one export target, **Open in CNC-Technik KUKA editor**.

When the target is chosen, the extension:

1. takes the main file of the program from the generated NC files: the first `.src` file, or the first file when there is no `.src`;
2. puts the text of that file on the Windows clipboard;
3. opens the [CNC-Technik KUKA editor](https://cnc-technik.de/kuka/) in the default browser;
4. shows a notification in ENCY with the name of the file on the clipboard.

In the editor press **Read from clipboard** to load the program. The editor needs no changes for this.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionNCOpenInWebEditorNet.settings.json**.
3. Restart ENCY.
4. Generate an NC program. ENCY shows the targets of installed NC files exporters by their captions (`GetTargetCaption`). Choose **Open in CNC-Technik KUKA editor**.

# Using another editor

The editor is described in one place, `WebEditorSettings.cs`:

| Constant | Value |
|---|---|
| `Url` | Page opened in the browser |
| `Caption` | Caption of the export target in ENCY |
| `MainFileExtension` | Extension of the file that goes to the clipboard when the postprocessor writes several files |
| `PasteHint` | What the notification tells the user to do in the editor |

Change them and rebuild.

# How it works

| File | What it does |
|---|---|
| `ExtensionOpenInWebEditor.cs` | The exporter: one target, reads the NC file names, runs the steps above, returns errors through `TResultStatus` |
| `MainProgramFile.cs` | Picks the file that goes to the editor |
| `NcFileReader.cs` | Reads the file as UTF-8, or in the Windows ANSI code page when it is not valid UTF-8 |
| `ClipboardText.cs` | Puts the text on the clipboard from a dedicated STA thread |
| `WebEditorSettings.cs` | The editor URL and the texts shown to the user |

## Clipboard and threads

ENCY calls the extension on an MTA thread, and the clipboard of .NET (`System.Windows.Forms.Clipboard`) works only on an STA thread. `ClipboardText` starts its own STA thread, waits for it and throws its error again on the calling thread, where `ExportToTarget` turns it into `TResultStatus`. This is the modal-window pattern of [UI patterns](../../../../docs/general/ui-patterns.md#pattern-2-modal-wpf-or-winforms-window) without a window.

Two details keep the host safe:

- An exception that leaves a thread ends the whole ENCY process. Everything on the STA thread runs inside `try`.
- `Clipboard` is used in a separate method that is not inlined. A host without the Windows Desktop runtime then reports a missing `System.Windows.Forms` as an error of that call. The windowed ENCY (`ency.exe`) has the Windows Desktop runtime, the console ENCY (`SCConsole.exe`) does not.

`SetDataObject(text, copy: true)` leaves the text on the clipboard after the STA thread ends.

# Limitations

- **A KUKA program is two files, `.src` and `.dat`, and only one of them goes to the clipboard.** The extension sends the `.src` file. The declarations of the `.dat` file do not reach the editor, and the notification lists the files that were not sent.
- When the postprocessor writes several `.src` files, only the first one is sent.
- Changes made in the editor do not come back to ENCY.
- The text goes through the clipboard, so it replaces whatever the user had copied before.
