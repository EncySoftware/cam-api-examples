using CAMAPI.GeomLibrary;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using System.Collections.Generic;
using STTypes;
using System;
using System.Runtime.InteropServices;

namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// Special struct for part - geometry
    /// </summary>
    public struct PartGeometry
    {
        /// <summary>
        /// Name of the converted file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Path of the converted file
        /// </summary>
        public string SourceCADModelFileID { get; set; }
        
        /// <summary>
        /// Type of the converted file (.osd, .stl or other)
        /// </summary>
        public string GeometryType { get; set; }

        /// <summary>
        /// Array of ModelItems
        /// </summary>
        public List<ModelItem> ModelItems { get; set; }

        /// <summary>
        /// Geometry position information, 
        /// which you can find in Part->Setup->SetupAndTooling->WorkpieceSetup->GeometryCS
        /// </summary>
        public GeomCS GeometryCS { get; set; }

        /// <summary>
        /// Special struct for geometry CS
        /// </summary>
        public struct GeomCS
        {
            /// <summary>
            /// Name of the geometry CS
            /// </summary>
            public string GeometryCSName { get; set; }
            
            /// <summary>
            /// Matrix of the geometry CS
            /// </summary>
            public TST3DMatrix GeometryCSMatrix { get; set; }
        }

        /// <summary>
        /// Struct for ModelItem
        /// </summary>
        public struct ModelItem
        {
            /// <summary>
            /// Caption of the model item
            /// </summary>
            public string Caption { get; set; }

            /// <summary>
            /// Class name of the model item
            /// </summary>
            public string ModelItemClassName { get; set; }
        }
    }

}