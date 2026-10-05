#define NOMINMAX
#include <Windows.h>
#include <OleAuto.h>
#include <atomic>
#include <cmath>
#include <iomanip>
#include <locale>
#include <new>
#include <sstream>
#include <string>

#include "DemoMath.h" // Generated from the IDL by STBuild.
// The generated header leaves packing at 1; restore it for the C++ classes below.
#pragma pack(pop)

namespace
{
HRESULT CopyBstr(const std::wstring& text, BSTR* result)
{
    if (result == nullptr)
        return E_POINTER;
    *result = SysAllocStringLen(text.data(), static_cast<UINT>(text.size()));
    return *result == nullptr ? E_OUTOFMEMORY : S_OK;
}

bool Finite(const TPoint3D& point)
{
    return std::isfinite(point.X) && std::isfinite(point.Y) && std::isfinite(point.Z);
}

bool Finite(const TVector3D& vector)
{
    return std::isfinite(vector.X) && std::isfinite(vector.Y) && std::isfinite(vector.Z);
}

class GeomCalculator final : public IGeomCalculator
{
public:
    explicit GeomCalculator(BSTR name)
        : name_(name == nullptr ? L"" : std::wstring(name, SysStringLen(name)))
    {
    }

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** object) override
    {
        if (object == nullptr)
            return E_POINTER;
        *object = nullptr;
        if (iid != __uuidof(IUnknown) && iid != __uuidof(IGeomCalculator))
            return E_NOINTERFACE;
        *object = static_cast<IGeomCalculator*>(this);
        AddRef();
        return S_OK;
    }

    ULONG STDMETHODCALLTYPE AddRef() override { return ++references_; }

    ULONG STDMETHODCALLTYPE Release() override
    {
        const ULONG remaining = --references_;
        if (remaining == 0)
            delete this;
        return remaining;
    }

    HRESULT __stdcall get_Name(BSTR* value) override { return CopyBstr(name_, value); }

    HRESULT __stdcall put_Name(BSTR value) override
    {
        name_ = value == nullptr ? L"" : std::wstring(value, SysStringLen(value));
        return S_OK;
    }

    HRESULT __stdcall Distance(TPoint3D a, TPoint3D b, double* result) override
    {
        if (result == nullptr)
            return E_POINTER;
        if (!Finite(a) || !Finite(b))
            return E_INVALIDARG;
        *result = std::hypot(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        return std::isfinite(*result) ? S_OK : E_INVALIDARG;
    }

    HRESULT __stdcall Translate(TPoint3D point, TVector3D offset, TPoint3D* result) override
    {
        if (result == nullptr)
            return E_POINTER;
        if (!Finite(point) || !Finite(offset))
            return E_INVALIDARG;
        *result = {point.X + offset.X, point.Y + offset.Y, point.Z + offset.Z};
        return Finite(*result) ? S_OK : E_INVALIDARG;
    }

    HRESULT __stdcall Transform(TPoint3D point, TMatrix3D matrix, TPoint3D* result) override
    {
        if (result == nullptr)
            return E_POINTER;
        if (!Finite(point) || !Finite(matrix.AxisX) || !Finite(matrix.AxisY) ||
            !Finite(matrix.AxisZ) || !Finite(matrix.Origin))
            return E_INVALIDARG;
        *result = {
            matrix.Origin.X + matrix.AxisX.X * point.X + matrix.AxisY.X * point.Y + matrix.AxisZ.X * point.Z,
            matrix.Origin.Y + matrix.AxisX.Y * point.X + matrix.AxisY.Y * point.Y + matrix.AxisZ.Y * point.Z,
            matrix.Origin.Z + matrix.AxisX.Z * point.X + matrix.AxisY.Z * point.Y + matrix.AxisZ.Z * point.Z
        };
        return Finite(*result) ? S_OK : E_INVALIDARG;
    }

    HRESULT __stdcall Normalize(TVector3D vector, TVector3D* unitVector, double* length) override
    {
        if (unitVector == nullptr || length == nullptr)
            return E_POINTER;
        if (!Finite(vector))
            return E_INVALIDARG;
        *length = std::hypot(vector.X, vector.Y, vector.Z);
        if (*length == 0.0 || !std::isfinite(*length))
            return E_INVALIDARG;
        *unitVector = {vector.X / *length, vector.Y / *length, vector.Z / *length};
        return S_OK;
    }

    HRESULT __stdcall DescribePoint(TPoint3D point, BSTR* result) override
    {
        if (!Finite(point))
            return E_INVALIDARG;
        std::wostringstream text;
        text.imbue(std::locale::classic());
        text << std::fixed << std::setprecision(3)
             << L"(" << point.X << L", " << point.Y << L", " << point.Z << L")";
        return CopyBstr(text.str(), result);
    }

private:
    std::atomic<ULONG> references_{1};
    std::wstring name_;
};

class GeomLibrary final : public IGeomLibrary
{
public:
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** object) override
    {
        if (object == nullptr)
            return E_POINTER;
        *object = nullptr;
        if (iid != __uuidof(IUnknown) && iid != __uuidof(IGeomLibrary))
            return E_NOINTERFACE;
        *object = static_cast<IGeomLibrary*>(this);
        AddRef();
        return S_OK;
    }

    ULONG STDMETHODCALLTYPE AddRef() override { return ++references_; }

    ULONG STDMETHODCALLTYPE Release() override
    {
        // The DLL owns the initial reference for the lifetime of the module.
        return --references_;
    }

    HRESULT __stdcall get_Name(BSTR* value) override
    {
        return CopyBstr(L"Geometry library", value);
    }

    HRESULT __stdcall CreateGeomCalculator(BSTR name, IGeomCalculator** result) override
    {
        if (result == nullptr)
            return E_POINTER;
        *result = new (std::nothrow) GeomCalculator(name);
        return *result == nullptr ? E_OUTOFMEMORY : S_OK;
    }

private:
    std::atomic<ULONG> references_{1};
};

GeomLibrary library;
} // namespace

// Returns a new owned reference to the process-wide library object.
extern "C" __declspec(dllexport) HRESULT __stdcall GetGeomLibrary(IGeomLibrary** result)
{
    if (result == nullptr)
        return E_POINTER;
    *result = &library;
    library.AddRef();
    return S_OK;
}
