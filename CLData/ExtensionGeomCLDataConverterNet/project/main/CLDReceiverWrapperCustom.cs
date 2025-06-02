using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.MCDFormerTypes;
using STTypes;

namespace ExtensionGeomCLDataConverterNet;

/// <summary>
/// Simple realization just to log commands
/// </summary>
public class CLDReceiverWrapperCustom : CLDRecevierWrapperDefault
{
    private readonly ComWrapper<IExtensionLogger> _logger;
    
    /// <summary>
    /// Simple realization just to log commands
    /// </summary>
    public CLDReceiverWrapperCustom(ICamApiCLDReceiver receiver) : base(receiver)
    {
        using var extensionManager = ExtensionManagerHelper.GetInstance();
        _logger = extensionManager.InvokeAndWrap(manager => manager.Logger);
    }
    
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