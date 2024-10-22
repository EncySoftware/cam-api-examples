using MachinigToolsImportTypes;
using CAMAPI.MachiningToolsImportHelper;
using CAMAPI.DotnetHelper;
using CAMAPI.GeomLibrary;
using CAMAPI.GeomModel;
using CAMAPI.GeomImporter;
using CAMAPI.ResultStatus;
using CAMHelper.NativeLibUtils;
using System.Runtime.InteropServices;

namespace STConsoleApp
{
    partial class Program
    {

        private static void CreateNew3DTool() {
            var importer = LoadImporter();
            if (importer==null) return;

            var storage = importer.OpenExistingToolsStorage(_myToolStorageFilePath);

            var tool = importer.CreateDrill();
            tool.OverallLength = 84;
            tool.CuttingDiameter = 5;
            tool.WorkingLength = 30;
            tool.ShankDiameter = 6;
            tool.ShoulderLength = 48;
            tool.ShankTaperAngle = 45;
            tool.Angle = 135;
            tool.SetOverhang(85);

            tool.SetName("My 3D drill");
            tool.SetIdentifier("Drill10");
            tool.SetTeethsCount(3);
            tool.SetMagazineNumber(1);
            tool.SetToolNumber(10);
            tool.SetUnits(TMTI_LinearUnits.luMillimeter);
            tool.SetDurability(70);
            tool.SetMaxPlungeAngle(90);

            if (tool is IMTI_MachiningToolCADModelAdapter adapter)
            {
                string stepFileName = Path.GetDirectoryName(_myToolStorageFilePath) + @"/../201714663_mod_0_4~tm01_00.stp";
                string osdFileName = Path.GetDirectoryName(_myToolStorageFilePath) + @"/201714663_mod_0_4~tm01_00.osd";
                if (Make3DModelByFile(stepFileName, osdFileName))
                {
                    adapter.CADModelFileName = osdFileName;
                    storage.AddToolItem(tool);
                    Console.WriteLine("3D tool Ok!");
                }
            }

        }

        /// <summary>
        /// Export function delegate
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr CreateGeometryLibraryPointer(); 

        private static bool Make3DModelByFile(string modelFileName, string osdFileName)
        {
            var result = false;

            IntPtr osdMakerHandle = IntPtr.Zero;
            try
            {
                string osdMakerPath = _binCAMDir + @"/OSDMaker.dll";
                osdMakerHandle = CAMHelper.NativeLibUtils.NativeLibLoader.LoadDll(osdMakerPath);
                if (osdMakerHandle == IntPtr.Zero)
                    return result;
                
                var CreateLibPointer = CAMHelper.NativeLibUtils.NativeLibLoader.GetProc<CreateGeometryLibraryPointer>(osdMakerHandle,
                    "CreateCAMAPIGeometryLibrary");
                var libPtr = CreateLibPointer();

                using var geomLib = new ApiComObject<ICAMAPIGeomLibrary>((ICAMAPIGeomLibrary)Marshal.GetTypedObjectForIUnknown(libPtr, typeof(ICAMAPIGeomLibrary)));

                using var fullModel = new ApiComObject<ICAMAPIGeometryModel>(geomLib.Instance.CreateGeometryModel(out TResultStatus resultStatus));
                using var importer = new ApiComObject<ICAMAPIGeometryImporter>(geomLib.Instance.CreateGeometryImporter(out resultStatus));

                importer.Instance.GeometryModel = fullModel.Instance;

                importer.Instance.ImportFile(modelFileName, "", false);
                
                if (File.Exists(osdFileName))
                    File.Delete(osdFileName);

                var exportDir = Path.GetDirectoryName(osdFileName);
                if(!Directory.Exists(exportDir))
                    Directory.CreateDirectory(exportDir);
                fullModel.Instance.ExportSelectedToOSD(osdFileName, out resultStatus);

                if (!(resultStatus.Code == TResultStatusCode.rsSuccess))
                    throw new Exception(resultStatus.Description);
                
                result = true;
            } finally {
                if (osdMakerHandle != IntPtr.Zero)
                {              
                    CAMHelper.NativeLibUtils.NativeLibLoader.FreeDll(osdMakerHandle);
                }
            }
            return result;
        }

    }
}
