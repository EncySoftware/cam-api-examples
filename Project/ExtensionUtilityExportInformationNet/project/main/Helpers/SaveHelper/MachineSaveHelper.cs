using CAMAPI.Application;
using CAMAPI.CoordinateSystem;
using CAMAPI.DotnetHelper;
using CAMAPI.Extension.PLM;
using CAMAPI.GeomModel;
using CAMAPI.Machine;
using CAMAPI.ModelFormerTypes;
using CAMAPI.PartStage;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;

namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// A helper class for saving machine data.
    /// </summary>
    public static class MachineSaveHelper
    {
        private static JsonBuilder? _jsonBuilder;

        /// <summary>
        /// Initializes the JSON builder for saving machine data.
        /// </summary>
        public static void Initialize(JsonBuilder builder)
        {
            _jsonBuilder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        /// <summary>
        /// Saves the machine data to the JSON builder.
        /// </summary>
        public static void SaveMachineInfoDetails(ComWrapper<ICamApiProject> projectCom)
        {
            using var machineInfoCom = ProjectHelper.MachineInformation(projectCom);
            var machineInfoGUID = MachineInfoHelper.GUID(machineInfoCom);
            var machineInfoCaption = MachineInfoHelper.MachineCaption(machineInfoCom);
            var machineInfoTypeName = MachineInfoHelper.MachineTypeName(machineInfoCom);
            var machineInfoSchemaFilePath = MachineInfoHelper.SchemaFilePath(machineInfoCom);
            var machineInfoXMLNodeName = MachineInfoHelper.XMLNodeName(machineInfoCom);
            
            _jsonBuilder?.BeginObject("MachineInfo");
            _jsonBuilder?.AddStrPair("GUID", machineInfoGUID);
            _jsonBuilder?.AddStrPair("MachineCaption", machineInfoCaption);
            _jsonBuilder?.AddStrPair("MachineTypeName", machineInfoTypeName);  
            _jsonBuilder?.AddStrPair("SchemaFilePath", machineInfoSchemaFilePath);
            _jsonBuilder?.AddStrPair("XMLNodeName", machineInfoXMLNodeName);
            _jsonBuilder?.EndObject();
        }

        /// <summary>
        /// Saves the machine data to the JSON builder.
        /// </summary>
        public static void SaveMachineDetails(ComWrapper<ICamApiMachine> machineCom)
        {
            var machineGUID = MachineHelper.GUID(machineCom);
            var machineCaption = MachineHelper.MachineCaption(machineCom);
            var machineXMLNodeName = MachineHelper.XMLNodeName(machineCom);
            
            _jsonBuilder?.BeginObject("Machine");
            _jsonBuilder?.AddStrPair("GUID", machineGUID);
            _jsonBuilder?.AddStrPair("MachineCaption", machineCaption);
            _jsonBuilder?.AddStrPair("XMLNodeName", machineXMLNodeName);
            _jsonBuilder?.EndObject();
        }

    }    
}