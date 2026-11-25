# LibraryToolForOperation usage example

This example demonstrates an operation popup extension that takes a tool from the component libraries (kits) and installs it as the tool of the operation through the tool assemblies API of CAMAPI: it finds mills of a diameter in the kit of the machine and in the enabled libraries, reads their numeric parameters and overhang, builds an assembly for the first mill and makes it the tool of the operation.

The example needs an ENCY build with the tool assemblies API and uses SDK `EncySoftware.CAMAPI.Sdk.Net` 3.0.23-dev.11, the first version with `GetParameters`, `GetParameter` and `GetOverhang` of `ICamApiToolComponent`.

# How it works

`ExtensionLibraryToolForOperation.Build` adds the item **Install a library mill D10** to the popup menu of an operation. `InstallLibraryToolOnClicked` runs the scenario on the selected operation and shows the report in a message box. The scenario is in `LibraryToolForOperation`:

1. `projectCom.MachineTools()` gives the tools installed in the magazines of the machine. A project in the classic tools mode has none: the wrapper is null and the example stops with a message.
2. `operationCom.AsWithToolAssembly()` gives the tool assembly of the operation, `ICamApiTechOperationWithToolAssembly`.
3. `machineToolsCom.FindTools(diameterMin: 9.9, diameterMax: 10.1)` finds the tools D10 ±0.1 mm in the kit of the machine and in the enabled libraries.
4. For every found tool the report has `ComponentCaption`, `LibraryPath`, `ComponentOverhang` and every key of `ComponentParameters` with its value from `ComponentParameter`.
5. The first tool with `ComponentKind() == tckMillTool` goes to `operationToolCom.InstallTool(millCom, allowUnknownAdapter: true)`. It builds an assembly for the mill and puts it in place of the assembly of the operation, as **Replace** in the assembly window; an operation without a tool gets the assembly in the first free position.
6. The report shows the returned node and `ToolNodeID` of the operation before and after.

Rules the example follows:

- Every COM object is wrapped and disposed: the context properties with `ComWrapper.Create`, the results of the helpers with `using var`. The list from `FindTools` holds a wrapper per tool; the example disposes all of them in `finally`.
- `GetParameters` returns the same keys as the MachineKit file API (`Diameter`, `Length`, `WorkingLength`, `TeethCount`, ...). Values are those the kit stores, units are not converted. An unknown key ends with `rsError`; `ComponentParameter` throws then.
- `GetOverhang` is the own overhang of the mill as the simulation takes it; 0 for a component that is not a mill.
- `allowUnknownAdapter: true` installs the mill through an unknown adapter when the libraries have no chain of adapters from the mill to the spindle. Mills of the standard libraries often have none, and the tool window installs them the same way.
- `InstallTool` replaces the tool of the operation: the other operations of the old tool move to the new one too. The correctors of the operation go to Auto.
- Tool components are read on the main thread of ENCY only. The popup item runs there; a call from another thread ends with `rsError`.
- The helpers throw when ENCY returns an error; the click handler writes the message into the report. An exception never leaves a method that ENCY calls.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/LibraryToolForOperationNet.settings.json**.
3. Restart ENCY.
4. Create a project in the tool assemblies mode on a machine with a kit and enable a library with mills D10 in the library window, for example **EndMills_D1-20.mKit**.
5. Add an operation that needs a mill, for example **Pocketing**, open its popup menu (right-click on the operation name) and select **Install a library mill D10**. A message box lists the found tools with their parameters and shows the tool node of the operation; the assembly window shows the new assembly.
