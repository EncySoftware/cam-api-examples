using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
using CAMAPI.UIDialogs.DotnetHelper;
using Geometry.VecMatrLib;
using STGeomApiTypes;
using STTypes;

namespace ExtensionUtilityImportSvgNet;

/// <summary>
/// Utility to import curves
/// </summary>
public class ExtensionImportSvg: IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }
    
    /// <summary>
    /// Add test
    /// </summary>
    private static void ReadSvg(string svgFile, ISTGeomFiler geomFile)
    {
        using var extensionsManagerCom = ExtensionManagerHelper.GetInstance();
        var converter = extensionsManagerCom.Invoke(extensionManager => new SvgToCamConverter(extensionManager.Logger));
        
        var isFirst = true;
        TST3DPoint firstPoint = default;
        var id = "";

        var callbacks = new SvgReaderCallbacks
        {
            OnMoveTo = (entityId, p) =>
            {
                firstPoint = p;
                id = entityId;
                isFirst = false;
                geomFile.StartCurve3d(id, p);
            },

            OnLineTo = p =>
            {
                if (isFirst)
                    throw new Exception("First point is not set (by OnModeTo)");
                geomFile.CutTo3d(p);
            },
            
            OnCircleTo = (entityId, radius, centerX, centerY) =>
            {
                var vT = new TST3DPoint
                {
                    X = centerX,
                    Y = centerY,
                    Z = 0
                };
                var vZ = new TST3DPoint
                {
                    X = 0,
                    Y = 0,
                    Z = 1
                };
                var vX = new TST3DPoint
                {
                    X = 1,
                    Y = 0,
                    Z = 0
                };
                geomFile.CreateCircle(entityId, radius, vT, vZ, vX);
                geomFile.AddEntity(entityId, $"svg({entityId})");
            },
            
            OnEllipseTo = (entityId, majRadius, minRadius, centerX, centerY, rotationDegrees ) =>
            {
                var vT = new TST3DPoint
                {
                    X = centerX,
                    Y = centerY,
                    Z = 0
                };
                var vZ = new TST3DPoint
                {
                    X = 0,
                    Y = 0,
                    Z = 1
                };
                var angleRad = rotationDegrees * Math.PI / 180.0;
                var vX = new TST3DPoint
                {
                    X = Math.Cos(angleRad),
                    Y = Math.Sin(angleRad),
                    Z = 0
                };
                geomFile.CreateEllipse(entityId, majRadius, minRadius, vT, vZ, vX);
                geomFile.AddEntity(entityId, $"svg({entityId})");
            },
            
            OnSetLineColor = color =>
            {
                geomFile.SetCurrentColor(color);
            },
            
            OnSetLineWidth = width =>
            {
                geomFile.SetCurrentLineWidth(width);
            },

            OnClosePath = closeCurve =>
            {
                geomFile.CutTo3d(firstPoint);
                if (closeCurve)
                    geomFile.CloseCurve3d(true);
                geomFile.AddEntity(id, $"svg({id})");
                isFirst = false;
            }
        };
        converter.ImportSvg(svgFile, callbacks);
    }
    
    private bool CreateSvgFile(string inputFile, string outputFile, ISTGeomFiler geomFile)
    {
        bool succeed;
        if (File.Exists(outputFile))
            File.Delete(outputFile);
        
        // beginning of the file
        if (!geomFile.StartFile(outputFile))
            throw new Exception("Can't start file: " + outputFile);
        try
        {
            try
            {
                // set point, we are going to use it as a coordinate system
                geomFile.SetCurrentTransform(T3DMatrix.Unit.vT, T3DMatrix.Unit.vZ, T3DMatrix.Unit.vX);
                    
                // item in geometry objects tree
                geomFile.StartGroupEntity("svg_entities");
                try
                {
                    ReadSvg(inputFile, geomFile);
                    succeed = true;
                }
                finally
                {
                    geomFile.CloseGroupEntity();
                }
            }
            finally
            {
                geomFile.CloseModel();
            }
        }
        finally
        {
            geomFile.CloseFile();
        }
        return succeed;
    }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            // ask svg-file to be imported
            using var dialogsHelperCom = UIDialogs.CreateHelper();
            var svgFile = dialogsHelperCom.Invoke(dialogsHelper => dialogsHelper.SelectFileDialog(
                "Select SVG file",
                "SVG files (*.svg)|*.svg|All files (*.*)|*.*",
                ""));
            
            // create sgf-file
            var sgfFilePath = Path.Combine(Directory.GetCurrentDirectory(), "example_curves.sgf");
            using var wrapperFactoryGeomFiler = SystemExtensionFactory.GetSingletonExtension<ICamApiFactoryGeometryFile>("Extension.Global.Singletons.GeomFile");
            using var wrapperGeomFile = wrapperFactoryGeomFiler.InvokeAndWrap(factory => factory.CreateObject());
            var succeed = wrapperGeomFile.Invoke(geomFile => CreateSvgFile(svgFile, sgfFilePath, geomFile));
            if (!succeed)
                throw new Exception("Can't create sgf file: " + sgfFilePath);
            
            // import temporary sgf-file
            if (File.Exists(sgfFilePath))
                Import(context, sgfFilePath);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
    
    private static void Import(IExtensionUtilityContext context, string sgfFilePath)
    {
        // active project
        using var applicationCom = ComWrapper.Create(context.CamApplication);
        using var activeProjectCom = applicationCom.InvokeAndWrap(application => 
            (application.GetActiveProject(out var status), status));
        
        // import the file
        activeProjectCom.Invoke(activeProject =>
        {
            using var geomImporterCom = ComWrapper.Create(activeProject.GeomImporter);
            geomImporterCom.Invoke(importer => 
                importer.ImportFile(sgfFilePath, "", true));
        });
    }
}
