using System.Globalization;
using System.Xml;

namespace ExtensionUtilityImportBtlxNet;

/// <summary>
/// Reads the parts of a BTLx file
/// </summary>
public static class BtlxReader
{
    /// <summary>
    /// Read every part of the file
    /// </summary>
    public static List<BtlxPart> Read(string filePath)
    {
        var document = new XmlDocument();
        document.Load(filePath);

        var root = document.DocumentElement;
        if (root == null || root.LocalName != "BTLx")
            throw new Exception($"Not a BTLx file: {filePath}");

        // the namespace differs between the versions of the format, so elements are matched by their local name
        var partNodes = root.SelectNodes("*[local-name()='Project']/*[local-name()='Parts']/*[local-name()='Part']");
        var parts = new List<BtlxPart>();
        if (partNodes == null)
            return parts;

        foreach (XmlElement partNode in partNodes)
            parts.Add(ReadPart(partNode, parts.Count));
        return parts;
    }

    private static BtlxPart ReadPart(XmlElement partNode, int index)
    {
        var designation = partNode.GetAttribute("Designation");
        var name = string.IsNullOrWhiteSpace(designation) ? $"Part {index + 1}" : $"{designation} {index + 1}";

        return new BtlxPart(
            name,
            ReadNumber(partNode.GetAttribute("Length")),
            ReadNumber(partNode.GetAttribute("Height")),
            ReadNumber(partNode.GetAttribute("Width")),
            ReadPlacement(partNode));
    }

    /// <summary>
    /// Position of the part in the project. A part listed with Count above one carries one
    /// transformation per copy, the first one is taken
    /// </summary>
    private static Frame3D? ReadPlacement(XmlElement partNode)
    {
        var position = partNode.SelectSingleNode(
            "*[local-name()='Transformations']/*[local-name()='Transformation']/*[local-name()='Position']");
        if (position == null)
            return null;

        return Frame3D.Orthonormal(
            ReadVector(position, "ReferencePoint"),
            ReadVector(position, "XVector"),
            ReadVector(position, "YVector"));
    }

    private static Vector3D ReadVector(XmlNode position, string elementName)
    {
        if (position.SelectSingleNode($"*[local-name()='{elementName}']") is not XmlElement element)
            throw new Exception($"The position of a part has no {elementName}");

        return new Vector3D(
            ReadNumber(element.GetAttribute("X")),
            ReadNumber(element.GetAttribute("Y")),
            ReadNumber(element.GetAttribute("Z")));
    }

    private static double ReadNumber(string text)
        => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
}
