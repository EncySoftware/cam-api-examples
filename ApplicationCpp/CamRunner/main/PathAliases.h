#pragma once
#include <Unknwn.h>
#include <windows.h>
#include <PathAliasesInterface.h>
#include <string>

// GUID of the IST_PathAliasLibrary interface
extern const IID IID_IST_PathAliasLibrary;

// Type of function exported from the DLL
typedef IUnknown* (__stdcall *GetPathAliasLibPointer)();

class PathAliases {
public:
    static IST_PathAliasLibrary* GetLibrary();
    static IST_AliasesList* InitFolders(const std::wstring& clientDllName, bool includePrIdInPath = false);
    static void FreeLibrary();
};