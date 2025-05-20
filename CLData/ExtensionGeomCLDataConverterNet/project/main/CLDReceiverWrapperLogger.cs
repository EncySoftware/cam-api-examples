using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.MCDFormerTypes;

namespace ExtensionGeomCLDataConverterNet;

/// <summary>
/// Simple realization just to log commands
/// </summary>
public class CLDReceiverWrapperLogger : CLDRecevierWrapperDefault
{
    private readonly ComWrapper<IExtensionLogger> _logger;
    
    /// <summary>
    /// Simple realization just to log commands
    /// </summary>
    public CLDReceiverWrapperLogger(ICamApiCLDReceiver receiver) : base(receiver)
    {
        using var extensionManager = ExtensionManagerHelper.GetInstance();
        _logger = extensionManager.InvokeAndWrap(manager => manager.Logger);
    }
    
    /// <summary>
    /// Simple implementation
    /// </summary>
    public override void AddComment(string comment)
    {
        _logger.Invoke(logger => logger.Info(comment));
        base.AddComment(comment);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _logger.Dispose();
        base.Dispose();
    }
}