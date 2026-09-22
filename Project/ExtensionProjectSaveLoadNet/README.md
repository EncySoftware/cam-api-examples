# Project order card

This example demonstrates saving and loading an extension's own data inside a
project. The order number, customer and notes are stored in a JSON document,
and a binary payload is stored beside it.

The manifest is saved with `Purpose=manifest`, which the extension chooses as its
unique lookup key. It contains the actual snapshot file names of the JSON and
binary payload. Both auxiliary files use an empty `Purpose` and are loaded directly
by the names recorded in the manifest. Purpose values may be repeated or empty;
snapshot file names remain unique.

All files are written with `TExtensionFileCompression.efcNormal`. Compression is
useful for data that is read and written sequentially; compressed streams do not
support arbitrary seeking or resizing. Use `efcNone` when random access is required.

The example keeps the current project's card in one process-wide `OrderCardStore`.
The project serializer updates this store during initialization, deserialization,
saving and finalization. The edit utility reads the active project's document ID
and edits the current store directly; it does not look up a serializer through the
`ExtensionManager`. Save As keeps the current state and updates its `ProjectDocumentId`.
Deserialization finds the manifest by `Purpose`, then opens both payload files by name.

SDK package: `EncySoftware.CAMAPI.SDK.Net` version `3.0.15-c-1328.1`.

1. Install the extension and restart the application.
2. Open or create a project and run **Edit project order card**.
3. Fill in the fields, press OK and explicitly save the project. Cancel discards
   the edits.
4. Reopen the project and run the utility again: the fields and binary payload are restored.
5. Change a field and save, or use Save As, to try creating new manifest and payload files.
6. Create a new project: its card is empty.
