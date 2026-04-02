using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// Root element of json  
    /// </summary>
    public class RootDto
    {
        /// <summary>
        /// Represents the main project object in the JSON.
        /// </summary>
        public CAMProjectDto? CAMProject { get; set; }
    }

    /// <summary>
    /// Represents the main project object in the JSON.
    /// </summary>
    public class CAMProjectDto
    {
        /// <summary>
        /// Represents the machine setup section of the project.
        /// </summary>
        public MachineSetupDto? MachineSetup { get; set; }
    }

    /// <summary>
    /// Represents the machine setup section of the project.
    /// </summary>
    public class MachineSetupDto
    {
        /// <summary>
        /// Represents a list of setup stages in the project.
        /// </summary>
        public List<SetupStagesListDto>? SetupStagesList { get; set; }
    }

    /// <summary>
    /// Represents a list of setup stages in the project.
    /// </summary>
    public class SetupStagesListDto
    {
        /// <summary>
        /// Index of the setup stage.
        /// </summary>
        public int SetupStageIndex { get; set; }

        /// <summary>
        /// List of parts in the setup stage.
        /// </summary>
        public List<PartDto>? PartStageList { get; set; }
    }

    /// <summary>
    /// Represents a part in the setup stage.
    /// </summary>
    public class PartDto
    {
        /// <summary>
        /// Index of the part.
        /// </summary>
        public int PartIndex { get; set; }

        /// <summary>
        /// Geometry of the part.
        /// </summary>
        public PartGeometryDto? PartGeometry { get; set; }

        /// <summary>
        /// Setup details of the part.
        /// </summary>
        public PartSetupDto? PartSetup { get; set; }
    }

    /// <summary>
    /// Represents the geometry of a part.
    /// </summary>
    public class PartGeometryDto
    {
        /// <summary>
        /// Identifier of the source CAD model file.
        /// </summary>
        public string? SourceCADModelFileID { get; set; }

        /// <summary>
        /// Coordinate system of the part geometry.
        /// </summary>
        public GeometryCSDto? GeometryCS { get; set; }
    }

    /// <summary>
    /// Represents the coordinate system of the part geometry.
    /// </summary>
    public class GeometryCSDto
    {
        /// <summary>
        /// Name of the coordinate system.
        /// </summary>
        public string? GeometryCSName { get; set; }

        /// <summary>
        /// Matrix representing the coordinate system.
        /// </summary>
        public MatrixDto? GeometryCSMatrix { get; set; }
    }

    /// <summary>
    /// Represents the setup details of a part.
    /// </summary>
    public class PartSetupDto
    {
        /// <summary>
        /// Matrix representing the world workpiece connector.
        /// </summary>
        public MatrixDto? WorldWorkpieceConnectorMatrix { get; set; }

        /// <summary>
        /// Offset coordinate system.
        /// </summary>
        public MatrixDto? OffsetCS { get; set; }
    }

    /// <summary>
    /// Represents a matrix with its components.
    /// </summary>
    public class MatrixDto
    {
        /// <summary>
        /// Vector representing the x-axis.
        /// </summary>
        public VectorDto? vX { get; set; }

        /// <summary>
        /// Vector representing the y-axis.
        /// </summary>
        public VectorDto? vY { get; set; }

        /// <summary>
        /// Vector representing the z-axis.
        /// </summary>
        public VectorDto? vZ { get; set; }

        /// <summary>
        /// Vector representing the translation.
        /// </summary>
        public VectorDto? vT { get; set; }

        /// <summary>
        /// Component of the matrix.
        /// </summary>
        public double A { get; set; }

        /// <summary>
        /// Component of the matrix.
        /// </summary>
        public double B { get; set; }

        /// <summary>
        /// Component of the matrix.
        /// </summary>
        public double C { get; set; }

        /// <summary>
        /// Component of the matrix.
        /// </summary>
        public double D { get; set; }
    }

    /// <summary>
    /// Represents a vector with its components.
    /// </summary>
    public class VectorDto
    {
        /// <summary>
        /// X-component of the vector.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y-component of the vector.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Z-component of the vector.
        /// </summary>
        public double Z { get; set; }
    }
}