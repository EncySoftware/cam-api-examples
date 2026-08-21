using System.Reflection;
using System.Xml;
using ExtensionNetProject;

// ReSharper disable once CheckNamespace
namespace CAMAPI;

using Extensions;
using ResultStatus;

/// <summary>
/// Factory for creating extensions. Namespace and class name always should be CAMAPI.ExtensionFactory,
/// so CAMAPI will find it
/// </summary>
public class ExtensionFactory : IExtensionFactory
{
    private const string NodeOperationXMlName = "OperationParamsNet.xml";
    
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
            if (extensionIdent == "Extension.Operation.Params.Net")
                return new ExtensionOperationParams();
            throw new Exception("Unknown extension identifier: " + extensionIdent);
        }
        catch (Exception e)
        {
            ret.Code = TResultStatusCode.rsError;
            ret.Description = e.Message;
        }
        return null;
    }
    
    private XmlNode? GetOperationsNode(XmlNode? rootNode)
    {
        if (rootNode == null)
            return null;
        foreach (XmlNode childNode in rootNode.ChildNodes)
        {
            if (childNode.Attributes?["ID"]?.Value != "Operations")
                continue;
            return childNode;
        }
        
        return null;
    } 
    
    private static void DeleteOperationNode(XmlNode? operationsNode, string operationXMlName)
    {
        if (operationsNode == null)
            return;
        var childNode = operationsNode.FirstChild;
        while (childNode!=null)
        {
            var siblingNode = childNode.NextSibling;
            if (childNode.InnerText.ToLower().EndsWith(operationXMlName.ToLower())) 
            {
                operationsNode.RemoveChild(childNode);
            }
            childNode = siblingNode;
        }
    }

    /// <summary>
    /// Add information to UserOperationsList.xml about our operation
    /// </summary>
    public void OnLibraryUnRegistered(IExtensionFactoryContext context, out TResultStatus ret)
    {
        ret = default;
        var pathToUserOperationsList = Path.Combine(context.Paths.TryUnfoldPath(@"$(PROGRAM_COMMON_APPDATA)\Operations"), "UserOperationsList.xml");
        if (!File.Exists(pathToUserOperationsList))
            return;
        
        try
        {
            var userOperationsListDoc = new XmlDocument();
            userOperationsListDoc.Load(pathToUserOperationsList);
            XmlNode? rootNode = userOperationsListDoc.DocumentElement;
            if (rootNode == null)
                return;
            var operationsNode = GetOperationsNode(rootNode);
            if (operationsNode == null)
                return;
            DeleteOperationNode(operationsNode, NodeOperationXMlName);
            userOperationsListDoc.Save(pathToUserOperationsList);
        }
        catch (Exception ex)
        {
            ret.Code = TResultStatusCode.rsError;
            ret.Description = ex.Message;
        }
    }

    /// <summary>
    /// Delete information from UserOperationsList.xml about our operation
    /// </summary>
    public void OnLibraryRegistered(IExtensionFactoryContext context, out TResultStatus ret)
    {
        ret = default;
        try
        {
            var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                                 ?? throw new Exception("Assembly location is null");
            var pathToOperationXml = Path.Combine(assemblyFolder, NodeOperationXMlName);
            var operationsFolder = context.Paths.TryUnfoldPath(@"$(PROGRAM_COMMON_APPDATA)\Operations");
            var pathToUserOperationsList = Path.Combine(operationsFolder, "UserOperationsList.xml");
            
            // update existing one
            if (File.Exists(pathToUserOperationsList))
            {
                var userOperationsListDoc = new XmlDocument();
                userOperationsListDoc.Load(pathToUserOperationsList);
                XmlNode? rootNode = userOperationsListDoc.DocumentElement;
                if (rootNode == null)
                    return;
                var operationsNode = GetOperationsNode(rootNode);
                if (operationsNode == null)
                    return;
                DeleteOperationNode(operationsNode, NodeOperationXMlName);
                var newOperationElement = userOperationsListDoc.CreateElement("SCInclude");
                newOperationElement.InnerText = pathToOperationXml;
                var optionalAttr = userOperationsListDoc.CreateAttribute("Optional");
                optionalAttr.Value = "true";
                newOperationElement.Attributes.Append(optionalAttr);
                operationsNode.AppendChild(newOperationElement);
                userOperationsListDoc.Save(pathToUserOperationsList);
            }
        
            // create new one
            else
            {
                // on a clean installation the folder does not exist yet, and Save would fail on it
                Directory.CreateDirectory(operationsFolder);

                var userOperationsListDoc = new XmlDocument();
                var xmlDeclaration = userOperationsListDoc.CreateXmlDeclaration("1.0", null, null);
                userOperationsListDoc.InsertBefore(xmlDeclaration, userOperationsListDoc.DocumentElement);

                var rootElement = userOperationsListDoc.CreateElement("SCCollection");
                var rootIdAttr = userOperationsListDoc.CreateAttribute("ID");
                rootIdAttr.Value = @"$(OPERATIONS_FOLDER)\UserOperationsList.xml";
                rootElement.Attributes.Append(rootIdAttr);
                userOperationsListDoc.AppendChild(rootElement);

                var nameSpaceElement = userOperationsListDoc.CreateElement("SCNameSpace");
                var nameSpaceIdAttr = userOperationsListDoc.CreateAttribute("ID");
                nameSpaceIdAttr.Value = "Operations";
                nameSpaceElement.Attributes.Append(nameSpaceIdAttr);
                rootElement.AppendChild(nameSpaceElement);

                var newOperationElement = userOperationsListDoc.CreateElement("SCInclude");
                newOperationElement.InnerText = pathToOperationXml;
                var optionalAttr = userOperationsListDoc.CreateAttribute("Optional");
                optionalAttr.Value = "true";
                newOperationElement.Attributes.Append(optionalAttr);
                nameSpaceElement.AppendChild(newOperationElement);
                userOperationsListDoc.Save(pathToUserOperationsList);
            }
        }
        catch (Exception e)
        {
            ret.Code = TResultStatusCode.rsError;
            ret.Description = e.Message;
        }
    }
}