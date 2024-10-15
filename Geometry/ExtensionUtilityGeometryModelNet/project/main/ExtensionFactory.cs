using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace CAMAPI;

/// <summary>
/// Singleton factory for creating extensions
/// </summary>
public class ExtensionFactory : IExtensionFactory
{
    /// <summary>
    /// Create extension by identifier from json-file
    /// </summary>
    /// <param name="extensionIdent">Identifier from json-file</param>
    /// <param name="ret">Structure to provide error text</param>
    /// <returns>New instance of extension</returns>
    public IExtension? Create(string extensionIdent, out TResultStatus result)
    {
        try
        {
            result = default;

            switch (extensionIdent)
            {
                case "GeometryModelExportExample":
                    return new ExtensionUtilityGeometryModelNet.GeometryModelExportExample();
                case "GeometryNodeTransformExample":
                    return new ExtensionUtilityGeometryModelNet.GeometryNodeTransformExample();
                case "GeometryNodesIteratorExample":
                    return new ExtensionUtilityGeometryModelNet.GeometryNodesIteratorExample();
                default:
                    result.Code = TResultStatusCode.rsError;
                    result.Description = "Unknown extension identifier: " + extensionIdent;
                    return null;
            }
        }
        catch (Exception e)
        {
            result.Code = TResultStatusCode.rsError;
            result.Description = e.Message;
        }
        return null;
    }

    public void OnLibraryRegistered(IExtensionFactoryContext Context, out TResultStatus result)
    {
        result = default; 
    }

    public void OnLibraryUnRegistered(IExtensionFactoryContext Context, out TResultStatus result)
    {
        result = default; 
    }
}