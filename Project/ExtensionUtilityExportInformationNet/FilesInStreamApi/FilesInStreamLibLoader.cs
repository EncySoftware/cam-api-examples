using System.Diagnostics;
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
/// Loads the native FilesInStream.dll and provides the root COM interface
/// <see cref="ICAMAPIFilesInStreamStorageLib"/>. Released via Dispose.
/// </summary>
public sealed class FilesInStreamLibLoader : IDisposable
{
    private const string FilesInStreamDllName = "FilesInStream.dll";
    private IntPtr _dllHandle = IntPtr.Zero;
    private GetFilesInStreamStorageLibPointerDelegate? _getLibPointer;
    private InitFilesInStreamStorageLibDelegate? _initLib;
    private FinalizeFilesInStreamStorageLibDelegate? _finalizeLib;
    private ComWrapper<ICAMAPIFilesInStreamStorageLib>? _lib;
    private bool _initializedByUs;

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
    public bool LoadDll(string dllPath)
    {
        if (File.Exists(dllPath))
            _dllHandle = NativeLibLoader.LoadDll(dllPath);
        if (_dllHandle != IntPtr.Zero)
        {
            _getLibPointer = NativeLibLoader.GetProc<GetFilesInStreamStorageLibPointerDelegate>(_dllHandle, "GetFilesInStreamStorageLibPointer");
            _initLib = NativeLibLoader.GetProc<InitFilesInStreamStorageLibDelegate>(_dllHandle, "InitFilesInStreamStorageLib");
            _finalizeLib = NativeLibLoader.GetProc<FinalizeFilesInStreamStorageLibDelegate>(_dllHandle, "FinalizeFilesInStreamStorageLib");

            if (_getLibPointer != null && _getLibPointer() == IntPtr.Zero)
            {
                _initLib?.Invoke();
                _initializedByUs = true;
            }
        }
        return (_dllHandle != IntPtr.Zero) && (_getLibPointer != null);
    }

    /// <summary>
    /// Wrapper of the library root object. null — until LoadDll has completed successfully.
    /// </summary>
    public ComWrapper<ICAMAPIFilesInStreamStorageLib>? LibCom
    {
        get
        {
            if (_lib == null && _getLibPointer != null)
            {
                var libPtr = _getLibPointer();
                if (libPtr != IntPtr.Zero)
                    _lib = new ComWrapper<ICAMAPIFilesInStreamStorageLib>(libPtr);
            }
            return _lib;
        }
    }

    /// <summary>
    /// Reads a storage file entirely into memory via COM IStream.
    /// </summary>
    public byte[] ReadAllBytes(ICAMAPIFilesInStreamStorage storage, int fileIndex)
    {
        var comStream = storage.GetFileReadStream(fileIndex);
        if (comStream is not System.Runtime.InteropServices.ComTypes.IStream stream)
            return [];
        try
        {
            stream.Stat(out STATSTG stats, 0);
            var buffer = new byte[stats.cbSize];

            var pcbRead = Marshal.AllocHGlobal(sizeof(int));
            try
            {
                stream.Read(buffer, buffer.Length, pcbRead);
                var bytesRead = Marshal.ReadInt32(pcbRead);
                if (bytesRead != buffer.Length)
                    Array.Resize(ref buffer, bytesRead);
            }
            finally
            {
                Marshal.FreeHGlobal(pcbRead);
            }
            return buffer;
        }
        finally
        {
            Marshal.ReleaseComObject(comStream);
        }
    }

    /// <summary>
    /// Disposal via using — simply calls FreeDll.
    /// </summary>
    public void Dispose()
    {
        if (_dllHandle == IntPtr.Zero)
            return;
        _lib?.Dispose();
        _lib = null;
        if (_initializedByUs)
        {
            _finalizeLib?.Invoke();
            _initializedByUs = false;
        }
        NativeLibLoader.FreeDll(_dllHandle);
        _dllHandle = IntPtr.Zero;
    }
}
