using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Windows;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.Logger;
using CAMAPI.ResultStatus;
using CAMAPI.UIDialogs.DotnetHelper;

namespace ExtensionUtilityNotifyNet;

/// <summary>
/// Utility to show dialog window
/// </summary>
public class ExtensionUtilityNotify: IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }
    
    private static string? AskUserForText()
    {
        string? result = null;

        var thread = new Thread(() =>
        {
            var window = new TextInputWindow();

            if (window.ShowDialog() == true)
                result = window.UserInput;
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return result;
    }
    
    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            var userText = AskUserForText();
            if (userText == null)
                return;
            UIDialogs.Notify(TLogEventType.leInfo, "User input", userText);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
