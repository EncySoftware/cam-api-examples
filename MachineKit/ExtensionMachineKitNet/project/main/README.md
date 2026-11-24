# ExtensionMachineKit usage example

This example demonstrates a utility extension that works with a MachineKit file (`.mKit`) through the MachineKit file API of CAMAPI: it creates the file, adds a connection and components to it, changes their parameters, saves the file and reads it back. The file opens in the tool library of ENCY.

The example needs an ENCY build with the MachineKit file API and uses SDK `EncySoftware.CAMAPI.Sdk.Net` 3.0.23-dev.8.

# How it works

`ExtensionMachineKit.Run` creates `%TEMP%\ExtensionMachineKitNet\Example.mKit` and shows the report in a message box. The scenario is in `MachineKitExample`:

1. `applicationCom.GetMachineKitManager()` gives the manager of `.mKit` files; `GetSupportedComponentTypes` and `GetSupportedConnectionTypes` list what it can create.
2. `manager.CreateKit(path)` opens a new empty kit in memory. The folder of the path must exist and the file must not, so the example deletes the file of the previous run first. Nothing is written until `Save`.
3. `CreateConnection(TMachineKitConnectionType.mktCylinder, "Shank D32")` creates a connection; `SetParameter` sets its `Diameter` to 32 and `SetConnectionCode` its code.
4. `CreateComponent(TMachineKitComponentType.mkctSleeve, "Sleeve D32")` creates a sleeve with `BarLength` 50, `TotalLength` 80, `FlangeDiameter` 40, `Diameter` 32 and `HoleDiameter` 20. `SetPlug` connects its plug to the shank, `SetSocket` connects its first socket from `GetSockets`.
5. `CreateComponent(TMachineKitComponentType.mkctConicalMill, ...)` creates a conical mill with `Length` 60, `ShoulderLength` 30, `WorkingLength` 20, `Diameter` 12 and `Angle` 10.
6. `TryCreateComponentWithModel(TMachineKitComponentType.mkctCustomMillTool, ..., runRecognition: true, ...)` imports `Models\tool.stp` as a custom mill and recognizes its cutting part.
7. `kit.Save()` writes the file, `kit.Close()` ends the session.
8. `manager.OpenKit(path)` opens the file read-only; the example writes every connection and component with `GetName`, `GetConnectionType` / `GetComponentType`, `GetConnectionCode`, `GetToolSubtype`, `GetPlug`, `GetSockets` with `GetSocketConnections`, and the values of `GetParameters` read by `GetParameter`.

Rules the example follows:

- Every wrapper the helpers return — the manager and each kit — is disposed with `using var`. `Close` ends the session without saving; disposing the wrapper does not save either.
- Components and connections are addressed by their local IDs, GUID strings that stay the same after `Save` and `OpenKit`.
- Parameter keys are stable names, not captions of the inspector. Lengths are in mm, angles in degrees. Each `SetParameter` applies the constraints of the component at once, so the order of the calls matters: the sleeve gets its lengths before its diameters.
- The helpers throw `InvalidOperationException` when ENCY returns an error; the example writes the message into the report. Any other exception goes to `resultStatus` as `rsError`: an exception must never leave a method that ENCY calls.
- `TryCreateComponentWithModel` returns `false` and the status instead of throwing. Its recognition status is separate: the call succeeds with `mrsPartial` or `mrsNotRecognized` too.
- A kit is not added to the tool library or to the project automatically; there is no `SaveAs`.

`commands/pack.cmd` puts `Models\tool.stp` into the `.dext` next to the library.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionMachineKitNet.settings.json**.
3. Restart ENCY.
4. In the utilities list (the **Utilities** button on the top panel or **Ctrl+Shift+E**), select "**Create a MachineKit file (C#)**". A message box shows the supported types, the result of the model import and the content of the saved file.
5. Open `%TEMP%\ExtensionMachineKitNet\Example.mKit` in the tool library: it has the connection **Shank D32** and the components **Sleeve D32**, **Conical mill D12** and **Custom mill from STEP**.
