#include "pch.h"
#include <windows.h>
#include <shellapi.h>
#include <stdexcept>
#include <string>
#include <filesystem>
#include <iostream>
#include <fstream>

#pragma comment(lib, "Shell32.lib")

#import "STTypes.tlb" no_namespace, named_guids
#import <CAMAPI.Logger.tlb> no_namespace, named_guids
#import <CAMAPI.ResultStatus.tlb> no_namespace, named_guids
#import "CAMAPI.Generic.List.tlb" no_namespace, named_guids
#import "CAMAPI.Singletons.tlb" no_namespace, named_guids
#import "CAMAPI.Extensions.tlb" no_namespace, named_guids
#import "CAMAPI.NCMaker.tlb" no_namespace, named_guids
#import "CAMAPI.Machine.tlb" no_namespace, named_guids
#import "CAMAPI.GeomModel.tlb" no_namespace, named_guids
#import "CAMAPI.Technologist.tlb" no_namespace, named_guids
#import "CAMAPI.Snapshot.tlb" no_namespace, named_guids
#import "CAMAPI.GeomImporter.tlb" no_namespace, named_guids
#import "CAMAPI.ToolsList.tlb" no_namespace, named_guids
#import "CAMAPI.Project.tlb" no_namespace, named_guids
#import "CAMAPI.TechnologyForm.tlb" no_namespace, named_guids
#import "CAMAPI.ApplicationMainForm.tlb" no_namespace, named_guids
#import "CAMAPI.Application.tlb" no_namespace, named_guids

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
    HRESULT STDMETHODCALLTYPE QueryInterface(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject
    ) override {
        if (riid == __uuidof(IExtension)) {
            *ppvObject = static_cast<IExtension*>(this);
            return S_OK;
        }

        if (riid == __uuidof(IExtensionUtility)) {
            *ppvObject = static_cast<IExtensionUtility*>(this);
            return S_OK;
        }

        return S_FALSE;
    };

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

    HRESULT __stdcall raw_Run(
        IExtensionUtilityContext* context,
        TResultStatus* ResultStatus
    ) override {
        ICamApiPaths* paths = nullptr;
        try
        {
            TResultStatus resultStatus;
        
			// get global context
			if (!Info)
				throw std::runtime_error("Info is null");
			IUnknown* extension = Info->InstanceInfo->ExtensionManager->GetSingletonExtension(
				"Extension.Global.Singletons.Paths", &resultStatus);
			if (resultStatus.Code == TResultStatusCode::rsError)
				throw std::runtime_error("Error getting global context: " + BSTRToString(resultStatus.Description));
            if (!extension)
                throw std::runtime_error("Error: extension is nullptr");
            HRESULT hr = extension->QueryInterface(__uuidof(ICamApiPaths), (void**)&paths);
            if (FAILED(hr) || !paths)
                throw std::runtime_error("Error querying ICamApiPaths interface");
			if (!paths)
				throw std::runtime_error("Error casting to ICamApiPaths");

            // export
            _bstr_t currentFolder = paths->GetMainProgramFolder();
            if (currentFolder.length() == 0)
                throw std::runtime_error("Cannot get MainProgramFolder");
            _bstr_t exportedFile = (_bstr_t)currentFolder + L"\\exported.stcp";
            ICamApiApplication* application = context->CamApplication;
            hr = application->ExportCurrentProject(exportedFile, true, &resultStatus);
            if (FAILED(hr) || resultStatus.Code == rsError)
                throw std::runtime_error("Error exporting project: " + BSTRToString(resultStatus.Description));
        }
        catch (const std::exception& ex)
        {
            ResultStatus->Code = TResultStatusCode::rsError;
            ResultStatus->Description = StringToBSTR(ex.what());
        }

        // Release the COM objects
        if (paths)
            paths->Release();
    }
};

extern "C" __declspec(dllexport) void* __stdcall CreateInstanceOfExtension(const wchar_t* PluginID) {
    IExtensionPtr resultPtr = new ExtensionUtilityExample();
    resultPtr.AddRef();
    void* res = resultPtr.GetInterfacePtr();
    return (res);
}