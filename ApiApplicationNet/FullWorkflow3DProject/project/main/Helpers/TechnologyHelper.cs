using System;
using System.IO;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CAMAPI.Application;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMAPI.GeomModel;
using CAMAPI.ModelFormerTypes;
using CAMAPI.NCMaker;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
using CAMAPI.Technologist;
using CAMAPI.TechOperation;
using STTypes;

namespace ApplicationFullWorkflow3DProjectNet;

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
    public static void CreateTechnology(ComWrapper<ICamApiApplication> applicationCom,
        ComWrapper<ICamApiProject> activeProjectCom,
        ComWrapper<ICamApiTechnologist> technologistCom,
        ComWrapper<ICAMAPIGeometryModel> geometryModelCom,
        ComWrapper<ICamApiPaths> pathsHelperCom)
    {
        // switch to model tab
        applicationCom.Invoke(application =>
        {
            application.MainWorkMode = TMainWorkMode.mwmMachining;
        });
        
        // arrange
        using var ncMakerCom = activeProjectCom.InvokeAndWrap(project => project.NCMaker);
        
        // setup workpiece
        SetupStage(technologistCom);
        
        // get root operation id
        using var operationRootCom = technologistCom.InvokeAndWrap(technologist => technologist.RootOperation);
        var operationRootId = operationRootCom.Invoke(operation => operation.Id);
        
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
        
        // // calculate entire technology
        technologistCom.Invoke(technologist =>
        {
            technologist.ResetAllOperationsToolpath();
            technologist.CalculateAllOperationsToolpath(true, out var ret);
            if (ret.Code == TResultStatusCode.rsError)
                throw new Exception(ret.Description);
        });
        
        // // generate CNC
        GenerateGCode(activeProjectCom, technologistCom, pathsHelperCom);
    }
    
    private static void SetupStage(ComWrapper<ICamApiTechnologist> technologistCom)
    {
        using var pslCom = technologistCom.InvokeAndWrap(technologist => technologist.PartAndStageList);
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
    
    private static string CreateOperationFirst(ComWrapper<ICamApiProject> projectCom,
        ComWrapper<ICamApiTechnologist> technologistCom,
        string prevOperationId,
        string typeId)
    {
        // create operation
        using var operationCom = technologistCom.InvokeAndWrap(technologist => 
            (technologist.CreateOperation(typeId, prevOperationId, "", out var status), status));
        var id = operationCom.Invoke(operation => operation.Id);
        
        // setup tool
        SetupOperationTool(projectCom, id, "12");
        
        // return id
        return id;
    }
    
    private static string CreateOperationSecond(ComWrapper<ICamApiProject> projectCom,
        ComWrapper<ICamApiTechnologist> technologistCom,
        ComWrapper<ICAMAPIGeometryModel> geometryModelCom,
        string prevOperationId,
        string typeId)
    {
        // create operation
        using var operationCom = technologistCom.InvokeAndWrap(technologist => 
            (technologist.CreateOperation(typeId, prevOperationId, "", out var status), status));
        var id = operationCom.Invoke(operation => operation.Id);
        
        // setup tool
        SetupOperationTool(projectCom, id, "11");
        
        // setup job assignment for the operation
        SelectGeometryItemById(geometryModelCom, [@"Part\Part1.igs\Edge{Face11,Face7}"]);
        using var mfCom = operationCom.InvokeAndWrap(operation => operation.ModelFormerJobAssignment);
        mfCom.Invoke(mf =>
        {
            if (mf is not ICamApiModelFormerWithZones mfWithZones)
                return;
            using var itemsCom = ComWrapper.Create(mfWithZones.AddRestrictedZoneSelected());
            if (itemsCom.Instance == null || itemsCom.It.Count == 0)
                throw new Exception("Cannot add restricted zone");
        });
        
        return id;
    }
    
    private static string CreateOperationThird(ComWrapper<ICamApiProject> projectCom,
        ComWrapper<ICamApiTechnologist> technologistCom,
        ComWrapper<ICAMAPIGeometryModel> geometryModelCom,
        string prevOperationId,
        string typeId)
    {
        // create operation
        using var operationCom = technologistCom.InvokeAndWrap(technologist => 
            (technologist.CreateOperation(typeId, prevOperationId, "", out var status), status));
        var id = operationCom.Invoke(operation => operation.Id);
        
        // setup tool
        SetupOperationTool(projectCom, id, "11");
        
        // setup job assignment for the operation
        AddHolesToJobAssignment(operationCom, geometryModelCom, [@"Part\Part1.igs\Face11"]);
        
        // setup properties
        using var xmlPropsCom = operationCom.InvokeAndWrap(operation => operation.XMLProp);
        xmlPropsCom.Invoke(xmlProps =>
        {
            xmlProps.Str["DrillingType"] = "HolePocketing";
        });
        
        // return id
        return id;
    }
    
    private static string CreateOperationForth(ComWrapper<ICamApiProject> projectCom,
        ComWrapper<ICamApiTechnologist> technologistCom,
        ComWrapper<ICAMAPIGeometryModel> geometryModelCom,
        string prevOperationId,
        string typeId)
    {
        // create operation
        using var operationCom = technologistCom.InvokeAndWrap(technologist => 
            (technologist.CreateOperation(typeId, prevOperationId, "", out var status), status));
        var id = operationCom.Invoke(operation => operation.Id);
        
        // setup tool, which is noy appropriate for this operation
        SetupOperationTool(projectCom, id, "35");
        
        // setup job assignment for the operation
        using var mfCom = operationCom.InvokeAndWrap(operation => operation.ModelFormerJobAssignment);
        mfCom.Invoke(mf =>
        {
            // add the curve
            SelectGeometryItemById(geometryModelCom, [@"Part\Part1.igs\Face6"]);
            if (mf is not ICamApiModelFormerWithCurve2D modelFormerWithCurve2D)
                return;
            using var curvesCom = ComWrapper.Create(modelFormerWithCurve2D.AddCurves2DSelected());
            if (curvesCom.Instance == null || curvesCom.It.Count == 0)
                throw new Exception("Cannot add curve");
            
            // add bottom level
            SelectGeometryItemById(geometryModelCom, [@"Part\Part1.igs\Face12"]);
            if (mf is not ICamApiModelFormerWithLevels modelFormerWithLevels)
                return;
            using var levelsCom =
                ComWrapper.Create(modelFormerWithLevels.AddLevelSelected(TModelFormerLevelType.amflBottomLevel));
            if (levelsCom.Instance == null || levelsCom.It.Count == 0)
                throw new Exception("Cannot add bottom level");
        });
        
        return id;
    }
    
    private static string CreateOperationFifth(ComWrapper<ICamApiProject> projectCom,
        ComWrapper<ICamApiTechnologist> technologistCom,
        ComWrapper<ICAMAPIGeometryModel> geometryModelCom,
        string prevOperationId,
        string typeId)
    {
        // create operation
        using var operationCom = technologistCom.InvokeAndWrap(technologist => 
            (technologist.CreateOperation(typeId, prevOperationId, "", out var status), status));
        var id = operationCom.Invoke(operation => operation.Id);
        
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
    
    private static void CreateOperationSixth(ComWrapper<ICamApiProject> projectCom,
        ComWrapper<ICamApiTechnologist> technologistCom,
        ComWrapper<ICAMAPIGeometryModel> geometryModelCom,
        string prevOperationId,
        string typeId)
    {
        // create operation
        using var operationCom = technologistCom.InvokeAndWrap(technologist => 
            (technologist.CreateOperation(typeId, prevOperationId, "", out var status), status));
        var id = operationCom.Invoke(operation => operation.Id);
        
        // setup tool
        SetupOperationTool(projectCom, id, "63");
        
        // setup job assignment for the operation
        AddHolesToJobAssignment(operationCom, geometryModelCom,
        [@"Part\Part1.igs\Face10", @"Part\Part1.igs\Face2"]);
        
        // setup properties
        using var xmlPropsCom = operationCom.InvokeAndWrap(operation => operation.XMLProp);
        xmlPropsCom.Invoke(xmlProps =>
        {
            xmlProps.Str["DrillingType"] = "ChipRemoving";
        });
    }

    private static void SetupOperationTool(ComWrapper<ICamApiProject> projectCom,
        string operationId,
        string toolNumber)
    {
        projectCom.Invoke(project =>
        {
            project.SetOperationTool(operationId, toolNumber, out var ret);
            if (ret.Code == TResultStatusCode.rsError)
                throw new Exception(ret.Description);
        });
    }

    private static void AddHolesToJobAssignment(ComWrapper<ICamApiTechOperation> operationCom,
        ComWrapper<ICAMAPIGeometryModel> geometryModelCom,
        string[] holesIds)
    {
        // select holes
        SelectGeometryItemById(geometryModelCom, holesIds);
        
        // add holes to the job assignment
        using var mfCom = operationCom.InvokeAndWrap(operation => operation.ModelFormerJobAssignment);
        mfCom.Invoke(mf =>
        {
            if (mf is not ICamApiModelFormerWithHoles modelFormerWithHoles)
                return;
            using var itemsCom = ComWrapper.Create(modelFormerWithHoles.AddHolesSelected());
            if (itemsCom.Instance == null || itemsCom.It.Count == 0)
                throw new Exception("Cannot add holes");
        });
    }

    /// <summary>
    /// Select geometry item by its identifier
    /// </summary>
    private static void SelectGeometryItemById(ComWrapper<ICAMAPIGeometryModel> geometryModelCom, string[] geomIds)
    {
        // clear selection
        geometryModelCom.Invoke(model => model.DeselectAll());
        
        // find and select items
        foreach (var id in geomIds)
        {
            using var nodeCom = geometryModelCom.InvokeAndWrap(
                model => (model.FindByFullName(id, out var status), status));
            if (nodeCom.Instance == null)
                throw new Exception($"Cannot find geometry item by id: {id}");
            nodeCom.Invoke(node => node.Selected = true);
        }
    }
    
    /// <summary>
    /// Generate G code
    /// </summary>
    private static void GenerateGCode(ComWrapper<ICamApiProject> projectCom,
        ComWrapper<ICamApiTechnologist> technologistCom,
        ComWrapper<ICamApiPaths> pathsHelperCom)
    {
        // arrange
        using var ncMakerCom = projectCom.InvokeAndWrap(project => project.NCMaker);
        
        // generate CLData file as first step
        using var operationCom = technologistCom.InvokeAndWrap(technologist =>
            (technologist.GetOperations(TCamApiReorderingMode.rmReordered, out var status), status));
        var operationIterator = operationCom.Instance;
        
        var clDataFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        clDataFile = Path.ChangeExtension(clDataFile, ".inpcld");
        projectCom.Invoke(project =>
        {
            project.SaveClData(clDataFile, operationIterator, out var ret);
            if (ret.Code == TResultStatusCode.rsError)
                throw new Exception("Error saving CLData: " + ret.Description);
        });
        
        // make settings for CNC generating
        using var settingsCom = ncMakerCom.InvokeAndWrap(ncMaker =>
            (ncMaker.CreateSettings(TCamApiNCMakerSettingsType.ncsSppx, out var status), status));
        using var settingsSppxCom = settingsCom.InvokeAndWrap(settings =>
            settings as ICamApiMakeCncSppxSettings
            ?? throw new Exception("Error creating settings: settings is not ICamApiMakeCncSppxSettings"));
        var resultNcCodeFile = settingsSppxCom.Invoke(settings =>
        {
            settings.OutputFolder = Path.GetTempPath();
            settings.NcFileName = OutputFile;
            return Path.Combine(settings.OutputFolder, settings.NcFileName);
        });
        // get postprocessor from all users documents folder
        var postProcessorFilePath = pathsHelperCom.Invoke(pathsHelper =>
            Path.Combine(pathsHelper.PostprocessorsFolder, "Mill", "Fanuc (30i)_Mill.sppx"));
        if (!File.Exists(postProcessorFilePath))
            throw new Exception("Postprocessor not found: " + postProcessorFilePath);
        
        // // generate CNC
        ncMakerCom.Invoke(ncMaker =>
        {
            using var listCom = ComWrapper.Create(ncMaker.Generate(clDataFile, postProcessorFilePath, settingsCom.Instance, out var ret));
            if (ret.Code == TResultStatusCode.rsError)
                throw new Exception("Error generating CNC: " + ret.Description);
            
            Process.Start("notepad.exe", resultNcCodeFile);
        });
    }
}