using CAMAPI.Application;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMAPI.NCMaker;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
using CAMAPI.TechOperation;
using CAMAPI.Technologist;
using STXMLPropTypes;

namespace WorkpieceMillingWorkflowNet;

/// <summary>
/// Creates the machining technology of the "Corners cleanup" sample project and generates G-code
/// </summary>
public static class TechnologyHelper
{
    /// <summary>
    /// Type of the roughing operation which removes the bulk of the stock
    /// </summary>
    private const string RoughingOperationType = "TSTRoughingWaterlineOp";

    /// <summary>
    /// Type of the finishing operation which drives the tool along the equidistant surface
    /// </summary>
    private const string ScallopOperationType = "TSTScallopOp";

    /// <summary>
    /// Type of the operation which cleans up the corners left by the previous tool
    /// </summary>
    private const string CornersOperationType = "TSTCornerRestMachiningOP";

    /// <summary>
    /// Relative path to the postprocessor
    /// </summary>
    private const string PostprocessorPath = @"Mill\Fanuc (30i)_Mill.sppx";

    /// <summary>
    /// Name of the output file with G-code
    /// </summary>
    private const string OutputFile = "WorkpieceMilling_NC.nc";

    /// <summary>
    /// Create the machining operations, calculate the toolpath and generate G-code
    /// </summary>
    public static void CreateTechnology(ComWrapper<ICamApiApplication> applicationCom,
        ComWrapper<ICamApiProject> activeProjectCom,
        ComWrapper<ICamApiTechnologist> technologistCom,
        ComWrapper<ICamApiPaths> pathsHelperCom)
    {
        // switch to machining tab
        applicationCom.Invoke(application =>
        {
            application.MainWorkMode = TMainWorkMode.mwmMachining;
        });

        using var rootOperationCom = technologistCom.RootOperation();
        var rootOperationId = rootOperationCom.Id();

        CreateRoughingOperation(activeProjectCom, technologistCom, rootOperationId);
        CreateScallopOperation(activeProjectCom, technologistCom, rootOperationId);
        CreateCornersOperation(activeProjectCom, technologistCom, rootOperationId);

        // calculate the toolpath of the whole technology
        technologistCom.Invoke(technologist =>
        {
            technologist.ResetAllOperationsToolpath();
            technologist.CalculateAllOperationsToolpath(true, out var ret);
            if (ret.Code == TResultStatusCode.rsError)
                throw new Exception(ret.Description);
        });

        // generate G-code
        GenerateGCode(activeProjectCom, technologistCom, pathsHelperCom);
    }

    /// <summary>
    /// Remove the bulk of the stock in horizontal layers with the end mill
    /// </summary>
    private static void CreateRoughingOperation(ComWrapper<ICamApiProject> activeProjectCom,
        ComWrapper<ICamApiTechnologist> technologistCom,
        string rootOperationId)
    {
        CreateOperation(activeProjectCom, technologistCom, RoughingOperationType, rootOperationId,
            ProjectHelper.RoughingToolNumber,
            xmlPropCom =>
            {
                xmlPropCom.SetFlt("Stock", 0.3);
                xmlPropCom.SetFlt("Stocks.Radial", 0.3);
                xmlPropCom.SetFlt("Stocks.Axial", 0.3);
                xmlPropCom.SetFlt("Tolerances.Outer", 0.2);
                xmlPropCom.SetStr("MillingType", "Both");
                xmlPropCom.SetStr("MachiningStrategy.Strategy", "Adaptish");
                xmlPropCom.SetStr("ZStep.ValueType", "Count");
                xmlPropCom.SetInt("ZStep.CountValue", 16);
                xmlPropCom.SetStr("Step.ValueType", "Percent");
                xmlPropCom.SetFlt("Step.PercentValue", 10);
                xmlPropCom.SetStr("ClearFlats", "False");

                // ramp into the material instead of plunging vertically
                xmlPropCom.SetFlt("Leads.Plunge.PlungeAngle", 3);
                xmlPropCom.SetStr("Leads.Plunge.MinLength.ValueType", "Distance");
                xmlPropCom.SetFlt("Leads.Plunge.MinLength.DistanceValue", 2);
                xmlPropCom.SetStr("Leads.Plunge.MaxLength.ValueType", "Distance");
                xmlPropCom.SetFlt("Leads.Plunge.MaxLength.DistanceValue", 10);
            });
    }

    /// <summary>
    /// Finish the shaped surfaces with a spherical mill following the equidistant
    /// </summary>
    private static void CreateScallopOperation(ComWrapper<ICamApiProject> activeProjectCom,
        ComWrapper<ICamApiTechnologist> technologistCom,
        string rootOperationId)
    {
        CreateOperation(activeProjectCom, technologistCom, ScallopOperationType, rootOperationId,
            ProjectHelper.ScallopToolNumber,
            xmlPropCom =>
            {
                xmlPropCom.SetFlt("Tolerances.Outer", 0.02);
                xmlPropCom.SetFlt("Step.PercentValue", 10);
                xmlPropCom.SetStr("MillingType", "Climb");
                xmlPropCom.SetStr("StartFrom", "Bottom");
            });
    }

    /// <summary>
    /// Clean up the corners the larger scallop tool could not reach
    /// </summary>
    private static void CreateCornersOperation(ComWrapper<ICamApiProject> activeProjectCom,
        ComWrapper<ICamApiTechnologist> technologistCom,
        string rootOperationId)
    {
        CreateOperation(activeProjectCom, technologistCom, CornersOperationType, rootOperationId,
            ProjectHelper.CornersToolNumber,
            xmlPropCom =>
            {
                xmlPropCom.SetStr("Strategy", "Combined");
                xmlPropCom.SetFlt("Step.PercentValue", 7);
                xmlPropCom.SetFlt("PrevToolDiameter", 20);
                xmlPropCom.SetFlt("Tolerances.Outer", 0.02);
                xmlPropCom.SetStr("MillingType", "Climb");
            });
    }

    /// <summary>
    /// Create an operation under the root one, assign its tool and apply the standard parameters
    /// </summary>
    private static void CreateOperation(ComWrapper<ICamApiProject> activeProjectCom,
        ComWrapper<ICamApiTechnologist> technologistCom,
        string operationType,
        string rootOperationId,
        string toolNumber,
        Action<ComWrapper<IST_XMLPropPointer>> setupParameters)
    {
        using var operationCom = technologistCom.CreateOperation(operationType, rootOperationId, "");
        activeProjectCom.SetOperationTool(operationCom.Id(), toolNumber);

        // set the standard parameters by their dotted paths in the operation XML
        using var xmlPropCom = operationCom.XMLProp();
        SetupLinkingParameters(xmlPropCom);
        setupParameters(xmlPropCom);
        operationCom.LoadFromXmlProp(xmlPropCom);
    }

    /// <summary>
    /// Take the approach and return rules from the root operation and position the links by the machine
    /// </summary>
    private static void SetupLinkingParameters(ComWrapper<IST_XMLPropPointer> xmlPropCom)
    {
        xmlPropCom.SetStr("Leads.AppRetGroup.Approach.Rule", "[TechOperation.ApproachLink_FromRoot]");
        xmlPropCom.SetStr("Leads.AppRetGroup.Return.Rule", "[TechOperation.ReturnLink_FromRoot]");
        xmlPropCom.SetStr("Leads.MotionPlannerOptions.Approach.MidPointMode", "FromMachine");
        xmlPropCom.SetStr("Leads.MotionPlannerOptions.Return.MidPointMode", "FromMachine");
        xmlPropCom.SetStr("CheckedGeometry.CheckFixtures", "false");
    }

    /// <summary>
    /// Generate G-code for the whole technology
    /// </summary>
    private static void GenerateGCode(ComWrapper<ICamApiProject> projectCom,
        ComWrapper<ICamApiTechnologist> technologistCom,
        ComWrapper<ICamApiPaths> pathsHelperCom)
    {
        using var ncMakerCom = projectCom.InvokeAndWrap(project => project.NCMaker);

        // save CLData for all operations as the first step
        using var operationsCom = technologistCom.GetOperations(TCamApiReorderingMode.rmReordered);

        var clDataFile = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), ".inpcld");
        projectCom.SaveClData(clDataFile, operationsCom);

        // configure output file for the G-code
        using var settingsCom = ncMakerCom.CreateSettings(TCamApiNCMakerSettingsType.ncsSppx);
        settingsCom.Invoke(s =>
        {
            var sppx = (ICamApiMakeCncSppxSettings)s;
            sppx.OutputFolder = Path.GetTempPath();
            sppx.NcFileName = OutputFile;
        });

        // get the postprocessor from the shared documents folder
        var postProcessorFilePath = pathsHelperCom.Invoke(pathsHelper =>
            Path.Combine(pathsHelper.PostprocessorsFolder, PostprocessorPath));
        if (!File.Exists(postProcessorFilePath))
            throw new Exception("Postprocessor not found: " + postProcessorFilePath);

        // generate G-code
        using var generatedFiles = ncMakerCom.Generate(clDataFile, postProcessorFilePath, settingsCom);
    }
}
