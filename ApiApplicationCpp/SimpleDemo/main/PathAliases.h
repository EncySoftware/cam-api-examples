#pragma once
#include <Unknwn.h>
#include <PathAliasesInterface.h>
#include <string>

/// <summary>
/// Type of function exported from the DLL
/// </summary>
typedef IUnknown* (__stdcall *GetPathAliasLibPointer)();

class PathAliases {
public:
    static IST_PathAliasLibrary* GetLibrary();
    static IST_AliasesList* InitFolders(const std::wstring& clientDllName, bool includePrIdInPath = false);
    static void FreeLibrary();
};