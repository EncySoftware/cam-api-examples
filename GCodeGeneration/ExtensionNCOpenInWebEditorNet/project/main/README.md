# ExtensionNCOpenInWebEditor usage example

This example demonstrates how to send a generated NC program to a web editor. It is an NC files exporter (`IExtensionNCFilesExporter`) with one export target, **Open in CNC-Technik KUKA editor**.

When the target is chosen, the extension:

1. picks the program files from the generated NC files (see [Which files are sent](#which-files-are-sent));
2. compresses each file and puts it into the editor link;
3. opens the link of the [CNC-Technik KUKA editor](https://cnc-technik.de/kuka/) in the default browser;
4. shows a notification in ENCY with the files that were sent and the files that were not.

The editor opens each file as `<name>.<extension>` and makes the `.src` file active.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionNCOpenInWebEditorNet.settings.json**.
3. Restart ENCY.
4. Generate an NC program. ENCY shows the targets of installed NC files exporters by their captions (`GetTargetCaption`). Choose **Open in CNC-Technik KUKA editor**.

# Which files are sent

- The main file is the first `.src` file, otherwise the first file with any of the extensions `src`, `dat`, `sub`, `mod`, `prg`, `sys`. The first three are KUKA files, the last three are ABB files.
- With the main file go the other generated files that have the same name and one of these extensions, one file per extension. A KUKA program goes as its `.src` and `.dat` files.
- When no generated file has one of these extensions, the export ends with an error that lists the generated files.

# The editor link

```
https://cnc-technik.de/kuka/#name=PROG1&src=<data>&dat=<data>&z=1
```

| Parameter | Value |
|---|---|
| `name` | Program name: the main file name without extension, escaped with `Uri.EscapeDataString`. The editor opens each file as `<name>.<key>` |
| `src`, `dat`, `sub`, `mod`, `prg`, `sys` | Content of the file with this extension |
| `z=1` | The content is compressed |

Each file goes as the bytes the postprocessor wrote, compressed with raw deflate (`DeflateStream`) and encoded in base64url without padding. The extension does not decode the text: the editor detects UTF-8 and Windows-1252 itself.

The program stays on the user's computer. Everything after `#` is the fragment of the link, and the browser does not send it to the server. The editor reads the program from the fragment and then clears the address bar.

## Long programs

A link passed to `Process.Start` may be about 32 000 characters long. A link longer than `MaxDirectLinkLength` is written into a small HTML page in the temp folder (`%TEMP%\ExtensionNCOpenInWebEditorNet\<name>.html`), and the browser opens this page. The page calls `location.replace` with the link, so the length of the program is limited only by the browser. The URL goes into the script as a JSON string (`JsonSerializer.Serialize`).

# Using another editor

The editor is described in one place, `WebEditorSettings.cs`:

| Constant | Value |
|---|---|
| `Url` | Editor page. The program is added to it after `#` |
| `Caption` | Caption of the export target in ENCY |
| `MainFileKey` | Extension of the main file, picked first when the postprocessor writes several files |
| `FileKeys` | Extensions the editor accepts. Each one is also the name of the link parameter that carries the file |
| `MaxDirectLinkLength` | Longest link passed to the browser directly |
| `RedirectFolder` | Folder in the temp folder for the redirect pages |

Change them and rebuild.

# How it works

| File | What it does |
|---|---|
| `ExtensionOpenInWebEditor.cs` | The exporter: one target, reads the NC file names, runs the steps above, returns errors through `TResultStatus` |
| `ProgramFiles.cs` | Picks the files that go to the editor and the ones that do not |
| `EditorLink.cs` | Builds the link from the file contents. A pure function without file or network access |
| `BrowserTarget.cs` | Chooses between the link and a redirect page, writes the page |
| `WebEditorSettings.cs` | The editor URL, the accepted extensions and the length limit |

# Limitations

- When the postprocessor writes several programs, only the files with the name of the main file are sent.
- Changes made in the editor do not come back to ENCY.
- Redirect pages stay in the temp folder. Each export of a program with the same name overwrites its page.
