# AddinImportSvgNet usage example

This example demonstrates how to create addin which will allow you to import files in the **.SVG** format via the **Import** button in the ENCY.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Start ENCY with or without debugging.
3. In the **Utilities** menu select **Addin Manager**, click "Add". And then you should fill the Addin form: the name of the Addin, path to .exe file (**bin\Debug\net8.0-windows\AddinImportSvgNet.exe**), input extensions (**SVG**), output extensions (**SGF**).
4. Restart ENCY.