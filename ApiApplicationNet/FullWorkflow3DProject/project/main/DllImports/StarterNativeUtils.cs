using System;
using System.Runtime.InteropServices;
using AppStarterInterface;
using CAMHelper.NativeLibUtils;
using STLoggingInterface;

namespace ApplicationFullWorkflow3DProjectNet;

/// <summary>
/// Provides access to the native SCStarterNativeUtils.dll and its COM interfaces.
/// Handles dynamic loading, function lookup, and lifetime management for the starter utilities.
/// </summary>
public static class StarterNativeUtils
{
    /// <summary>
    /// Cached handle to the loaded SCStarterNativeUtils.dll module.
    /// Used to avoid loading the library multiple times.
    /// </summary>
    private static IntPtr hModule = IntPtr.Zero;

    /// <summary>
    /// Cached COM interface pointer to the native ISCStarterNativeUtils instance.
    /// This interface is acquired once from the DLL and reused across calls.
    /// </summary>
    private static ISCStarterNativeUtils? fNativeLib;

    /// <summary>
    /// File name of the native starter utilities DLL.
    /// </summary>
    public static readonly string DllName = "SCStarterNativeUtils.dll";
    
    /// <summary>
    /// Delegate type that matches the native GetLibPointer export.
    /// The native function returns a COM pointer to ISCStarterNativeUtils.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr GetLibPointerDelegate();

    /// <summary>
    /// Loads SCStarterNativeUtils.dll on first use and initializes the cached ISCStarterNativeUtils instance.
    /// Safe to call repeatedly; after the first successful initialization, subsequent calls are no‑ops.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the DLL cannot be loaded or the GetLibPointer export cannot be found,
    /// or when the COM interface cannot be created from the returned pointer.
    /// </exception>
    /// <exception cref="Exception">
    /// Thrown when the native GetLibPointer function returns a null pointer.
    /// </exception>
    private static void LoadStarterNativeUtilsLibrary()
    {
        if (hModule == IntPtr.Zero){
            hModule = NativeLibLoader.LoadDll(DllName);
            if (hModule == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to load '{DllName}'.");
        }

        var getStarterNativeUtilsLibrary =
            NativeLibLoader.GetProc<GetLibPointerDelegate>(hModule, "GetLibPointer") 
                    ?? throw new MissingMethodException($"Failed to get 'GetLibPointer' from '{DllName}'.");
        
        
        IntPtr rawPtr = getStarterNativeUtilsLibrary();
        if (rawPtr == IntPtr.Zero)
            throw new Exception("GetLibPointer returned nullptr.");


        fNativeLib = (ISCStarterNativeUtils)Marshal.GetTypedObjectForIUnknown(
            rawPtr,
            typeof(ISCStarterNativeUtils));


        if (fNativeLib == null)
            throw new InvalidOperationException("ISCStarterNativeUtils was not initialized correctly.");   
    }

    /// <summary>
    /// Returns a cached instance of <see cref="ISCStarterNativeUtils"/>.
    /// The underlying native DLL is loaded and initialized on first use.
    /// </summary>
    /// <returns>COM interface to the starter utilities library</returns>
    public static ISCStarterNativeUtils GetLibrary()
    {
        LoadStarterNativeUtilsLibrary();
        return fNativeLib!;
    }

    /// <summary>
    /// Initializes the logging subsystem via the native starter utilities library.
    /// This is a thin wrapper around the COM Logger/InitLogs functionality.
    /// </summary>
    /// <param name="aprocDependentFileName">
    /// If true, the log file name will depend on the current process name or ID.
    /// </param>
    /// <returns>An <see cref="IST_Logger"/> Initialized IST_Logger COM interface for writing log messages</returns>
    public static IST_Logger InitLogs(bool aprocDependentFileName)
    {
        LoadStarterNativeUtilsLibrary();

        fNativeLib!.InitLogs(aprocDependentFileName);

        var logger = fNativeLib.Logger() 
            ?? throw new InvalidOperationException("Logger() returned null.");
        return logger;
    }

    /// <summary>
    /// Releases the cached COM interface and unloads the native SCStarterNativeUtils.dll
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
