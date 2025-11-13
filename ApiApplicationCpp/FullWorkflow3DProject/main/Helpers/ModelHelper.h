#pragma once
#include "../CamApi.SDK.h"

class ModelHelper {
public:
    /// <summary>
    /// Prepare the model: import file
    /// </summary>
    static HRESULT PrepareModel(ICamApiApplication* application,
        ICamApiProject* project,
        ICamApiPaths* pathsHelper);
};
