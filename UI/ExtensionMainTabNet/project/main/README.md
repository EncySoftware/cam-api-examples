# ExtensionMainTab usage example

This example demonstrates an extension of the `main_tab` group: a tab of its own in the ENCY main window, next to **Model**, **Machining** and **Simulation**. The page of the tab takes the left panel of the main window; the 3D view stays as the last built-in tab showed it.

The page shows the name of the active project and the **Go to the Model tab** button, which makes the **Model** tab active.

The example needs ENCY 3.0.22 or newer: older builds have no `main_tab` group.

# Settings file

The tab is described in `ExtensionMainTabNet.settings.json`:

```json
"main_tab": {
    "id": "Extension.MainTab.Net",
    "name": "Main tab by C#",
    "caption": "Example",
    "hint_text": "Tab of the C# main tab example",
    "icon_path": "",
    "order": 400
}
```

- `caption` is the text on the button of the tab, `hint_text` is its hint.
- `icon_path` is the icon of the button, relative to the folder of the library; empty means no icon.
- `order` is the place of the tab: the built-in tabs have 100 (Model), 200 (Machining) and 300 (Simulation), 1000 by default. 400 puts the tab after **Simulation**.

ENCY shows the button of the tab without loading the extension. It loads the extension when the tab is opened for the first time and keeps it until ENCY closes, the extension is disabled or a library of extensions is installed or removed; the next opening of the tab creates the page anew.

# How it works

`ExtensionMainTab` implements `IExtensionMainTab`. ENCY calls its methods on the ENCY main thread:

- `CreatePage` — creates the page: `MainTabPageHost.Create(context, app => new MainTabPage(app))` wraps `context.CamApplication` into `ComWrapper`, starts an STA thread of the page and puts the WPF content into a child window of `HostWindowHandle`. ENCY keeps the size of that window equal to the client area of `HostWindowHandle`. Then the extension subscribes to the theme change with `RegisterThemeChangedHandler`: the handler runs on the ENCY main thread, reads the new palette through `host.CamApplication` and posts it to the page, so the page follows the theme while it is shown;
- `OnShow` — the tab has become active: the extension reads the name of the active project and the theme colours through `host.CamApplication` on the ENCY main thread and posts them to the page. ENCY calls `OnShow` right after `CreatePage`, so until then the page has the colours of its XAML and no project name;
- `OnHide` — another tab has become active; ENCY takes the focus back from the page itself;
- `DestroyPage` — ENCY closes, or the extension is disabled or reinstalled: the extension unsubscribes from the theme change through `host.CamApplication` first (a failed unsubscription does not stop the rest) and disposes of `MainTabPageHost`. The host releases `CamApplication` on the page thread after the call the page is making to ENCY now and destroys the page window there. `Dispose` does not wait for the thread: ENCY waits for the page window to go away and serves the calls the page makes to ENCY meanwhile.

The context of a call lives only during the call; `MainTabPageHost` owns the `ComWrapper` made from `context.CamApplication` and gives it as `host.CamApplication` to the content factory and to the extension. The extension neither creates nor releases the wrapper.

Rules of the page thread:

- ENCY waits for the page while it creates the content, so the factory passed to `MainTabPageHost.Create` must not call ENCY, even through the wrapper it gets; the page gets what it shows in `OnShow`.
- Code of the page calls ENCY only through `ComWrapper` (`SetActiveMainTab` in `MainTabPage.OnModelClick`): on an STA thread it runs the call on an MTA thread and processes the messages of the page while it waits. Never call ENCY directly from the page thread: ENCY waits for that thread while it moves the focus, and switching the tab takes the focus from the page.
- ENCY code only posts to the page thread (`Dispatcher.BeginInvoke`), never waits for it: the page may be waiting for its own call to ENCY.
- An exception left on the page thread closes ENCY, so the page catches its exceptions and shows them; the methods of `IExtensionMainTab` return errors in `TResultStatus`.
- A key with **Ctrl** or **Alt** and the function keys go to ENCY when the page does not handle them, so the ENCY shortcuts work while the page has the focus.

`ICamApiApplicationMainForm.ActiveMainTabId` gives the active tab: `"model"`, `"machining"`, `"simulation"` (`MainTabIds` in `CAMAPI.DotnetHelper`) or the id of an extension; `SetActiveMainTab` with the id `Extension.MainTab.Net` opens this tab from other code. `RegisterActiveMainTabChangedHandler` subscribes to the change of the active tab.

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionMainTabNet.settings.json**.
3. Restart ENCY.
4. Click the **Example** tab after **Simulation** at the top of the main window. The page shows the name of the active project.
5. Click **Go to the Model tab**. The **Model** tab becomes active.
