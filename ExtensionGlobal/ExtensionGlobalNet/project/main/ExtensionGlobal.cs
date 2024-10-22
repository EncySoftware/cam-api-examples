using System.Runtime.InteropServices;
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.GeomImporter;
using CAMAPI.Project;
using CAMAPI.ResultStatus;

namespace ExtensionGlobalNet;

/// <summary>
/// Empty extension to show entry point on initializing / finalizing
/// </summary>
public class ExtensionGlobal: IExtension, IExtensionGlobal
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public TResultStatus OnSCInitializing()
    {
        TResultStatus resultStatus = default;
        try
        {
            // do something
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
        return resultStatus;
    }

    /// <inheritdoc />
    public TResultStatus OnSCFinalizing()
    {
        TResultStatus resultStatus = default;
        try
        {
            // do something
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
        return resultStatus;
    }
}
