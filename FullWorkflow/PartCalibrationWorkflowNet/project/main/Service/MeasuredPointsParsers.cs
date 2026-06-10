using System.Globalization;
using System.IO;
using System.Xml.Linq;
using PartCalibrationWorkflowNet.Model;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Strategy for parsing a measured-points report file.
/// </summary>
public interface IMeasuredPointsParser
{
    /// <summary>Display name shown in the combobox.</summary>
    string DisplayName { get; }

    /// <summary>File extension filter for the open dialog (e.g. "*.txt").</summary>
    string FileFilter { get; }

    /// <summary>Parse the file and return its points (Name optional).</summary>
    List<Point3D> Parse(string filePath);
}

/// <summary>
/// In-memory registry of available parsers. Plugins or downstream code can
/// <see cref="Register"/> new parsers without touching the rest of the wizard.
/// </summary>
public static class MeasuredPointsParserRegistry
{
    private static readonly List<IMeasuredPointsParser> Parsers = new()
    {
        new PlainTextParser(),
        new DxfParser(),
        new XmlParser(),
    };

    public static IReadOnlyList<IMeasuredPointsParser> All => Parsers;

    public static IMeasuredPointsParser? FindByDisplayName(string name) =>
        Parsers.FirstOrDefault(p =>
            string.Equals(p.DisplayName, name, StringComparison.OrdinalIgnoreCase));

    public static void Register(IMeasuredPointsParser parser) => Parsers.Add(parser);
}

/// <summary>
/// Plain text: one point per non-empty line, three numbers separated by ';',
/// ',' or whitespace. Lines starting with '#' are ignored.
/// </summary>
internal sealed class PlainTextParser : IMeasuredPointsParser
{
    public string DisplayName => "Plain text (X;Y;Z)";
    public string FileFilter  => "*.txt;*.csv";

    public List<Point3D> Parse(string filePath)
    {
        var separators = new[] { ';', ',', ' ', '\t' };
        var result = new List<Point3D>();
        int idx = 1;
        foreach (var rawLine in File.ReadAllLines(filePath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var parts = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            // A non-numeric first field is the point name ("name;x;y;z").
            string? name = null;
            var o = 0;
            if (parts.Length >= 4 &&
                !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                name = parts[0];
                o = 1;
            }
            if (parts.Length - o < 3)
                throw new FormatException($"Line {idx}: expected at least 3 numbers, got '{rawLine}'.");
            if (!double.TryParse(parts[o], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                !double.TryParse(parts[o + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
                !double.TryParse(parts[o + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                throw new FormatException($"Line {idx}: cannot parse '{rawLine}' as X;Y;Z numbers.");
            result.Add(new Point3D { Name = name ?? $"m{idx}", X = x, Y = y, Z = z });
            idx++;
        }
        return result;
    }
}

/// <summary>
/// Minimal DXF reader: scans for POINT entities and reads the 10/20/30 codes.
/// Sufficient for probing-report exports without bringing in a full DXF parser.
/// </summary>
internal sealed class DxfParser : IMeasuredPointsParser
{
    public string DisplayName => "DXF (POINT entities)";
    public string FileFilter  => "*.dxf";

    public List<Point3D> Parse(string filePath)
    {
        var result = new List<Point3D>();
        var lines = File.ReadAllLines(filePath);
        int idx = 1;
        for (int i = 0; i < lines.Length - 1; i++)
        {
            if (lines[i].Trim() != "0" || lines[i + 1].Trim() != "POINT") continue;
            double x = 0, y = 0, z = 0;
            int j = i + 2;
            while (j < lines.Length - 1 && lines[j].Trim() != "0")
            {
                var code = lines[j].Trim();
                if (!double.TryParse(lines[j + 1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
                {
                    j += 2;
                    continue;
                }
                switch (code)
                {
                    case "10": x = val; break;
                    case "20": y = val; break;
                    case "30": z = val; break;
                }
                j += 2;
            }
            result.Add(new Point3D { Name = $"m{idx++}", X = x, Y = y, Z = z });
            i = j - 1;
        }
        return result;
    }
}

/// <summary>
/// Simple XML reader: matches every &lt;Point X="" Y="" Z=""/&gt; element
/// regardless of namespace; optional Name attribute is preserved.
/// </summary>
internal sealed class XmlParser : IMeasuredPointsParser
{
    public string DisplayName => "XML (&lt;Point X Y Z/&gt;)";
    public string FileFilter  => "*.xml";

    public List<Point3D> Parse(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var result = new List<Point3D>();
        int idx = 1;
        foreach (var el in doc.Descendants().Where(e => e.Name.LocalName == "Point"))
        {
            var x = Read(el, "X");
            var y = Read(el, "Y");
            var z = Read(el, "Z");
            var name = el.Attribute("Name")?.Value ?? $"m{idx}";
            result.Add(new Point3D { Name = name, X = x, Y = y, Z = z });
            idx++;
        }
        return result;
    }

    private static double Read(XElement el, string attr)
    {
        var s = el.Attribute(attr)?.Value
                ?? throw new FormatException($"<Point> is missing the '{attr}' attribute.");
        return double.Parse(s, CultureInfo.InvariantCulture);
    }
}
