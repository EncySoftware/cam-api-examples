#include "pch.h"
#include "PathAliases.h"
#include "NativeLibLoader.h"

const IID IID_IST_PathAliasLibrary =
{ 0x4D6DAEC4, 0x3A84, 0x4D68, { 0x84, 0xDB, 0x2C, 0x6D, 0xAA, 0xDC, 0x27, 0x3F } };

// Keep the DLL handle inside this translation unit
static HMODULE hModule = nullptr;

IST_PathAliasLibrary* PathAliases::GetLibrary() {
    if (hModule == nullptr) {
        const std::string dllName = "PathAliases.dll";
        hModule = NativeLibLoader::LoadDll(dllName);
    }

    auto getPathAliasLibrary = NativeLibLoader::GetProc<GetPathAliasLibPointer>(
        hModule, "GetPathAliasLibPointer"
    );

    IUnknown* pUnknown = getPathAliasLibrary();
    if (!pUnknown)
        throw std::runtime_error("GetPathAliasLibPointer returned nullptr.");

    IST_PathAliasLibrary* pLib = nullptr;
    HRESULT hr = pUnknown->QueryInterface(IID_IST_PathAliasLibrary, reinterpret_cast<void**>(&pLib));

    if (FAILED(hr))
        throw std::runtime_error("QueryInterface for IST_PathAliasLibrary failed, HRESULT=0x" +
                                 std::to_string(hr));
    pUnknown->Release();

    std::cout << "IST_PathAliasLibrary interface acquired successfully." << std::endl;
    return pLib;
}

IST_AliasesList* PathAliases::InitFolders(const std::wstring& clientDllName, bool includePrIdInPath) {
    // Get COM interface
    IST_PathAliasLibrary* lib = GetLibrary();
    if (!lib)
        throw std::runtime_error("Failed to get IST_PathAliasLibrary instance.");

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
