using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.DotnetHelper;
using CAMAPI.CustomAttributes;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;
using STCustomPropTypes;

namespace ExtensionAttributesManageNet;

internal class ShowProjectAttributesExample : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Shows dialog window with attributes of the active project in the application
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

            // Get attributes manager to ask for attributes of the application
            using var manager = ComWrapper.Create(app.It.AttributesManager);

            // Check the user created example library before we ask for attributes from this library
            if (!LibraryChecker.CheckIsExampleLibraryAttached(manager.It)) 
                return;

            // Get active project from the application and cast it to object with attributes to be possible to ask them.
            using var prj = ComWrapper.Create(app.It.GetActiveProject(out resultStatus));
            var prjObj = (ICamApiObjectWithAttributes)prj.It;

            // Attributes tree is an exploded tree of attributes INSTANCES copied from attributes TYPES described in the library
            using var prjAttributesTree = ComWrapper.Create(manager.It.GetAttributesForObject(prjObj));

            // Using the attributes tree we can view and modify values of attributes asking them by name or by TypeID.
            // We will use a "full name" of the attribute. Full name - is divided by "." symbol set of names of attribute nodes we
            // need to visit to reach desired one. Here it is "My Project attributes.External project ID" because we placed "External project ID" 
            // attribute inside "My Project attributes" category.
            // If the External project ID is not filled yet, we will generate a new ID for it and initialize other attributes of the project.
            if (string.IsNullOrEmpty(prjAttributesTree.It.Str.Value["My Project attributes.External project ID"]))
            {
                // Get attributes of the application
                using var appAttributesTree = ComWrapper.Create(manager.It.GetAttributesForObject((ICamApiObjectWithAttributes)app.It));

                // Generate new ID for the project
                prjAttributesTree.It.Str.Value["My Project attributes.External project ID"] = Guid.NewGuid().ToString();
                
                // Fill the project author from the current user name attribute we have in the application
                prjAttributesTree.It.Str.Value["My Project attributes.Project author"] = 
                    appAttributesTree.It.Str.Value["My Application attributes.User name"];
                // Assign status as "In progress" to the project
                prjAttributesTree.It.Int.Value["My Project attributes.Project status"] = 1; //"In progress" status
            }

            // We can use iterator to visit all nodes of the attributes tree
            using var attributesIterator = ComWrapper.Create(prjAttributesTree.It.CreateIterator());
  
            // Enumerate attributes in Debug console
            AttributesDescriber.Describe(attributesIterator.It);

            // Create window to show attributes tree in inspector window
            using var window = new CamApiInspectorWindow();
            window.Caption = "Attributes of the project example: " + Path.GetFileName(prj.It.FilePath);
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