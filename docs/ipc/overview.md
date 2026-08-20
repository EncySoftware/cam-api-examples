# ENCY CAM IPC — Standalone Application Development Overview

## What is CAMIPC?
CAMIPC is the inter-process API for standalone .NET/C++ applications that communicate with a running ENCY instance via IPC (sockets + JSON).

## Quick Start
1. Clone ENCY connection → [ipc/connection.md](connection.md)
2. Get ICamIpcApplication → access project, technologist, geometry
3. All calls use TExecuteContext for error checking

## How CAMAPI vs CAMIPC differ
| Aspect | CAMAPI (plugin) | CAMIPC (standalone) |
|---|---|---|
| Process | Same process as ENCY | Separate process |
| Loading | ENCY loads your DLL | You load CAMIPC.Helper.Cam.dll |
| Entry | ExtensionFactory.Create() | CreateHelper() → GetRunningCamAppList() |
| Threading | COM MTA (ENCY's thread) | Your thread, use TExecuteContext |
| Interface prefix | ICamApi* | ICamIpc* |

## Documentation Map
| Topic | File |
|---|---|
| Connecting to ENCY | [ipc/connection.md](connection.md) |
| Project, Technologist, Operations | [ipc/project.md](project.md) |
| Geometry | [ipc/geometry.md](geometry.md) |
| Feature finder | [ipc/feature-finder.md](feature-finder.md) |
| Tools & Machine | [ipc/tools-machine.md](tools-machine.md) |
| NC Generation & Simulation | [ipc/nc-simulation.md](nc-simulation.md) |
| Application, Logger, XmlProp | [ipc/application.md](application.md) |
| UI (viewport, Prime) | [ipc/ui.md](ui.md) |
| Clouds, Tests, PLM | [ipc/extra.md](extra.md) |
| Macros — record & save over IPC | [ipc/macros.md](macros.md) |
| COM lifetime (.NET) | [general/com-lifetime.md](../general/com-lifetime.md) |
| Error handling | [general/error-handling.md](../general/error-handling.md) |
