using System;
using System.IO;
using System.Runtime.InteropServices;
using CAMAPI.ResultStatus;
using CAMHelper.NativeLibUtils;
using CAMIPC.Application;
using CAMIPC.ExecuteContext;
using CAMIPC.Helper;

namespace ApplicationEmptyNet;

public static class CamHelper
{
    private static IIpcHelper? _helper;
    private static ICamIpcApplication? _application;
    private static IntPtr _helperObjectPtr;
    private static IntPtr _helperDllPtr;
    private delegate IntPtr CreateHelperDelegate();
    
    /// <summary>
    /// Connect to CAM application
    /// </summary>
    private static IIpcHelper GetHelper()
    {
        if (_helper != null)
            return _helper;

        const string camFolder = @"C:\Program Files\ENCY Software\ENC\Bin64";
        var helperPath = Path.Combine(camFolder, "CAMIPC.Helper.Common.dll");
        if (!File.Exists(helperPath))
            throw new Exception($"{helperPath} not found");

        _helper = NativeLibLoader.CreateComObject<IIpcHelper, CreateHelperDelegate>(
                      helperPath, "CreateHelper", out _helperObjectPtr, out _helperDllPtr)
                  ?? throw new Exception("Can't create helper");
        
        return _helper;
    }
    
    /// <summary>
    /// Get instance of running CAM application to interact with it
    /// </summary>
    public static ICamIpcApplication GetApplication()
    {
        if (_application != null)
            return _application;

        var executeContext = new TExecuteContext();
        var instances = GetHelper().GetRunningCamAppList(ref executeContext);
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        if (instances.Count == 0)
            throw new Exception("ENCY running instance not found");

        _application = instances.Get(0, executeContext);
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        return _application;
    }
    
    /// <summary>
    /// Destructor. Release COM objects
    /// </summary>
    public static void Clean()
    {
        if (_helper != null)
        {
            if (_application != null)
                Marshal.ReleaseComObject(_application);
            NativeLibLoader.FreeDll(_helper, _helperObjectPtr, _helperDllPtr);
        }
    }
}