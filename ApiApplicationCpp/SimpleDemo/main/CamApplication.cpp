#include "pch.h"
#include "CamApplication.h"
#include "NativeLibLoader.h"
#include <iostream>

/// <summary>
/// Keep the DLL handle inside this translation unit
/// </summary>
static HMODULE hModule = nullptr;

/// <summary>
/// Function pointer type for RunApplication
/// </summary>
using RunSCConsole = int (__stdcall*)(LPCWSTR);

ICamApiApplication* CamApplication::Run(const std::wstring& params) {
    const std::string dllName = "SCKernelConsole.dll";
    if (!hModule)
        hModule = NativeLibLoader::LoadDll(dllName);

    auto runFunc = NativeLibLoader::GetProc<RunApplication>(hModule, "RunApplication");

    // converting wstring → UTF-8
    std::string utf8Params;
    {
        int size = WideCharToMultiByte(CP_UTF8, 0, params.c_str(), -1, nullptr, 0, nullptr, nullptr);
        utf8Params.resize(size - 1);
        WideCharToMultiByte(CP_UTF8, 0, params.c_str(), -1, utf8Params.data(), size, nullptr, nullptr);
    }

    // call RunApplication
    auto rawPtr = runFunc(utf8Params.c_str());
    if (!rawPtr)
        throw std::runtime_error("RunApplication returned nullptr.");

    // build result
    auto* pApp = reinterpret_cast<ICamApiApplication*>(rawPtr);
    std::cout << "ICamApiApplication interface acquired successfully." << std::endl;
    return pApp;
}

void CamApplication::FreeLibrary() {
    if (hModule) {
        auto closeFunc = NativeLibLoader::GetProc<CloseApplication>(hModule, "CloseApplication");
        closeFunc();

        ::FreeLibrary(hModule);
        hModule = nullptr;
        std::cout << "SCKernelConsole.dll unloaded successfully." << std::endl;
    }
}