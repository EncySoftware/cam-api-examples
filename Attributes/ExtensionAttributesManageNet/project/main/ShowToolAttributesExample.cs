using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.DotnetHelper;
using CAMAPI.CustomAttributes;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;
using STCustomPropTypes;
using CAMAPI.TechnologyForm;

namespace ExtensionAttributesManageNet;

/// <summary>
/// Show parameters of the Tool from the current operation on clicking of corresponding popup menu item
/// </summary>
public class ShowToolAttributesExample : IExtension, IExtensionOperationPopup
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Shows dialog window with attributes of the current operation's tool on clicking of corresponding popup menu item
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
            // We want to show popup menu item only for operations which have tool, so they are not groups
            using var tool = ComWrapper.Create(operation.It.ToolEntity);
            if (tool.Instance==null)
                return;
            // Here we add the popup menu item itself and say the callback method sould be called when user clicks on it
            using var popupItems = ComWrapper.Create(context.OperationPopup);
            popupItems.It.AddItem("Show Tool attributes", "Show Tool attributes", true, new ToolMenuItemClickHandler(), out resultStatus);
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
internal class ToolMenuItemClickHandler : ICamApiTechnologyFormOperationPopupItemOnClicked
{    
    /// <summary>
    /// The callback method for the popup menu item "Show Tool attributes"
    /// </summary>
    public void OnItemClicked(IExtensionOperationPopupItemOnClickedContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        using var contextW = ComWrapper.Create(context);
        try
        {
            // Get operation the user clicked on
            using var operation = ComWrapper.Create(context.SelectedOperation);
            // Get tool of this operation
            using var tool = ComWrapper.Create(operation.It.ToolEntity);

            // Get ExtensionManager to ask Application instance
            using var extensionManager = ExtensionManagerHelper.GetInstance();

            // Ask Application from ExtensionManager
            using var appGetter = ComWrapper.Create(extensionManager.It.GetSingletonExtension("Extension.Global.Singletons.Application", out resultStatus) as ICamApiApplicationSingleton);
            using var app = ComWrapper.Create(appGetter.It.GetApplication(out resultStatus));

            // Get attributes manager to ask for attributes of the Tool
            using var manager = ComWrapper.Create(app.It.AttributesManager);

            // Check the user created example library before we ask for attributes from this library
            if (!LibraryChecker.CheckIsExampleLibraryAttached(manager.It)) 
                return;

            // Cast tool to ICamApiObjectWithAttributes to get the attributes tree
            var toolObj = (ICamApiObjectWithAttributes)tool.It;

            // Attributes tree is an exploded tree of attributes INSTANCES copied from attributes TYPES described in the library
            using var toolAttributesTree = ComWrapper.Create(manager.It.GetAttributesForObject(toolObj));

            // Using the attributes tree we can view and modify values of attributes asking them by name or by TypeID.
            // For example we created "Initialized" boolean attribute before in our example library.
            // And now we can check we filled attributes of this tool before or not using this "Initialized" attribute. 
            // We will use a "full name" of the attribute. Full name - is divided by "." symbol set of names of attribute nodes we
            // need to visit to reach desired one. Here it is "My Tool attributes.Initialized" because we placed "Initialized" 
            // attribute inside "My Tool attributes" category.
            if (!toolAttributesTree.It.Bol.Value["My Tool attributes.Initialized"])
            {
                // If we didnt fill attributes of this tool before, we fill them now
                // Firs we mark "Initialized" attribute as true
                toolAttributesTree.It.Bol.Value["My Tool attributes.Initialized"] = true;
                // We can use different indexers like Bol, Str, Int, Flt depending on exact attribute value type (Boolean, String, Integer, Floating point number).
                toolAttributesTree.It.Str.Value["My Tool attributes.Tool assembly code"] = Guid.NewGuid().ToString().Substring(0, 8);
                // Also we can use Arr indexer to reach attributes from the array of attributes using index instead of name.
                // Here we ask Components array.
                using var components = ComWrapper.Create(toolAttributesTree.It.Arr.Value["My Tool attributes.Components"]);
                if (components.Instance != null)
                {
                    // For example we want to initialize our assemble with 3 components.
                    int initialCount = 3;
                    
                    // If we defined DefaultTypeID for the array of attributes, we can simply assign the Count property. So the system knows what type of attribute it should add. 
                    // components.It.Count = initialCount;
                    for (int i = 0; i < initialCount; i++)
                    {
                        // But if we didnt define DefaultTypeID for the array of attributes, we need to add new item manually specifying its type.
                        components.It.AddNewItemOfTypeName("My Tool component", -1);
                        // Get the component by its index
                        using var component = ComWrapper.Create(components.It.Item[i]);
                        // Fill the values of nested attributes using their names
                        component.It.Str.Value["Component code"] = $"TC-00{i + 1}0";
                        component.It.Str.Value["Component name"] = $"Component {i + 1}";
                    }
                }
            }

            // We can use iterator to visit all nodes of the attributes tree
            using var attributesIterator = ComWrapper.Create(toolAttributesTree.It.CreateIterator());
  
            // Enumerate attributes in Debug console
            AttributesDescriber.Describe(attributesIterator.It);

            // Create window to show attributes tree in inspector window
            using var window = new CamApiInspectorWindow();
            window.Caption = "Attributes of the tool";
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