namespace VecMatrLibExamples;

using System.IO;
using Geometry.VecMatrLib;
using static Geometry.VecMatrLib.VML;
using GeneralUtils.STDefLib;
using static GeneralUtils.STDefLib.STDef;

public class RotationsConverterExamples
{

    const double tolerance = 0.000001;

    public static void RunExamples()
    {
        DoubleSideConversionAllConventions();
        ShouldConvertAngleToMatrix();
        ShouldConvertMatrixToAngles();
    }

    public static void DoubleSideConversionAllConventions()
    {
        Console.WriteLine();
        Console.WriteLine("DoubleSideConversionAllConventions");
        TLocation pos = new TLocation(10, 20, 30, 45, 30, 90, 0);
        Console.WriteLine($"Source location: {pos}");
        foreach (TRotationConvention conv in Enum.GetValues(typeof(TRotationConvention))) {
            if (conv==TRotationConvention.AxisAngle)
                break;
            bool movable = false;
            do
            {
                TComplexRotationConvention cc = new TComplexRotationConvention(conv, true, movable);
                T3DMatrix m = TRotationsConverter.LocationToMatrix(pos, cc);
                var pos2 = TRotationsConverter.MatrixToLocation(m, cc);
                
                Console.WriteLine();
                Console.WriteLine($"Convention: {conv}, Movable: {movable}");
                Console.WriteLine($"result: {pos2}");
                var ok = TLocation.Equals(pos, pos2, tolerance);
                Console.WriteLine($"Equals: {ok}");
                movable = !movable;
            } while (movable);
        }
    }

    public static void ShouldConvertAngleToMatrix()
    {
        Console.WriteLine();
        Console.WriteLine("ShouldConvertAngleToMatrix");
        TLocation pos = new TLocation(76.25679686320001d, 29.7033955373d, 102.8415213944d, 95.47881831490001d, 24.7935443974d, -87.1229679707d, 0);
        Console.WriteLine($"Source location: {pos}");
        TComplexRotationConvention cc = new TComplexRotationConvention(TRotationConvention.ZYX, true, true);
        T3DMatrix m = TRotationsConverter.LocationToMatrix(pos, cc);
        Console.WriteLine($"Result matrix: {m}");
    }

    public static void ShouldConvertMatrixToAngles()
    {
        Console.WriteLine();
        Console.WriteLine("ShouldConvertMatrixToAngles");
        T3DMatrix m = new (
            (76.25679686320001d, 29.7033955373d, 102.8415213944d), 
            (-0.08667707000109359d, 0.9036773934973412d, -0.4193497991151026d), 
            (-0.00997516880399315d, -0.421700150077826d, -0.9066804726206862d), 
            (-0.9961865194547441d, -0.07440532175987406d, 0.04556606797225592d)
        );
        Console.WriteLine($"Source matrix: {m}");

        TComplexRotationConvention cc = new TComplexRotationConvention(TRotationConvention.ZYX, true, true);
        TLocation pos = TRotationsConverter.MatrixToLocation(m, cc);
        Console.WriteLine($"Result location: {pos}");
    }
}