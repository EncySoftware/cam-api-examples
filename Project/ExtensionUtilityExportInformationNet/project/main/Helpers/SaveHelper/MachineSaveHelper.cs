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
            using var machineInfoCom = projectCom.MachineInformation();
            var machineInfoGUID = machineInfoCom.GUID();
            var machineInfoCaption = machineInfoCom.MachineCaption();
            var machineInfoTypeName = machineInfoCom.MachineTypeName();
            var machineInfoSchemaFilePath = machineInfoCom.SchemaFilePath();
            var machineInfoXMLNodeName = machineInfoCom.XMLNodeName();
            
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
            var machineGUID = machineCom.GUID();
            var machineCaption = machineCom.MachineCaption();
            var machineXMLNodeName = machineCom.XMLNodeName();
            
            _jsonBuilder?.BeginObject("Machine");
            _jsonBuilder?.AddStrPair("GUID", machineGUID);
            _jsonBuilder?.AddStrPair("MachineCaption", machineCaption);
            _jsonBuilder?.AddStrPair("XMLNodeName", machineXMLNodeName);
            _jsonBuilder?.EndObject();
        }

    }    
}