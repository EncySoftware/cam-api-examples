# Milling workflow with programmatic stock

This example reproduces the technology of the standard ENCY sample project
`Projects\Milling\3D\Corners cleanup.stcp`, but the **stock and the fixtures are built through the
model former API** instead of being imported or left at their defaults. The extension:

1. sets the machine, strips its optional nodes and loads the three cutting tools of the reference project, both taken from `resources/`;
2. mounts the workpiece on the base table of the machine;
3. imports the `Milling_3D\Forming mould.3dm` part to machine;
4. builds the stock as a box around the part (`ICamApiModelFormerWithBoxPrimitives.AddBoxAroundPart`) and clamps it in a vise taken from the fixtures library (`ICamApiModelFormerWithFixtures.ImportComponentFromFile`);
5. creates the three machining operations and calculates the toolpath;
6. simulates the result and generates G-code.

## Bundled resources

The example **ships the machine and the tool library of the reference project**, so it reproduces the
technology one to one and does not depend on what is installed on the user's machine:

| Resource | Content |
|---|---|
| `resources/machine/Slovtos/` | The "Slovtos" 4-axis milling machine: the `Slovtos.xml` schema plus the `.osd` node geometry it references by name — keep the folder together |
| `resources/tools/CornersCleanupTools.db` | Tool library with the three tools of the reference project |

Both are copied next to the extension DLL on build, and the paths are resolved from the assembly
folder (`Assembly.GetExecutingAssembly().Location`). The machine is assigned with
`ICamApiProject.SetMachine(guid, filePath, typeName)`, which accepts a schema outside the standard
machines folder; the tools are loaded with `OpenExistingLibrary` followed by `AddToolToProject`.

The technology repeats the reference project:

| Operation | Type | Tool |
|---|---|---|
| Roughing waterline | `TSTRoughingWaterlineOp` | Ø14 end mill |
| Scallop finishing | `TSTScallopOp` | Ø20 ball mill, R10 |
| Corners cleanup | `TSTCornerRestMachiningOP` | Ø10 ball mill, R5 |

The standard parameters of the operations (stocks, tolerances, milling type, steps) have no typed API,
so they are set through the operation XML by dotted paths — `ICamApiTechOperation.XMLProp` followed by
`LoadFromXmlProp`.

## Machine setup and mounting

The Slovtos schema ships with a turn table and a tail stock enabled by default, and the reference
project switches both off. The example does the same through the project — `ICamApiMachine.XMLProp`
with the selectors `Schema.AxisY.AxisX.TurnTable.ActiveNode` and `...TailStock.ActiveNode` set to
`Base0` — so the shared schema file itself is never modified. `SetStr` takes effect immediately, there
is no reload call afterwards.

Both the workpiece and the vise are then mounted on the **base table** of the machine rather than on
the default connector. The connector is located by its identifier (`BaseTableWrk`) among
`ICamApiMachine.WorkpieceConnector[...]`, never by a hard-coded index: removing the turn table also
removes its connector and shifts the whole list. For the same reason the lookup runs only after the
machine nodes are stripped.

The workpiece is raised 105 mm above the table — the height of the vise — with
`ICamApiWorkpieceSetup.SetOffset`, exactly as in the reference project. Without it the part sits
directly on the table and the finishing operations drive the tool holder into it. The vise itself is
added at the connector origin, so it is moved under the part with `SetSetupLCS`.

The difference from `FullWorkflow3DProject` is step 3: the workpiece and the fixtures are created
programmatically through the model former, not taken from imported geometry.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab by specifying the file **bin/Debug/WorkpieceMillingWorkflowNet.settings.json**.
3. In the utilities menu, select the "**Milling workflow with programmatic stock**" item. The workflow will run on the active project.
