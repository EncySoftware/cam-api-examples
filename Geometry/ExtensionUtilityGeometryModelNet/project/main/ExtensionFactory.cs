using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace CAMAPI;

/// <summary>
/// Factory for creating extensions. Namespace and class name always should be CAMAPI.ExtensionFactory,
/// so CAMAPI will find it
/// </summary>
public class ExtensionFactory : IExtensionFactory
{
    /// <summary>
    /// Create extension by identifier from json-file
    /// </summary>
    /// <param name="extensionIdent">Identifier from json-file</param>
    /// <param name="resultStatus">Structure to provide error text</param>
    /// <returns>New instance of extension</returns>
    public IExtension? Create(string extensionIdent, out TResultStatus resultStatus)
    {
        try
        {
            resultStatus = default;

            switch (extensionIdent)
            {
                case "GeometryModelExportExample":
                    return new ExtensionUtilityGeometryModelNet.GeometryModelExportExample();
                case "GeometryNodeTransformExample":
                    return new ExtensionUtilityGeometryModelNet.GeometryNodeTransformExample();
                case "GeometryNodesIteratorExample":
                    return new ExtensionUtilityGeometryModelNet.GeometryNodesIteratorExample();
                default:
                    resultStatus.Code = TResultStatusCode.rsError;
                    resultStatus.Description = "Unknown extension identifier: " + extensionIdent;
                    return null;
            }
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
        return null;
    }

    /// <inheritdoc />
    public void OnLibraryRegistered(IExtensionFactoryContext Context, out TResultStatus resultStatus)
    {
        resultStatus = default; 
    }

    /// <inheritdoc />
    public void OnLibraryUnRegistered(IExtensionFactoryContext Context, out TResultStatus resultStatus)
    {
        resultStatus = default; 
    }
}