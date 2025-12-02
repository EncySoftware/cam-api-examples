#pragma once
#include "../CamApi.SDK.h"

class SimulationHelper {
public:
    /// <summary>
    /// Run simulation
    /// </summary>
    static HRESULT RunSimulation(ICamApiApplication* application, ICamApiProject* project);
};
