#include "pch.h"
#include <string>
#include <comutil.h>
#include "ModelHelper.h"
#include "../Utils.h"

#include <iostream>
#include <filesystem>

namespace fs = std::filesystem;

/// <summary>
/// Prepare the model: import file
/// </summary>
HRESULT ModelHelper::PrepareModel(ICamApiApplication* application,
    ICamApiProject* project,
    ICamApiPaths* pathsHelper) {
    const std::wstring importFilePath = L"Milling_25D\\Part1.igs";

    TResultStatus resultStatus = {};
    HRESULT result = S_OK;
    BSTR modelsFolder = nullptr;
    ICAMAPIGeometryImporter* importer = nullptr;

    try {
        // switch to model tab
        HRESULT hr = application->put_MainWorkMode(mwmModel);
        if (FAILED(hr))
            return hr;

        // get the path to the file we will import
        hr = pathsHelper->get_ModelsFolder(&modelsFolder);
        if (FAILED(hr) || !modelsFolder)
            throw std::runtime_error("get_ModelsFolder failed.");
        fs::path importFile = fs::path(modelsFolder) / importFilePath;
        std::cout << "Import file: " << importFile << "\n";
        if (!fs::exists(importFile))
            throw std::runtime_error("Cannot find file to import: " + importFile.string());

        // get importer
        hr = project->get_GeomImporter(&importer);
        if (FAILED(hr) || !importer)
            throw std::runtime_error("get_GeomImporter failed.");

        // import the file
        const _bstr_t bstrImportFile(importFile.native().c_str());
        const _bstr_t targetFolder(L"Part");
        hr = importer->ImportFile(bstrImportFile, targetFolder, false, &resultStatus);
        if (resultStatus.Code == rsError)
            throw std::runtime_error("ImportFile failed: " + Utils::BSTRToString(resultStatus.Description));
        if (FAILED(hr))
            throw std::runtime_error("ImportFile failed.");

        std::cout << "Model import completed." << std::endl;
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in PrepareModel(): " << ex.what() << std::endl;
        result = E_FAIL;
    }

    // clean
    if (importer)
        importer->Release();
    if (modelsFolder)
        SysFreeString(modelsFolder);

    // return
    return result;
}