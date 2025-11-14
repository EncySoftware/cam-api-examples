using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;

using Geometry.VecMatrLib;
using STTypes;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using CAMAPI.Logger;
using CAMAPI.UIDialogs.DotnetHelper;

namespace ExtensionOperationFlipToolNet;

/// <summary>
/// Extension to create operation in the current project
/// </summary>
internal class ExtensionOperationFlipTool : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    private static ToolParameters ShowAxesDialog(List<AxesInfo> axes)
    {
        ToolParameters result = null;

        var thread = new Thread(() =>
        {
            var window = new TextInputWindow(axes);
            window.SettingsApplied += (settings) =>
            {
                result = settings;
            
            };
            if (window.ShowDialog() == true)
                result = window.Settings;
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return result;
    }
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
            using var projectCom = applicationCom.InvokeAndWrap(application =>
                application.GetActiveProject(out var resultStatus));
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(resultStatus.Description);

            // get technologist
            using var technologistCom = projectCom.InvokeAndWrap(project => project.Technologist);

            // get current operation
            using var currentOperationCom = technologistCom.InvokeAndWrap(technologist => technologist.CurrentOperation);

            // get machine configuration
            using var machineConfigurationCom = currentOperationCom.InvokeAndWrap(op => op.MachineConfiguration);
            // axes
            var axesLines = new List<AxesInfo>();
            machineConfigurationCom.Invoke(machineConfiguration =>
            {
                var axesCount = machineConfiguration.AxesCount;
                for (var i = 0; i < axesCount; i++)
                {
                    var axisId = machineConfiguration.AxisId[i];
                    var axisDefined = machineConfiguration.AxisDefined[i];
                    var axisValue = machineConfiguration.AxisValue[i];
                    var axisAvailable = machineConfiguration.AxisAvailable[i];

                    if (axisAvailable)
                    {
                        axesLines.Add(
                            new AxesInfo { Id = axisId, Defined = axisDefined, Value = axisValue }
                        );
                    }
                }
            });

            var settings = ShowAxesDialog(axesLines);
            if (settings == null)
                return;

            var axesDict = settings.Axes.ToDictionary(a => a.Id, a => a);
            machineConfigurationCom.Invoke(machineConfiguration =>
            {
                var axesCount = machineConfiguration.AxesCount;
                for (var i = 0; i < axesCount; i++)
                {
                    var axesId = machineConfiguration.AxisId[i];
                    if (settings.AxesIds.Contains(axesId) && axesDict.TryGetValue(axesId, out var axesInfo))
                    {
                        //var axesInfo = settings.Axes[i];
                        machineConfiguration.AxisDefined[i] = axesInfo.Defined;
                        machineConfiguration.AxisValue[i] = axesInfo.Value;
                    }
                }
            });
        

            // operation 
            using var machineCom = projectCom.InvokeAndWrap(project => project.Machine);
            using var evaluatorCom = machineCom.InvokeAndWrap(machine => machine.CreateEvaluator());
            var evaluator = evaluatorCom.It;

            // filled in the properties for a specific operation
            currentOperationCom.Invoke(operation =>
            {
                operation.InitMachineEvaluator(evaluator, out var ret);
                if (ret.Code == TResultStatusCode.rsError)
                    throw new Exception(ret.Description);
            });

            // point and normal position of the tool tip at the current moment in time with the current operation parameters
            var position = new TST5DPoint
            {
                P = evaluator.GetAbsoluteMatrix().vT,
                n = evaluator.GetAbsoluteMatrix().vZ
            };

            // tool below
            position.n.X = 0;
            position.n.Y = 0;
            position.n.Z = -1;

            if (!evaluator.CalcNextPos5D(position, false, false, true))
                throw new Exception("Cannot calculate machine position for given point");

            // position the machine
            evaluator.SetNextPos(false);

            // update in configuration
            using var machineConfigCom = currentOperationCom.InvokeAndWrap(op => op.MachineConfiguration);
            machineConfigCom.Invoke(machineConfig =>
            {
                machineConfig.SetAxesValues(evaluator, true, out var resultStatus);
                if (resultStatus.Code == TResultStatusCode.rsError)
                    throw new Exception(resultStatus.Description);
            });

        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}