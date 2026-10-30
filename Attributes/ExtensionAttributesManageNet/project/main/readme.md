# Attributes usage examples

This set of examples demonstrates how to work with additional attributes (CustomAttributes) of objects in the CAM system. 
Attributes - are user defined additional properties. They can be associated with different CAM-system objects like: application, project, part, operation, tool, etc.
After installing this extension, several items are added to the utilities list of the application and to the context menu of the operation:

- Attributes: create library
- Attributes: show Application attributes
- Attributes: show Project attributes
- Attributes: show Part attributes
- Show Operation attributes
- Show Tool attributes

How to check these examples.

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionAttributesManageNet.settings.json**.
3. In the utilities list (the **Utilities** button on the top panel or **Ctrl+Shift+E**), select the "**Attributes: create library**" item. A new attribute library should be created, and the contents of the library will be opened in Windows Notepad.
4. In the utilities list, select the "**Attributes: show Application attributes**" item. A window with several attributes associated with the global Application object will be displayed.
5. In the utilities list, select the "**Attributes: show Project attributes**" item. A window with several attributes associated with the current CAM system project will be displayed.
6. Create an operation of type **Part**. In the context menu of this operation, select the "**Attributes: show Part attributes**" item. A window with several attributes associated with the current Part object will be displayed.
7. Create any regular machining operation, for example, "**Hole machining operation**". In the context menu of this operation, select the "**Show Operation attributes**" item. A window with several attributes associated with the current operation will be displayed.
8. Create any regular machining operation, for example, "**Hole machining operation**". In the context menu of this operation, select the "**Show Tool attributes**" item. A window with several attributes associated with the tool of the current operation will be displayed.


