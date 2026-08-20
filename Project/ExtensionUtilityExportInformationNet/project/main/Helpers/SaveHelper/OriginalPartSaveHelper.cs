using CAMAPI.Application;
using CAMAPI.CoordinateSystem;
using CAMAPI.DotnetHelper;
using CAMAPI.Extension.PLM;
using CAMAPI.GeomModel;
using CAMAPI.Machine;
using CAMAPI.ModelFormerTypes;
using CAMAPI.PartStage;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;

namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// A helper class for saving original part data.
    /// </summary>
    public static class OriginalPartSaveHelper
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
        /// Saves the original part data to the JSON builder.
        /// </summary>
        public static void SaveOriginalPartData(
            ComWrapper<ICamApiFacesToTriangulatedFilesConverter> converterCom,
            ComWrapper<ICamApiPartStage> partStageCom,
            ComWrapper<ICamApiMachineEvaluator> evaluatorCom,
            ComWrapper<ICamApiListCoordinateSystem> listCSCom,
            ComWrapper<ICamApiApplication> applicationCom,
            out PartGeometry partGeometry)
        {
            partGeometry = new PartGeometry();

            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");
            
            if (partStageCom.IsNull)
                return;

            using var partStageAsOpCom = partStageCom.AsInstanceOf<ICamApiTechOperation>();
            if (partStageAsOpCom == null)
                return;

            var partStageName = partStageAsOpCom.Name();
            _jsonBuilder.AddStrPair("PartStageName", partStageName);
            

            partStageAsOpCom.InitMachineEvaluator(evaluatorCom);
            
            using var modelFormerPartCom = partStageAsOpCom.ModelFormerPart();
            if (modelFormerPartCom.IsNull)
                return;

            _jsonBuilder.BeginObject("PartGeometry");
            _jsonBuilder.AddStrPair("GeometryType", "OSD");
            var convertedFileName = $"{partStageName}.osd"; 
            _jsonBuilder.AddStrPair("FileName", convertedFileName);

            // .osd files would stored in project/main/Output
            var convertedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "project", "main", "Output", convertedFileName); 
            FacesConverter.ConvertingFromFacesToFiles(converterCom, modelFormerPartCom, convertedFilePath);
            _jsonBuilder.AddStrPair("SourceCADModelFileID", convertedFilePath);

            _jsonBuilder.BeginArray("ModelItems");
            var (Caption, ClassName) = ShowModelItemData(modelFormerPartCom, applicationCom);
            _jsonBuilder.EndArray(); // ModelItems closing


            _jsonBuilder.BeginObject("GeometryCS");

            using var workpieceSetupCom = partStageCom.WorkpieceSetup();
            var GeometryCSName = workpieceSetupCom.WorkpieceSideCoordinateSystemName();
            _jsonBuilder.AddStrPair("GeometryCSName", GeometryCSName);

            using var geomCSCom = listCSCom.GetByName(GeometryCSName);
            var geomCSMatrix = geomCSCom.Matrix();
            GeometrySaveHelper.ShowMatrixData(geomCSMatrix, "GeometryCSMatrix", _jsonBuilder);

            _jsonBuilder.EndObject(); // GeometryCS closing
            _jsonBuilder.EndObject(); // PartGeometry closing
            
            partGeometry.FileName = convertedFileName;
            partGeometry.SourceCADModelFileID = convertedFilePath;
            partGeometry.GeometryType = "OSD";

            var modelItem = new PartGeometry.ModelItem{
                Caption = Caption,
                ModelItemClassName = ClassName
            };

            partGeometry.ModelItems = [modelItem];
        
            var partGeometryCS = new PartGeometry.GeomCS{
                GeometryCSName = GeometryCSName,
                GeometryCSMatrix = geomCSMatrix
            };
            partGeometry.GeometryCS = partGeometryCS;
        }

        /// <summary>
        /// Saves the original part data to the JSON builder.
        /// </summary>
        public static void SaveAndWritePartGeometryData(
            ComWrapper<ICamApiFacesToTriangulatedFilesConverter> converterCom,
            ComWrapper<ICamApiPartStage> partStageCom,
            ComWrapper<ICamApiMachineEvaluator> evaluatorCom,
            ComWrapper<ICamApiListCoordinateSystem> listCSCom,
            ComWrapper<ICamApiApplication> applicationCom,
            PartKey key)
        {
            SaveOriginalPartData(converterCom, partStageCom, evaluatorCom, listCSCom, applicationCom, out var partGeometry);
            PartGeometryStore.Parts[key] = partGeometry;
        }

        /// <summary>
        /// Saves the model item data to the JSON builder.
        /// </summary>
        public static (string Caption, string ClassName) ShowModelItemData(
            ComWrapper<ICamApiModelFormer> modelFormerCom,
            ComWrapper<ICamApiApplication> applicationCom)
        {
            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");
            
            string finalCaption = "";
            string finalClassName = "";

            int low = modelFormerCom.LowItemIndex();
            int top = modelFormerCom.TopItemIndex();
            for (var i = low; i <= top; i++){
                using var modelItemCom = modelFormerCom.Item(i);
                if (modelItemCom.IsNull)
                    continue;

                (string Caption, string ClassName) modelData = default;
                // Link to part of previous operation    
                using var linkModelItemCom = modelItemCom.AsInstanceOf<ICamApiLinkModelItem>();
                if (linkModelItemCom != null)
                {
                    // if link is simple, not to some geometry of prev operations and etc.
                    var isSimpleLink = linkModelItemCom.IsSimpleLink();
                    if (isSimpleLink) {
                        using var linkModelFormerCom = linkModelItemCom.LinkModelFormer();
                        return ShowModelItemData(linkModelFormerCom, applicationCom);
                    }
                }

                _jsonBuilder.BeginObject();
                finalCaption = !string.IsNullOrWhiteSpace(modelData.Caption) 
                    ? modelData.Caption 
                    : modelItemCom.Caption(); // Could be simplified, but protected from mistakes
                finalClassName = !string.IsNullOrWhiteSpace(modelData.ClassName) 
                    ? modelData.ClassName 
                    : modelItemCom.ClassName();
                _jsonBuilder.AddStrPair("Caption", finalCaption);
                _jsonBuilder.AddStrPair("ModelItemClassName", finalClassName);


                using var geomNodeBasedCom = modelItemCom.AsInstanceOf<ICamApiGeometryNodeBasedModelItem>();
                if (geomNodeBasedCom == null){
                    _jsonBuilder.EndObject(); 
                    continue;
                }
                    
                using var geomEntityCom = geomNodeBasedCom.GeometryEntity();
                using var importedGeomCom = geomEntityCom.AsInstanceOf<ICamApiImportedGeometryEntity>();
                if (importedGeomCom == null){
                    _jsonBuilder.EndObject(); 
                    continue;
                }

                var geomNodeFullName = importedGeomCom.CADModelLocalFileName();
                var importedFromPLM  = importedGeomCom.ImportedFromPLM();
                var plmObjectId      = importedGeomCom.PLMObjectID();
                _jsonBuilder.AddStrPair("GeometryNodeFullName", geomNodeFullName);
                _jsonBuilder.AddBoolPair("ImportedFromPLM", importedFromPLM);
                _jsonBuilder.AddStrPair("PLMObjectID", plmObjectId); 
                if (!importedFromPLM){
                    _jsonBuilder.EndObject(); 
                    continue;
                }


                using var plmAppCom = applicationCom.AsInstanceOf<IPLMApplication>();
                if (plmAppCom == null){
                    _jsonBuilder.EndObject(); 
                    continue;
                }

                plmAppCom.Invoke(plm => {
                    var plmObj = plm.FindObjectByID(plmObjectId);
                    _jsonBuilder.AddStrPair("IdInPLM", plmObj.IdInPLM);
                    _jsonBuilder.AddStrPair("NameInPLM", plmObj.Name);
                    _jsonBuilder.AddStrPair("ItemType", plmObj.ItemType.ToString());
                    _jsonBuilder.AddFltPair("TimeStamp", plmObj.TimeStamp);
                    _jsonBuilder.AddStrPair("ConnectionId", plmObj.ConnectionId);
                });
                
                _jsonBuilder.EndObject();
            }
            return (finalCaption, finalClassName);
        }
    }    
}