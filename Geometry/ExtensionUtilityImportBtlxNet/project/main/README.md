# ExtensionUtilityImportBtlx usage example

This example demonstrates how to import the parts of a BIM file into the geometry tree of a project.

The format is [BTLx](https://www.design2machine.com/btlx/index.html) - the XML exchange format of timber construction. A BTLx file lists the parts (beams) of a structure. The utility reads the file and puts every `Part` into the geometry tree as a mesh of a beam `Length` x `Height` x `Width`, all of them in one group named after the file. Then it shows a summary: how many parts were imported and into which group.

The file is parsed with `System.Xml` only, the example has no third-party dependencies.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension in the CAM system settings window, Extensions tab by specifying the file **bin/Debug/ExtensionUtilityImportBtlxNet.settings.json**.
3. Restart ENCY.
4. In the utilities menu, select the "**Import BTLx by C#**" item and pick a `.btlx` file. A small file made for this example lies next to the extension: **bin/Debug/assets/sample.btlx**.

`sample.btlx` holds two beams and is valid against the BTLx 2.1.0 schema. The expected summary is:

```
Parts imported: 2
Geometry group: sample
```

# How it works

| File | What it does |
|---|---|
| `BtlxReader.cs`, `BtlxModel.cs` | Read the parts. Elements are matched by local name, so BTLx 2.0 and 2.1 files are both read. |
| `BeamGeometryImporter.cs` | Places the parts and writes them as meshes through `ISTGeomReceiver` into an `.sgf` file, which `GeomImporter.ImportFile` then loads. |
| `BtlxImporter.cs` | Ties the steps together. |
| `ExtensionImportBtlx.cs` | The utility itself: file dialog, import, summary, errors returned through `TResultStatus`. |

## Parts

A part has its length along X, its height along Y and its width along Z. When the file gives the part a `Transformation`, its `ReferencePoint`, `XVector` and `YVector` are the coordinate system of the part in the project. A part without a transformation is laid into a row along -Y, height up, with a gap of 50 mm.

## Geometry

The corners of every beam are turned into project coordinates and written as a mesh of twelve triangles. The meshes go into a temporary `.sgf` file under one group, and `GeomImporter.ImportFile` loads that file into the geometry tree.

# Limitations

- The machining a BTLx file lists for its parts (`Drilling`, `JackRafterCut`, `Lap` and so on) is not read.
- A part listed with `Count` above one carries one transformation per copy; only the first copy is imported.
- The beams are imported as stock-like meshes, the result of the machining is not modelled.
- All lengths are taken as millimetres.
