using CAMHelper.NativeLibUtils;
using Geometry.VecMatrLib;
using STGeomApiTypes;

namespace AddinImportObjNet;

/// <summary>
/// Addin to import .obj files into CAM geometry
/// </summary>
public static class Program
{
    private delegate IntPtr GetGeomFilerDelegate();

    /// <summary>
    /// Using STGeomFile.dll, this addin imports OBJ files into CAM geometry.
    /// </summary>
    public static void Main(string [] args)
    {
        // read params
        if (args.Length < 1)
            throw new Exception("Usage: AddinImportObjNet <in-file> <out-file>");
        var objFile = args[0];
        var sgfFilePath = args.Length > 1 ? args[1] : "temp.sgf";
            
        // connect STGeomFile.dll
        var dllPath = Path.Combine(@"C:\Program Files\ENCY Software\ENCY 2\Bin64", "STGeomFile.dll");
        if (!File.Exists(dllPath))
            throw new Exception($"{dllPath} not found");
        if (!File.Exists(dllPath))
            throw new Exception("STGeomFile.dll not found: " + dllPath);
        var geomFile = NativeLibLoader.CreateComObject<ISTGeomFiler, GetGeomFilerDelegate>(dllPath,
                           "CreateGeomFiler", out var objectPointer, out var dllPointer)
                       ?? throw new Exception("Error creating geomFiler");
        try
        {
            Import(geomFile, objFile, sgfFilePath);
        }
        finally
        {
            NativeLibLoader.FreeDll(geomFile, objectPointer, dllPointer);
        }

    }
    
    private static bool Import(ISTGeomFiler geomFile, string inputFile, string outputFile)
    {
        bool succeed;
        if (File.Exists(outputFile))
            File.Delete(outputFile);
        
        // try to read file first
        var objReader = new ObjReader(inputFile);
        
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
                geomFile.StartGroupEntity("obj_entities");
                try
                {
                    objReader.BuildModel(geomFile);
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
