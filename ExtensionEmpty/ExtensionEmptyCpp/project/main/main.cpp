#include "pch.h"
#include <windows.h>
#include <shellapi.h>
#include <stdexcept>
#include <string>
#include <filesystem>
#include <iostream>
#include <fstream>

#pragma comment(lib, "Shell32.lib")

#import <STTypes.tlb> no_namespace, named_guids
#import <CAMAPI.Logger.tlb> no_namespace, named_guids
#import <CAMAPI.ResultStatus.tlb> no_namespace, named_guids
#import "CAMAPI.Generic.List.tlb" no_namespace, named_guids
#import "CAMAPI.Singletons.tlb" no_namespace, named_guids
#import "CAMAPI.Extensions.tlb" no_namespace, named_guids
#import "CAMAPI.NCMaker.tlb" no_namespace, named_guids
#import "CAMAPI.Machine.tlb" no_namespace, named_guids
#import "CAMAPI.Technologist.tlb" no_namespace, named_guids
#import "CAMAPI.Snapshot.tlb" no_namespace, named_guids
#import "CAMAPI.GeomModel.tlb" no_namespace, named_guids
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

// Convert a std::string to a BSTR
BSTR StringToBSTR(const char* str) {
    int length = MultiByteToWideChar(CP_UTF8, 0, str, -1, NULL, 0);
    if (length == 0)
        return NULL;

    wchar_t* wstr = new wchar_t[length];
    if (MultiByteToWideChar(CP_UTF8, 0, str, -1, wstr, length) == 0) {
        delete[] wstr;
        return NULL;
    }

    BSTR bstr = SysAllocString(wstr);
    delete[] wstr;

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
        ICamApiApplication* application = nullptr;
        ICamApiProject* project = nullptr;

        try {
            // get project
            HRESULT hr = context->get_CamApplication(&application);
            if (FAILED(hr) || !application)
                throw std::runtime_error("Failed to get CamApplication from context");

            project = application->GetActiveProject(ResultStatus);
            if (FAILED(hr) || ResultStatus->Code == rsError)
                throw std::runtime_error("Error getting project: " + BSTRToString(ResultStatus->Description));

            // save params in some temp file to show it later
            fs::path tempDir = fs::temp_directory_path();
            fs::path tempFilePath = tempDir / ("tempfile_41.txt");
            if (fs::exists(tempFilePath))
                fs::remove(tempFilePath);

            {
                std::ofstream file(tempFilePath, std::ios::trunc);
                if (!file)
                    throw std::runtime_error("Failed to open file for writing");

                file << "Project file path: " << BSTRToString(project->FilePath) << "\n";
                file << "Project id: " << BSTRToString(project->Id) << "\n";
                file.close();
            }

            HINSTANCE result = ShellExecute(
                NULL,
                L"open",
                L"notepad.exe",
                tempFilePath.c_str(),
                NULL,
                SW_SHOWNORMAL
            );

            ResultStatus = {};
        }
        catch (const std::exception& e) {
            ResultStatus->Code = rsError;
            ResultStatus->Description = StringToBSTR(e.what());
        }

        // Release the COM objects
        if (application)
            application->Release();

        return S_OK;
    }
};

extern "C" __declspec(dllexport) void* __stdcall CreateInstanceOfExtension(const wchar_t* PluginID) {
    IExtensionPtr resultPtr = new ExtensionUtilityExample();
    resultPtr.AddRef();
    void* res = resultPtr.GetInterfacePtr();
    return (res);
}