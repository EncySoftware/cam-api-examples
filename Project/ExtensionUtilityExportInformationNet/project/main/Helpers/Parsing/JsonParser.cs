using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Geometry.VecMatrLib;
using STTypes;

namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// Parser from json to CAMProjectData 
    /// </summary>
    public class SimpleJsonProjectParser
    {
        /// <summary>
        /// Root element 
        /// </summary>
        public CAMProjectData CAMProject { get; private set; } = new CAMProjectData();

        private static readonly JsonSerializerOptions s_propNameCaseInsensitive = new()
        {
            PropertyNameCaseInsensitive = true
        };
        
        /// <summary>
        /// Loading file to parser 
        /// </summary>
        public void Load(string fileName)
        {
            string json = File.ReadAllText(fileName);

            var root = JsonSerializer.Deserialize<RootDto>(json, s_propNameCaseInsensitive)
                       ?? throw new InvalidOperationException("Cannot deserialize JSON.");

            CAMProject = MapProject(root);
        }

        private static CAMProjectData MapProject(RootDto root)
        {
            var result = new CAMProjectData();

            if (root.CAMProject?.MachineSetup?.SetupStagesList == null)
                return result;

            foreach (var g in root.CAMProject.MachineSetup.SetupStagesList)
            {
                var group = new SetupStageData
                {
                    SetupStageIndex = g.SetupStageIndex
                };

                if (g.PartStageList != null)
                {
                    foreach (var item in g.PartStageList)
                    {
                        var part = new PartData
                        {
                            PartIndex = item.PartIndex,
                            GeometryFileName = item.PartGeometry?.SourceCADModelFileID,
                            GeometryMatrix = ToMatrix(item.PartGeometry?.GeometryCS?.GeometryCSMatrix),
                            SetupMatrix = ToMatrix(item.PartSetup?.WorldWorkpieceConnectorMatrix),
                            OffsetMatrix = ToMatrix(item.PartSetup?.OffsetCS)
                        };

                        group.PartList.Add(part);
                    }
                }

                result.SetupStageList.Add(group);
            }

            return result;
        }

        private static TST3DMatrix ToMatrix(MatrixDto? dto)
        {
            if (dto == null)
                return T3DMatrix.Zero;
            
            return new TST3DMatrix
            {
                vX = new TST3DPoint { X = dto.vX?.X ?? 0, Y = dto.vX?.Y ?? 0, Z = dto.vX?.Z ?? 0 },
                vY = new TST3DPoint { X = dto.vY?.X ?? 0, Y = dto.vY?.Y ?? 0, Z = dto.vY?.Z ?? 0 },
                vZ = new TST3DPoint { X = dto.vZ?.X ?? 0, Y = dto.vZ?.Y ?? 0, Z = dto.vZ?.Z ?? 0 },
                vT = new TST3DPoint { X = dto.vT?.X ?? 0, Y = dto.vT?.Y ?? 0, Z = dto.vT?.Z ?? 0 },
                A = dto.A,
                B = dto.B,
                C = dto.C,
                D = dto.D
            };
        }

        /// <summary>
        /// Root element obtained as a result of the parser
        /// </summary>
        public class CAMProjectData
        {
            /// <summary>
            /// Data of lists of setup stages  
            /// </summary>
            public List<SetupStageData> SetupStageList { get; } = new List<SetupStageData>();
        }

        /// <summary>
        /// Machine setup element obtained as a result of the parser
        /// </summary>
        public class SetupStageData
        {
            /// <summary>
            /// Setup stage index data 
            /// </summary>
            public int SetupStageIndex { get; set; }
            
            /// <summary>
            /// Data of lists of parts  
            /// </summary>
            public List<PartData> PartList { get; } = [];
        }

        /// <summary>
        /// Part data obtained as a result of the parser
        /// </summary>
        public class PartData
        {
            /// <summary>
            /// Part index data  
            /// </summary>
            public int PartIndex { get; set; }
            
            /// <summary>
            /// Geometry file name data  
            /// </summary>
            public string? GeometryFileName { get; set; }

            /// <summary>
            /// Geometry matrix data  
            /// </summary>
            public TST3DMatrix GeometryMatrix { get; set; }
            
            /// <summary>
            /// Setup matrix data  
            /// </summary>
            public TST3DMatrix SetupMatrix { get; set; }
            
            /// <summary>
            /// Offset matrix data  
            /// </summary>
            public TST3DMatrix OffsetMatrix { get; set; }
        }

    }
}