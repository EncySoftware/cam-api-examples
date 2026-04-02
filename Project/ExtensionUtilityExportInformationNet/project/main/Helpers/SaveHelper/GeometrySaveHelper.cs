using STTypes;

namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// Class to help save geometric data in JSON format.
    /// </summary>
    public static class GeometrySaveHelper
    {
        /// <summary>
        /// Displays the matrix data in JSON format.
        /// </summary>
        public static void ShowMatrixData(TST3DMatrix matrix, string matrixName, JsonBuilder jsonBuilder){
            if (jsonBuilder == null)
                throw new Exception("Create JSON builder!");
        
            jsonBuilder.BeginObject(matrixName);
            
            ShowPointData(matrix.vX, "vX", jsonBuilder);
            ShowPointData(matrix.vY, "vY", jsonBuilder);
            ShowPointData(matrix.vZ, "vZ", jsonBuilder);
            ShowPointData(matrix.vT, "vT", jsonBuilder);

            jsonBuilder.AddFltPair("A", matrix.A);
            jsonBuilder.AddFltPair("B", matrix.B);
            jsonBuilder.AddFltPair("C", matrix.C);
            jsonBuilder.AddFltPair("D", matrix.D);

            jsonBuilder.EndObject();
        }

        /// <summary>
        /// Displays the point data in JSON format.
        /// </summary>
        public static void ShowPointData(TST3DPoint point, string pointName, JsonBuilder jsonBuilder){
            if (jsonBuilder == null)
                throw new Exception("Create JSON builder!");

            jsonBuilder.BeginObject(pointName);
            jsonBuilder.AddFltPair("X", point.X);
            jsonBuilder.AddFltPair("Y", point.Y);
            jsonBuilder.AddFltPair("Z", point.Z);
            jsonBuilder.EndObject();    
        }
    }
}