using System.Diagnostics;
using System.Runtime.InteropServices;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.Generic.List;
using CAMAPI.GeomModel;
using CAMAPI.GeomPicker;
using CAMAPI.ResultStatus;
using CAMAPI.CustomAttributes;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;
using Geometry.VecMatrLib;
using STCustomPropTypes;

namespace ExtensionUtilityLCSCreatorNet;

/// <summary>
/// Utility to import geometry from Milling_25D\Part1.igs into the active project
/// </summary>
public class ExtensionLCSCreator
    : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }


    public double EnsureRotWithinRange(double value, double min, double max)
    {
        if (value > max)
            return max;
        else if (value < min)
            return min;
        else
            return value;
    }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            if (Info == null)
                throw new Exception("Extension Info is not set");

            //
            using var applicationCom = ComWrapper.Create(context.CamApplication);

            // catch an active project
            using var projectCom = applicationCom.InvokeAndWrap(application =>
                application.GetActiveProject(out var resultStatus));
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(resultStatus.Description);

            // get the list of all coordinate systems
            using var coordSystemListCom = projectCom.InvokeAndWrap(project => project.CoordinateSystems);
            // get the count of the list
            var coordSystemListCount = coordSystemListCom.Invoke(coordSystemListCom => coordSystemListCom.Count);

            // our container to store all properties, we are asking from user
            var currentValues = new DialogWindowValues();

            // create window and add caption
            using var window = new CamApiInspectorWindow();
            window.Caption = "Define coordinate system";

            // properties to provide to user
            using var propIterator = new SimplePropIterator();
            // property with dropdown menu
            propIterator.AddEnumIdProp("Choose parent CS",
                () => currentValues.EnumIdValue,
                value => currentValues.EnumIdValue = value,
                list =>
                {
                    for (int i = 0; i < coordSystemListCount; i++){
                        using var coordSystem = coordSystemListCom.InvokeAndWrap(csSystem => csSystem.CoordinateSystem[i]);
                        var coordSystemName = coordSystem.Invoke(coordinateSystem => coordinateSystem.Name);
                        list.Add(coordSystemName, coordSystemName, "");
                    }
                }
            );
            // property for X
            propIterator.AddDoubleProp("X:",
                () => currentValues.DoubleXValue,
                value => { 
                    double newValue = EnsureRotWithinRange(value, -32767, 32767);
                    currentValues.DoubleXValue = newValue;
                }
            );
            // property for Y
            propIterator.AddDoubleProp("Y:",
                () => currentValues.DoubleYValue,
                value => { 
                    double newValue = EnsureRotWithinRange(value, -32767, 32767);
                    currentValues.DoubleYValue = newValue;
                }
            );
            // property for Z
            propIterator.AddDoubleProp("Z:",
                () => currentValues.DoubleZValue,
                value => { 
                    double newValue = EnsureRotWithinRange(value, -32767, 32767);
                    currentValues.DoubleZValue = newValue;
                }
            );
            // property for RX
            propIterator.AddDoubleProp("RX`:",
                () => currentValues.DoubleRXValue,
                value => { 
                    double newValue = EnsureRotWithinRange(value, -360, 360);
                    currentValues.DoubleRXValue = newValue;
                }
            );
            // property for RY
            propIterator.AddDoubleProp("RY`:",
                () => currentValues.DoubleRYValue,
                value => { 
                    double newValue = EnsureRotWithinRange(value, -360, 360);
                    currentValues.DoubleRYValue = newValue;
                }
            );
            // property for RZ
            propIterator.AddDoubleProp("RZ`:",
                () => currentValues.DoubleRZValue,
                value => { 
                    double newValue = EnsureRotWithinRange(value, -360, 360);
                    currentValues.DoubleRZValue = newValue;
                }
            );
        
            window.SetPropIterator(propIterator);
            // show
            window.SetButtons(MessageBoxHelper.BuildButtons(TUIButtonType.btOk, TUIButtonType.btCancel));
            switch (window.Show())
            {
                case TUIButtonType.btOk:
                    //matrix for shifting
                    var shiftMatrix = new T3DMatrix(
                        new T3DPoint(
                            currentValues.DoubleXValue,
                            currentValues.DoubleYValue,
                            currentValues.DoubleZValue
                        )
                    );

                    // for rotation
                    var tRotationsConverter = new TRotationsConverter();
                    var tLocation = new TLocation(
                        T3DPoint.Zero,
                        new TRotation(
                            currentValues.DoubleRXValue,
                            currentValues.DoubleRYValue,
                            currentValues.DoubleRZValue,
                            0)
                    );
                    var tComplexRotationConvention = new TComplexRotationConvention(
                        TRotationConvention.XYZ,
                        true,
                        true
                    );
                    
                    // rotation matrix
                    var rotMatrix = TRotationsConverter.LocationToMatrix(tLocation, tComplexRotationConvention);
                    // final matrix with rotation & shifting
                    var newMatrix = rotMatrix * shiftMatrix;
                    coordSystemListCom.InvokeAndWrap(coordSystem =>
                        coordSystem.Add("New_LCS", newMatrix, currentValues.EnumIdValue, out var resultStatus));
                    if (resultStatus.Code == TResultStatusCode.rsError)
                        throw new Exception(resultStatus.Description);
                    break;
                case TUIButtonType.btCancel:
                    throw new Exception("Cancel button pressed");
                    break;
                default:
                    throw new Exception("Unknown button pressed");
                    break;
            }
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
    
}