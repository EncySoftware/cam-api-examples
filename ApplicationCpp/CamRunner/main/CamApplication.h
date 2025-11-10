#pragma once
#include <windows.h>
#include <string>
#include "CamApi.SDK.h"

// GUID of the ICamApiApplication interface
extern const IID IID_ICamApiApplication;

// Type of functions exported from the DLL
typedef uintptr_t (__stdcall *RunApplication)(const char* params);
typedef void (__stdcall *CloseApplication)();

class CamApplication {
public:
    static ICamApiApplication* Run(const std::wstring& params);
    static void FreeLibrary();
};

