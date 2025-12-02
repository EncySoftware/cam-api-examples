#include "pch.h"
#include <iostream>
#include <filesystem>
#include <string>
#include <comutil.h>

#include "ProjectHelper.h"
#include "../Utils.h"

namespace fs = std::filesystem;

/// <summary>
/// Change machine of the project
/// </summary>
HRESULT ChangeMachine(ICamApiApplication* application, ICamApiPaths* pathsHelper) {
    TResultStatus resultStatus = {};
    HRESULT result = S_OK;
    const std::wstring machine_file_path = L"Schemas\\Milling\\4-axis\\Lider\\Lider.xml";

    BSTR machines_folder = nullptr;
    ICamApiMachinesLibrary* machines_library = nullptr;
    ICamApiMachineInfo* machine_info = nullptr;

    try {
        // get the path to the machine file
        HRESULT hr = pathsHelper->get_MachinesFolder(&machines_folder);
        if (FAILED(hr) || !machines_folder)
            throw std::runtime_error("get_MachinesFolder failed.");
        fs::path machine_file = fs::path(machines_folder) / machine_file_path;
        if (!fs::exists(machine_file))
            throw std::runtime_error("Cannot find machine file: " + machine_file.string());

        // find the machine in the library
        hr = application->get_MachinesLibrary(&machines_library);
        if (FAILED(hr) || !machines_library)
            throw std::runtime_error("get_MachinesLibrary failed.");

        GUID machineGuid;
        hr = CLSIDFromString(L"{F4BD0FFD-8C44-4CFC-A865-CF595EEEF38F}", &machineGuid);
        if (FAILED(hr))
            throw std::runtime_error("Invalid machine GUID");

        _bstr_t bstrMachineFile(machine_file.native().c_str());
        _bstr_t bstrName(L"DMU60");
        hr = machines_library->FindMachine(machineGuid, bstrMachineFile, bstrName, &machine_info);
        if (FAILED(hr))
            throw std::runtime_error("FindMachine failed.");
        if (!machine_info)
            throw std::runtime_error("Machine DMU60 not found in library.");

        // set the machine to the project
        application->SetActiveProjectMachine(machine_info, &resultStatus);
        if (resultStatus.Code == rsError)
            throw std::runtime_error("SetActiveProjectMachine failed: " + Utils::BSTRToString(resultStatus.Description));
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in ChangeMachine(): " << ex.what() << std::endl;
        result = E_FAIL;
    }

    // clean
    if (machine_info)
        machine_info->Release();
    if (machines_library)
        machines_library->Release();
    if (machines_folder)
        SysFreeString(machines_folder);

    // return
    return result;
}

HRESULT AddToolToProject(ICamApiMachiningToolsManager* machining_tool_manager,
    BSTR LibraryPath,
    BSTR ToolID) {
    TResultStatus resultStatus = {};
    auto error_header = "AddToolToProject failed for tool " + Utils::BSTRToString(ToolID) + ": ";

    HRESULT hr = machining_tool_manager->AddToolToProject(LibraryPath, ToolID, &resultStatus);
    if (resultStatus.Code == rsError)
        throw std::runtime_error(error_header + Utils::BSTRToString(resultStatus.Description));
    if (FAILED(hr))
        throw std::runtime_error(error_header + "COM call failed.");
    return S_OK;
}

/// <summary>
/// Setup machining tools
/// </summary>
HRESULT SetupMachiningTools(ICamApiApplication* application, ICamApiPaths* pathsHelper) {
    HRESULT result = S_OK;

    BSTR library_folder = nullptr;

    ICamApiMachiningToolsManager* machining_tool_manager = nullptr;
    try {
        // path to the tool library
        HRESULT hr = pathsHelper->get_LibrariesFolder(&library_folder);
        if (FAILED(hr) || !library_folder)
            throw std::runtime_error("get_LibrariesFolder failed.");
        fs::path tools_folder = fs::path(library_folder) / L"Tools" / L"Examples";
        fs::path tools_library_path = tools_folder / L"ToolKit.db";
        if (!fs::exists(tools_library_path))
            throw std::runtime_error("Cannot find tool library: " + tools_library_path.string());
        _bstr_t bstrLibraryPath(tools_library_path.native().c_str());

        // add
        hr = application->get_MachiningToolsManager(&machining_tool_manager);
        if (FAILED(hr) || !machining_tool_manager)
            throw std::runtime_error("get_MachiningToolsManager failed.");

        _bstr_t bstrToolId_11(L"11");
        AddToolToProject(machining_tool_manager, bstrLibraryPath, bstrToolId_11);

        _bstr_t bstrToolId_12(L"12");
        AddToolToProject(machining_tool_manager, bstrLibraryPath, bstrToolId_12);

        _bstr_t bstrToolId_35(L"35");
        AddToolToProject(machining_tool_manager, bstrLibraryPath, bstrToolId_35);

        _bstr_t bstrToolId_75(L"75");
        AddToolToProject(machining_tool_manager, bstrLibraryPath, bstrToolId_75);

        _bstr_t bstrToolId_63(L"63");
        AddToolToProject(machining_tool_manager, bstrLibraryPath, bstrToolId_63);
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in SetupMachiningTools(): " << ex.what() << std::endl;
        result = E_FAIL;
    }

    // clean
    if (machining_tool_manager)
        machining_tool_manager->Release();
    if (library_folder)
        SysFreeString(library_folder);

    // return
    return result;
}

/// <summary>
/// Prepare the project: change machine, setup machining tools
/// </summary>
HRESULT ProjectHelper::PrepareProject(ICamApiApplication* application,
    ICamApiPaths* pathsHelper) {
    HRESULT result = S_OK;

    try {
        // switch to model tab
        HRESULT hr = application->put_MainWorkMode(mwmMachining);
        if (FAILED(hr))
            throw std::runtime_error("put_MainWorkMode failed.");

        // change machine
        hr = ChangeMachine(application, pathsHelper);
        if (FAILED(hr))
            throw std::runtime_error("ChangeMachine failed.");

        // setup machining tools
        hr = SetupMachiningTools(application, pathsHelper);
        if (FAILED(hr))
            throw std::runtime_error("SetupMachiningTools failed.");
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in PrepareProject(): " << ex.what() << std::endl;
        result = E_FAIL;
    }

    // return
    return result;
}

HRESULT ProjectHelper::SaveProject(ICamApiApplication *application) {
    TResultStatus resultStatus = {};
    HRESULT result = S_OK;

    try {
        fs::path current = fs::current_path();
        fs::path project_file = current / "result_project.stcp";
        if (!fs::exists(project_file))
            fs::remove(project_file);
        _bstr_t bstrProjectFile(project_file.native().c_str());
        HRESULT hr = application->SaveCurrentProject(bstrProjectFile, &resultStatus);
        if (resultStatus.Code == rsError)
            throw std::runtime_error("SaveCurrentProject failed: " + Utils::BSTRToString(resultStatus.Description));
        if (FAILED(hr))
            throw std::runtime_error("SaveCurrentProject failed.");
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in SaveProject(): " << ex.what() << std::endl;
        return E_FAIL;
    }

    // return
    return result;
}
