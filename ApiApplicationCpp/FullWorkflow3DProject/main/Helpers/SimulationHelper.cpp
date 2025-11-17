#include "pch.h"
#include "SimulationHelper.h"

#include <comutil.h>
#include <filesystem>
#include <iostream>
#include <ostream>

#include "Utils.h"

/// <summary>
/// Run simulation
/// </summary>
HRESULT SimulationHelper::RunSimulation(ICamApiApplication *application, ICamApiProject *project) {
    TResultStatus resultStatus = {};
    HRESULT result = S_OK;

    ICamApiSimulator* simulator = nullptr;
    try {
        // switch to model tab
        HRESULT hr = application->put_MainWorkMode(mwmModel);
        if (FAILED(hr))
            return hr;

        // get simulator
        hr = project->get_Simulator(&simulator);
        if (FAILED(hr) || !simulator)
            throw std::runtime_error("get_Simulator failed.");

        // setup simulation parameters
        hr = simulator->put_BreakOnStopCommand(VARIANT_FALSE);
        if (FAILED(hr))
            throw std::runtime_error("put_BreakOnStopCommand failed.");

        hr = simulator->put_BreakOnEndOfOperation(VARIANT_FALSE);
        if (FAILED(hr))
            throw std::runtime_error("put_BreakOnEndOfOperation failed.");

        hr = simulator->put_BreakOnErrors(VARIANT_FALSE);
        if (FAILED(hr))
            throw std::runtime_error("put_BreakOnErrors failed.");

        hr = simulator->put_CheckGouges(VARIANT_TRUE);
        if (FAILED(hr))
            throw std::runtime_error("put_CheckGouges failed.");

        hr = simulator->put_CheckHolderCollisions(VARIANT_TRUE);
        if (FAILED(hr))
            throw std::runtime_error("put_CheckHolderCollisions failed.");

        hr = simulator->put_CheckMachineCollisions(VARIANT_TRUE);
        if (FAILED(hr))
            throw std::runtime_error("put_CheckMachineCollisions failed.");

        // run simulation
        hr = simulator->ResetSimulationResults();
        if (FAILED(hr))
            throw std::runtime_error("ResetSimulationResults failed.");

        hr = simulator->FastSimulateAllOperations();
        if (FAILED(hr))
            throw std::runtime_error("FastSimulateAllOperations failed.");

        // save results
        const auto outputStlFile = std::filesystem::current_path() / L"Part1_Simulated.stl";

        const _bstr_t bstrOutputStlFile(outputStlFile.wstring().c_str());
        hr = simulator->SaveMachiningResultToSTL(nullptr,bstrOutputStlFile,&resultStatus);
        if (resultStatus.Code == rsError)
            throw std::runtime_error("SaveMachiningResultToSTL failed: " + Utils::BSTRToString(resultStatus.Description));
        if (FAILED(hr))
            throw std::runtime_error("SaveMachiningResultToSTL failed.");

        std::cout << "Simulation completed. Result saved to: " << outputStlFile.string() << std::endl;
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in RunSimulation(): " << ex.what() << std::endl;
        result = E_FAIL;
    }

    // clean
    if (simulator)
        simulator->Release();

    // return
    return result;
}
