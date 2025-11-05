using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Project;
using CAMAPI.ResultStatus;

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
    public static void RunSimulation(ComWrapper<ICamApiApplication> applicationCom,
        ComWrapper<ICamApiProject> activeProjectCom)
    {
        // switch to simulation tab
        applicationCom.Invoke(application =>
        {
            application.MainWorkMode = TMainWorkMode.mwmSimulating;
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
            var outputStlFile = Path.Combine(Path.GetTempPath(), OutputStlFile);
            simulator.SaveMachiningResultToSTL(null, outputStlFile, out var ret);
            if (ret.Code == TResultStatusCode.rsError)
                throw new Exception(ret.Description);
        });
    }
    
}