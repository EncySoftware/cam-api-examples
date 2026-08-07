using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CAMAPI.Application;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMAPI.ModelFormerTypes;
using CAMAPI.NCMaker;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;
using CAMIPC.Application;
using CAMIPC.ExecuteContext;
using CAMIPC.GeomModel;
using CAMIPC.ModelFormerTypes;
using CAMIPC.NCMaker;
using CAMIPC.Project;
using CAMIPC.Singletons;
using CAMIPC.Technologist;
using CAMIPC.TechOperation;
using STTypes;

namespace FullWorkflow3DProject;

/// <summary>
/// Static class to create technology
/// </summary>
[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
public static class TechnologyHelper
{
    /// <summary>
    /// Relative path to the output file with G-code
    /// </summary>
    private const string OutputFile = "Part1_NC.nc";
    
    /// <summary>
    /// Create technology with several operations
    /// </summary>
    public static void CreateTechnology(ComWrapper<ICamIpcApplication> applicationCom,
        ComWrapper<ICamIpcProject> activeProjectCom,
        ComWrapper<ICamIpcTechnologist> technologistCom,
        ComWrapper<ICamIpcGeometryModel> geometryModelCom,
        ComWrapper<ICamIpcPaths> pathsHelperCom)
    {
        var executeContext = new TExecuteContext();
        
        // switch to model tab
        applicationCom.Invoke(application =>
        {
            application.SetMainWorkMode(TMainWorkMode.mwmMachining, ref executeContext);
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
        });
        
        // arrange
        using var ncMakerCom = activeProjectCom.InvokeAndWrap(project => project.GetNCMaker(ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        // setup workpiece
        SetupStage(technologistCom);
        
        // get root operation id
        using var operationRootCom = technologistCom.InvokeAndWrap(technologist => technologist.RootOperation);
        var operationRootId = operationRootCom.Invoke(operation => operation.GetId(ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        // create 1st operation - face milling
        var operation1Id = CreateOperationFirst(activeProjectCom,
            technologistCom,
            operationRootId,
            "TSTFaceMillingOp");
            
        // create 2nd operation - roughing waterline
        var operation2Id = CreateOperationSecond(activeProjectCom,
            technologistCom,
            geometryModelCom,
            operation1Id,
            "TSTRoughingWaterlineOp");
            
        // create 3rd operation - hole machining
        var operation3Id = CreateOperationThird(activeProjectCom,
            technologistCom,
            geometryModelCom,
            operation2Id,
            "HoleMachiningOp");
            
        // create 4th operation - 2D contouring
        var operation4Id = CreateOperationForth(activeProjectCom,
            technologistCom,
            geometryModelCom,
            operation3Id,
            "TST2DContouringOp");
            
        // create 5th operation - hole machining
        var operation5Id = CreateOperationFifth(activeProjectCom,
            technologistCom,
            geometryModelCom,
            operation4Id,
            "HoleMachiningOp");
            
        // create 6th operation - hole machining
        CreateOperationSixth(activeProjectCom,
            technologistCom,
            geometryModelCom,
            operation5Id,
            "HoleMachiningOp");
        
        // calculate entire technology
        technologistCom.Invoke(technologist =>
        {
            technologist.ResetAllOperationsToolpath(ref executeContext);
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            
            technologist.CalculateAllOperationsToolpath(true, ref executeContext);
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
        });
        
        // generate CNC
        GenerateGCode(activeProjectCom, technologistCom, pathsHelperCom);
    }
    
    private static void SetupStage(ComWrapper<ICamIpcTechnologist> technologistCom)
    {
        var execcuteContext = new TExecuteContext();
        using var pslCom = technologistCom.InvokeAndWrap(technologist => technologist.GetPartAndStageList(ref execcuteContext));
        if (execcuteContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(execcuteContext.ResultStatus.Description);
        
        using var partStageCom = pslCom.InvokeAndWrap(psl => psl.GetPartStage(0, 0));
        
        // setup workpiece setup
        using var workpieceSetupCom = partStageCom.InvokeAndWrap(partStage => partStage.WorkpieceSetup);
        workpieceSetupCom.Invoke(workpieceSetup =>
        {
            var offset = new TST3DMatrix
            {
                vX = new TST3DPoint { X = 1, Y = 0, Z = 0 },
                vY = new TST3DPoint { X = 0, Y = 1, Z = 0 },
                vZ = new TST3DPoint { X = 0, Y = 0, Z = 1 },
                vT = new TST3DPoint { X = 0, Y = 0, Z = 100 }
            };
            workpieceSetup.Offset = offset;
        });
        
        // setup workpiece coordinate system
        using var workpieceCoordinateSystemCom = partStageCom.InvokeAndWrap(partStage => partStage.WorkpieceCoordinateSystem);
        workpieceCoordinateSystemCom.Invoke(workpieceCoordinateSystem =>
        {
            workpieceCoordinateSystem.Offset = new TST3DPoint { X = -100, Y = -60, Z = 0 };
        });
    }
    
    private static string CreateOperationFirst(ComWrapper<ICamIpcProject> projectCom,
        ComWrapper<ICamIpcTechnologist> technologistCom,
        string prevOperationId,
        string typeId)
    {
        var executeContext = new TExecuteContext();
        
        // create operation
        using var operationCom = technologistCom.InvokeAndWrap(technologist =>
            technologist.CreateOperation(typeId, prevOperationId, "", ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        var id = operationCom.Invoke(operation => operation.GetId(ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        // setup tool
        SetupOperationTool(projectCom, id, "12");
        
        // return id
        return id;
    }
    
    private static string CreateOperationSecond(ComWrapper<ICamIpcProject> projectCom,
        ComWrapper<ICamIpcTechnologist> technologistCom,
        ComWrapper<ICamIpcGeometryModel> geometryModelCom,
        string prevOperationId,
        string typeId)
    {
        var executeContext = new TExecuteContext();
        
        // create operation
        using var operationCom = technologistCom.InvokeAndWrap(technologist =>
            technologist.CreateOperation(typeId, prevOperationId, "", ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        var id = operationCom.Invoke(operation => operation.GetId(ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        // setup tool
        SetupOperationTool(projectCom, id, "11");
        
        // setup job assignment for the operation
        SelectGeometryItemById(geometryModelCom, [@"Part\Part1.igs\Edge{Face11,Face7}"]);
        using var mfCom = operationCom.InvokeAndWrap(operation => operation.ModelFormerJobAssignment);
        mfCom.Invoke(mf =>
        {
            if (mf is not ICamIpcModelFormerWithZones mfWithZones)
                throw new Exception("Not ICamIpcModelFormerWithZones");
            using var itemsCom = ComWrapper.Create(mfWithZones.AddRestrictedZoneSelected());
            itemsCom.Invoke(items =>
            {
                if (items.Count == 0)
                    throw new Exception("Cannot add restricted zone");
            });
        });
        
        return id;
    }
    
    private static string CreateOperationThird(ComWrapper<ICamIpcProject> projectCom,
        ComWrapper<ICamIpcTechnologist> technologistCom,
        ComWrapper<ICamIpcGeometryModel> geometryModelCom,
        string prevOperationId,
        string typeId)
    {
        var executeContext = new TExecuteContext();
        
        // create operation
        using var operationCom = technologistCom.InvokeAndWrap(technologist =>
            technologist.CreateOperation(typeId, prevOperationId, "", ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        var id = operationCom.Invoke(operation => operation.GetId(ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        // setup tool
        SetupOperationTool(projectCom, id, "11");
        
        // setup job assignment for the operation
        AddHolesToJobAssignment(operationCom, geometryModelCom, [@"Part\Part1.igs\Face11"]);
        
        // setup properties
        using var xmlPropsCom = operationCom.InvokeAndWrap(operation => operation.XmlProp);
        xmlPropsCom.Invoke(xmlProps =>
        {
            xmlProps.Str["DrillingType"] = "HolePocketing";
        });
        
        // return id
        return id;
    }
    
    private static string CreateOperationForth(ComWrapper<ICamIpcProject> projectCom,
        ComWrapper<ICamIpcTechnologist> technologistCom,
        ComWrapper<ICamIpcGeometryModel> geometryModelCom,
        string prevOperationId,
        string typeId)
    {
        var executeContext = new TExecuteContext();
        
        // create operation
        using var operationCom = technologistCom.InvokeAndWrap(technologist =>
            technologist.CreateOperation(typeId, prevOperationId, "", ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        var id = operationCom.Invoke(operation => operation.GetId(ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        // setup tool, which is noy appropriate for this operation
        SetupOperationTool(projectCom, id, "35");
        
        // setup job assignment for the operation
        using var mfCom = operationCom.InvokeAndWrap(operation => operation.ModelFormerJobAssignment);
        mfCom.Invoke(mf =>
        {
            // add the curve
            SelectGeometryItemById(geometryModelCom, [@"Part\Part1.igs\Face6"]);
            if (mf is not ICamIpcModelFormerWithCurve2D modelFormerWithCurve2D)
                throw new Exception("Not ICamIpcModelFormerWithCurve2D");
            using var curvesCom = ComWrapper.Create(modelFormerWithCurve2D.AddCurves2DSelected());
            curvesCom.Invoke(curves =>
            {
                if (curves.Count == 0)
                    throw new Exception("Cannot add curve");
            });
            
            // add bottom level
            SelectGeometryItemById(geometryModelCom, [@"Part\Part1.igs\Face12"]);
            if (mf is not ICamIpcModelFormerWithLevels modelFormerWithLevels)
                throw new Exception("Not ICamIpcModelFormerWithLevels");
            using var levelsCom =
                ComWrapper.Create(modelFormerWithLevels.AddLevelSelected(TModelFormerLevelType.amflBottomLevel));
            levelsCom.Invoke(items =>
            {
                if (items.Count == 0)
                    throw new Exception("Cannot add level");
            });
        });
        
        return id;
    }
    
    private static string CreateOperationFifth(ComWrapper<ICamIpcProject> projectCom,
        ComWrapper<ICamIpcTechnologist> technologistCom,
        ComWrapper<ICamIpcGeometryModel> geometryModelCom,
        string prevOperationId,
        string typeId)
    {
        var executeContext = new TExecuteContext();
        
        // create operation
        using var operationCom = technologistCom.InvokeAndWrap(technologist =>
            technologist.CreateOperation(typeId, prevOperationId, "", ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        var id = operationCom.Invoke(operation => operation.GetId(ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        // setup tool
        SetupOperationTool(projectCom, id, "75");
        
        // setup job assignment for the operation
        var holes = new[]
        {
            @"Part\Part1.igs\Face21",
            @"Part\Part1.igs\Face4",
            @"Part\Part1.igs\Face18",
            @"Part\Part1.igs\Face19"
        };
        AddHolesToJobAssignment(operationCom, geometryModelCom, holes);
        
        // return result
        return id;
    }
    
    private static void CreateOperationSixth(ComWrapper<ICamIpcProject> projectCom,
        ComWrapper<ICamIpcTechnologist> technologistCom,
        ComWrapper<ICamIpcGeometryModel> geometryModelCom,
        string prevOperationId,
        string typeId)
    {
        var executeContext = new TExecuteContext();
        
        // create operation
        using var operationCom = technologistCom.InvokeAndWrap(technologist =>
            technologist.CreateOperation(typeId, prevOperationId, "", ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        var id = operationCom.Invoke(operation => operation.GetId(ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        // setup tool
        SetupOperationTool(projectCom, id, "63");
        
        // setup job assignment for the operation
        AddHolesToJobAssignment(operationCom, geometryModelCom,
        [@"Part\Part1.igs\Face10", @"Part\Part1.igs\Face2"]);
        
        // setup properties
        using var xmlPropsCom = operationCom.InvokeAndWrap(operation => operation.XmlProp);
        xmlPropsCom.Invoke(xmlProps =>
        {
            xmlProps.Str["DrillingType"] = "ChipRemoving";
        });
    }

    private static void SetupOperationTool(ComWrapper<ICamIpcProject> projectCom,
        string operationId,
        string toolNumber)
    {
        var executeContext = new TExecuteContext();
        projectCom.Invoke(project =>
        {
            project.SetOperationTool(operationId, toolNumber, ref executeContext);
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
        });
    }

    private static void AddHolesToJobAssignment(ComWrapper<ICamIpcTechOperation> operationCom,
        ComWrapper<ICamIpcGeometryModel> geometryModelCom,
        string[] holesIds)
    {
        // select holes
        SelectGeometryItemById(geometryModelCom, holesIds);

        // add holes to the job assignment
        using var mfCom = operationCom.InvokeAndWrap(operation => operation.ModelFormerJobAssignment);
        mfCom.Invoke(mf =>
        {
            if (mf is not ICamIpcModelFormerWithHoles modelFormerWithHoles)
                throw new Exception("Not ICamIpcModelFormerWithHoles");
            using var itemsCom = ComWrapper.Create(modelFormerWithHoles.AddHolesSelected());
            itemsCom.Invoke(items =>
            {
                if (items.Count == 0)
                    throw new Exception("Cannot add holes");
            });
        });
    }

    /// <summary>
    /// Select geometry item by its identifier
    /// </summary>
    private static void SelectGeometryItemById(ComWrapper<ICamIpcGeometryModel> geometryModelCom,
        string[] geomIds)
    {
        var executeContext = new TExecuteContext();
        
        // clear selection
        geometryModelCom.Invoke(model => model.DeselectAll());
        
        // find and select items
        foreach (var id in geomIds)
        {
            using var nodeCom = geometryModelCom.InvokeAndWrap(
                model => model.FindByFullName(id, ref executeContext));
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            nodeCom.Invoke(node => node.Selected = true);
        }
    }
    
    /// <summary>
    /// Generate G code
    /// </summary>
    private static void GenerateGCode(ComWrapper<ICamIpcProject> projectCom,
        ComWrapper<ICamIpcTechnologist> technologistCom,
        ComWrapper<ICamIpcPaths> pathsHelperCom)
    {
        // arrange
        var executeContext = new TExecuteContext();
        using var ncMakerCom = projectCom.InvokeAndWrap(project => project.GetNCMaker(ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        // generate CLData file as first step
        using var operationsCom = technologistCom.InvokeAndWrap(technologist =>
            technologist.GetOperations(TCamApiReorderingMode.rmReordered, ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        var clDataFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        operationsCom.Invoke(operations =>
        {
            clDataFile = Path.ChangeExtension(clDataFile, ".inpcld");
            projectCom.Invoke(project =>
            {
                project.SaveClData(clDataFile, operations, ref executeContext);
                if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                    throw new Exception(executeContext.ResultStatus.Description);
            });
        });
        
        // generate CNC
        using var settingsCom = ncMakerCom.InvokeAndWrap(ncmaker =>{
            var settings = ncmaker.CreateSettings(TCamApiNCMakerSettingsType.ncsSppx, ref executeContext);
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            return settings;
        });
        var resultNcCodeFile = settingsCom.Invoke(s =>
        {
            var sppx = (ICamApiMakeCncSppxSettings)s;
            sppx.OutputFolder = Path.GetTempPath();
            sppx.NcFileName   = OutputFile;
            return Path.Combine(sppx.OutputFolder, sppx.NcFileName);
        });
        
        // get postprocessor from all users documents folder
        var postProcessorFilePath = pathsHelperCom.Invoke(pathsHelper =>
            Path.Combine(pathsHelper.PostprocessorsFolder, "Mill", "Fanuc (30i)_Mill.sppx"));
        if (!File.Exists(postProcessorFilePath))
            throw new Exception("Postprocessor not found: " + postProcessorFilePath);
        
        // generate CNC
        ncMakerCom.Invoke(ncmaker => {
            ncmaker.Generate(clDataFile, postProcessorFilePath, settingsCom.Instance, ref executeContext);
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);

            Process.Start("notepad.exe", resultNcCodeFile);
        });
    }
}