using System.Runtime.InteropServices;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using ExtensionProjectSaveLoadNet;

// The loader expects this exact namespace and class name.
namespace CAMAPI;

[ComVisible(true), ClassInterface(ClassInterfaceType.None)]
public sealed class ExtensionFactory : IExtensionFactory
{
    public void OnLibraryRegistered(IExtensionFactoryContext context, out TResultStatus ret) => ret = default;
    public void OnLibraryUnRegistered(IExtensionFactoryContext context, out TResultStatus ret) => ret = default;

    public IExtension? Create(string extensionIdent, out TResultStatus ret)
    {
        ret = default;
        try
        {
            return extensionIdent switch
            {
                EditOrderCard.ExtensionId => new EditOrderCard(),
                ProjectSerializer.ExtensionId => new ProjectSerializer(),
                _ => throw new ArgumentException($"Unknown extension identifier: {extensionIdent}")
            };
        }
        catch (Exception e)
        {
            ret.Code = TResultStatusCode.rsError;
            ret.Description = $"ExtensionFactory.Create: {e.Message}";
            return null;
        }
    }
}
