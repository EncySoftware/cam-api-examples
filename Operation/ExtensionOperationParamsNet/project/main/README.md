# ExtensionOperationParams usage example

This example demonstrates how to create an extension that creates a new operation with parameters that are read from GetPropIterator C# method in the CAM system.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab by specifying the file **bin/Debug/ExtensionOperationParamsNet.settings.json**.
3. Restart Ency.
4. After that you should see the new "**Operation .NET with params**" operation in the "Spray" section of the New operation window.