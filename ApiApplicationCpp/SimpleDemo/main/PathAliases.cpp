#include "pch.h"
#include "PathAliases.h"
#include "NativeLibLoader.h"

/// <summary>
/// Cache the DLL handle inside this translation unit
/// </summary>
static HMODULE hModule = nullptr;

IST_PathAliasLibrary* PathAliases::GetLibrary() {
    if (hModule == nullptr) {
        const std::string dllName = "PathAliases.dll";
        hModule = NativeLibLoader::LoadDll(dllName);
    }

    // call
    auto getPathAliasLibrary = NativeLibLoader::GetProc<GetPathAliasLibPointer>(
        hModule, "GetPathAliasLibPointer"
    );
    if (!getPathAliasLibrary)
        throw std::runtime_error("Failed to get GetPathAliasLibPointer function.");
    auto rawPtr = getPathAliasLibrary();
    if (!rawPtr)
        throw std::runtime_error("GetPathAliasLibPointer returned nullptr.");

    // build result
    auto* pLib = reinterpret_cast<IST_PathAliasLibrary*>(rawPtr);
    std::cout << "IST_PathAliasLibrary interface acquired successfully." << std::endl;
    return pLib;
}

IST_AliasesList* PathAliases::InitFolders(const std::wstring& clientDllName, bool includePrIdInPath) {
    // Get COM interface
    IST_PathAliasLibrary* lib = GetLibrary();

    // Call InitializeCAMFolders2
    IST_AliasesList* list = nullptr;
    HRESULT hr = lib->InitializeCAMFolders2(SysAllocString(clientDllName.c_str()), includePrIdInPath, &list);
    if (FAILED(hr))
        throw std::runtime_error("InitializeCAMFolders2 failed, HRESULT=0x" + std::to_string(hr));

    if (!list)
        throw std::runtime_error("InitializeCAMFolders2 returned null list pointer.");

    std::wcout << L"InitializeCAMFolders2 succeeded for: " << clientDllName << std::endl;
    return list;
}

void PathAliases::FreeLibrary() {
    if (hModule) {
        ::FreeLibrary(hModule);
        hModule = nullptr;
        std::cout << "PathAliases.dll unloaded successfully." << std::endl;
    }
}
