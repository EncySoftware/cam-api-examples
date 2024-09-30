using System;
using System.IO;
using STTypes;
using MachinigToolsImportTypes;
using CAMAPI.MachiningToolsImportHelper;

namespace DIN4000ImportPlugin
{

    public class ToolFromCsvMaker: IDisposable
    {
        string SCInstallFolder;

        public ToolFromCsvMaker(string scInstallFolder)
        {
            SCInstallFolder = Path.TrimEndingDirectorySeparator(scInstallFolder);
        }    

        private IMTI_MachiningToolsImportLibrary LoadImporter() 
        {
            string assemblyPath = SCInstallFolder + @"\Bin64\" + MTIMachiningToolsImportHelper.DllName;
            IMTI_MachiningToolsImportLibrary importer = null;
            if (File.Exists(assemblyPath))
                importer = MTIMachiningToolsImportHelper.CreateImporter(assemblyPath);
            return importer;
        }

        public void Dispose()
        {
            MTIMachiningToolsImportHelper.FinalizeImporter();
        }

        private bool SameText(string s1, string s2)
        {
            return String.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
        }

        public int ImportedToolsCount { get; set; }

        private IMTI_MachiningToolsItem CreateNewTool(IMTI_MachiningToolsImportLibrary importer, ToolRecord rec) {

            IMTI_AxialToolItems tool = null;
            string DINToolType = rec.NSM + "-" + rec.BLD;
            if (SameText(DINToolType, "DIN4000-82-1") || SameText(DINToolType, "DIN4000-82-2")) //End, slotting and milling cutters
            {
                if (rec.G1 != 0)
                {
                    tool = importer.CreateTorusMill();
                    var ctl = (tool as IMTI_TorusMill);
                    ctl.OverallLength = rec.B5;
                    ctl.CuttingDiameter = rec.A1;
                    ctl.WorkingLength = rec.B2;
                    ctl.ShankDiameter = rec.C3;
                    ctl.ShoulderLength = rec.B5 - rec.C4;
                    ctl.ShankTaperAngle = 45;
                    ctl.Radius = rec.G1;
                }
                else
                {
                    tool = importer.CreateCylindricalMill();
                    var ctl = (tool as IMTI_CylindricalMill);
                    ctl.OverallLength = rec.B5;
                    ctl.CuttingDiameter = rec.A1;
                    ctl.WorkingLength = rec.B2;
                    ctl.ShankDiameter = rec.C3;
                    ctl.ShoulderLength = rec.B5 - rec.C4;
                    ctl.ShankTaperAngle = 45;
                }
            }
            else if (SameText(DINToolType, "DIN4000-82-6")) // Ball nose milling cutters
            {
                tool = importer.CreateSpphericalMill();
                var ctl = (tool as IMTI_SphericalMill);
                ctl.OverallLength = rec.B5;
                ctl.CuttingDiameter = rec.A1;
                ctl.WorkingLength = rec.B2;
                ctl.ShankDiameter = rec.C3;
                ctl.ShoulderLength = rec.B5 - rec.C4;
                ctl.ShankTaperAngle = 45;
            }
            else if (SameText(DINToolType, "DIN4000-81-1")) // Drills (standard)
            {
                tool = importer.CreateDrill();
                var ctl = (tool as IMTI_Drill);
                ctl.OverallLength = rec.B5;
                ctl.CuttingDiameter = rec.A11;
                ctl.WorkingLength = rec.B4;
                ctl.ShankDiameter = rec.C3;
                ctl.ShoulderLength = rec.B5 - rec.C4;
                ctl.ShankTaperAngle = 45;
                ctl.Angle = rec.E1;
            }
            else
                return tool;

            tool.SetName(rec.J21);
            // tool.SetIdentifier("m001");
            // tool.SetMagazineNumber(1);
            // tool.SetToolNumber(1);
            tool.SetOverhang(rec.B5);

            return tool;
        }


        public void ImportRecsToDB(IEnumerable<ToolRecord> recs, string resultDBFileName)
        {
            var importer = LoadImporter();
            if (importer==null) 
                return;

            IMTI_MachiningToolsStorage storage = null;
            if (File.Exists(resultDBFileName))
                File.Delete(resultDBFileName);
            if (File.Exists(resultDBFileName))
                storage = importer.OpenExistingToolsStorage(resultDBFileName);
            else
                storage = importer.CreateNewToolsStorage(resultDBFileName);

            foreach (ToolRecord rec in recs) 
            {
                var tool = CreateNewTool(importer, rec);
                if (tool != null)
                {
                    ImportedToolsCount++;
                    storage.AddToolItem(tool);
                }
            }

        }
    }
}