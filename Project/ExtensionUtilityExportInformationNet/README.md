# Project information export examples

This set of examples demonstrates how to read information about the current project through CAMAPI and export it into a single JSON file.
The export collects project details, machine information, setup stages with parts (geometry, workpiece setup, coordinate systems), operations with their parameters (feeds, spindle, coolants, statistics, stocks), the tools list and project screenshots.
Toolpaths of the operations are exported into separate JSON files, and the resulting project JSON is automatically opened in a bundled web viewer.
After installing this extension, several items are added to the Utilities menu of the application:

- Export project information
- Import exported project information for test
- Import exported toolpath information for test

How to build.

The repository root is the **ExtensionUtilityExportInformationNet** folder. It contains two .NET projects:

- **project/main/ExtensionUtilityExportInformationNet.csproj** — the extension itself (net10.0-windows);
- **viewer/main/ProjectInfoViewer.csproj** — the web viewer (net8.0), published automatically as a part of the extension build.

All CAMAPI references are resolved from the **EncySoftware.CAMAPI.SDK.Net** package, so no locally installed CAM system is needed to build.

There are two equivalent ways to build (both require .NET 10 SDK):

1. Run **commands/build.cmd** (or menu **"Terminal/Run build task"** in VSCode) — it builds via the bundled stbuild system (`.stbuild/build.cmd --Target Compile --Variant Debug`).
2. Run dotnet directly from the repository root:

   ```
   dotnet build "project/main/ExtensionUtilityExportInformationNet.csproj" -c Debug
   ```

In both cases the output goes to **project/main/bin/Debug/**: the extension dll, its **.settings.json**, and the **Viewer** subfolder with **ProjectInfoViewer.exe** (the csproj has a `PublishViewer` target that runs `dotnet publish` for the viewer after every build, so no separate viewer build is needed). If you need to rebuild only the viewer, run from the repository root:

```
dotnet publish "viewer/main/ProjectInfoViewer.csproj" -c Release -o "project/main/bin/Debug/Viewer"
```

How to check these examples.

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab by specifying the file **bin/Debug/ExtensionUtilityExportInformationNet.settings.json**.
3. Open or create a project with a machine, several parts and calculated machining operations.
4. In the utilities menu, select the "**Export project information**" item. The file **test.json** with the full project description will be created in the working directory of the CAM system. Toolpaths of the operations will be saved as separate JSON files in the **project/main/OperationToolpathsJSON** folder (subfolder **Designed** — operations in the project tree order, **Reordered** — in the execution order). If the **Viewer** folder is deployed next to the extension dll, the exported JSON will be opened in the web viewer with the Overview, Operations, Setup, Tools and JSON tabs.
5. In the utilities menu, select the "**Import exported project information for test**" item. The utility reads **test.json** back and imports the part geometry with its matrices into the current project. It is intended for checking that the exported data is complete and correct.
6. In the utilities menu, select the "**Import exported toolpath information for test**" item. The utility parses the JSON files from **project/main/OperationToolpathsJSON/Designed** and creates the toolpath points as a geometry file **toolpath_points.sgf** in the working directory of the CAM system.
