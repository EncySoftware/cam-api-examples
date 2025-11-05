using System;
using System.IO;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using CAMHelper.NativeLibUtils;
using CAMIPC.Application;
using CAMIPC.ExecuteContext;
using CAMIPC.Helper;

namespace FullWorkflow3DProject;

public class CamHelper : IDisposable
{
    private readonly ComWrapper<IIpcHelper> _helper;
    private readonly ComWrapper<ICamIpcApplication>? _application;
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
        
        // fill an object to connect to CAM application
        _helperDllPtr = NativeLibLoader.LoadDll(helperPath, out var resultLoadDll);
        if (_helperDllPtr == IntPtr.Zero || resultLoadDll != 0)
            throw new Exception($"Error loading: {resultLoadDll}");
        var proc = NativeLibLoader.GetProc<CreateHelperDelegate>(_helperDllPtr, "CreateHelper");
        _helper = new ComWrapper<IIpcHelper>(proc());
        
        // instance of the main application - we should get it in the same thread
        var executeContext = new TExecuteContext();
        using var instancesCom =
            _helper.InvokeAndWrap(helper => helper.GetRunningCamAppList(ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        _application = instancesCom.InvokeAndWrap(instances =>
        {
            if (instances.Count == 0)
                throw new Exception("ENCY running instance not found");
            return instances.Get(0, executeContext);
        });
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
    }
    
    /// <summary>
    /// Destructor. Release COM objects
    /// </summary>
    public void Dispose()
    {
        _application?.Dispose();
        _helper.Dispose();
        NativeLibLoader.FreeDll(_helperDllPtr);
    }
}