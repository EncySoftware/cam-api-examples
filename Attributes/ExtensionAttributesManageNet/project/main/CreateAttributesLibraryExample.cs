using System.Diagnostics;
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.DotnetHelper;
using CAMAPI.CustomAttributes;
using CAMAPI.TechOperation;
using CAMAPI.Project;
using CAMAPI.Technologist;
using CAMAPI.Tools;

namespace ExtensionAttributesManageNet;

internal class CreateLibraryExample : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Creates a new library of attributes with simple set of attributes and shows the resulting library file in notepad.
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
            
            // Get attributes manager
            using var manager = ComWrapper.Create(app.It.AttributesManager);

            // Remove libraries that were created before by this example (in debug mode only)
            if (false)
                RemoveOldExampleLibraries(manager.It);      

            // Just some random path to store our example library of attributes.
            var exampleFolder = Path.Combine(Path.GetTempPath(), "API_Examples", "Attributes_E9967F10-1A77-4589-AC92-86C2329B5108");
            Directory.CreateDirectory(exampleFolder);
            var libFileName = Path.Combine(exampleFolder, "ExampleAttributes.atrlib");

            // If example library already attached to application, do nothing
            var idx = manager.It.ApplicationLibraries.IndexOfLibFileName(libFileName);
            if ((idx<0) || !File.Exists(libFileName))
            {
                // Check maybe it already exists on a hard drive and just not attached to application
                if (File.Exists(libFileName)) {
                    // Load it from file
                    using var lib = ComWrapper.Create(manager.It.LoadExistingLibrary(libFileName)); 
                    // And attach to the application so that it can be used
                    using var appLibs = ComWrapper.Create(manager.It.ApplicationLibraries);
                    appLibs.It.AddLibrary(lib.It);
                }
                // Otherwise create new library from scratch and attach to the application
                else             
                    CreateNewExampleAttributesLibrary(manager.It, libFileName);
            }
            
            // Show library file in notepad for demonstration purposes
            if (File.Exists(libFileName))
                Process.Start("notepad.exe", libFileName);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    private void CreateNewExampleAttributesLibrary(ICamApiCustomAttributesManager manager, string libFileName)
    {
        // Here we are describing the set of attribute TYPES that each CAM system object should have.
        // For example, we can create attributes for an abstract application, project, part, operation, and tool.
        // To use the defined attributes, you should request them from the specific object instance (specific operation or tool).
        // When you request the attributes, the manager creates copies of the attribute INSTANCES from the attribute TYPES 
        // related to the specific object type. This process is demonstrated in other files of this example (like ShowProjectAttributesExample.cs).

        // Create new empty library
        using var lib = ComWrapper.Create(manager.CreateNewLibrary()); 

        // Create global attributes for application (CAM system)
        MakeAttributesForApplication(manager, lib.It);

        // Create attributes that are specific to a project of CAM system
        MakeAttributesForProject(manager, lib.It);

        // Create attributes that are specific to a Part of CAM system
        MakeAttributesForPart(manager, lib.It);

        // Create attributes that are specific to an operation
        MakeAttributesForOperation(manager, lib.It);

        // Create attributes that are specific to a tool
        MakeAttributesForTool(manager, lib.It);

        // Save created library to file 
        if (File.Exists(libFileName))
            File.Delete(libFileName);
        manager.SaveLibrary(lib.It, libFileName);

        // Connect the library to the application so that attributes from it can be used
        using var appLibs = ComWrapper.Create(manager.ApplicationLibraries);
        appLibs.It.AddLibrary(lib.It);
    }

    private void MakeAttributesForApplication(ICamApiCustomAttributesManager manager, ICamApiCustomAttributesLibrary lib)
    {
        using var appCategoryW = ComWrapper.Create(manager.CreateCategoryType());
        var appCategory = appCategoryW.It;
        appCategory.Name = "My Application attributes";
        appCategory.Description = "A parent group for all my application attributes to make it look cute.";
        appCategory.TypeCategoryID = InterfaceInfo.IID<ICamApiApplication>();
        lib.Attributes.AddAttribute(appCategory);

        using (var aw = ComWrapper.Create(manager.CreateStringType()))
        {
            var aStr = aw.It;
            aStr.Name = "User name";
            aStr.Description = "Name of the current CAM system user";
            aStr.TypeCategoryID = appCategory.TypeID;
            aStr.Value = "Enter name";
            lib.Attributes.AddAttribute(aStr);
        }

        using (var aw = ComWrapper.Create(manager.CreateIntegerType()))
        {
            var aInt = aw.It;
            aInt.Name = "User code";
            aInt.Description = "Employee code of this enterprise";
            aInt.TypeCategoryID = appCategory.TypeID;
            aInt.Restrictions = TAttributeValueRestriction.avrRange;
            aInt.Bounds.Min = 100000;
            aInt.Bounds.Max = 200000;
            aInt.Value = 100000;
            lib.Attributes.AddAttribute(aInt);
        }

        using (var aw = ComWrapper.Create(manager.CreateStringType()))
        {
            var aStr = aw.It;
            aStr.Name = "Department";
            aStr.Description = "The department in which the employee works.";
            aStr.TypeCategoryID = appCategory.TypeID;
            aStr.Value = "D3";
            aStr.Restrictions = TAttributeValueRestriction.avrEnum;
            aStr.EnumValues.AddValue("D1", "Technology");
            aStr.EnumValues.AddValue("D2", "Manufacturing");
            aStr.EnumValues.AddValue("D3", "Shop floor");
            lib.Attributes.AddAttribute(aStr);
        }
    }

    private void MakeAttributesForProject(ICamApiCustomAttributesManager manager, ICamApiCustomAttributesLibrary lib)
    {
        using var prjCategoryW = ComWrapper.Create(manager.CreateCategoryType());
        var prjCategory = prjCategoryW.It;
        prjCategory.Name = "My Project attributes";
        prjCategory.TypeCategoryID = InterfaceInfo.IID<ICamApiProject>();
        lib.Attributes.AddAttribute(prjCategory);

        using (var aw = ComWrapper.Create(manager.CreateStringType()))
        {
            var aStr = aw.It;
            aStr.Name = "External project ID";
            aStr.Description = "Indetifier of the project inside external program (PLM)";
            aStr.TypeCategoryID = prjCategory.TypeID;
            aStr.Value = "";
            lib.Attributes.AddAttribute(aStr);
        }

        using (var aw = ComWrapper.Create(manager.CreateStringType()))
        {
            var aStr = aw.It;
            aStr.Name = "Project author";
            aStr.Description = "Person who modified the project last time";
            aStr.TypeCategoryID = prjCategory.TypeID;
            aStr.Value = "Unknown";
            aStr.Restrictions = TAttributeValueRestriction.avrEnum;
            aStr.EnumValues.AddValue("Unknown", "Unknown person");
            aStr.EnumValues.AddValue("Person1", "Person 1");
            aStr.EnumValues.AddValue("Person2", "Person 2");
            aStr.EnumValues.Extendable = true;
            lib.Attributes.AddAttribute(aStr);
        }

        using (var aw = ComWrapper.Create(manager.CreateIntegerType()))
        {                
            var aInt = aw.It;
            aInt.Name = "Project status";
            aInt.Description = "Degree of the project completion";
            aInt.TypeCategoryID = prjCategory.TypeID;
            aInt.Value = 0;
            aInt.Restrictions = TAttributeValueRestriction.avrEnum;
            aInt.EnumValues.AddValue(0, "Initiated");
            aInt.EnumValues.AddValue(1, "In progress");
            aInt.EnumValues.AddValue(2, "Under approval");
            aInt.EnumValues.AddValue(3, "Finished");
            lib.Attributes.AddAttribute(aInt);
        }       
    }

    private void MakeAttributesForPart(ICamApiCustomAttributesManager manager, ICamApiCustomAttributesLibrary lib)
    {
        using var partCategoryW = ComWrapper.Create(manager.CreateCategoryType());
        var partCategory = partCategoryW.It;
        partCategory.Name = "My Part attributes";
        partCategory.TypeCategoryID = InterfaceInfo.IID<ICamApiPartItem>();
        lib.Attributes.AddAttribute(partCategory);

        using (var aw = ComWrapper.Create(manager.CreateStringType()))
        {
            var aStr = aw.It;
            aStr.Name = "Part code";
            aStr.TypeCategoryID = partCategory.TypeID;
            aStr.Value = "000-ABC";
            lib.Attributes.AddAttribute(aStr);
        }

        using (var aw = ComWrapper.Create(manager.CreateStringType()))
        {                
            var aStr = aw.It;
            aStr.Name = "Part material";
            aStr.TypeCategoryID = partCategory.TypeID;
            aStr.Value = "Steel";
            aStr.Restrictions = TAttributeValueRestriction.avrEnum;
            aStr.EnumValues.Extendable = true;
            aStr.EnumValues.AddValue("Steel", "");
            aStr.EnumValues.AddValue("Bronze", "");
            aStr.EnumValues.AddValue("Stainless steel", "");
            aStr.EnumValues.AddValue("Brass", "");
            aStr.EnumValues.AddValue("Aluminium alloy", "");
            lib.Attributes.AddAttribute(aStr);
        }       
    }

    private void MakeAttributesForOperation(ICamApiCustomAttributesManager manager, ICamApiCustomAttributesLibrary lib)
    {
        using var opCategoryW = ComWrapper.Create(manager.CreateCategoryType());
        var opCategory = opCategoryW.It;
        opCategory.Name = "My Operation attributes";
        opCategory.TypeCategoryID = InterfaceInfo.IID<ICamApiTechOperation>();
        lib.Attributes.AddAttribute(opCategory);

        using (var aw = ComWrapper.Create(manager.CreateStringType()))
        {
            var aStr = aw.It;
            aStr.Name = "G-code file name";
            aStr.Description = "Short file name for the resulting G-code file the operation will produce. We can use it inside postprocessor.";
            aStr.TypeCategoryID = opCategory.TypeID;
            aStr.Value = "";
            lib.Attributes.AddAttribute(aStr);
        }

        using (var aw = ComWrapper.Create(manager.CreateFloatType()))
        {                
            var aFlt = aw.It;
            aFlt.Name = "Delay after operation (sec)";
            aFlt.Description = "Delay in seconds after operation we wait in postprocessor, for example.";
            aFlt.TypeCategoryID = opCategory.TypeID;
            aFlt.Units = "sec";
            aFlt.Value = 0;
            aFlt.Restrictions = TAttributeValueRestriction.avrEnum;
            aFlt.EnumValues.Extendable = true;
            aFlt.EnumValues.AddValue(0, "None");
            aFlt.EnumValues.AddValue(1.5, "Micro");
            aFlt.EnumValues.AddValue(15, "Small");
            aFlt.EnumValues.AddValue(60, "Middle");
            aFlt.EnumValues.AddValue(5*60, "Large");
            lib.Attributes.AddAttribute(aFlt);
        }       
    }

    private void MakeAttributesForTool(ICamApiCustomAttributesManager manager, ICamApiCustomAttributesLibrary lib)
    {
        // Just group for all tool attributes
        using var toolCategoryW = ComWrapper.Create(manager.CreateCategoryType());
        var toolCategory = toolCategoryW.It;
        toolCategory.Name = "My Tool attributes";
        toolCategory.TypeCategoryID = InterfaceInfo.IID<ICamApiMachiningTool>();
        lib.Attributes.AddAttribute(toolCategory);

        // Additional hidden parameter we use to know if we initialized the set of tool attributes or not.
        using (var aw = ComWrapper.Create(manager.CreateBooleanType()))
        {
            var aBol = aw.It;
            aBol.Name = "Initialized";
            aBol.TypeCategoryID = toolCategory.TypeID;
            aBol.Value = false;
            aBol.Visible = false;
            lib.Attributes.AddAttribute(aBol);
        }

        // Suppose the tool is an assembly that can contain several components inside. 
        // Count of components can vary.
        // The assembly itself has a unique code. And each component has a unique code too.

        using (var aw = ComWrapper.Create(manager.CreateStringType()))
        {
            var aStr = aw.It;
            aStr.Name = "Tool assembly code";
            aStr.TypeCategoryID = toolCategory.TypeID;
            aStr.Value = "TA-001";
            lib.Attributes.AddAttribute(aStr);
        }

        // We define a new type "My Tool component". Each component in our assembly will use this type and 
        // all nested attributes of this type will be added to each component automatically.
        using var toolComponentCategoryW = ComWrapper.Create(manager.CreateCategoryType());
        var toolComponentCategory = toolComponentCategoryW.It;
        toolComponentCategory.Name = "My Tool component";
        lib.Attributes.AddAttribute(toolComponentCategory);

        // Defaine array of components inside the assembly.
        using (var arw = ComWrapper.Create(manager.CreateArrayType()))
        {
            var aArr = arw.It;
            aArr.Name = "Components";
            aArr.TypeCategoryID = toolCategory.TypeID;
            // We are defining DefaultTypeID to be possible to change count of components inside the assembly 
            // with just changing the Count property programmatically or by user in edit window.
            aArr.DefaultTypeID = toolComponentCategory.TypeID;  
            // Wa also make a limit what types the array can contain.
            aArr.AddItemAt(Guid.Empty, toolComponentCategory.TypeID, -1);
            lib.Attributes.AddAttribute(aArr);
        }
        // Later we describe what attributes each component should have    
        using (var aw = ComWrapper.Create(manager.CreateStringType()))
        {                
            var aStr = aw.It;
            aStr.Name = "Component name";
            aStr.TypeCategoryID = toolComponentCategory.TypeID;
            aStr.Value = "Component 1";
            lib.Attributes.AddAttribute(aStr);
        }       

        using (var aw = ComWrapper.Create(manager.CreateStringType()))
        {                
            var aStr = aw.It;
            aStr.Name = "Component code";
            aStr.TypeCategoryID = toolComponentCategory.TypeID;
            aStr.Value = "TC-001";
            lib.Attributes.AddAttribute(aStr);
        }       
    }

    /// <summary>
    /// Just remove old example libraries for debug purposes if we running this example more than once.
    /// </summary>
    /// <param name="manager">Attributes manager</param>
    private void RemoveOldExampleLibraries(ICamApiCustomAttributesManager manager)
    {
        using var appLibs = ComWrapper.Create(manager.ApplicationLibraries);
        for (int i = appLibs.It.Count-1; i>=0; i--)
        {
            var fileName = appLibs.It.LibFileName[i];
            if (!string.IsNullOrEmpty(fileName) &&
                fileName.Contains("API_Examples") && 
                fileName.Contains("ExampleAttributes.atrlib"))
            {
                var dir = Path.GetDirectoryName(fileName);
                if (Directory.Exists(dir))
                    try {
                        Directory.Delete(dir, true);
                    } catch (Exception) { }
                appLibs.It.RemoveLibrary(i);
            }
        }
    }

}