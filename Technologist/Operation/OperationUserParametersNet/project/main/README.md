# Operation user parameters example

This example demonstrates how to read and edit the **user parameters** of the currently
selected technology operation through the CAMAPI .NET helpers. User parameters are the
name/value/comment triples stored in the operation's header/tail template (all values are strings).

The library registers a single utility that opens a window (a non-modal WPF window kept alive via
`IExtensionLazyUnloadable`) with three buttons. Each button re-navigates to whatever operation is
currently selected, so you can keep the window open and switch operations in the tree:

- **Get** — reads the whole list of the current operation and shows every entry
  (index, name, value, comment). If the operation has no user parameters (a group, the root
  operation, or an operation type without a toolpath template), the window explains that.
- **Add** — adds a parameter with the fixed name `ApiExampleParam`. If a parameter with
  that name already exists, its value and comment are updated instead of creating a duplicate.
- **Delete** — looks the `ApiExampleParam` parameter up with `FindByName` and removes it
  with `Delete`.

## API used

- `ICamApiTechOperation.GetUserParameters` (helper `operationCom.GetUserParameters()`) — returns the
  live `ICamApiUserParametersList`; the returned wrapper `IsNull` for operations without an MCD template.
- `ICamApiUserParametersList` (helpers `Count`, `GetItem`, `FindByName`, `IndexOf`, `Add`, `Delete`).
- `ICamApiUserParameter` (helpers `Name`, `Value`/`SetValue`, `Comment`/`SetComment`).

> **Note.** A parameter `Name` may be a postprocessor macro expression (for example
> `'P' + Str([$ParentNode.Attribute(ItemIndex)])`) that ENCY evaluates per node when generating the
> NC program. The list has no single "expanded" value; the human-readable label is the `Comment`.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab, by specifying the file
   **bin/Debug/OperationUserParametersNet.settings.json**.
3. Restart ENCY.
4. Run the **Operation user params** utility to open the window, select an operation in the technology
   tree, then use the **Get**, **Add** and **Delete** buttons.
