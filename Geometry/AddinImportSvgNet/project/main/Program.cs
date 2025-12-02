using CAMHelper.NativeLibUtils;
using Geometry.VecMatrLib;
using STGeomApiTypes;
using STTypes;

namespace AddinImportSvgNet;

/// <summary>
/// Addin to import .svg files into CAM geometry
/// </summary>
public static class Program
{
    private delegate IntPtr GetGeomFilerDelegate();

    /// <summary>
    /// Using STGeomFile.dll, this addin imports SVG files into CAM geometry.
    /// </summary>
    public static void Main(string [] args)
    {
        // read params
        if (args.Length < 1)
            throw new Exception("Usage: AddinImportSvgNet <in-file> <out-file>");
        var svgFile = args[0];
        var sgfFilePath = args.Length > 1 ? args[1] : "temp.sgf";
            
        // connect STGeomFile.dll
        var currentFolder = @"C:/Program Files/ENCY Software/ENCY 2/Bin64";
        var dllPath = Path.Combine(currentFolder, "STGeomFile.dll");
        if (!File.Exists(dllPath))
            throw new Exception($"{dllPath} not found");
        if (!File.Exists(dllPath))
            throw new Exception("STGeomFile.dll not found: " + dllPath);
        var geomFile = NativeLibLoader.CreateComObject<ISTGeomFiler, GetGeomFilerDelegate>(dllPath,
                           "CreateGeomFiler", out var objectPointer, out var dllPointer)
                       ?? throw new Exception("Error creating geomFiler");
        try
        {
            Import(geomFile, svgFile, sgfFilePath);
        }
        finally
        {
            NativeLibLoader.FreeDll(geomFile, objectPointer, dllPointer);
        }

    }
    
    private static void ReadSvg(string svgFile, ISTGeomFiler geomFile)
    {
        var converter = new SvgToCamConverter();
        
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

    private static bool Import(ISTGeomFiler geomFile, string inputFile, string outputFile)
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
}
