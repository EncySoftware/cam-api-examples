#pragma once
#include "../CamApi.SDK.h"

class TechnologyHelper {
public:
    /// <summary>
    /// Create technology with several operations
    /// </summary>
    static HRESULT CreateTechnology(ICamApiApplication* application,
                                    ICamApiProject* project,
                                    ICamApiTechnologist* technologist,
                                    ICAMAPIGeometryModel* geom_model,
                                    ICamApiPaths* pathsHelper);
};
