using System.Runtime.InteropServices;
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace ExtensionUtilityMessageBoxNet;

/// <summary>
/// Utility to show simple message box
/// </summary>
public class ExtensionUtilityMessageBox: IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        resultStatus = default;
        MessageBox(new IntPtr(0), "Some text", "Some text", 0);  
    }
}
