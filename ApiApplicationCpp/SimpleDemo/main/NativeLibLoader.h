#pragma once
#include <windows.h>
#include <string>
#include <iostream>

/// <summary>
/// Universal library to load DLL
/// </summary>
class NativeLibLoader {
public:
    static HMODULE LoadDll(const std::string& libName);

    template <typename T>
    static T GetProc(HMODULE hModule, const std::string& funcName) {
        if (!hModule)
            throw std::invalid_argument("Null DLL handle passed to GetProc.");

        FARPROC proc = GetProcAddress(hModule, funcName.c_str());
        if (!proc)
            throw std::runtime_error("Function '" + funcName + "' not found in the DLL.");

        return reinterpret_cast<T>(proc);
    }
};
