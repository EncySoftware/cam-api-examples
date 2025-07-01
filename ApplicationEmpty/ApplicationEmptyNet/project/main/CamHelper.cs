using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using CAMHelper.NativeLibUtils;
using CAMIPC.Application;
using CAMIPC.ExecuteContext;
using CAMIPC.Helper;

namespace ApplicationEmptyNet;

public class CamHelper : IDisposable
{
    private readonly ComWrapper<IIpcHelper> _helper;
    private ComWrapper<ICamIpcApplication>? _application;
    private readonly IntPtr _helperDllPtr;
    private delegate IntPtr CreateHelperDelegate();
    public ComWrapper<ICamIpcApplication> GetApplication() => _application 
                                                              ?? throw new Exception("Application not found");
    
    /// <summary>
    /// Connect to CAM application
    /// </summary>
    public CamHelper()
    {
        // path to CAM application
        const string camFolder = @"C:\Program Files\ENCY Software\ENCY\Bin64";
        var helperPath = Path.Combine(camFolder, "CAMIPC.Helper.Cam.dll");
        if (!File.Exists(helperPath))
            throw new Exception($"{helperPath} not found");
        
        // fill object to connect to CAM application
        _helperDllPtr = NativeLibLoader.LoadDll(helperPath, out var resultLoadDll);
        if (_helperDllPtr == IntPtr.Zero || resultLoadDll != 0)
            throw new Exception($"Error loading: {resultLoadDll}");
        var proc = NativeLibLoader.GetProc<CreateHelperDelegate>(_helperDllPtr, "CreateHelper");
        _helper = new ComWrapper<IIpcHelper>(proc());
        
        // instance of main application - we should get it in the same thread
        var executeContext = new TExecuteContext();
        _helper.Invoke(helper =>
        {
            if (helper == null)
                throw new Exception("Can't get helper instance");
            using var instancesCom = ComWrapper.Create(helper.GetRunningCamAppList(ref executeContext));
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            
            instancesCom.Invoke(instances =>
            {
                if (instances == null)
                    throw new Exception("Can't get running instances");
                if (instances.Count == 0)
                    throw new Exception("ENCY running instance not found");
                _application = ComWrapper.Create(instances.Get(0, executeContext));
                if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                    throw new Exception(executeContext.ResultStatus.Description);
            });
        });
    }
    
    /// <summary>
    /// Destructor. Release COM objects
    /// </summary>
    public void Dispose()
    {
        _application?.Dispose();
        _helper?.Dispose();
        NativeLibLoader.FreeDll(_helperDllPtr);
    }
}