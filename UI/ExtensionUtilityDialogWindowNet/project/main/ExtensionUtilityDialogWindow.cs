using System.Diagnostics;
using System.Runtime.InteropServices;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;
using CAMHelper.NativeLibUtils;
using STCustomPropTypes;

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
            using var window = new CamApiInspectorWindow();
            
            // properties to provide to user
            using var propIterator = new SimplePropIterator();
          
            propIterator.AddStringProp("String value",
                    () => currentValues.StringValue,
                    value => currentValues.StringValue = value)
                .WithHint("Any text, written to the file as is");

            propIterator.AddEnumIndexedProp("Enum indexed value",
                    () => currentValues.EnumIndexedValue,
                    value => currentValues.EnumIndexedValue = value,
                    list =>
                    {
                        list.Add("Value 1", "");
                        list.Add("Value 2", "");
                        list.Add("Value 3", "");
                    })
                .WithHint("The value is the index of the item, from 0");

            propIterator.AddEnumIdProp("Enum value",
                    () => currentValues.EnumIdValue,
                    value => currentValues.EnumIdValue = value,
                    list =>
                    {
                        list.Add("A", "Value 1", "");
                        list.Add("B", "Value 2", "");
                        list.Add("C", "Value 3", "");
                    })
                .WithHint("The value is the id of the item: A, B or C");

            var group = propIterator.AddComplexProp("Group");
            group.AddStringProp("Sub string value",
                () => currentValues.SubStringValue,
                value => currentValues.SubStringValue = value);

            // number with units and a button in its row
            var depth = propIterator.AddDoubleProp("Depth",
                    () => currentValues.Depth,
                    value => currentValues.Depth = value)
                .WithUnits("mm")
                .WithHint("Depth of the pocket from the top of the stock");
            depth.AddButton("Round", "", "Round the depth to whole millimetres",
                () => currentValues.Depth = Math.Round(currentValues.Depth));

            propIterator.AddStringProp("Output folder",
                    () => Path.GetTempPath(),
                    _ => { })
                .WithReadOnly(() => true)
                .WithHint("Folder of the text file the values are written to on OK");

            window.SetPropIterator(propIterator);
            
            // show
            window.SetButtons(MessageBoxHelper.BuildButtons(TUIButtonType.btOk, TUIButtonType.btCancel));
            switch (window.Show())
            {
                case TUIButtonType.btOk:
                    var tmpFileName = Path.GetTempFileName();
                    File.AppendAllText(tmpFileName,
                        $"Enum indexed value: {currentValues.EnumIndexedValue}{Environment.NewLine}" +
                        $"Enum id value: {currentValues.EnumIdValue}{Environment.NewLine}" +
                        $"String value: {currentValues.StringValue}{Environment.NewLine}" + 
                        $"Sub string value: {currentValues.SubStringValue}{Environment.NewLine}" +
                        $"Depth: {currentValues.Depth} mm");
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
