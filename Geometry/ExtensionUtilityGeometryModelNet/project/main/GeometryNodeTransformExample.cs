using CAMAPI.Extensions;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.GeomModel;
using CAMAPI.ResultStatus;
using STTypes;
using System.Runtime.InteropServices;
using System.Numerics;

namespace ExtensionUtilityGeometryModelNet;

public class GeometryNodeTransformExample : IExtension, IExtensionUtility
{
    public IExtensionInfo? Info { get; set; }
     private const string nodeFullName = "Part\\49-1.igs";

    TST3DMatrix MakeUnitMatrix()
    {
        TST3DMatrix matrix = new TST3DMatrix();
        matrix.vT.X = 0;  
        matrix.vT.Y = 0;
        matrix.vT.Z = 0;
        matrix.vX.X = 1;  
        matrix.vX.Y = 0;
        matrix.vX.Z = 0;
        matrix.vY.X = 0;  
        matrix.vY.Y = 1;
        matrix.vY.Z = 0;
        matrix.vZ.X = 0;  
        matrix.vZ.Y = 0;
        matrix.vZ.Z = 1;
        matrix.D = 1;
        return matrix;
    }

    TST3DMatrix MakeShiftMatrix(double shiftX, double shiftY, double shiftZ)
    {
        TST3DMatrix matrix = MakeUnitMatrix();
        matrix.vT.X = shiftX;  
        matrix.vT.Y = shiftY;
        matrix.vT.Z = shiftZ;
        return matrix;
    }

    TST3DMatrix MakeRotMatrix(double ang, int axis)
    {
        TST3DMatrix matrix = MakeUnitMatrix();
        double cs = Math.Cos(ang);
        double sn = Math.Sin(ang);
        switch (axis) {
            case 1: // on X axis
                matrix.vY.Y = cs;           
                matrix.vY.Z = sn;           
                matrix.vZ.Y = -sn;          
                matrix.vZ.Z = cs;           
                break;
            case 2: // on Y axis
                matrix.vX.X = cs;           
                matrix.vX.Z = -sn;          
                matrix.vZ.X = sn;                
                matrix.vZ.Z = cs;
                break;
            case 3: // on Z axis
                matrix.vX.X = cs;        
                matrix.vX.Y = sn;
                matrix.vY.X = -sn;
                matrix.vY.Y = cs;
                break;
            default:
                throw new Exception("Invalid axis number");
        }
        return matrix;
    }

    TST3DMatrix MakeScaleMatrix(double scaleValue, double shiftX, double shiftY, double shiftZ)
    {
        TST3DMatrix matrix = MakeUnitMatrix();
        matrix.vX.X *= scaleValue;
        matrix.vY.Y *= scaleValue;
        matrix.vZ.Z *= scaleValue;
        matrix.vT.X = shiftX;  
        matrix.vT.Y = shiftY;
        matrix.vT.Z = shiftZ;
        return matrix;
    }

    public void Run(IExtensionUtilityContext Context, out TResultStatus result)
    {
        result = default;      
        var prj = Context.CamApplication.GetActiveProject(out TResultStatus r);
        try
        {
            if (r.Code == TResultStatusCode.rsSuccess)
            {
                using var fullModel = new ApiComObject<ICAMAPIGeometryModel>(prj.CAMAPIGeomModel);
                var geomNode = new ApiComObject<ICAMAPIGeometryTreeNode>(fullModel.Instance.FindByFullName(nodeFullName, out result));

                // TST3DMatrix matr = MakeShiftMatrix(100, 200, 0);
                // TST3DMatrix matr = MakeRotMatrix(60, 1);
                TST3DMatrix matr = MakeScaleMatrix(2, 100, 200, 0);

                result = fullModel.Instance.Transform(geomNode.Instance, matr);
                if (!(result.Code == TResultStatusCode.rsSuccess))
                    throw new Exception(result.Description);
            }
        } finally {
            Marshal.ReleaseComObject(prj);
        }
    }
}
