#include "pch.h"
#include "Utils.h"

#include <string>

/// <summary>
/// Convert a BSTR to a std::string
/// </summary>
std::string Utils::BSTRToString(const BSTR bstr) {
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

BSTR Utils::StringToBSTR(const std::string &str) {
    const char* chars = str.c_str();
    int length = MultiByteToWideChar(CP_UTF8, 0, chars, -1, nullptr, 0);
    if (length == 0)
        return nullptr;

    auto* wstr = new wchar_t[length];
    if (MultiByteToWideChar(CP_UTF8, 0, chars, -1, wstr, length) == 0) {
        delete[] wstr;
        return nullptr;
    }

    BSTR bstr = SysAllocString(wstr);
    delete[] wstr;

    return bstr;
}

std::wstring Utils::ToWideUtf8(const std::string& s) {
    if (s.empty()) return L"";
    int n = MultiByteToWideChar(CP_UTF8, 0, s.c_str(), (int)s.size(), nullptr, 0);
    std::wstring w(n, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, s.c_str(), (int)s.size(), w.data(), n);
    return w;
}
