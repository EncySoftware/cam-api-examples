#pragma once
#include <STLoggingInterface.h>
#include <AssemblyLoaderTypes.h>
#include <AppStarterInterface.h>

// Type of function exported from the DLL
typedef IUnknown* (__stdcall *GetLibPointer)();

class StarterNativeUtils {
public:
    static ISCStarterNativeUtils* GetLibrary();
    static IST_Logger* InitLogs(bool aprocDependentFileName);
    static void FreeLibrary();
};