using ExtensionNCFilesExporterNet;

// ReSharper disable once CheckNamespace
namespace CAMAPI;

using Extensions;
using ResultStatus;

/// <summary>
/// Factory for creating extensions. Namespace and class name always should be CAMAPI.ExtensionFactory,
/// so CAMAPI will find it.
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
    /// Create new instance of our extension
    /// </summary>
    public IExtension? Create(string extensionIdent, out TResultStatus ret)
    {
        try
        {
            ret = default;
            if (extensionIdent == "Extension.NCFilesExporter.Example.Net")
                return new ExtensionNCFilesExporter();
            throw new Exception("Unknown extension identifier: " + extensionIdent);
        }
        catch (Exception e)
        {
            ret.Code = TResultStatusCode.rsError;
            ret.Description = e.Message;
        }
        return null;
    }
}
