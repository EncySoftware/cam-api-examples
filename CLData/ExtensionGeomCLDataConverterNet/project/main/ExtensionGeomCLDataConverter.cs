using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.MCDFormerTypes;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;

namespace ExtensionGeomCLDataConverterNet;

/// <summary>
/// Show parameters of project
/// </summary>
public class ExtensionGeomClDataConverter : IExtension, IExtensionGeomCLDataConverter
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    private ICamApiCLDReceiver? _wrapper;
    
    /// <summary>
    /// Create wrapper over receiver
    /// </summary>
    public ICamApiCLDReceiver? GetCLDReceiverWrapper(ICamApiTechOperation operation,
        ICamApiCLDReceiver receiver,
        out TResultStatus ret)
    {
        ret = default;
        try
        {
            if (receiver == null)
                throw new Exception("Receiver is null");
            
            _wrapper = new CLDReceiverWrapperCustom(receiver);
            return _wrapper;
        }
        catch (Exception e)
        {
            ret.Code = TResultStatusCode.rsError;
            ret.Description = e.Message;
            return null;
        }
    }
    
    /// <summary>
    /// Clean COM-objects
    /// </summary>
    public void FinalizeConverter()
    {
        if (_wrapper is IDisposable disposable)
        {
            disposable.Dispose();
            _wrapper = null;
        }
    }
}