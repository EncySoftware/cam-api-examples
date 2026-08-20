using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExtensionUtilityExportInformationNet;

///
public class Point3D
{
    ///
    [JsonPropertyName("X")]
    public double X { get; set; }
    ///
    [JsonPropertyName("Y")]
    public double Y { get; set; }
    ///
    [JsonPropertyName("Z")]
    public double Z { get; set; }
}

///
public class ToolpathPoint
{
    ///
    [JsonPropertyName("EndPoint")]
    public Point3D EndPoint { get; set; } = new();

    ///
    [JsonPropertyName("vZ")]
    public Point3D? vZ { get; set; }
}

///
public class ToolpathCommand
{
    ///
    [JsonPropertyName("CommandCode")]
    public string CommandCode { get; set; } = "";

    ///
    [JsonPropertyName("CommandCaption")]
    public string CommandCaption { get; set; } = "";

    ///
    [JsonPropertyName("FeedType")]
    public int? FeedType { get; set; }

    ///
    [JsonPropertyName("Points")]
    public List<ToolpathPoint>? Points { get; set; }
}

///
public class CAMToolpath
{
    ///
    [JsonPropertyName("Commands")]
    public List<ToolpathCommand> Commands { get; set; } = new();
}

///
public class ToolpathRoot
{
    ///
    [JsonPropertyName("CAMToolpath")]
    public CAMToolpath CAMToolpath { get; set; } = new();
}