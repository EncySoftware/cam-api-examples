using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.DotnetHelper;
using CAMAPI.CustomAttributes;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;
using STCustomPropTypes;
using CAMAPI.TechnologyForm;
using System.Diagnostics;

namespace ExtensionAttributesManageNet;

/// <summary>
/// Show parameters of the Operation user on clicking of the popup menu item
/// </summary>
public class ShowOperationAttributesExample : IExtension, IExtensionOperationPopup
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Shows dialog window with attributes of the current operation on clicking of corresponding popup menu item
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
            // We want to show popup menu item only for operations which have toolpath, so they are not groups
            if (operation.It.IsGroup)
                return;
            // Here we add the popup menu item itself and say the callback method sould be called when user clicks on it
            using var popupItems = ComWrapper.Create(context.OperationPopup);
            popupItems.It.AddItem("Show Operation attributes", "Show Operation attributes", true, new OperationMenuItemClickHandler(), out resultStatus);
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
internal class OperationMenuItemClickHandler : ICamApiTechnologyFormOperationPopupItemOnClicked
{    
    /// <summary>
    /// The callback method for the popup menu item "Show Operation attributes"
    /// </summary>
    public void OnItemClicked(IExtensionOperationPopupItemOnClickedContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        using var contextW = ComWrapper.Create(context);
        try
        {
            // Get operation the user clicked on
            using var operation = ComWrapper.Create(context.SelectedOperation);
            // Cast operation to ICamApiObjectWithAttributes to get the attributes tree
            var operationObj = (ICamApiObjectWithAttributes)operation.It;

            // Check the user created example library before we ask for attributes from this library
            if (!LibraryChecker.CheckIsExampleLibraryAttached(operationObj)) 
                return;

            // Attributes tree is an exploded tree of attributes INSTANCES copied from attributes TYPES described in the library
            using var opAttributesTree = ComWrapper.Create(operationObj.Attributes);

            // Example of how to create and use attributes if you dont want to declare them in any library
            AddUntypedAttributes(opAttributesTree);

            // Using the attributes tree we can view and modify values of attributes asking them by name or by TypeID.
            // We will use a "full name" of the attribute. Full name - is divided by "." symbol set of names of attribute nodes we
            // need to visit to reach desired one. Here it is "My Operation attributes.G-code file name" because we placed "G-code file name" 
            // attribute inside "My Operation attributes" category.
            // If the "G-code file name" still has default value we will initialize it with the operation name
            if (string.IsNullOrEmpty(opAttributesTree.It.Str.Value["My Operation attributes.G-code file name"]))
                opAttributesTree.It.Str.Value["My Operation attributes.G-code file name"] = operation.It.Name.Replace(" ", "_") + ".gcode";

            // We can use iterator to visit all nodes of the attributes tree
            using var attributesIterator = ComWrapper.Create(opAttributesTree.It.CreateIterator());
            attributesIterator.It.HideUndefinedNodes = true;
  
            // Enumerate attributes in Debug console
            AttributesDescriber.Describe(attributesIterator.It);

            // Create window to show attributes tree in inspector window
            using var window = new CamApiInspectorWindow();
            window.Caption = "Attributes of the operation: " + operation.It.Name;
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

    private static void AddUntypedAttributes(ComWrapper<ICamApiCustomAttributesTree> opAttributesTree)
    {
        // Explicit declaration of attributes in libraries is not a prerequisite for their use. 
        // Attributes that are not explicitly declared in any libraries are called untyped.
        // In other words, you can call a method named "CreateUntyped*" in an instance of an object 
        // with attributes, configure the properties of the created attribute, and simply start using it.
        
        // If we already added untyped attributes, do nothing
        if (opAttributesTree.It.HasNode("Untyped attributes")) 
            return;

        // Create a category of untyped attributes for beauty
        using var rootNode = ComWrapper.Create(opAttributesTree.It.Root);
        using var categoryAttribute = ComWrapper.Create(rootNode.It.CreateUntypedCategoryChild("Untyped attributes"));
        using var category = ComWrapper.Create(opAttributesTree.It.FindNodeByName("Untyped attributes"));
        // This message will appear in hint by the question mark.
        categoryAttribute.It.Description = 
          "Demonstartion of Untyped attributes usage. Attributes inside the category was not defined in any library. They are created on the fly.";

        // Add some attribute to the category
        using var commentsAttribute = ComWrapper.Create(category.It.CreateUntypedStringChild("Comments"));
        // And then just use it
        opAttributesTree.It.Str.Value["Untyped attributes.Comments"] = "Here you can comment the operation";

        // After adding the attribute, you can adjust it.
        using (var a = ComWrapper.Create(category.It.CreateUntypedIntegerChild("Priority"))) 
        {
            a.It.Restrictions = TAttributeValueRestriction.avrEnum;
            a.It.EnumValues.AddValue(0, "Low");
            a.It.EnumValues.AddValue(1, "Medium");
            a.It.EnumValues.AddValue(2, "High");
            a.It.EnumValues.AddValue(3, "Critical");
        }
        // And then just use it 
        opAttributesTree.It.Int.Value["Untyped attributes.Priority"] = 1;

        // You can mark attributes as read-only, for example, if the user does not need to change them.
        using (var a = ComWrapper.Create(category.It.CreateUntypedBooleanChild("Mandatory"))) 
        {
            a.It.ReadOnly = true;
        }
        // And then just use it 
        opAttributesTree.It.Bol.Value["Untyped attributes.Mandatory"] = true;

        // You can make invisible attributes, for example, if the user does not need to know about them
        using (var a = ComWrapper.Create(category.It.CreateUntypedIntegerChild("Invisible number"))) 
        {
            a.It.Visible = false;
        }
        // But you still can use them
        opAttributesTree.It.Int.Value["Untyped attributes.Invisible number"] = 12345;
        opAttributesTree.It.Int.Value["Untyped attributes.Invisible number"] = 2*opAttributesTree.It.Int.Value["Untyped attributes.Invisible number"];

        // You can make arrays of attributes
        using var arrAtr = ComWrapper.Create(category.It.CreateUntypedArrayChild("Depth per pass"));
        using var arr = ComWrapper.Create(opAttributesTree.It.FindNodeByName("Untyped attributes.Depth per pass"));
        double depth = 0;
        for (var i = 0; i < 4; i++)
        {
            depth = Math.Sqrt(1/(double)(i+1))*1.5 + depth;
            using var ch =  ComWrapper.Create(arr.It.CreateUntypedFloatChild("Depth step"));
            // We should define default attribute type if we want to allow the user to be able to create new attributes 
            // of this type in the inspector window by changing the count of attributes
            if (i==1)
                arrAtr.It.DefaultAttributeType = ch.It;
            // Otherwise we can just use it 
            opAttributesTree.It.Flt.Value[$"Untyped attributes.Depth per pass({i})"] = depth;
        }
    }

}