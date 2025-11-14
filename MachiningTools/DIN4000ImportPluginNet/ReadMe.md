# Machining tools import API

An example of cutting tools import from DIN4000 csv format.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Start ENCY.
3. Run this project with **./DIN4000ImportPluginNet/dotnet run** command.
4. After this, a file **"DIN4000ImportPlugin_result.db"** will appear in the path **./out**.
4. To load and use these tools, in the **Machining** tab you need to open the **Operation parameters** setting window or the **Tool** window, click the **Open Library** button and select the **DIN4000ImportPlugin_result.db** file.

For more information about ENCY cutting tools, visit:
- [Machining tool features](https://docs.encycam.com/ENCY/2/en/10332.html)