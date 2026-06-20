using OperationUserParametersNet;

// ReSharper disable once CheckNamespace
namespace CAMAPI;

using Extensions;
using ResultStatus;

/// <summary>
/// Factory for creating extensions. Namespace and class name always should be CAMAPI.ExtensionFactory,
/// so CAMAPI will find it. This library exposes a single utility that opens a window with
/// Get / Add / Delete buttons operating on the user parameters of the current technology operation.
/// </summary>
public class ExtensionFactory : IExtensionFactory
{
    /// <inheritdoc />
    public void OnLibraryRegistered(IExtensionFactoryContext context, out TResultStatus ret)
    {
        ret = default;
    }

    /// <inheritdoc />
    public void OnLibraryUnRegistered(IExtensionFactoryContext context, out TResultStatus ret)
    {
        ret = default;
    }

    /// <summary>
    /// Create a new instance of the extension matching <paramref name="extensionIdent"/>.
    /// </summary>
    /// <param name="extensionIdent">
    /// Unique identifier of the extension, matching the "id" field in the settings json.
    /// </param>
    /// <param name="ret">Error to return, because throwing an exception across the COM boundary will not work.</param>
    /// <returns>New instance of the requested extension, or null on error.</returns>
    public IExtension? Create(string extensionIdent, out TResultStatus ret)
    {
        try
        {
            ret = default;
            return extensionIdent switch
            {
                "Extension.Utility.OperationUserParameters" => new OperationUserParametersExtension(),
                _ => throw new Exception("Unknown extension identifier: " + extensionIdent)
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
