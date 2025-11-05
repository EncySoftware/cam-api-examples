using System;
using System.IO;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using CAMIPC.Application;
using CAMIPC.ExecuteContext;
using CAMIPC.Project;

namespace FullWorkflow3DProject;

/// <summary>
/// Static class for operations in mode of simulation
/// </summary>
public static class SimulationHelper
{
    /// <summary>
    /// Relative path to the output STL file
    /// </summary>
    private const string OutputStlFile = "Part1_Simulated.stl";
    
    /// <summary>
    /// Run simulation
    /// </summary>
    public static void RunSimulation(ComWrapper<ICamIpcApplication> applicationCom,
        ComWrapper<ICamIpcProject> activeProjectCom)
    {
        var executeContext = new TExecuteContext();
        
        // switch to simulation tab
        applicationCom.Invoke(application =>
        {
            application.SetMainWorkMode(TMainWorkMode.mwmSimulating, ref executeContext);
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
        });
        
        // arrange
        using var simulatorCom = activeProjectCom.InvokeAndWrap(project => project.Simulator);
        
        // run simulation and save results
        simulatorCom.Invoke(simulator =>
        {
            // setup simulation parameters
            simulator.BreakOnStopCommand = false;
            simulator.BreakOnEndOfOperation = false;
            simulator.BreakOnErrors = false;
            
            simulator.CheckGouges = true;
            simulator.CheckHolderCollisions = true;
            simulator.CheckMachineCollisions = true;

            // run simulation
            simulator.ResetSimulationResults();
            simulator.FastSimulateAllOperations();
            
            // save results
            var outputStlFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, OutputStlFile);
            simulator.SaveMachiningResultToSTL(null, outputStlFile, ref executeContext);
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
        });
    }
    
}