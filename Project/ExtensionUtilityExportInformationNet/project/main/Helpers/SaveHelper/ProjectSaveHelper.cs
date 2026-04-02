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
    /// A helper class for saving project data.
    /// </summary>
    public static class ProjectSaveHelper
    {
        private static JsonBuilder? _jsonBuilder;

        /// <summary>
        /// Initializes the JSON builder for saving project data.
        /// </summary>
        public static void Initialize(JsonBuilder builder)
        {
            _jsonBuilder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        /// <summary>
        /// Saves the project data to the JSON builder.
        /// </summary>
        public static void SaveProjectDetails(ComWrapper<ICamApiProject> projectCom)
        {
            var projectFilepath = ProjectHelper.FilePath(projectCom);
            var projectId = ProjectHelper.Id(projectCom);
            _jsonBuilder?.AddStrPair("FilePath", projectFilepath);
            _jsonBuilder?.AddStrPair("Id", projectId);  
        }

    }    
}