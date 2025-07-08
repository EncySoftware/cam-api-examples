using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.DotnetHelper;
using CAMAPI.CustomAttributes;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;
using STCustomPropTypes;

namespace ExtensionAttributesManageNet;

internal class ShowApplicationAttributesExample : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Shows dialog window with attributes of the CAM application
    /// </summary>
    /// <param name="context">Information about current running instance</param>
    /// <param name="resultStatus">Structure to return error</param>
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            // We use ComWrapper everywhere to limit the lifetime of the COM objects and 
            // make it predicable for CAM system and not rely on the garbage collector.
            using var ctx = ComWrapper.Create(context);
            
            // Get application
            using var app = ComWrapper.Create(context.CamApplication);
            var appObj = (ICamApiObjectWithAttributes)app.It;
            
            // Check the user created example library before we ask for attributes from this library
            if (!LibraryChecker.CheckIsExampleLibraryAttached(appObj)) 
                return;

            // Attributes tree is an exploded tree of attributes INSTANCES copied from attributes TYPES described in the library
            using var appAttributesTree = ComWrapper.Create(appObj.Attributes);

            var allCorrect = false;
            do
            {
                // We can use iterator to visit all nodes of the attributes tree
                using var attributesIterator = ComWrapper.Create(appAttributesTree.It.CreateIterator());

                // We can clear all attributes that are not defined in the library
                appAttributesTree.It.ClearAllUndefinedAttributes();
                // Otherwise we can just hide undefined attributes
                attributesIterator.It.HideUndefinedNodes = true;

                // Enumerate attributes in Debug console
                AttributesDescriber.Describe(attributesIterator.It);

                // Create window to show attributes tree in inspector window
                using var window = new CamApiInspectorWindow();
                window.Caption = "Attributes of the application example";
                // The attributes tree iterator (ICamApiCustomAttributesTreeIterator) can be casted to IST_CustomPropIterator the window wants to show.
                window.SetPropIterator((IST_CustomPropIterator)attributesIterator.It);
                window.SetButtons((ushort)TUIButtonTypeFlags.btfOk);

                window.Show();

                // Using the attributes tree we can view and modify values of attributes asking them by name or by TypeID.
                // We will use a "full name" of the attribute. Full name - is divided by "." symbol set of names of attribute nodes we
                // need to visit to reach desired one. Here it is "My Application attributes.User name" because we placed "User name" 
                // attribute inside "My Application attributes" category.
                var userName = appAttributesTree.It.Str.Value["My Application attributes.User name"];
                allCorrect = !string.Equals(userName, "Enter name");
                if (!allCorrect)
                {
                    // The user didnt fill his name. Ask them repeat.
                    using var dialogs = UIDialogs.CreateHelper();
                    dialogs.It.MessageBox("You should change User name",
                        TMessageDialogType.mdtWarning, (ushort)TUIButtonTypeFlags.btfOk, TUIButtonType.btOk, "");
                }
            } while (!allCorrect);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

}