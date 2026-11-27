using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using ExtensionBatchNCExportNet;

namespace CAMAPI;

/// <summary>Factory for the batch NC generation and export utility.</summary>
public sealed class ExtensionFactory : IExtensionFactory
{
    /// <inheritdoc />
    public void OnLibraryRegistered(IExtensionFactoryContext context, out TResultStatus ret) => ret = default;

    /// <inheritdoc />
    public void OnLibraryUnRegistered(IExtensionFactoryContext context, out TResultStatus ret) => ret = default;

    /// <inheritdoc />
    public IExtension? Create(string extensionIdent, out TResultStatus ret)
    {
        ret = default;
        switch (extensionIdent)
        {
            case "Extension.BatchNCExport.Net":
                return new BatchNCExportUtility();
            default:
                ret.Code = TResultStatusCode.rsError;
                ret.Description = "Unknown extension identifier: " + extensionIdent;
                return null;
        }
    }
}
