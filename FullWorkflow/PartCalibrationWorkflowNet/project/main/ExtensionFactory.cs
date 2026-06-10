using PartCalibrationWorkflowNet;

// ReSharper disable once CheckNamespace
namespace CAMAPI;

using Extensions;
using ResultStatus;

/// <summary>
/// Factory for creating extensions. Namespace and class name must always be CAMAPI.ExtensionFactory.
/// </summary>
// ReSharper disable once UnusedType.Global
public class ExtensionFactory : IExtensionFactory
{
    /// <inheritdoc />
    public void OnLibraryRegistered(IExtensionFactoryContext context, out TResultStatus ret) => ret = default;

    /// <inheritdoc />
    public void OnLibraryUnRegistered(IExtensionFactoryContext context, out TResultStatus ret) => ret = default;

    /// <inheritdoc />
    public IExtension? Create(string extensionIdent, out TResultStatus ret)
    {
        ret = default;
        try
        {
            return extensionIdent switch
            {
                "Extension.Utility.PartCalibration.Workflow" => new CalibrationWorkflowExtension(),
                _ => null
            };
        }
        catch (Exception e)
        {
            ret.Code = TResultStatusCode.rsError;
            ret.Description = e.Message;
            return null;
        }
    }
}
