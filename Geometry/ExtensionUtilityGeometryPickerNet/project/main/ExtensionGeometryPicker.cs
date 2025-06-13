using System.Diagnostics;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.Generic.List;
using CAMAPI.GeomModel;
using CAMAPI.GeomPicker;
using CAMAPI.ResultStatus;

namespace ExtensionUtilityGeometryPickerNet;

/// <summary>
/// Utility to import geometry from Milling_25D\Part1.igs into the active project
/// </summary>
public class ExtensionGeometryPicker: IExtension,
    IExtensionUtility,
    IExtensionLazyUnloadable
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Extension can be closed only if this property is true. We set it to true after user closes dialog window
    /// </summary>
    public bool CanUnload { get; set; }
    
    class GeomPickerOnClose(IExtensionLazyUnloadable extension) : ICamApiGeomPickerOnClose
    {
        /// <summary>
        /// Call notepad and show selected items in it
        /// </summary>
        private static void ViewInNotepad(List<string> items)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            File.WriteAllText(tempFile, "Items: " + Environment.NewLine);
            foreach (var item in items)
                File.AppendAllText(tempFile, item + Environment.NewLine);
            
            // show temp file
            Process.Start("notepad.exe", tempFile);           
        }
        
        public void OnConfirm(IListString selectedItemsObj)
        {
            using var selectedItemsCom = ComWrapper.Create(selectedItemsObj);
            var selectedItems = selectedItemsCom.Instance
                                ?? throw new Exception("Error creating selected items");
            var items = new List<string>();
            for (var i = 0; i < selectedItems.Count(); i++)
                items.Add(selectedItems.Get(i));
            ViewInNotepad(items);
            extension.CanUnload = true;
        }

        public void OnCancel()
        {
            ViewInNotepad(["Canceled"]);
            extension.CanUnload = true;
        }
    }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        CanUnload = false;
        resultStatus = default;
        try
        {
            using var contextCom = ComWrapper.Create(context);
            
            using var geomPickerCom = SystemExtensionFactory.CreateExtension<ICamApiGeomPicker>("Extension.GeomPicker");
            var geomPicker = geomPickerCom.Instance
                             ?? throw new Exception("Error creating geom picker");
            geomPicker.AvailableEntityTypes = (ushort) (TGeometryEntityTypeFlag.etfFace | TGeometryEntityTypeFlag.etfFace);
            geomPicker.OnClose = new GeomPickerOnClose(this);
            geomPicker.Show();
        }
        catch (Exception e)
        {
            CanUnload = true;
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}