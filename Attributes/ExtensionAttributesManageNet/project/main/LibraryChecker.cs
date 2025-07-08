using CAMAPI.DotnetHelper;
using CAMAPI.CustomAttributes;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;

namespace ExtensionAttributesManageNet;

/// <summary>
/// Check the user created example attributes library before.
/// </summary>
internal static class LibraryChecker
{

    /// <summary>
    /// Check the user created example attributes library before.
    /// </summary>
    private static bool IsLibrarAttached(ICamApiCustomAttributesManager manager)
    {
        if (manager==null)
            return false;
        using var appLibs = ComWrapper.Create(manager.ApplicationLibraries);
        if (appLibs.It.Count<1) 
            return false;
        for (int i = 0; i < appLibs.It.Count; i++)
        {
            string fn = appLibs.It.LibFileName[i];
            if (!String.IsNullOrEmpty(fn) && fn.Contains("ExampleAttributes.atrlib")) {
                if (File.Exists(fn))
                    return true;
            }
        }
        return false;
    }


    /// <summary>
    /// Show message if user didnt create example attributes library before.
    /// </summary>
    public static bool CheckIsExampleLibraryAttached(ICamApiObjectWithAttributes objectWithAttributes)
    {
        using var manager = ComWrapper.Create(objectWithAttributes.AttributesManager); 
        var res = IsLibrarAttached(manager.It);
        if (!res)
        {
            using var dialogs = UIDialogs.CreateHelper();
            dialogs.It.MessageBox("Example attributes library does not connected. Click to 'Attributes: Create library' item before.",
                TMessageDialogType.mdtInformation, (ushort)TUIButtonTypeFlags.btfOk, TUIButtonType.btOk, "");
        }
        return res;
    }
}