#include "pch.h"
#include <windows.h>
#include <shellapi.h>
#include <stdexcept>
#include <string>
#include <filesystem>
#include <iostream>
#include <fstream>
#include "CamApi.SDK.h"

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
        ICamApiApplication* application = nullptr;
        ICamApiProject* project = nullptr;

        BSTR id = nullptr;
        BSTR path = nullptr;
        try {
            // get project
            HRESULT hr = context->get_CamApplication(&application);
            if (FAILED(hr) || !application)
                throw std::runtime_error("Failed to get CamApplication from context");

            hr = application->GetActiveProject(ResultStatus , &project);
            if (FAILED(hr) || ResultStatus->Code == rsError)
                throw std::runtime_error("Error getting project: " + BSTRToString(ResultStatus->Description));

            // get project id
            hr = project->get_Id(&id);
            if (FAILED(hr))
                throw std::runtime_error("Error in project->get_Id");

            // get file path
            hr = project->get_FilePath(&path);
            if (FAILED(hr))
                throw std::runtime_error("Error in project->get_FilePath");

            // save params in some temp file to show it later
            fs::path tempDir = fs::temp_directory_path();
            fs::path tempFilePath = tempDir / ("tempfile_41.txt");
            if (fs::exists(tempFilePath))
                fs::remove(tempFilePath);

            {
                std::ofstream file(tempFilePath, std::ios::trunc);
                if (!file)
                    throw std::runtime_error("Failed to open file for writing");

                file << "Project file path: " << BSTRToString(path) << "\n";
                file << "Project id: " << BSTRToString(id) << "\n";
                file.close();
            }

            HINSTANCE result = ShellExecuteW(
                NULL,
                L"open",
                L"notepad.exe",
                tempFilePath.wstring().c_str(),
                NULL,
                SW_SHOWNORMAL
            );

            ResultStatus->Code = rsSuccess;
            ResultStatus->Description = nullptr;
        }
        catch (const std::exception& e) {
            ResultStatus->Code = rsError;
            ResultStatus->Description = StringToBSTR(e.what());
        }

        // Release the COM objects
        if (application)
            application->Release();
        if (path)
            SysFreeString(path);
        if (id)
            SysFreeString(id);

        return S_OK;
    }
};

extern "C" __declspec(dllexport) void* __stdcall CreateInstanceOfExtension(const wchar_t* PluginID) {
    auto* ext = new ExtensionUtilityExample();
    ext->AddRef();
    return static_cast<IExtension*>(ext);
}