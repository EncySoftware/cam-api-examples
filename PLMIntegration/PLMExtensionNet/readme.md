# External System Integration Example

This example demonstrates how to implement an extension to integrate with an external system. It simulates interaction with the external system by saving and loading items — including 3D models, machines, postprocessors, tools, and projects — using local or network folders.

## Features

- Simulates communication with an external system
- Supports saving and loading of various item types
- Utilizes a directory-based approach for data storage

## Installation

To add this extension to the application:

1. Open the **Settings** window on **Extensions** tab in the CAM application.
2. Install the extension using the latest version of the DEXT file from [the Assets section](#https://github.com/EncySoftware/cam-api-examples/releases).
3. Open the **Connection** tab and configure the extension:
   - Required: set a **PLMFolder** parameter — a local or network folder where all items will be stored.

For detailed setup instructions, refer to the [Full Documentation](#https://confluence.encycam.com/display/SC1/.PLMIntegrations+v19).

## Usage

After the extension is installed and properly configured:

- Additional buttons will appear in the relevant windows of the CAM application.
- These buttons enable interaction with the external system using the specified directory structure.

Further information is available in the [Full Documentation](#https://confluence.encycam.com/display/SC1/.PLMIntegrations+v19).