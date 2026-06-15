using CAMAPI.CustomAttributes;
using CAMAPI.Machine;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;
using CAMAPI.Technologist;
using CAMAPI.DotnetHelper;
using CAMAPI.MCDFormerTypes;

namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// A helper class for saving operation data.
    /// </summary>
    public static class OperationSaveHelper
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
        /// Saves the operations data for reordered and designed order operations.
        /// </summary>
        public static void SaveOperationsData(
            ComWrapper<ICamApiTechnologist> technologistCom,
            ComWrapper<ICamApiMachineEvaluator> evaluatorCom)
        {
            using var techOpIterCom = TechnologistHelper.GetOperations(technologistCom, TCamApiReorderingMode.rmReordered);
            SaveOperationListData(techOpIterCom, "ReorderedOperationsList", TCamApiReorderingMode.rmReordered, evaluatorCom);

            using var techOpIterCom2 = TechnologistHelper.GetOperations(technologistCom, TCamApiReorderingMode.rmDesigned);
            SaveOperationListData(techOpIterCom2, "DesignedOperationsList", TCamApiReorderingMode.rmDesigned, evaluatorCom);
        }
        
        /// <summary>
        /// Saves the operation list data to the JSON builder.
        /// </summary>
        public static void SaveOperationListData(
            ComWrapper<ICamApiTechOperationIterator> techOpIterCom,
            string ListName, TCamApiReorderingMode mode,
            ComWrapper<ICamApiMachineEvaluator> evaluatorCom)
        {
            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");

            _jsonBuilder.BeginArray(ListName);
            ToolpathSaveHelper.ResetOperationIndex();
            foreach (var operationCom in TechOperationIteratorHelper.AsEnumerable(techOpIterCom))
                SaveOperationData(operationCom, mode, evaluatorCom);
            _jsonBuilder.EndArray(); // OperationsList closing
        }

        /// <summary>
        /// Saves the operation data to the JSON builder.
        /// </summary>
        public static void SaveOperationData(
            ComWrapper<ICamApiTechOperation> operationCom,
            TCamApiReorderingMode mode,
            ComWrapper<ICamApiMachineEvaluator> evaluatorCom)
        {
            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");

            
            _jsonBuilder.BeginObject(); // operation
            
            using var parentOpCom = TechOperationHelper.GetParentOperation(operationCom, mode);
            var parentOpId = parentOpCom.IsNull ? "Null" : TechOperationHelper.Id(parentOpCom);
            _jsonBuilder.AddStrPair("ParentOperationID", parentOpId);

            _jsonBuilder.AddStrPair("OperationID", TechOperationHelper.Id(operationCom));
            _jsonBuilder.AddStrPair("OperationType", TechOperationHelper.OperationType(operationCom));
            _jsonBuilder.AddStrPair("OperationName", TechOperationHelper.Name(operationCom));
            _jsonBuilder.AddIntPair("OperationPartIndex", TechOperationHelper.PartIndex(operationCom));
            _jsonBuilder.AddIntPair("OperationSetupStageIndex", TechOperationHelper.SetupStageIndex(operationCom));

            TechOperationHelper.InitMachineEvaluator(operationCom, evaluatorCom);
            ShowWorkpieceCS(evaluatorCom); 

            _jsonBuilder.BeginObject("Toolpath");
            ToolpathSaveHelper.SaveOperationToolpath(operationCom, mode);
            _jsonBuilder.EndObject(); // Toolpath closing

            _jsonBuilder.BeginObject("Status");
            ShowOperationStatus(operationCom);
            _jsonBuilder.EndObject(); // Status closing

            _jsonBuilder.BeginObject("Statistics");
            ShowOperationStatistics(operationCom);
            _jsonBuilder.EndObject(); // Statistics closing

            _jsonBuilder.BeginArray("Feeds");
            ShowOperationFeeds(operationCom);
            _jsonBuilder.EndArray(); // Feeds closing

            _jsonBuilder.BeginArray("Coolants");
            ShowOperationCoolants(operationCom);
            _jsonBuilder.EndArray(); // Coolants closing

            _jsonBuilder.BeginObject("Spindle");
            ShowOperationSpindle(operationCom);
            _jsonBuilder.EndObject(); // Spindle closing
            
            _jsonBuilder.BeginObject("Stocks");
            ShowOperationStocks(operationCom);
            _jsonBuilder.EndObject(); // Stocks closing

            _jsonBuilder.BeginArray("CustomAttributes");
            ShowOperationCustomAttributes(operationCom);
            _jsonBuilder.EndArray(); // CustomAttributes closing

            _jsonBuilder.EndObject(); // Operation closing
        }
        
        /// <summary>
        /// Shows the operation status in the JSON builder.
        /// </summary>
        public static void ShowOperationStatus(ComWrapper<ICamApiTechOperation> operationCom)
        {
            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");

            _jsonBuilder.AddBoolPair("Enabled", TechOperationHelper.Enabled(operationCom));
            _jsonBuilder.AddBoolPair("Calculated", TechOperationHelper.Calculated(operationCom));
            _jsonBuilder.AddBoolPair("Simulated", TechOperationHelper.Simulated(operationCom));
            _jsonBuilder.AddBoolPair("IsRapidError", TechOperationHelper.IsRapidError(operationCom));
            _jsonBuilder.AddBoolPair("IsHolderError", TechOperationHelper.IsHolderError(operationCom));
            _jsonBuilder.AddBoolPair("IsCompensationError", TechOperationHelper.IsCompensationError(operationCom));
            _jsonBuilder.AddBoolPair("IsPlungeError", TechOperationHelper.IsPlungeError(operationCom));
            _jsonBuilder.AddBoolPair("IsTravelError", TechOperationHelper.IsTravelError(operationCom));
            _jsonBuilder.AddBoolPair("IsCollisionError", TechOperationHelper.IsCollisionError(operationCom));
            _jsonBuilder.AddBoolPair("IsGougeError", TechOperationHelper.IsGougeError(operationCom));
            _jsonBuilder.AddBoolPair("IsToolOverloadError", TechOperationHelper.IsToolOverloadError(operationCom));
            _jsonBuilder.AddBoolPair("IsTurnDirectionError", TechOperationHelper.IsTurnDirectionError(operationCom));
            _jsonBuilder.AddBoolPair("IsMachiningResultCalculated", TechOperationHelper.IsMachiningResultCalculated(operationCom));
            _jsonBuilder.AddBoolPair("IsError", TechOperationHelper.IsError(operationCom));
        }
        
        /// <summary>
        /// Shows the workpiece coordinate system in the JSON builder.
        /// </summary>
        public static void ShowWorkpieceCS(ComWrapper<ICamApiMachineEvaluator> evaluatorCom)
        {
            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");
                
            _jsonBuilder.BeginArray("WorkpieceCSList");
            _jsonBuilder.BeginObject(); // WorkpieceCS

            var workpieceCSID = MachineEvaluatorHelper.GetCurrentWorkpieceCSID(evaluatorCom);
            _jsonBuilder.AddStrPair("WorkpieceCSID", workpieceCSID);

            var workpieceCS_WorldMatrix = MachineEvaluatorHelper.GetCurrentWorkpieceCSWorldMatrix(evaluatorCom);
            GeometrySaveHelper.ShowMatrixData(workpieceCS_WorldMatrix, "workpieceCS_World", _jsonBuilder);

            var workpieceCS_WorkpieceConnectorMatrix = MachineEvaluatorHelper.GetCurrentWorkpieceCSMatrix(evaluatorCom);
            GeometrySaveHelper.ShowMatrixData(workpieceCS_WorkpieceConnectorMatrix, "WorkpieceCS_WorkpieceConnector", _jsonBuilder);

            _jsonBuilder.EndObject(); // WorkpieceCS
            _jsonBuilder.EndArray(); // WorkpieceCSList closing
        }
        ///
        public static void ShowOperationStatistics(ComWrapper<ICamApiTechOperation> operationCom) 
        {
            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");

            var timeStats = TechOperationHelper.GetTimeStatistics(operationCom);
            _jsonBuilder.BeginObject("Time");
            _jsonBuilder.AddStrPair("RapidTime", FormatMinutesToHms(timeStats.RapidTime));
            _jsonBuilder.AddStrPair("IdleWorkTime", FormatMinutesToHms(timeStats.IdleWorkTime));
            _jsonBuilder.AddStrPair("EffectiveWorkTime", FormatMinutesToHms(timeStats.EffectiveWorkTime));
            _jsonBuilder.AddStrPair("AuxiliaryTime", FormatMinutesToHms(timeStats.AuxiliaryTime));
            var totalTime = timeStats.RapidTime + timeStats.IdleWorkTime + timeStats.EffectiveWorkTime + timeStats.AuxiliaryTime;
            _jsonBuilder.AddStrPair("TotalTime", FormatMinutesToHms(totalTime));
            _jsonBuilder.EndObject();

            var blocksStats = TechOperationHelper.GetBlocksStatistics(operationCom);
            _jsonBuilder.BeginObject("Blocks");
            _jsonBuilder.AddIntPair("Lines", blocksStats.Lines + blocksStats.MultiGoTo);
            _jsonBuilder.AddIntPair("Arcs", blocksStats.Arcs);
            _jsonBuilder.AddIntPair("Feeds", blocksStats.Feeds);
            _jsonBuilder.AddIntPair("TotalBlocks", blocksStats.TotalBlocks);
            _jsonBuilder.EndObject();

            var lengthStats = TechOperationHelper.GetLengthStatistics(operationCom);
            _jsonBuilder.BeginObject("Lengths");
            _jsonBuilder.AddFltPair("WorkLength", lengthStats.WorkLength);
            _jsonBuilder.AddFltPair("RapidLength", lengthStats.RapidLength);
            _jsonBuilder.AddFltPair("ReturnLength", lengthStats.ReturnLength);
            _jsonBuilder.AddFltPair("NextLength", lengthStats.NextLength);
            _jsonBuilder.AddFltPair("FirstLength", lengthStats.FirstLength);
            _jsonBuilder.AddFltPair("EngageLength", lengthStats.EngageLength);
            _jsonBuilder.AddFltPair("RetractLength", lengthStats.RetractLength);
            _jsonBuilder.AddFltPair("PlungeLength", lengthStats.PlungeLength);
            _jsonBuilder.AddFltPair("FinishLength", lengthStats.FinishLength);
            _jsonBuilder.AddFltPair("ApproachLength", lengthStats.ApproachLength);
            _jsonBuilder.AddFltPair("ApproachFromSafeLength", lengthStats.ApproachFromSafeLength);
            _jsonBuilder.AddFltPair("ReturnToSafeLength", lengthStats.ReturnToSafeLength);
            _jsonBuilder.AddFltPair("TransitionOnSafeLength", lengthStats.TransitionOnSafeLength);
            _jsonBuilder.AddFltPair("LongNextLength", lengthStats.LongNextLength);
            _jsonBuilder.EndObject();

            var volumeStats = TechOperationHelper.GetVolumeStatistics(operationCom);
            _jsonBuilder.BeginObject("Volumes");
            _jsonBuilder.AddFltPair("MachResultVolume", volumeStats.MachResultVolume);
            _jsonBuilder.AddFltPair("WorkpieceVolume", volumeStats.WorkpieceVolume);
            _jsonBuilder.AddFltPair("VolumeRemovalRate", volumeStats.VolumeRemovalRate);
            _jsonBuilder.EndObject();
        }

        private static string FormatMinutesToHms(double minutes)
        {
            var totalMs = Math.Round(minutes * 60 * 1000, MidpointRounding.AwayFromZero);
            var ts = TimeSpan.FromMilliseconds(totalMs);
            return $"{(int)ts.TotalHours:D2}h:{ts.Minutes:D2}m:{ts.Seconds:D2}.{ts.Milliseconds:D3}s";
        }

        /// <summary>
        /// Shows all feed types for the operation in the JSON builder.
        /// </summary>
        public static void ShowOperationFeeds(ComWrapper<ICamApiTechOperation> operationCom)
        {
            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");
                
            foreach (TFeedTypeFlag feedType in Enum.GetValues<TFeedTypeFlag>())
            {
                TCamApiFeedInfo feedInfo = default;
                TResultStatus rs = default;

                operationCom.Invoke(op => {feedInfo = op.GetFeedValue(feedType, out rs);});

                _jsonBuilder.BeginObject(); // One feed
                _jsonBuilder.AddStrPair("FeedType", feedType.ToString());

                if (rs.Code == TResultStatusCode.rsError)
                {
                    _jsonBuilder.AddStrPair("NotSupported", rs.Description);
                    _jsonBuilder.EndObject();
                    continue;
                }

                _jsonBuilder.AddStrPair("Measurement", feedInfo.Measurement.ToString());

                if (feedInfo.ValuePercent != 0)
                    _jsonBuilder.AddFltPair("ValuePercent", feedInfo.ValuePercent);
                if (feedInfo.ValuePerMinute != 0)
                    _jsonBuilder.AddFltPair("ValuePerMinute", feedInfo.ValuePerMinute);
                if (feedInfo.ValuePerRevolution != 0)
                    _jsonBuilder.AddFltPair("ValuePerRevolution", feedInfo.ValuePerRevolution);
                if (feedInfo.ValuePerTooth != 0)
                    _jsonBuilder.AddFltPair("ValuePerTooth", feedInfo.ValuePerTooth);
                        
                _jsonBuilder.EndObject(); // Feed closing
            }
        }
        
        /// <summary>
        /// Shows all coolant channels for the operation in the JSON builder.
        /// </summary>
        public static void ShowOperationCoolants(ComWrapper<ICamApiTechOperation> operationCom)
        {
            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");

            int count = 0;
            TResultStatus rs = default;
            operationCom.Invoke(op => { count = op.GetCoolantTubesCount(); });
            if (count == 0)
            {
                _jsonBuilder.BeginObject();
                _jsonBuilder.AddStrPair("NotSupported", rs.Description ?? "No coolant tubes");
                _jsonBuilder.EndObject();
                return;
            }
            

            for (int i = 1; i <= count; i++)
            {
                TCamApiCoolantTubeInfo  info = default;
                TResultStatus rsGet = default;
                var idx = i;

                
                operationCom.Invoke(op => { info = op.GetCoolantTubeInfo(idx, out rsGet); });
                if (rsGet.Code == TResultStatusCode.rsError)
                    continue;

                bool state = false;
                TResultStatus rsState = default;
                operationCom.Invoke(op => { state = op.GetCoolantTubeState(idx, out rsState); });

                _jsonBuilder.BeginObject();
                _jsonBuilder.AddStrPair("Name", info.Name ?? string.Empty);
                _jsonBuilder.AddBoolPair("Available", info.Available != 0);
                _jsonBuilder.AddBoolPair("Enabled", state);
                _jsonBuilder.AddIntPair("TubeIndex", idx);
                _jsonBuilder.EndObject();
            }
        }

        /// <summary>
        /// Show spindle information of the operation in the JSON builder.
        /// </summary>
        public static void ShowOperationSpindle(ComWrapper<ICamApiTechOperation> operationCom)
        {
            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");

            TCamApiSpindleState spindleState = default;

            operationCom.Invoke(op=>spindleState = op.GetSpindleState());

            _jsonBuilder.AddStrPair("RotationMode", spindleState.RotationMode.ToString());
            _jsonBuilder.AddFltPair("SurfaceSpeedValue", spindleState.SurfaceSpeedValue);
            _jsonBuilder.AddFltPair("MaxRPMValue", spindleState.MaxRPMValue);
            _jsonBuilder.AddFltPair("RPMValue", spindleState.RPMValue);
            _jsonBuilder.AddStrPair("RotationDirection", spindleState.RotationDirection.ToString());
            _jsonBuilder.AddIntPair("Range", spindleState.Range);
        }

        /// <summary>
        /// Shows the operation stocks (profile / axial / radial machining allowances) in the JSON builder.
        /// </summary>
        public static void ShowOperationStocks(ComWrapper<ICamApiTechOperation> operationCom)
        {
            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");

            var stocks = TechOperationHelper.GetStocks(operationCom);
            _jsonBuilder.AddFltPair("Profile", stocks.Profile);
            _jsonBuilder.AddFltPair("Axial", stocks.Axial);
            _jsonBuilder.AddFltPair("Radial", stocks.Radial);
        }
        
        /// <summary>
        /// Shows the operation custom attributes in the JSON builder.
        /// </summary>
        public static void ShowOperationCustomAttributes(ComWrapper<ICamApiTechOperation> operationCom)
        {
            using var operationAsAttrCom = operationCom.AsInstanceOf<ICamApiObjectWithAttributes>()
                ?? throw new Exception("Operation does not support attributes");
            
            using var attrTree = ObjectWithAttributesHelper.Attributes(operationAsAttrCom);
            using var attrTreeIt = CustomAttributesTreeHelper.CreateIterator(attrTree);   
            DescribeCustomAttributes(attrTreeIt);
        }

        /// <summary>
        /// Describes the custom attributes recursively.
        /// </summary>
        public static void DescribeCustomAttributes(ComWrapper<ICamApiCustomAttributesTreeIterator> it)
        {
            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");

            using var node = CustomAttributesTreeIteratorHelper.CurrentNode(it);
            
            _jsonBuilder.BeginObject();
            _jsonBuilder.AddStrPair("AttributeName", CustomAttributesTreeNodeHelper.Name(node));
            switch (CustomAttributesTreeNodeHelper.ValueType(node))
            {
                case TCustomAttributeValueType.avtString:
                    _jsonBuilder.AddStrPair("Value", CustomAttributesTreeNodeHelper.ValueAsString(node));
                    break;
                case TCustomAttributeValueType.avtInteger:
                    _jsonBuilder.AddIntPair("Value", CustomAttributesTreeNodeHelper.ValueAsInteger(node));
                    break;
                case TCustomAttributeValueType.avtFloat:
                    _jsonBuilder.AddFltPair("Value", CustomAttributesTreeNodeHelper.ValueAsDouble(node));
                    break;
                case TCustomAttributeValueType.avtBoolean:
                    _jsonBuilder.AddBoolPair("Value", CustomAttributesTreeNodeHelper.ValueAsBoolean(node));
                    break;
            }

            _jsonBuilder.EndObject();

            using var chIt = CustomAttributesTreeIteratorHelper.GetCopy(it);
            if (CustomAttributesTreeIteratorHelper.MoveToChild(chIt))
            {
                DescribeCustomAttributes(chIt);
                CustomAttributesTreeIteratorHelper.MoveToParent(chIt);
            }
            if (CustomAttributesTreeIteratorHelper.MoveToSibling(chIt))
                DescribeCustomAttributes(chIt);
        }        
    }
}