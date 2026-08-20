using System;
using System.IO;
using CAMAPI.Application;
using CAMAPI.CoordinateSystem;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.Extension.PLM;
using CAMAPI.GeomModel;
using CAMAPI.Machine;
using CAMAPI.ModelFormerTypes;
using CAMAPI.PartStage;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;
using STTypes;
using Geometry.VecMatrLib;

namespace ExtensionUtilityExportInformationNet;

/// <summary>
/// Extension to demonstrate entry point "utility" 
/// </summary>
public class ImportExportedInformation : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

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
            

            using var applicationCom = ComWrapper.Create(context.CamApplication);
            using var projectCom = applicationCom.InvokeAndWrap(application => 
                (application.GetActiveProject(out var status), status)) ?? throw new Exception("Active project is not found");
            using var geomImporterCom = projectCom.InvokeAndWrap(project => project.GeomImporter);
            using var fullModelCom = projectCom.InvokeAndWrap(prj => prj.CAMAPIGeomModel);
            
                    

            var parser = new SimpleJsonProjectParser();
            var currentDirectory = Directory.GetCurrentDirectory();
            string relativePath = Path.Combine(currentDirectory, "test.json");
            parser.Load(relativePath);

            string fileName = "";
            T3DMatrix geomMatrix = T3DMatrix.Zero;
            T3DMatrix offsetMatrix = T3DMatrix.Zero;
            T3DMatrix setupMatrix = T3DMatrix.Zero;

            foreach (var group in parser.CAMProject.SetupStageList){
                foreach(var part in group.PartList){
                    fileName = part.GeometryFileName;
                    geomMatrix = part.GeometryMatrix;
                    offsetMatrix = part.OffsetMatrix;
                    setupMatrix = part.SetupMatrix;
                    
                    using var nodeIterator = fullModelCom.InvokeAndWrap(fm=> fm.GetNodes(out var ret));
                    var nodeFullName = fullModelCom.Invoke(fullModel => fullModel.ActiveNode.FullName);
                    //var tes = nodeIterator.Instance.Current().FullName;
                    if (nodeIterator.Instance.MoveToChild()){
                        fullModelCom.Invoke(model => {
                            model.ActiveNode = nodeIterator.Instance.Current();
                        });
                    }
                                            
                    
                    Console.WriteLine(String.Concat(fileName,nodeFullName));
                    geomImporterCom.Invoke(importer =>
                    {
                        var resultStatus = importer.ImportFile(fileName, @"", true);
                        if (resultStatus.Code == TResultStatusCode.rsError)
                            throw new Exception("Can't import file: " + resultStatus.Description);
                    });

                    //using var fullModelCom = projectCom.InvokeAndWrap(prj => prj.CAMAPIGeomModel);
                    nodeFullName = fullModelCom.Invoke(fullModel => fullModel.ActiveNode.FullName);
                    using var geomNodeCom = fullModelCom.InvokeAndWrap(
                        fullModel => fullModel.FindByFullName(nodeFullName, out var ret));

                    
                    T3DMatrix finalMatrix = geomMatrix * offsetMatrix * setupMatrix;
                    
                    fullModelCom.Invoke(fullModel => 
                    {
                        var ret = fullModel.Transform(geomNodeCom.Instance, finalMatrix);
                        if (ret.Code == TResultStatusCode.rsError)
                            throw new Exception("Couldn`t transform model: " + ret.Description);
                    });
                }
            }

            
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
        
    }
}