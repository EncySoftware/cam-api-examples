using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;
using CAMAPI.Project;

using System.Reflection.Emit;
using System.Runtime.InteropServices;
using CAMAPI.Logger;
using CAMAPI.UIDialogs.DotnetHelper;
using System.Reflection.PortableExecutable;

namespace ExtensionMachinePropsChangeNet;

/// <summary>
/// Extension to create operation in the current project
/// </summary>
internal class ExtensionMachinePropsChange : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Rename operations in the current project
    /// </summary>
    /// <param name="context">Information about current running instance</param>
    /// <param name="resultStatus">Structure to return error</param>
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        // We use ComWrapper everywhere to limit the lifetime of the COM objects and 
        // make it predicable for CAM system and not rely on the garbage collector.
        try
        {
            using var applicationCom = ComWrapper.Create(context.CamApplication);

            // catch an active project
            using var activeProjectCom = applicationCom.InvokeAndWrap(application =>
                application.GetActiveProject(out var resultStatus));
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(resultStatus.Description);

            // get machine 
            using var machineCom = activeProjectCom.InvokeAndWrap(project => project.Machine);
            using var machineXMLPropCom = machineCom.InvokeAndWrap(machine => 
                machine.XMLProp);

            // for ABB IRB2400-16 Laser universal
            machineXMLPropCom.Invoke(machineXMLProp =>
            {
                machineXMLProp.Flt["MachineDimensions.SpindleModel.RotationAngleZ"] = 90;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead1.HeadPosition.ToolLength"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead1.HeadPosition.X"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead1.HeadPosition.Y"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead1.HeadPosition.Z"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead1.HeadPosition.q1"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead1.HeadPosition.q2"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead1.HeadPosition.q3"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead1.HeadPosition.q4"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead1.ImageOrientation.RX"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead1.ImageOrientation.RY"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead1.ImageOrientation.RZ"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead1.ImageOrientation.FocusToHead"] = 10;
                
                
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead2.HeadPosition.ToolLength"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead2.HeadPosition.X"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead2.HeadPosition.Y"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead2.HeadPosition.Z"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead2.HeadPosition.q1"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead2.HeadPosition.q2"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead2.HeadPosition.q3"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead2.HeadPosition.q4"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead2.ImageOrientation.RX"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead2.ImageOrientation.RY"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead2.ImageOrientation.RZ"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead2.ImageOrientation.FocusToHead"] = 10;
                

                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead3.HeadPosition.ToolLength"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead3.HeadPosition.X"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead3.HeadPosition.Y"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead3.HeadPosition.Z"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead3.HeadPosition.q1"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead3.HeadPosition.q2"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead3.HeadPosition.q3"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead3.HeadPosition.q4"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead3.ImageOrientation.RX"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead3.ImageOrientation.RY"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead3.ImageOrientation.RZ"] = 10;
                machineXMLProp.Flt["Schema.Rails1.Rails2.Rails3.RobotFrame.AxisA1.AxisA2.AxisA3.AxisA4.AxisA5.AxisA6.ToolBlockSelector.MultiToolBlock.LaserHead3.ImageOrientation.FocusToHead"] = 10;
                

                machineXMLProp.Flt["MachineDimensions.TableCenter.X"] =1000;
                machineXMLProp.Flt["MachineDimensions.TableCenter.Y"] =0;
                machineXMLProp.Flt["MachineDimensions.TableCenter.Z"] =540;
                machineXMLProp.Flt["MachineDimensions.TableCenter.q1"] =0;
                machineXMLProp.Flt["MachineDimensions.TableCenter.q2"] =0;
                machineXMLProp.Flt["MachineDimensions.TableCenter.q3"] =0;
                machineXMLProp.Flt["MachineDimensions.TableCenter.q4"] =0;
                
                
                machineXMLProp.Flt["MachineDimensions.SpindleCenter.X"] =-1000;
                machineXMLProp.Flt["MachineDimensions.SpindleCenter.Y"] =500;
                machineXMLProp.Flt["MachineDimensions.SpindleCenter.Z"] =625;
                machineXMLProp.Flt["MachineDimensions.SpindleCenter.q1"] =90;
                machineXMLProp.Flt["MachineDimensions.SpindleCenter.q2"] =-90;
                machineXMLProp.Flt["MachineDimensions.SpindleCenter.q3"] =0;
                machineXMLProp.Flt["MachineDimensions.SpindleCenter.q4"] =0;
            });           
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}