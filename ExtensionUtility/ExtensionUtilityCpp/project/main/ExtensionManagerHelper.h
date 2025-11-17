#ifndef EXTENSION_MANAGER_HELPER_H
#define EXTENSION_MANAGER_HELPER_H

#include "pch.h"
#include <windows.h>
#include <stdexcept>

#include "CAMAPI.SDK.h"

class ExtensionManagerHelper {
private:
    static IExtensionManager* FExtensionManager;

public:
    static void Initialize();
    static void Finalize();
    static IExtensionManager* GetInstance();
};

extern struct StaticInitializer staticInitializer;

#endif // EXTENSION_MANAGER_HELPER_H
