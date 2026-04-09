using CAMAPI.DotnetHelper;
using CAMAPI.Machine;
using CAMAPI.PartStage;

namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// A helper class for saving part setup data.
    /// </summary>
    public static class PartSetupSaveHelper
    {
        private static JsonBuilder? _jsonBuilder;

        /// <summary>
        /// Initializes the JSON builder for saving part data.
        /// </summary>
        public static void Initialize(JsonBuilder builder)
        {
            _jsonBuilder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        /// <summary>
        /// Saves the part setup data to the JSON builder.
        /// </summary>
        public static void SavePartSetupData(
            ComWrapper<ICamApiPartStage> partStageCom,
            ComWrapper<ICamApiMachine> machineCom,
            ComWrapper<ICamApiMachineEvaluator> evaluatorCom)
        {
            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");

            _jsonBuilder.BeginObject("PartSetup");

            if (partStageCom.IsNull || machineCom.IsNull || evaluatorCom.IsNull){
                _jsonBuilder.EndObject(); // PartSetup force closing
                return;
            }
                
            using var workpieceSetupCom = partStageCom.WorkpieceSetup();
            var MachineSideConnectorIndex = workpieceSetupCom.MachineSideConnectorIndex();
            _jsonBuilder.AddIntPair("WorkpieceConnectorIndex", MachineSideConnectorIndex);
                    
            using var workpieceConnectorCom = machineCom.WorkpieceConnector(MachineSideConnectorIndex);
            var workpieceConnectorName = workpieceConnectorCom.Name();
            _jsonBuilder.AddStrPair("WorkpieceConnectorName", workpieceConnectorName);

            var worldWorkpieceConnectorMatrix = evaluatorCom.GetWorldWorkpieceConnectorMatrix(MachineSideConnectorIndex);
            GeometrySaveHelper.ShowMatrixData(worldWorkpieceConnectorMatrix, "WorldWorkpieceConnectorMatrix", _jsonBuilder);    
            
            var workpieceSetupCS = workpieceSetupCom.Offset();
            GeometrySaveHelper.ShowMatrixData(workpieceSetupCS, "OffsetCS", _jsonBuilder);     
                
            _jsonBuilder.BeginArray("WorkpieceCSList");
            
            _jsonBuilder.BeginObject(); // WorkpieceCS
            var workpieceCSID = evaluatorCom.GetCurrentWorkpieceCSID();
            _jsonBuilder.AddStrPair("WorkpieceCSID", workpieceCSID);
            
            var workpieceCS_WorldMatrix = evaluatorCom.GetCurrentWorkpieceCSWorldMatrix();
            GeometrySaveHelper.ShowMatrixData(workpieceCS_WorldMatrix, "WorkpieceCS_World", _jsonBuilder);

            var workpieceCS_WorkpieceConnectorMatrix = evaluatorCom.GetCurrentWorkpieceCSMatrix();
            GeometrySaveHelper.ShowMatrixData(workpieceCS_WorkpieceConnectorMatrix, "WorkpieceCS_WorkpieceConnector", _jsonBuilder);

            _jsonBuilder.EndObject();
            
            _jsonBuilder.EndArray(); // WorkpieceCSList closing
            _jsonBuilder.EndObject(); // PartSetup closing  
        }
    }
}