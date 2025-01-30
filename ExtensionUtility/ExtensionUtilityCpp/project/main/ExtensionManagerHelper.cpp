#include "pch.h"
#include "ExtensionManagerHelper.h"

IExtensionManager* ExtensionManagerHelper::FExtensionManager = nullptr;

void ExtensionManagerHelper::Initialize() {
    FExtensionManager = nullptr;
}

void ExtensionManagerHelper::Finalize() {
    FExtensionManager = nullptr;
}

IExtensionManager* ExtensionManagerHelper::GetInstance() {
    if (!FExtensionManager) {
        HMODULE handle = GetModuleHandleA("CAMAPI.ExtensionManager.dll");
        if (!handle) {
            throw std::runtime_error("Error getting handle to CAMAPI.ExtensionManager.dll");
        }

        using GetExtensionManagerDelegate = uintptr_t(*)();
        GetExtensionManagerDelegate GetExtensionManager =
            reinterpret_cast<GetExtensionManagerDelegate>(
                GetProcAddress(handle, "GetExtensionManager")
                );

        if (!GetExtensionManager) {
            throw std::runtime_error("Error locating GetExtensionManager in CAMAPI.ExtensionManager.dll");
        }

        FExtensionManager = reinterpret_cast<IExtensionManager*>(GetExtensionManager());
    }
    return FExtensionManager;
}

struct StaticInitializer {
    StaticInitializer() { ExtensionManagerHelper::Initialize(); }
    ~StaticInitializer() { ExtensionManagerHelper::Finalize(); }
};

StaticInitializer staticInitializer;