using System;
using System.Runtime.InteropServices;
using System.Text;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using CAMHelper.NativeLibUtils;

namespace ApplicationSimpleDemoNet;

/// <summary>
/// Provides managed access to the native SCKernelConsole.dll and its main COM application interface.
/// Responsible for dynamic DLL loading, exported function binding, and lifetime management.
/// </summary>
public class KernelConsole
{
    /// <summary>
    /// Cached handle to the loaded SCKernelConsole.dll module.
    /// Used to avoid loading the library multiple times.
    /// </summary>
    private static IntPtr hModule = IntPtr.Zero;

    /// <summary>
    /// Cached COM interface pointer to the native ICamApiApplication instance.
    /// This interface is acquired once from the DLL and reused across calls.
    /// </summary>
    private static ICamApiApplication? fNativeLib;

    /// <summary>
    /// File name of the native kernel console DLL.
    /// </summary>
    public static readonly string DllName = "SCKernelConsole.dll";

    /// <summary>
    /// Delegate type that matches the native RunApplication export.
    /// The native function takes command-line parameters and returns a COM pointer to ICamApiApplication.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr RunApplicationDelegate([MarshalAs(UnmanagedType.LPWStr)] string utf8Params);

    /// <summary>
    /// Delegate type that matches the native CloseApplication export.
    /// The native function shuts down the application instance and releases associated resources.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void CloseApplicationDelegate();

    /// <summary>
    /// Loads SCKernelConsole.dll on first use and initializes the cached ICamApiApplication instance.
    /// This method is safe to call multiple times; subsequent calls reuse the already initialized COM object.
    /// </summary>
    /// <param name="parameters">Command-line arguments to pass to the native kernel console.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the DLL cannot be loaded or the RunApplication export cannot be found,
    /// or when the COM interface cannot be created from the returned pointer.
    /// </exception>
    /// <exception cref="Exception">
    /// Thrown when the native RunApplication function returns a null pointer.
    /// </exception>
    private static void LoadKernelConsoleLibrary(string parameters)
    {
        if (hModule == IntPtr.Zero){
            hModule = NativeLibLoader.LoadDll(DllName);
            if (hModule == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to load '{DllName}'.");
        }

        if (fNativeLib != null)
            return;

        var runApplication =
            NativeLibLoader.GetProc<RunApplicationDelegate>(hModule, "RunApplication") 
                    ?? throw new MissingMethodException($"Failed to get 'RunApplication' from '{DllName}'.");
        
        string utf8Params = parameters ?? string.Empty;
    

        IntPtr rawPtr = runApplication(utf8Params);
        if (rawPtr == IntPtr.Zero)
            throw new Exception("RunApplication returned null pointer.");


        fNativeLib = (ICamApiApplication)Marshal.GetTypedObjectForIUnknown(
            rawPtr,
            typeof(ICamApiApplication));


        if (fNativeLib == null)
            throw new InvalidOperationException("ICamApiApplication was not initialized correctly.");


        Console.WriteLine("ICamApiApplication interface acquired successfully.");
    }

    /// <summary>
    /// Starts the kernel console and returns the ICamApiApplication interface.
    /// The underlying native DLL is loaded and initialized on first use.
    /// </summary>
    /// <param name="@params">
    /// Command-line parameters for the native kernel console.
    /// </param>
    /// <returns>Initialized ICamApiApplication COM interface</returns>
    public static ICamApiApplication Run(string @params)
    {
        LoadKernelConsoleLibrary(@params);
        return fNativeLib!;
    }

    /// <summary>
    /// Releases the cached COM interface and unloads the native SCKernelConsole.dll.
    /// Should be called during application shutdown to free native resources.
    /// </summary>
    public static void FreeLibrary()
    {
        if (hModule != IntPtr.Zero)
        {
            var closeApplication =
                NativeLibLoader.GetProc<CloseApplicationDelegate>(hModule, "CloseApplication");
            closeApplication();
        }
        if (fNativeLib != null)
        {
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
