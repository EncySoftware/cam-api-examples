# ExtensionOperationToolNet usage example

This example demonstrates how to create an extensions that will show toollist of project and change current tool in the CAM system.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab by specifying the file **bin/Debug/ExtensionOperationToolNet.settings.json**.
3. Restart Ency.
4. In the utilities menu, select the "**Operation: show tool list**" item. A window showing the current project's tools will appear..
5. In the utilities menu, select the "**Operation: change current tool**" item. The current tool will be replaced.