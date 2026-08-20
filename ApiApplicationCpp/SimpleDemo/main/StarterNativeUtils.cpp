#include "pch.h"
#include <Unknwn.h>
#include "StarterNativeUtils.h"
#include "NativeLibLoader.h"

#include <comutil.h>
#include <chrono>

/// <summary>
/// Keep the DLL handle inside this translation unit
/// </summary>
static HMODULE hModule = nullptr;

ISCStarterNativeUtils* StarterNativeUtils::GetLibrary() {
    if (hModule == nullptr) {
        const std::string dllName = "SCStarterNativeUtils.dll";
        hModule = NativeLibLoader::LoadDll(dllName);
    }

    // call
    auto getLibFunc = NativeLibLoader::GetProc<GetLibPointer>(
        hModule, "GetLibPointer"
    );
    if (!getLibFunc)
        throw std::runtime_error("Failed to get GetLibPointer function.");
    auto rawPtr = getLibFunc();
    if (!rawPtr)
        throw std::runtime_error("GetLibPointer returned nullptr.");

    // build result
    auto* pLib = reinterpret_cast<ISCStarterNativeUtils*>(rawPtr);
    std::cout << "ISCStarterNativeUtils interface acquired successfully." << std::endl;
    return pLib;
}

IST_Logger* StarterNativeUtils::InitLogs(bool aprocDependentFileName) {
    ISCStarterNativeUtils* lib = GetLibrary();

    HRESULT hr = lib->InitLogs(aprocDependentFileName);
    if (FAILED(hr))
        throw std::runtime_error("InitLogs failed, HRESULT=0x" + std::to_string(hr));

    std::cout << "InitLogs executed successfully." << std::endl;

    IST_Logger* logger = nullptr;
    hr = lib->Logger(&logger);
    if (FAILED(hr) || !logger)
        throw std::runtime_error("Logger() failed, HRESULT=0x" + std::to_string(hr));

    std::cout << "Logger interface acquired successfully." << std::endl;
    return logger;
}

void StarterNativeUtils::FreeLibrary() {
    if (hModule) {
        ::FreeLibrary(hModule);
        hModule = nullptr;
        std::cout << "StarterNativeUtils.dll unloaded successfully." << std::endl;
    }
}
