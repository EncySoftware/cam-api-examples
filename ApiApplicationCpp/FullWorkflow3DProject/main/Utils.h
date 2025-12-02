#pragma once
#include <string>

#include "CamApi.SDK.h"

class Utils {
public:
    /// <summary>
    /// Convert a BSTR to a std::string
    /// </summary>
    static std::string BSTRToString(BSTR bstr);

    /// <summary>
    /// Convert a std::string to a BSTR
    /// </summary>
    static BSTR StringToBSTR(const std::string& str);

    /// <summary>
    /// Convert a UTF-8 std::string to a wide std::wstring
    /// </summary>
    static std::wstring ToWideUtf8(const std::string &s);
};
