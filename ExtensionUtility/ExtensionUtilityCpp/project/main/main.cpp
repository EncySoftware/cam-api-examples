#include "pch.h"
#include <windows.h>
#include <shellapi.h>
#include <stdexcept>
#include <string>
#include <filesystem>
#include <iostream>
#include <codecvt>
#include <comutil.h>
#include "ExtensionManagerHelper.h"
#include "CAMAPI.SDK.h"

#pragma comment(lib, "Shell32.lib")

namespace fs = std::filesystem;

int main() {
    // Initialize COM
    CoInitialize(nullptr);

    // Uninitialize COM
    CoUninitialize();

    return 0;
}

// Convert a BSTR to a std::string
std::string BSTRToString(BSTR bstr) {
    // Check for null BSTR
    if (!bstr) {
        return "";
    }

    // Get the length of the BSTR in characters
    int wslen = SysStringLen(bstr);

    // Convert the wide string to a narrow string
    int len = WideCharToMultiByte(CP_UTF8, 0, bstr, wslen, nullptr, 0, nullptr, nullptr);
    std::string str(len, '\0');
    WideCharToMultiByte(CP_UTF8, 0, bstr, wslen, &str[0], len, nullptr, nullptr);

    return str;
}

BSTR StringToBSTR(const std::string& str) {
    // Convert std::string (UTF-8) to wide string (UTF-16)
    int wslen = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, nullptr, 0);
    if (wslen == 0) {
        return nullptr; // Conversion failed
    }

    // Allocate a wide string buffer to hold the converted string
    BSTR bstr = SysAllocStringLen(nullptr, wslen - 1); // wslen includes null terminator, subtract 1
    if (!bstr) {
        return nullptr; // Memory allocation failed
    }

    // Perform the actual conversion
    MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, bstr, wslen);

    return bstr;
}

class ExtensionUtilityExample :
    public IExtension,
    public IExtensionUtility
{
private:
    long m_refCount = 3;
    struct IExtensionInfo* info = nullptr;

public:
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject) override
    {
        if (!ppvObject)
            return E_POINTER;

        *ppvObject = nullptr;

        if (riid == __uuidof(IUnknown) ||
            riid == __uuidof(IExtension))
        {
            *ppvObject = static_cast<IExtension*>(this);
        }
        else if (riid == __uuidof(IExtensionUtility))
        {
            *ppvObject = static_cast<IExtensionUtility*>(this);
        }
        else
        {
            return E_NOINTERFACE;
        }

        AddRef();
        return S_OK;
    }

    ULONG STDMETHODCALLTYPE AddRef() override {
        return InterlockedIncrement(&m_refCount);
    };

    ULONG STDMETHODCALLTYPE Release() override {
        return InterlockedDecrement(&m_refCount);
    };

    ~ExtensionUtilityExample() {
        if (info)
            info->Release();
    }

    HRESULT __stdcall get_Info(IExtensionInfo** Value) override {
        if (!Value) {
            return E_POINTER;
        }

        *Value = info;
        if (info) {
            info->AddRef();
        }
        return S_OK;
    }

    HRESULT __stdcall put_Info(IExtensionInfo* Value) override {
        if (info) {
            info->Release();
        }

        info = Value;
        if (info) {
            info->AddRef();
        }

        return S_OK;
    }

    HRESULT __stdcall Run(
        IExtensionUtilityContext* context,
        TResultStatus* ResultStatus
    ) override {
        ICamApiApplication* application;
        BSTR name = nullptr;
        IExtension* extension = nullptr;
        IExtensionManager* extension_manager = nullptr;
        ICamApiPaths* cam_paths = nullptr;
        BSTR main_program_folder = nullptr;
        try
        {
            TResultStatus resultStatus;

			// get global context
            HRESULT hr = context->get_CamApplication(&application);
            if (FAILED(hr) || !application)
                throw std::runtime_error("get_CamApplication failed.");

            const _bstr_t name(L"Extension.Global.Singletons.Paths");
            hr = ExtensionManagerHelper::GetInstance()->GetSingletonExtension(
                name, &resultStatus, &extension);
            if (FAILED(hr) || !extension)
                throw std::runtime_error("GetSingletonExtension failed.");

            hr = extension->QueryInterface(IID_ICamApiPaths, (void**)&cam_paths);
            if (FAILED(hr) || !cam_paths)
                throw std::runtime_error("QueryInterface for ICamApiPaths failed.");

            // save
            hr = cam_paths->get_MainProgramFolder(&main_program_folder);
            if (FAILED(hr) || !main_program_folder)
                throw std::runtime_error("get_MainProgramFolder failed.");
            _bstr_t exportedFile = (_bstr_t)main_program_folder + L"\\exported.stcp";
            hr = application->SaveCurrentProject(exportedFile, &resultStatus);
            if (FAILED(hr) || resultStatus.Code == rsError)
                throw std::runtime_error("Error exporting project: " + BSTRToString(resultStatus.Description));
        }
        catch (const std::exception& ex)
        {
            ResultStatus->Code = TResultStatusCode::rsError;
            ResultStatus->Description = StringToBSTR(ex.what());
        }

        // Release the COM objects
        if (main_program_folder)
            SysFreeString(main_program_folder);
        if (cam_paths)
            cam_paths->Release();
        if (extension_manager)
            extension_manager->Release();
        if (extension)
            extension->Release();
        if (name)
            SysFreeString(name);
        if (application)
            application->Release();

        return S_OK;
    }
};

extern "C" __declspec(dllexport) void* __stdcall CreateInstanceOfExtension(const wchar_t* PluginID) {
    auto* ext = new ExtensionUtilityExample();
    ext->AddRef();
    return static_cast<IExtension*>(ext);
}