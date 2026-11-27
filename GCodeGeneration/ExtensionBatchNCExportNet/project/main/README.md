# Batch NC generation and PLM export

This standalone utility generates a separate NC program for each calculated operation
or operation group in the current project and passes each result to an installed
`IExtensionNCFilesExporter`. It does not implement a PLM client.

## Build and installation

Requires .NET 8 SDK or later and `EncySoftware.CAMAPI.Sdk.Net` version
`2.0.24-dev.5`, as used by the other examples in this repository. Configure the
package source that supplies this SDK version in your local NuGet configuration.
The SDK must provide `SaveCLDataInfo`, `GenerateNC` and `IExtensionNCFilesExporter`.

From the example folder, run `commands/build.cmd` (Debug) or `commands/pack.cmd`
(Release and DEXT). Alternatively, from the repository root:

```powershell
dotnet build GCodeGeneration/ExtensionBatchNCExportNet/project/main/ExtensionBatchNCExportNet.csproj
```

Register `project/main/bin/Debug/ExtensionBatchNCExportNet.settings.json` in the
application extension settings and restart the application. The DEXT package is
created in `project/main/bin/Release`.

## Usage

1. Open a project from PLM. For a local project, the utility displays a warning
   without opening its settings window.
2. Choose **Batch NC generation and export** in the utilities menu.
3. Choose an installed exporter under **Export to**. A single target is selected
   automatically; otherwise the last selection for this project is restored.
   If no targets are available, the window explains the problem and disables export.
4. Choose **By operation** or **By operation group**. The entire project is processed,
   independently of the currently selected operation.
5. Find a postprocessor by name or subfolder. The standard folder is obtained from
   the application API. Use **Browse…** for a different location.
6. Click **Generate and export**. The **NC output folder** is selected automatically;
   use **Change folder…** only when a different location is needed.

Only enabled, calculated operations without errors are included. Group mode expands
structural project, setup and part nodes (`TSTRoot*`, `TSTStage*`, `TSTPart*`). Each
outer user group is one batch including its nested groups. Operations outside user
groups form individual batches. Empty groups and excluded operations are skipped.
Traversal uses design order (`rmDesigned`). The utility does not calculate toolpaths.

The selected exporter determines the PLM destination using the current project.
The utility does not read internal application configuration or switch connections.
PLM connection settings alone are insufficient: an NC exporter must be installed.
Technical extension identifiers do not need to be entered manually.

## Postprocessor settings

Both `.sppx` and `.dll` are supported. For DLL postprocessors, the utility looks for
`Settings.xml` beside the DLL or up to two directories above it (the standard
`bin/debug` layout). Otherwise it reads the embedded settings resource without
loading the assembly. **DLL settings…** selects another XML file.

The required output parameter is:
`Settings/UserDefinedParameters/Group[@ID="OutFiles"]/Parameter[@ID="NCFileName"]`.
Each batch receives its own settings copy with an absolute output path. Original
XML and DLL files are not modified. Settings from the standard generation window
are not imported. Manually selected XML files, including an empty path for embedded
settings, are preserved when filtering or switching postprocessors.

## Results and failures

Default output location: `%TEMP%/BatchNCExport/Project_<project name>/NC_<GUID>`.
Each batch uses a numbered subfolder, avoiding collisions between equally named
operations. The primary NC filename is `program.nc`; all additional files returned
by `NCFileNames` are passed to the exporter as part of the generation result.

One compact modal result is shown after the batch run, with buttons to open the
output folder, open `export.log`, or close. Errors stop subsequent batches; completed
exports are not rolled back. Exporter messages are recorded in the log, and exporters
may display their own notifications. The last result remains accessible on the next
run. Files are retained for inspection.

Mode, target, postprocessor and last result are stored per project in
`%LOCALAPPDATA%/BatchNCExport/postprocessors.json`, with up to 12 recent postprocessors.
Cancelling the settings window does not save preferences or create a run folder.

## API flow and verification

For each batch: `SaveCLDataInfo` → `GenerateNC` → `ExportToTarget`. Operation IDs
remain attached to each generation result for the PLM integration to use.

Verify a PLM project containing multiple setups, outer and nested groups, duplicate
operation names, and uncalculated/disabled operations. Check that only eligible
operations are exported, grouping is correct, and changing the tree selection does
not change the batch contents. Verify the resulting file links in PLM. A successful
build does not verify the installed application, postprocessor or PLM server.

## Local validation limitation

The offline build could not restore version `2.0.24-dev.5` (NU1102). The locally
available SDK and installed application assemblies do not expose the required new
NC generation interfaces. Restore the matching SDK before building and testing
this example; do not substitute the SDK from a different product repository.
