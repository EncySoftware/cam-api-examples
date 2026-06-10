using ExtensionToolsListPopupNet;

// ReSharper disable once CheckNamespace
namespace CAMAPI;

using System;
using Extensions;
using ResultStatus;

/// <summary>
/// Factory for creating extensions. Namespace and class name always should be CAMAPI.ExtensionFactory,
/// so CAMAPI will find it
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
    /// <param name="extensionIdent">
    /// Unique identifier, if out library has more than one extension. Should accord with
    /// value in settings json, describing this library
    /// </param>
    /// <param name="ret">Error to return it, because throw exception will not work</param>
    /// <returns>Instance of out extension</returns>
    public IExtension? Create(string extensionIdent, out TResultStatus ret)
    {
        try
        {
            ret = default;
            if (extensionIdent == "Extension.ToolsListPopup.Example.Net")
                return new ExtensionToolsListPopup();
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
