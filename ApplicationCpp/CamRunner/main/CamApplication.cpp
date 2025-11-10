#include "pch.h"
#include "CamApplication.h"
#include "NativeLibLoader.h"
#include <iostream>

const IID IID_ICamApiApplication =
{ 0x79A78312, 0xDA90, 0x46E8, { 0x84, 0x18, 0xC4, 0xE7, 0x1B, 0xBD, 0x16, 0x27 } };

// кэшируем хэндл DLL
static HMODULE hModule = nullptr;

// сигнатуры возможных экспортов
using RunSCConsole = int (__stdcall*)(LPCWSTR);

ICamApiApplication* CamApplication::Run(const std::wstring& params) {
    const std::string dllName = "SCKernelConsole.dll";
    if (!hModule)
        hModule = NativeLibLoader::LoadDll(dllName);

    auto runFunc = NativeLibLoader::GetProc<RunApplication>(hModule, "RunApplication");

    // Конвертация параметров из wstring → UTF-8
    std::string utf8Params;
    {
        int size = WideCharToMultiByte(CP_UTF8, 0, params.c_str(), -1, nullptr, 0, nullptr, nullptr);
        utf8Params.resize(size - 1);
        WideCharToMultiByte(CP_UTF8, 0, params.c_str(), -1, utf8Params.data(), size, nullptr, nullptr);
    }

    // Вызов функции
    auto rawPtr = runFunc(utf8Params.c_str());
    if (!rawPtr)
        throw std::runtime_error("RunApplication returned nullptr.");

    // Приведение к COM-интерфейсу
    ICamApiApplication* pApp = reinterpret_cast<ICamApiApplication*>(rawPtr);
    return pApp;
}

void CamApplication::FreeLibrary() {
    if (hModule) {
        auto closeFunc = NativeLibLoader::GetProc<CloseApplication>(hModule, "CloseApplication");
        closeFunc();

        ::FreeLibrary(hModule);
        hModule = nullptr;
        std::cout << "SCKernelConsole.dll unloaded successfully." << std::endl;
    }
}