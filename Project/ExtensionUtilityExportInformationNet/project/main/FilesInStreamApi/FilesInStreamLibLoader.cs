using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using CAMAPI.DotnetHelper;
using CAMAPI.FilesInStream;
using CAMHelper.NativeLibUtils;

namespace ExtensionUtilityExportInformationNet;

internal delegate IntPtr GetFilesInStreamStorageLibPointerDelegate();
internal delegate void InitFilesInStreamStorageLibDelegate();
internal delegate void FinalizeFilesInStreamStorageLibDelegate();

/// <summary>
/// Direct work with FilesInStream.dll — modeled one-to-one after
/// FilesInStream/tests/FilesInStreamWrapperTest/FilesInStreamHelperDotnet.cs.
/// </summary>
public class FilesInStreamLibLoader : IDisposable
{
    private const string FilesInStreamDllName = "FilesInStream.dll";

    private IntPtr fDllHandle = IntPtr.Zero;
    private GetFilesInStreamStorageLibPointerDelegate? GetFilesInStreamStorageLibPointer;
    private InitFilesInStreamStorageLibDelegate? InitFilesInStreamStorageLib;
    private FinalizeFilesInStreamStorageLibDelegate? FinalizeFilesInStreamStorageLib;

    private ComWrapper<ICAMAPIFilesInStreamStorageLib>? fLib;

    /// <summary>
    /// Path to FilesInStream.dll next to the executable module of the current process.
    /// </summary>
    public static string GetDefaultDllPath()
    {
        var mainModulePath = Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Cannot resolve current process main module path.");
        var binDir = Path.GetDirectoryName(mainModulePath)
            ?? throw new InvalidOperationException("Cannot resolve directory of " + mainModulePath);
        return Path.Combine(binDir, FilesInStreamDllName);
    }

    /// <summary>
    /// Loads the dll and initializes the library.
    /// </summary>
    public bool LoadDll(string DllName)
    {
        if (File.Exists(DllName))
            fDllHandle = NativeLibLoader.LoadDll(DllName);
        if (fDllHandle != IntPtr.Zero)
        {
            GetFilesInStreamStorageLibPointer = NativeLibLoader.GetProc<GetFilesInStreamStorageLibPointerDelegate>(fDllHandle, "GetFilesInStreamStorageLibPointer");
            InitFilesInStreamStorageLib = NativeLibLoader.GetProc<InitFilesInStreamStorageLibDelegate>(fDllHandle, "InitFilesInStreamStorageLib");
            FinalizeFilesInStreamStorageLib = NativeLibLoader.GetProc<FinalizeFilesInStreamStorageLibDelegate>(fDllHandle, "FinalizeFilesInStreamStorageLib");

            InitFilesInStreamStorageLib?.Invoke();
        }
        return (fDllHandle != IntPtr.Zero) && (GetFilesInStreamStorageLibPointer != null);
    }

    /// <summary>
    /// Finalizes the library and unloads the dll.
    /// </summary>
    public void FreeDll()
    {
        if (fDllHandle != IntPtr.Zero)
        {
            fLib?.Dispose();
            FinalizeFilesInStreamStorageLib?.Invoke();
            NativeLibLoader.FreeDll(fDllHandle);
        }
    }

    /// <summary>
    /// Root object of the library. null — until LoadDll has completed successfully.
    /// </summary>
    public ICAMAPIFilesInStreamStorageLib? Lib
    {
        get
        {
            if (fLib == null) {
                if (GetFilesInStreamStorageLibPointer != null) {
                    var libPtr = GetFilesInStreamStorageLibPointer();
                    if (libPtr != IntPtr.Zero)
                        fLib = ComWrapper.Create((ICAMAPIFilesInStreamStorageLib)Marshal.GetObjectForIUnknown(libPtr));
                }
            }
            return fLib?.Instance;
        }
    }

    /// <summary>
    /// Reads a storage file entirely into memory via COM IStream.
    /// </summary>
    public unsafe byte[] ReadAllBytes(ICAMAPIFilesInStreamStorage storage, int fileIndex)
    {
        CAMAPI.FilesInStream.IStream? comStream;
        comStream = storage.GetFileReadStream(fileIndex);
        if (comStream == null)
            return new byte[0];

        System.Runtime.InteropServices.ComTypes.IStream stm = (comStream as System.Runtime.InteropServices.ComTypes.IStream);

        // Get the stream size
        stm.Stat(out STATSTG stats, 0);
        long streamSize = stats.cbSize;

        // Prepare buffer
        byte[] buffer = new byte[streamSize];
        int pcbRead = 0;

        // Read the data
        stm.Read(buffer, (int)streamSize, new IntPtr(&pcbRead));

        return buffer;
    }

    /// <summary>
    /// Disposal via using — simply calls FreeDll.
    /// </summary>
    public void Dispose()
    {
        FreeDll();
        fDllHandle = IntPtr.Zero;
    }
}
