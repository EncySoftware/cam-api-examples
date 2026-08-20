# ENCY CAM API — Plugin Development Overview

## What is CAMAPI?
CAMAPI is the in-process plugin API. Your plugin (DLL) is loaded by ENCY and called at specific injection points. Plugins can be written in .NET, C++, or Delphi.

## Quick Start
1. Choose your entry point → [Extension Entry Points](../general/extension-entry-points.md)
2. Implement `CAMAPI.ExtensionFactory` class
3. Register your library via JSON config

## Documentation Map
| Topic | File |
|---|---|
| Choosing injection point | [general/extension-entry-points.md](../general/extension-entry-points.md) |
| Extension system (IExtensionManager) | [api/entry-points.md](entry-points.md) |
| Project, Technologist, Operations | [api/project.md](project.md) |
| Geometry (model, import, B-rep, sketcher) | [api/geometry.md](geometry.md) |
| Feature recognition (holes, pockets, …) | [api/feature-finder.md](feature-finder.md) |
| Tools & Machine | [api/tools-machine.md](tools-machine.md) |
| NC Generation & Simulation | [api/nc-simulation.md](nc-simulation.md) |
| Probing Operations (measuring cycles) | [api/probing.md](probing.md) |
| Application, Events, Logger | [api/application.md](application.md) |
| UI (dialogs, viewport) | [api/ui.md](ui.md) |
| COM lifetime (.NET) | [general/com-lifetime.md](../general/com-lifetime.md) |
| Error handling | [general/error-handling.md](../general/error-handling.md) |
| UI patterns (.NET) | [general/ui-patterns.md](../general/ui-patterns.md) |

## Key Concepts
- All COM objects must be wrapped in `ComWrapper<T>` with `using` — see [com-lifetime.md](../general/com-lifetime.md)
- Never throw exceptions across COM boundary — use `TResultStatus` — see [error-handling.md](../general/error-handling.md)
- Entry point determines what context you receive — see [extension-entry-points.md](../general/extension-entry-points.md)
