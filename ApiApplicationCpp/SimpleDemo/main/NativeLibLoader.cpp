#include "pch.h"
#include "NativeLibLoader.h"

HMODULE NativeLibLoader::LoadDll(const std::string& libName) {
    HMODULE h = LoadLibraryA(libName.c_str());
    if (!h) {
        DWORD err = GetLastError();
        throw std::runtime_error("Failed to load DLL '" + libName +
                                 "' (error " + std::to_string(err) + ")");
    }

    std::cout << "DLL successfully loaded: " << libName << std::endl;
    return h;
}


