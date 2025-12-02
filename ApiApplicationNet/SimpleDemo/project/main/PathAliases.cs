using System;
using System.Runtime.InteropServices;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using CAMHelper.NativeLibUtils;
using PathAliasesInterface;

namespace ApplicationSimpleDemoNet;

/// <summary>
/// Provides access to the native PathAliases.dll and its COM interfaces.
/// Handles loading the DLL, resolving exports, and exposing high‑level helpers for alias initialization.
/// </summary>
public static class PathAliases
{
    /// <summary>
    /// Cached handle to the loaded PathAliases.dll module.
    /// Used to avoid loading the library multiple times.
    /// </summary>
    private static IntPtr hModule = IntPtr.Zero;

    /// <summary>
    /// Cached COM interface pointer to the native IST_PathAliasLibrary.
    /// This is acquired once from the DLL and reused across calls.
    /// </summary>
    private static IST_PathAliasLibrary? fNativeLib;
    
    /// <summary>
    /// File name of the native path aliases DLL.
    /// </summary>
    public static readonly string DllName = "PathAliases.dll";
    
    /// <summary>
    /// Delegate type that matches the native GetPathAliasLibPointer export.
    /// The native function returns a COM pointer to IST_PathAliasLibrary.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr GetPathAliasLibPointerDelegate();
    
    /// <summary>
    /// Loads PathAliases.dll on first use and initializes the cached IST_PathAliasLibrary instance.
    /// Safe to call multiple times; once initialized, subsequent calls reuse the same COM object.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the DLL cannot be loaded or the GetPathAliasLibPointer export cannot be found,
    /// or when the COM interface cannot be created from the returned pointer.
    /// </exception>
    /// <exception cref="Exception">
    /// Thrown when the native GetPathAliasLibPointer function returns a null pointer.
    /// </exception>
    private static void LoadPathAliasLibrary()
    {
        if (hModule == IntPtr.Zero){
            hModule = NativeLibLoader.LoadDll(DllName);
            if (hModule == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to load '{DllName}'.");
        }

        var getPathAliasLibrary =
            NativeLibLoader.GetProc<GetPathAliasLibPointerDelegate>(hModule, "GetPathAliasLibPointer") 
                   ?? throw new MissingMethodException($"Failed to get 'GetPathAliasLibPointer' from '{DllName}'.");


        IntPtr rawPtr = getPathAliasLibrary();
        if (rawPtr == IntPtr.Zero)
            throw new Exception("GetPathAliasLibPointer returned nullptr.");


        fNativeLib = (IST_PathAliasLibrary)Marshal.GetTypedObjectForIUnknown(
            rawPtr, 
            typeof(IST_PathAliasLibrary));


        if (fNativeLib == null)
            throw new InvalidOperationException("IST_PathAliasLibrary was not initialized correctly.");
    }

    /// <summary>
    /// Returns a cached instance of <see cref="IST_PathAliasLibrary"/>.
    /// The underlying native DLL is loaded and initialized on first use.
    /// </summary>
    /// <returns>COM interface to the path alias library.</returns>
    public static IST_PathAliasLibrary GetLibrary()
    {
        LoadPathAliasLibrary();
        return fNativeLib!;
    }

    /// <summary>
    /// Initializes CAM folder aliases for the specified client DLL.
    /// This is a thin wrapper around the native InitializeCAMFolders2 method.
    /// </summary>
    public static IST_AliasesList InitFolders(string clientDllName, bool includePrIdInPath)
    {
        LoadPathAliasLibrary();
        return fNativeLib!.InitializeCAMFolders2(clientDllName, includePrIdInPath);
    }

    /// <summary>
    /// Releases the cached COM interface and unloads the native PathAliases.dll
    /// Should be called during application shutdown to free native resources.
    /// </summary>
    public static void FreeLibrary()
    {
        if (fNativeLib != null){
            Marshal.ReleaseComObject(fNativeLib);
            fNativeLib = null;
        }

        if (hModule != IntPtr.Zero)
        {
            NativeLibLoader.FreeDll(hModule);
            hModule = IntPtr.Zero;
        }
    }
}
