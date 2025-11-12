#include "pch.h"
#include <codecvt>
#include <filesystem>
#include <iostream>

#include "PathAliases.h"
#include "CamApplication.h"
#include "StarterNativeUtils.h"

std::wstring Utf8ToWide(const std::string& utf8)
{
    if (utf8.empty()) return L"";
    int size_needed = MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(), -1, nullptr, 0);
    std::wstring wstr(size_needed - 1, 0);
    MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(), -1, &wstr[0], size_needed);
    return wstr;
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
    ICamApiApplication* application;
    TResultStatus resultStatus = {};
    try {
        // init paths
        auto list = PathAliases::InitFolders(L"SCConsole.exe", false);
        if (list) {
            BSTR companyName = nullptr;
            hr = list->get_CompanyName(&companyName);
            if (SUCCEEDED(hr) && companyName) {
                std::wcout << L"CompanyName: " << companyName << std::endl;
                SysFreeString(companyName);
            }
            list->Release();
        }

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
        BSTR path = nullptr;
        hr = application->get_ExecutablePath(&path);
        if (SUCCEEDED(hr)) {
            std::wcout << L"Executable path: " << path << std::endl;
            SysFreeString(path);
        } else {
            wprintf(L"Error: 0x%08X\n", hr);
        }

        // get extension manager
        IExtensionManager* pExtMgr = nullptr;
        hr = application->GetExtensionManager(&resultStatus, &pExtMgr);
        if (FAILED(hr)) {
            throw std::runtime_error("GetExtensionManager failed.");
        }

        // call simple method to be sure extension manager works
        hr = pExtMgr->get_ApiVersion(&path);
        if (SUCCEEDED(hr)) {
            std::wcout << L"Extension Manager API Version: " << path << std::endl;
            SysFreeString(path);
        } else {
            wprintf(L"Error getting API Version: 0x%08X\n", hr);
        }
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
