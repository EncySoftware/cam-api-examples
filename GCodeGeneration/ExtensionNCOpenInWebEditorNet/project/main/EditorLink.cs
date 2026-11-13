using System.Buffers.Text;
using System.IO.Compression;

namespace ExtensionNCOpenInWebEditorNet;

/// <summary>
/// Content of one program file for the editor link
/// </summary>
/// <param name="Key">Link parameter of the file, for example <c>src</c> or <c>dat</c></param>
/// <param name="Content">Bytes of the file as the postprocessor wrote them</param>
public sealed record EditorLinkPart(string Key, byte[] Content);

/// <summary>
/// Builds the editor link that carries the program
/// </summary>
public static class EditorLink
{
    /// <summary>
    /// <c>editorUrl#name=NAME&amp;key=DATA&amp;...&amp;z=1</c>, each DATA is the file in raw deflate and base64url without padding
    /// </summary>
    public static string Build(string editorUrl, string programName, IEnumerable<EditorLinkPart> parts)
    {
        var parameters = new List<string> { "name=" + Uri.EscapeDataString(programName) };
        parameters.AddRange(parts.Select(part => part.Key + "=" + Compress(part.Content)));
        parameters.Add("z=1");
        return editorUrl + "#" + string.Join("&", parameters);
    }

    private static string Compress(byte[] content)
    {
        using var compressed = new MemoryStream();
        using (var deflate = new DeflateStream(compressed, CompressionLevel.SmallestSize))
            deflate.Write(content);
        return Base64Url.EncodeToString(compressed.ToArray());
    }
}
