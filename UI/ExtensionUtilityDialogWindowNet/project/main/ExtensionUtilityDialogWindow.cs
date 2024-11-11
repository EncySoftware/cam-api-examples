using System.Diagnostics;
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;

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
            
            // our container to store all properties, we are asking from user
            var currentValues = new DialogWindowValues();
            
            // create window
            using var window = new CamApiInspectorWindow(Info);
            
            // properties to provide to user
            using var propIterator = new SimplePropIterator(Info.InstanceInfo.ExtensionManager);
            propIterator.AddStringProp("String value",
                () => currentValues.StringValue,
                value => currentValues.StringValue = value);

            propIterator.AddEnumIndexedProp("Enum indexed value",
                () => currentValues.EnumIndexedValue,
                value => currentValues.EnumIndexedValue = value,
                list =>
                {
                    list.Add("Value 1", "");
                    list.Add("Value 2", "");
                    list.Add("Value 3", "");
                });

            propIterator.AddEnumIdProp("Enum value",
                () => currentValues.EnumIdValue,
                value => currentValues.EnumIdValue = value,
                list =>
                {
                    list.Add("A", "Value 1", "");
                    list.Add("B", "Value 2", "");
                    list.Add("C", "Value 3", "");
                });
            
            var group = propIterator.AddComplexProp("Group");
            group.AddStringProp("Sub string value",
                () => currentValues.SubStringValue,
                value => currentValues.SubStringValue = value);
            
            window.SetPropIterator(propIterator);
            
            // show
            window.SetButtons((ushort)(TUIButtonType.btOk | TUIButtonType.btCancel));
            switch (window.Show())
            {
                case TUIButtonType.btOk:
                    var tmpFileName = Path.GetTempFileName();
                    File.AppendAllText(tmpFileName,
                        $"Enum indexed value: {currentValues.EnumIndexedValue}{Environment.NewLine}" +
                        $"Enum id value: {currentValues.EnumIdValue}{Environment.NewLine}" +
                        $"String value: {currentValues.StringValue}{Environment.NewLine}" + 
                        $"Sub string value: {currentValues.SubStringValue}");
                    Process.Start("notepad.exe", tmpFileName);
                    break;
                case TUIButtonType.btCancel:
                    throw new Exception("Cancel button pressed");
                default:
                    throw new Exception("Unknown button pressed");
            }
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
