# Geometry interfaces between C++ and C#

This standalone example shows how a C# application calls a C++ geometry library through COM interfaces generated from one IDL file. It does not load a CAM application. `GetGeomLibrary` returns a process-wide `IGeomLibrary` object; `CreateGeomCalculator` creates an `IGeomCalculator` for the caller. The calculator performs vector, point, and affine-matrix operations.

## Start here

Install .NET 10 SDK and Visual Studio 2022 with the C++ toolset. STBuild comes from the build system package referenced by the containing repository. From Windows Explorer or a terminal, use:

| Command | Result |
|---|---|
| `commands\build.cmd` | Generate the IDL projections and build the C++ DLL and C# client. |
| `commands\run.cmd` | Build, then run the C# client. |

Both command files call the local STBuild project in `.stbuild/build/build.cs`. They preserve the build exit code. The same targets can be called directly from the example directory:

```powershell
dotnet run --project .stbuild/build/stbuild.csproj -- --root .stbuild --target Compile --variant Release
dotnet run --project .stbuild/build/stbuild.csproj -- --root .stbuild --target Run --variant Release
```

If Visual Studio is installed without `vswhere`, set `MSBUILD_EXE_PATH` to its `MSBuild.exe`. The Delphi builder is intentionally disabled in `build.cs`: this example generates only the C++ header and .NET interop assembly. Generated output goes to `Generated`; it can be recreated from the IDL. No CAM runtime or Delphi compiler is required.

## IDL features illustrated

`Idl/main/src/DemoMath.idl` is the shared contract:

| IDL construct | Example |
|---|---|
| `record` and `field` | `TPoint3D`, `TVector3D`, and `TMatrix3D`, whose fields include other records. |
| `double` | Coordinates, distance, vector length. |
| `string` | Library and calculator names and formatted point text. |
| `propertyR` / `propertyRW` | Read-only library name and editable calculator name. |
| Interface return | `IGeomLibrary.CreateGeomCalculator` returns `IGeomCalculator*`. |
| `out` parameters | `Normalize` returns both unit vector and original length. |
| Struct return | `Translate` and `Transform` return `TPoint3D`. |

`TMatrix3D` stores its basis vectors in `AxisX`, `AxisY`, and `AxisZ`; `Origin` is the translation. `Transform` computes `Origin + AxisX*x + AxisY*y + AxisZ*z`. `Normalize` reports `E_INVALIDARG` for a zero vector or nonfinite inputs; invalid floating-point inputs are rejected by the other operations as well. COM errors appear as exceptions in C#.

## Build graph and ownership

`.stbuild/buildspace.json` lists the IDL, C++, and C# projects. `build.cs` reads those project descriptions and runs MIDL, TlbImp, MSBuild, and `dotnet build` in that order. The C++ DLL exports `GetGeomLibrary` without COM class registration. The library object keeps a module-owned reference and gives each caller a new reference. Each calculator has its own reference count. The C# client releases its factory reference and COM wrappers.

Expected output from `commands\run.cmd`:

```text
Library: Geometry library
Calculator: Robot path geometry
Translated: (5.000, 1.000, 5.000)
Distance: 4.583
Normalized: (0.600, 0.800, 0.000); length 5.000
Transformed: (8.000, 21.000, 33.000)
```
