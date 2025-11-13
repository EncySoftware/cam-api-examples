#include "pch.h"
#include <codecvt>
#include <comutil.h>
#include <filesystem>
#include <iostream>

#include "PathAliases.h"
#include "CamApplication.h"
#include "Helpers/ModelHelper.h"
#include "Helpers/ProjectHelper.h"
#include "StarterNativeUtils.h"
#include "Helpers/TechnologyHelper.h"

std::wstring Utf8ToWide(const std::string& utf8)
{
    if (utf8.empty()) return L"";
    int size_needed = MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(), -1, nullptr, 0);
    std::wstring wstr(size_needed - 1, 0);
    MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(), -1, &wstr[0], size_needed);
    return wstr;
}

HRESULT run(ICamApiApplication* application) {
    HRESULT result = S_OK;
    TResultStatus resultStatus = {};
    IExtensionManager* extension_manager = nullptr;
    IExtension* extension = nullptr;
    ICamApiPaths* cam_paths = nullptr;
    ICamApiProject* project = nullptr;
    ICAMAPIGeometryModel* geom_model = nullptr;
    ICamApiTechnologist* technologist = nullptr;

    try {
        // get extension manager
        HRESULT hr = application->GetExtensionManager(&resultStatus, &extension_manager);
        if (FAILED(hr))
            throw std::runtime_error("GetExtensionManager failed.");

        // get object to access CAM system paths
        _bstr_t name(L"Extension.Global.Singletons.Paths");
        hr = extension_manager->GetSingletonExtension(name, &resultStatus, &extension);
        if (FAILED(hr) || !extension)
            throw std::runtime_error("GetSingletonExtension failed.");

        hr = extension->QueryInterface(IID_ICamApiPaths, (void**)&cam_paths);
        if (FAILED(hr) || !cam_paths)
            throw std::runtime_error("QueryInterface for ICamApiPaths failed.");

        // get active project
        hr = application->GetActiveProject(&resultStatus, &project);
        if (FAILED(hr) || !project)
            throw std::runtime_error("GetActiveProject failed.");

        // get manager for geometry model
        hr = project->get_CAMAPIGeomModel(&geom_model);
        if (FAILED(hr) || !geom_model)
            throw std::runtime_error("get_CAMAPIGeomModel failed.");

        // get manager over technology
        hr = project->get_Technologist(&technologist);
        if (FAILED(hr) || !technologist)
            throw std::runtime_error("get_Technologist failed.");

        // prepare model
        hr = ModelHelper::PrepareModel(application, project, cam_paths);
        if (FAILED(hr))
            throw std::runtime_error("ModelHelper::PrepareModel failed.");

        // prepare the project
        hr = ProjectHelper::PrepareProject(application, cam_paths);
        if (FAILED(hr))
            throw std::runtime_error("ProjectHelper::PrepareProject failed.");

        // create technology
        hr = TechnologyHelper::CreateTechnology(application, project, technologist, geom_model, cam_paths);
        if (FAILED(hr))
            throw std::runtime_error("TechnologyHelper::CreateTechnology failed.");

        // save project
        hr = ProjectHelper::SaveProject(application);
        if (FAILED(hr))
            throw std::runtime_error("ProjectHelper::SaveProject failed.");
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in run(): " << ex.what() << std::endl;
        result = E_FAIL;
    }

    // clean
    if (technologist)
        technologist->Release();
    if (geom_model)
        geom_model->Release();
    if (project)
        project->Release();
    if (cam_paths)
        cam_paths->Release();
    if (extension)
        extension->Release();
    if (extension_manager)
        extension_manager->Release();

    // return
    return result;
}

int main(int argc, char* argv[]) {
    std::cout << "Current working directory: "
          << std::filesystem::current_path()
          << std::endl;

    HRESULT hr = CoInitialize(nullptr);
    if (FAILED(hr)) {
        std::cerr << "COM initialization failed: 0x" << std::hex << hr << std::endl;
        return 1;
    }

    IST_Logger* logger = nullptr;
    ICamApiApplication* application = nullptr;
    try {
        // init paths
        auto list = PathAliases::InitFolders(L"SCConsole.exe", false);

        // init logger
        logger = StarterNativeUtils::InitLogs(false);
        if (!logger)
            throw std::runtime_error("Logger initialization failed.");

        // convert params
        std::wcout << L"Received " << argc - 1 << L" argument(s):" << std::endl;
        for (int i = 1; i < argc; ++i) {
            std::wcout << L"  arg" << i << L": " << argv[i] << std::endl;
        }

        std::wstring params;
        for (int i = 1; i < argc; ++i) {
            if (i > 1) params += L" ";
            params += Utf8ToWide(argv[i]);
        }

        // run CAM
        application = CamApplication::Run(params);
        if (!application)
            throw std::runtime_error("CamApplication::Run returned nullptr.");

        // execute main logic
        hr = run(application);
        if (FAILED(hr))
            throw std::runtime_error("run() function failed.");
    }
    catch (const std::exception& ex) {
        std::cerr << "Error: " << ex.what() << std::endl;
    }

    // dispose objects
    if (logger) {
        logger->Release();
    }
    CamApplication::FreeLibrary();
    StarterNativeUtils::FreeLibrary();
    PathAliases::FreeLibrary();
    CoUninitialize();
    return 0;
}
