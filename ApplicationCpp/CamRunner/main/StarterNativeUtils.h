#pragma once
#include <windows.h>
#include <STLoggingInterface.h>
#include <AssemblyLoaderTypes.h>
#include <AppStarterInterface.h>

// GUID of the ISCStarterNativeUtils interface
extern const IID IID_ISCStarterNativeUtils;

// Type of function exported from the DLL
typedef IUnknown* (__stdcall *GetLibPointer)();

class StarterNativeUtils {
public:
    static ISCStarterNativeUtils* GetLibrary();
    static IST_Logger* InitLogs(bool aprocDependentFileName);
    static void FreeLibrary();
};