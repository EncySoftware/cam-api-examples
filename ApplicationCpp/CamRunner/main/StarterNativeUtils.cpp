#include "pch.h"
#include <Unknwn.h>
#include "StarterNativeUtils.h"
#include "NativeLibLoader.h"

#include <comutil.h> // для SysAllocString и _bstr_t (если нужно)
#include <chrono>

const IID IID_ISCStarterNativeUtils =
{ 0x8737E4D6, 0xAD3B, 0x4145, { 0x96, 0x4A, 0x0E, 0x2A, 0x2E, 0x0A, 0x51, 0xD6 } };

// Keep the DLL handle inside this translation unit
static HMODULE hModule = nullptr;

ISCStarterNativeUtils* StarterNativeUtils::GetLibrary() {
    if (hModule == nullptr) {
        const std::string dllName = "SCStarterNativeUtils.dll";
        hModule = NativeLibLoader::LoadDll(dllName);
    }

    auto getLibFunc = NativeLibLoader::GetProc<GetLibPointer>(
        hModule, "GetLibPointer"
    );

    IUnknown* pUnknown = getLibFunc();
    if (!pUnknown)
        throw std::runtime_error("GetLibPointer returned nullptr.");

    ISCStarterNativeUtils* pLib = nullptr;
    HRESULT hr = pUnknown->QueryInterface(IID_ISCStarterNativeUtils, reinterpret_cast<void**>(&pLib));

    if (FAILED(hr))
        throw std::runtime_error("QueryInterface for ISCStarterNativeUtils failed, HRESULT=0x" +
                                 std::to_string(hr));
    pUnknown->Release();

    std::cout << "ISCStarterNativeUtils interface acquired successfully." << std::endl;
    return pLib;
}

IST_Logger* StarterNativeUtils::InitLogs(bool aprocDependentFileName) {
    ISCStarterNativeUtils* lib = GetLibrary();
    if (!lib)
        throw std::runtime_error("Failed to get ISCStarterNativeUtils instance.");

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
