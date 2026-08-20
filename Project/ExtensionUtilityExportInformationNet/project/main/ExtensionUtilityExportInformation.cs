using System;
using System.IO;
using CAMAPI.Application;
using CAMAPI.CoordinateSystem;
using CAMAPI.CustomAttributes;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.Extension.PLM;
using CAMAPI.GeomModel;
using CAMAPI.Machine;
using CAMAPI.ModelFormerTypes;
using CAMAPI.PartStage;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;
using System.Diagnostics;
using STTypes;



namespace ExtensionUtilityExportInformationNet;

/// <summary>
/// Extension to demonstrate entry point "utility" 
/// </summary>
public class ExtensionUtilityExportInformation : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    private JsonBuilder? jsonBuilder;

    /// <summary>
    /// Utility to create copy of current project in another folder
    /// </summary>
    /// <param name="context">Information about current running instance</param>
    /// <param name="resultStatus">Structure to return error</param>
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        
        try
        {
            using var contextCom = new ComWrapper<IExtensionUtilityContext>(context);
            using var classFactoryCom = SystemExtensionFactory.GetSingletonExtension<ICamApiClassFactory>("Extension.Global.ClassFactory")
                                  ?? throw new Exception("Class factory is failed");
           
            var classFactory = classFactoryCom.Instance
                                  ?? throw new NullReferenceException("Failed to create application singleton object");


            if (classFactory.CreateObjectOfClass("TFacesToTriangulatedFilesConverter") is not ICamApiFacesToTriangulatedFilesConverter converter)
                return;
            using var converterCom = new ComWrapper<ICamApiFacesToTriangulatedFilesConverter>(converter);
            

            jsonBuilder = new JsonBuilder();
            PartSetupSaveHelper.Initialize(jsonBuilder);
            ProjectSaveHelper.Initialize(jsonBuilder);
            CopyPartSaveHelper.Initialize(jsonBuilder);
            OriginalPartSaveHelper.Initialize(jsonBuilder);
            OperationSaveHelper.Initialize(jsonBuilder);
            ToolpathSaveHelper.Initialize(jsonBuilder);
            MachineSaveHelper.Initialize(jsonBuilder);
            ToolSaveHelper.Initialize(jsonBuilder);
            ScreenshotSaveHelper.Initialize(jsonBuilder);
            

            using var applicationCom = ComWrapper.Create(context.CamApplication);
            using var projectCom = ApplicationHelper.GetActiveProject(applicationCom);

            jsonBuilder.BeginObject();
            jsonBuilder.BeginObject("CAMProject");
            ProjectSaveHelper.SaveProjectDetails(projectCom);
            
            jsonBuilder.BeginObject("MachineSetup");

            
            using var technologistCom = ProjectHelper.Technologist(projectCom);
            using var pslCom = TechnologistHelper.PartAndStageList(technologistCom);
            using var listCoordinateSystemCom = ProjectHelper.CoordinateSystems(projectCom);


            using var machineCom = ProjectHelper.Machine(projectCom);
            using var evaluatorCom = MachineHelper.CreateEvaluator(machineCom);

            MachineSaveHelper.SaveMachineInfoDetails(projectCom);
            MachineSaveHelper.SaveMachineDetails(machineCom);

            int SetupStagesCount = PartAndStageListHelper.SetupStagesCount(pslCom);
            int PartsCount = PartAndStageListHelper.PartsCount(pslCom);


            jsonBuilder.BeginArray("SetupStagesList");
            
            
            for (var setupStageIdx = 0; setupStageIdx < SetupStagesCount; setupStageIdx++){
                jsonBuilder.BeginObject();
                jsonBuilder.AddIntPair("SetupStageIndex", setupStageIdx);

                jsonBuilder.BeginArray("PartStageList");
                for (var partIdx = 0; partIdx < PartsCount; partIdx++){
                    jsonBuilder.BeginObject();
                    jsonBuilder.AddIntPair("PartIndex", partIdx); 
                    
                
                    using var partStageCom = PartAndStageListHelper.GetPartStage(pslCom, partIdx, setupStageIdx);
                    using var partCom = PartAndStageListHelper.Part(pslCom, partIdx);

                    var prototypePartIndex = PartHelper.PrototypePartIndex(partCom);
                    var PartExternalID = PartHelper.ExternalID(partCom); 
                    jsonBuilder.AddBoolPair("IsCopy", PartHelper.IsPartCopy(partCom));
                    jsonBuilder.AddIntPair("PrototypePartIndex", prototypePartIndex); 
                    jsonBuilder.AddIntPair("PartExternalID", PartExternalID); 
                    
                    var key = new PartKey(setupStageIdx, partIdx);
                    if (PartHelper.IsPartCopy(partCom)){
                        bool isOriginalFoundInPGStore = false;

                        for (int i = setupStageIdx; i >= 0; i--){
                            var searchKey = new PartKey(i, prototypePartIndex);
                            if (PartGeometryStore.Parts.TryGetValue(searchKey, out var pg)){
                                CopyPartSaveHelper.SaveCopyPartData(pg); 
                                isOriginalFoundInPGStore = true;
                                break;
                            }
                        }
                        if (!isOriginalFoundInPGStore){
                            OriginalPartSaveHelper.SaveAndWritePartGeometryData(converterCom, partStageCom, evaluatorCom, listCoordinateSystemCom, applicationCom, key);
                        }
                    }else{
                        OriginalPartSaveHelper.SaveAndWritePartGeometryData(converterCom, partStageCom, evaluatorCom, listCoordinateSystemCom, applicationCom, key);  
                    }

                    //using var anotherPartStageCom = PartAndStageListHelper.GetPartStage(pslCom, partIdx, setupStageIdx);
                    PartSetupSaveHelper.SavePartSetupData(partStageCom, machineCom, evaluatorCom);
                                        
                    jsonBuilder.EndObject(); // PartStage item closing    
                }
                jsonBuilder.EndArray(); // PartStageList array closing
                jsonBuilder.EndObject(); // SetupStage item closing
            }    
            

            jsonBuilder.EndArray(); // SetupStagesList closing
            jsonBuilder.EndObject(); // MachineSetup closing

            OperationSaveHelper.SaveOperationsData(technologistCom, evaluatorCom);

            ToolSaveHelper.SaveToolDetails(projectCom);
            
            string jsonFullPath = Path.Combine(ExportOutputPaths.Root, "test.json");
            ScreenshotSaveHelper.SaveScreenshots(projectCom, Path.GetDirectoryName(jsonFullPath)!);

            jsonBuilder.EndObject(); // CAMProject closing
            jsonBuilder.EndObject(); // json closing

            string json = jsonBuilder.GetJsonString(pretty: true);
            File.WriteAllText(jsonFullPath, json);
            ViewerLauncher.Show(jsonFullPath, ExportOutputPaths.Root, applicationCom);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
        
    }
}