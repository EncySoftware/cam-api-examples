using System.Diagnostics;
using CAMAPI.DotnetHelper;
using CAMAPI.CustomAttributes;

namespace ExtensionAttributesManageNet;

/// <summary>
/// Prints all attributes inside tree iterator to Debug console
/// </summary>
internal static class AttributesDescriber
{
    /// <summary>
    /// Prints all attributes inside tree iterator to Debug console
    /// </summary>
    public static void Describe(ICamApiCustomAttributesTreeIterator it, int indentCount = 0)
    {
        // Decribe attribute of current node
        using var node = ComWrapper.Create(it.CurrentNode);
        using var a = ComWrapper.Create(node.It.Attribute);
        var indent = new string(' ', indentCount*4);
        Debug.Write(indent);
        Debug.Write("Attribute '" + a.It.Name + "' of type " + a.It.ValueType);
        switch (a.It.ValueType)
        {
            case TCustomAttributeValueType.avtString:
                Debug.Write(" with value '" + ((ICamApiStringCustomAttribute)a.It).Value + "'");
                break;
            case TCustomAttributeValueType.avtInteger:
                Debug.Write(" with value '" + ((ICamApiIntegerCustomAttribute)a.It).Value + "'");
                break;
            case TCustomAttributeValueType.avtFloat:
                Debug.Write(" with value '" + ((ICamApiFloatCustomAttribute)a.It).Value + "'");
                break;
            case TCustomAttributeValueType.avtBoolean:
                Debug.Write(" with value '" + ((ICamApiBooleanCustomAttribute)a.It).Value + "'");
                break;
        }
        Debug.WriteLine("");

        // Enumerate children of current node
        using var chItW = ComWrapper.Create(it.GetCopy());
        var chIt = chItW.It;
        if (chIt.MoveToChild())
        {
            do
            {
                Describe(chIt, indentCount+1);

            } while (chIt.MoveToSibling());
            chIt.MoveToParent();
        }
    }

}