using System.Diagnostics;
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.UIDialogs;

namespace ExtensionUtilityDialogWindowNet;

/// <summary>
/// Utility to show dialog window
/// </summary>
public class ExtensionUtilityDialogWindow: IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            if (Info == null)
                throw new Exception("Extension Info is not set");
            
            // container to store all properties, we are asking from user
            var props = new DialogWindowProps();
            
            // create window
            var window = new CamApiInspectorWindow(Info.InstanceInfo.ExtensionManager);
            try
            {
                // properties so provide to user
                var group1 = window.AddStringProperty("String value",
                    () => props.StringValue,
                    value => props.StringValue = value);
                group1.AddStringProperty("Sub string value",
                    () => props.SubStringValue,
                    value => props.SubStringValue = value);
                window.SetButtons((ushort)(TUIButtonType.btOk | TUIButtonType.btCancel));
                
                // show
                switch (window.Show())
                {
                    case TUIButtonType.btOk:
                        var tmpFileName = Path.GetTempFileName();
                        File.AppendAllText(tmpFileName,
                            $"String value: {props.StringValue}{Environment.NewLine}" + 
                            $"Sub string value: {props.SubStringValue}");
                        Process.Start("notepad.exe", tmpFileName);
                        break;
                    case TUIButtonType.btCancel:
                        throw new Exception("Cancel button pressed");
                    default:
                        throw new Exception("Unknown button pressed");
                }
            }
            finally
            {
                window.Clear();
            }
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
