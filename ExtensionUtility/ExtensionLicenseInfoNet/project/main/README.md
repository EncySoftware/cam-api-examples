# ExtensionLicenseInfo usage example

This example demonstrates a utility extension that greets the user by the name of the ENCY account and shows the license ENCY runs under: its edition, whether it is a trial one, its expiration date and the days left. It reads the real account and license of the running ENCY.

The example needs ENCY 3.4.1-dev.8 or newer and uses SDK `EncySoftware.CAMAPI.Sdk.Net` 3.0.23-dev.9.

# How it works

`ExtensionLicenseInfo.Run` wraps `context.CamApplication` in `ComWrapper` and shows a message box with the card:

1. `applicationCom.UserAccount()` gives the account ENCY is signed in with. `DisplayName()` reads the `name` claim of the access token of the account. It is empty when ENCY is not signed in, the token carries no name or the server gave no token; the card then says that the user is not signed in. `DisplayName` is a method of the helper `UserAccountHelper`, not of `ICamApiUserAccount`. It gets the token first, so it may block ENCY for up to 15 seconds when the server does not answer.
2. `applicationCom.LicenseInfo()` gives `ICamApiLicenseInfo`, the `LicenseInfo` property of `ICamApiApplication`. The helper `LicenseInfoHelper` reads it:
   - `Edition()` - name of the edition, for display only: it may be localized, so do not branch on it. Empty without a valid license.
   - `IsTrial()` - the license is a trial one.
   - `ExpirationDate()` - the day the license stops being valid as `YYYY-MM-DD`; empty for a perpetual license.
   - `DaysLeft()` - days left before the license stops working, the same remaining time the version window shows. `-1` when the license has no time limit, or while the term of an account license has not yet come from the server.
3. `UIDialogs.CreateHelper()` gives the dialogs helper; its `MessageBox` shows the card with the **OK** button built by `MessageBoxHelper.BuildButtons`.

Every property reads the current license anew.

Every wrapper is disposed with `using`. An exception goes to `resultStatus` as `rsError`: an exception must never leave a method that ENCY calls.

The card looks like this:

```
Hello, John Smith!

Edition: 5X
Trial: yes
Expires: 2026-12-25
Days left: 12
```

# How to build

1. Compile this project with **"./commands/build.cmd"** or menu **"Terminal/Run build task"** in VSCode.
2. Install the extension on the CAM start page: open **Extensions**, switch to **Manage** at the top, click **Install from file** on the right and select the file **bin/Debug/ExtensionLicenseInfoNet.settings.json**.
3. Restart ENCY.
4. In the utilities list (the **Utilities** button on the top panel or **Ctrl+Shift+E**), select "**Show the license and the account (C#)**". A message box shows the greeting and the license.
