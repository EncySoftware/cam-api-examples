#pragma once
#include "../CamApi.SDK.h"

class ProjectHelper {
public:
    /// <summary>
    /// Prepare the project: change machine, setup machining tools
    /// </summary>
    static HRESULT PrepareProject(ICamApiApplication* application, ICamApiPaths* pathsHelper);

    /// <summary>
    /// Save current project into file
    /// </summary>
    static HRESULT SaveProject(ICamApiApplication* application);
};