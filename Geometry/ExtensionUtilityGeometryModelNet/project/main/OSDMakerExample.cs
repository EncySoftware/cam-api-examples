using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
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

/// <summary>
/// Utility to export geometry node to .osd file
/// </summary>
public class OSDMakerExample : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext Context, out TResultStatus resultStatus)
    {        
        resultStatus = default;
        string osdMakerPath = @"C:/Program Files/ENCY Software/ENCY/Bin64/OSDMaker.dll";
        IntPtr osdMakerHandle = IntPtr.Zero;

        try
        {
            osdMakerHandle = NativeLibLoader.LoadDll(osdMakerPath);
            if (osdMakerHandle == IntPtr.Zero)
            {              
                resultStatus.Code = TResultStatusCode.rsError;
                resultStatus.Description = "Loading OSDMaker.dll is failed.";
                return;
            }
            
            var CreateLibPointer = NativeLibLoader.GetProc<CreateGeometryLibraryPointer>(osdMakerHandle,
                "CreateCAMAPIGeometryLibrary");
            var libPtr = CreateLibPointer();

            using var geomLib = new ApiComObject<ICAMAPIGeomLibrary>((ICAMAPIGeomLibrary)Marshal.GetTypedObjectForIUnknown(libPtr, typeof(ICAMAPIGeomLibrary)));
            
            if (resultStatus.Code == TResultStatusCode.rsSuccess)
            {                
                var importFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                        @"ENCY\Version 1\Models\Milling_3D\49-1.igs");
                var exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CAMAPI Geometry examples");
                using var fullModel = new ApiComObject<ICAMAPIGeometryModel>(geomLib.Instance.CreateGeometryModel(out resultStatus));
                using var importer = new ApiComObject<ICAMAPIGeometryImporter>(geomLib.Instance.CreateGeometryImporter(out resultStatus));

                importer.Instance.GeometryModel = fullModel.Instance;

                importer.Instance.ImportFile(importFileName, "", false);
                
                if(!Directory.Exists(exportDir))
                    Directory.CreateDirectory(exportDir);
                fullModel.Instance.ExportSelectedToOSD(Path.Combine(exportDir, "ExportedModelExample.osd"), out resultStatus);

                if (!(resultStatus.Code == TResultStatusCode.rsSuccess))
                    throw new Exception(resultStatus.Description);
            }
        } finally {
            if (osdMakerHandle != IntPtr.Zero)
            {              
                NativeLibLoader.FreeDll(osdMakerHandle);
            }
        }
    }
}
