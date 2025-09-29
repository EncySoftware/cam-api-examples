using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.DotnetHelper;
using CAMAPI.CustomAttributes;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;
using STCustomPropTypes;
using CAMAPI.TechnologyForm;
using CAMAPI.Technologist;

namespace ExtensionAttributesManageNet;

internal class ShowPartAttributesExample : IExtension, IExtensionOperationPopup
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Shows dialog window with attributes of the Part on clicking of corresponding popup menu item
    /// </summary>
    /// <param name="context">Running main application info</param>
    /// <param name="resultStatus">Object to contain some message to calling code</param>
    public void Build(IExtensionOperationPopupBuildContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        // We use ComWrapper everywhere to limit the lifetime of the COM objects and 
        // make it predicable for CAM system and not rely on the garbage collector.
        using var contextW = ComWrapper.Create(context);
        try
        {
            // Get operation the user clicked on
            using var operation = ComWrapper.Create(context.SelectedOperation);
            // We want to show popup menu item only for TSTPartOpGroup operation type
            if (!operation.It.IsGroup || !String.Equals(operation.It.OperationType, "TSTPartOpGroup", StringComparison.InvariantCultureIgnoreCase)) 
                return;
            // Here we add the popup menu item itself and say the callback method sould be called when user clicks on it
            using var popupItems = ComWrapper.Create(context.OperationPopup);
            popupItems.It.AddItem("Show Part attributes", "Show Part attributes", true, new PartMenuItemClickHandler(), out resultStatus);
        }
        catch (Exception ex)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = ex.Message;
        }
    }
}

/// <summary>
/// The class that contains callback method for the popup menu item
/// </summary>
internal class PartMenuItemClickHandler : ICamApiTechnologyFormOperationPopupItemOnClicked
{    
    /// <summary>
    /// The callback method for the popup menu item "Show Part attributes"
    /// </summary>
    public void OnItemClicked(IExtensionOperationPopupItemOnClickedContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        using var contextW = ComWrapper.Create(context);
        try
        {
            // Get operation the user clicked on
            using var partOperationCom = ComWrapper.Create(context.SelectedOperation);
            var partIndex = partOperationCom.Invoke(op => op.PartIndex);
            
            // Get the Technologist  - a tree of operations of the project
            using var technologistCom = partOperationCom.InvokeAndWrap(operation => (ICamApiTechnologist)operation.Technologist);
            
            // Get the Part item that related to the current operation
            using var partAndStageListCom = technologistCom.InvokeAndWrap(t => t.PartAndStageList);
            using var partCom = partAndStageListCom.InvokeAndWrap(list => list.Part[partIndex]);

            // Cast to ICamApiObjectWithAttributes to get the attributes tree
            var partObj = (ICamApiObjectWithAttributes)partCom.It;

            // Check the user created example library before we ask for attributes from this library
            if (!LibraryChecker.CheckIsExampleLibraryAttached(partObj)) 
                return;
            
            // Attributes tree is an exploded tree of attributes INSTANCES copied from attributes TYPES described in the library
            using var partAttributesTree = ComWrapper.Create(partObj.Attributes);

            // Using the attributes tree we can view and modify values of attributes asking them by name or by TypeID.
            // We will use a "full name" of the attribute. Full name - is divided by "." symbol set of names of attribute nodes we
            // need to visit to reach desired one. Here it is "My Part attributes.Part code" because we placed "Part code" 
            // attribute inside "My Part attributes" category.
            // If the Part code still has default value we will initialize it with new unique value
            if (string.Equals(partAttributesTree.It.Str.Value["My Part attributes.Part code"], "000-ABC")) 
                partAttributesTree.It.Str.Value["My Part attributes.Part code"] = Guid.NewGuid().ToString().Substring(0, 8);

            // We can use iterator to visit all nodes of the attributes tree
            using var attributesIterator = ComWrapper.Create(partAttributesTree.It.CreateIterator());
  
            // Enumerate attributes in Debug console
            AttributesDescriber.Describe(attributesIterator.It);

            // Create window to show attributes tree in inspector window
            using var window = new CamApiInspectorWindow();
            window.Caption = "Attributes of the Part: " + partCom.It.ExternalID;
            // The attributes tree iterator (ICamApiCustomAttributesTreeIterator) can be casted to IST_CustomPropIterator the window wants to show.
            window.SetPropIterator((IST_CustomPropIterator)attributesIterator.It);
            window.SetButtons((ushort)TUIButtonTypeFlags.btfOk);
            window.Show();

        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

}