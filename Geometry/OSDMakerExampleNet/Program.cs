using CAMAPI.DotnetHelper;
using CAMAPI.GeomLibrary;
using CAMAPI.GeomModel;
using CAMAPI.GeomImporter;
using CAMAPI.ResultStatus;
using CAMHelper.NativeLibUtils;
using System.Runtime.InteropServices;

namespace ExtensionUtilityGeometryModelNet;

/// <summary>
/// Export function delegate
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.StdCall)]
public delegate IntPtr CreateGeometryLibraryPointer();

public class Program
{
    private static string binCAMDir = @"C:/Program Files/ENCY Software/ENCY/Bin64";
    private static string modelName = @"Milling_3D/49-1.igs";

    static void Main(string[] args)
    {        
        TResultStatus resultStatus = default;
        IntPtr osdMakerHandle = IntPtr.Zero;

        try
        {
            if (args.Length>0)
                modelName = args[0];

            Environment.CurrentDirectory = binCAMDir;            

            var osdMakerPath = Path.Combine(binCAMDir, "OSDMaker.dll");   
            var importFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                    @"ENCY\Version 1\Models", modelName);
            var exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CAMAPI Geometry examples");

            osdMakerHandle = NativeLibLoader.LoadDll(osdMakerPath);
            if (osdMakerHandle == IntPtr.Zero)
            {              
                Console.WriteLine("Loading OSDMaker.dll is failed.");
                return;
            }
            
            var CreateLibPointer = NativeLibLoader.GetProc<CreateGeometryLibraryPointer>(osdMakerHandle,
                "CreateCAMAPIGeometryLibrary");
            var libPtr = CreateLibPointer();

            using var geomLib = new ApiComObject<ICAMAPIGeomLibrary>((ICAMAPIGeomLibrary)Marshal.GetTypedObjectForIUnknown(libPtr, typeof(ICAMAPIGeomLibrary)));
            using var fullModel = new ApiComObject<ICAMAPIGeometryModel>(geomLib.Instance.CreateGeometryModel(out resultStatus));
            using var importer = new ApiComObject<ICAMAPIGeometryImporter>(geomLib.Instance.CreateGeometryImporter(out resultStatus));

            importer.Instance.GeometryModel = fullModel.Instance;

            importer.Instance.ImportFile(importFileName, "", false);
            
            if(!Directory.Exists(exportDir))
                Directory.CreateDirectory(exportDir);
            fullModel.Instance.ExportSelectedToOSD(Path.Combine(exportDir, "ExportedModelExample.osd"), out resultStatus);

            if (!(resultStatus.Code == TResultStatusCode.rsSuccess))
                throw new Exception(resultStatus.Description);            
        } finally {
            if (osdMakerHandle != IntPtr.Zero)
            {              
                NativeLibLoader.FreeDll(osdMakerHandle);
            }
        }
    }
}