using CAMAPI.GeomLibrary;
using CAMAPI.DotnetHelper;
using CAMAPI.ModelFormerTypes;
using CAMAPI.ResultStatus;
using CAMAPI.GeomModel;
using STTypes;
using CAMHelper.NativeLibUtils;
using System;
using System.Runtime.InteropServices;

namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// Class for converting faces to triangulated files(in this case, OSD-files) 
    /// </summary>
    public static class FacesConverter
    {
        /// <summary>
        /// Tolerance for faces to triangulated files converter
        /// </summary>
        public const double ConverterTolerance = 0.192;
        /// <summary>
        /// Converts faces from a model to files.
        /// </summary>
        /// <param name="converterCom"></param>
        /// <param name="modelFormerPartCom"></param>
        /// <param name="ModelPath">The path where the converted files will be saved.</param>
        public static void ConvertingFromFacesToFiles(
            ComWrapper<ICamApiFacesToTriangulatedFilesConverter> converterCom, 
            ComWrapper<ICamApiModelFormer> modelFormerPartCom, string ModelPath)
        {
            var Matrix4FaceList = new TST3DMatrix
            {
                vX = new TST3DPoint { X = 1, Y = 0, Z = 0 },
                vY = new TST3DPoint { X = 0, Y = 1, Z = 0 },
                vZ = new TST3DPoint { X = 0, Y = 0, Z = 1 },
                vT = new TST3DPoint { X = 0, Y = 0, Z = 0 }
            };

            using var faceListCom = modelFormerPartCom.GetFaceList(Matrix4FaceList); // global or techop.lcs
            converterCom.SetColor(0xC8C8C8);
            converterCom.SetTolerance(ConverterTolerance);
            converterCom.SaveFacesToOSD(faceListCom, ModelPath);
        }

    }
}