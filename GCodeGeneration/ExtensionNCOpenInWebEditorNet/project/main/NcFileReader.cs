using System.Text;

namespace ExtensionNCOpenInWebEditorNet;

/// <summary>
/// Reads an NC file into a string
/// </summary>
public static class NcFileReader
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Read the file as UTF-8, or in the Windows ANSI code page when it is not valid UTF-8
    /// </summary>
    public static string ReadText(string path)
    {
        var bytes = File.ReadAllBytes(path);
        try
        {
            using var reader = new StreamReader(new MemoryStream(bytes), StrictUtf8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }
        catch (DecoderFallbackException)
        {
            // Code page 0 is the ANSI code page of Windows
            var ansi = CodePagesEncodingProvider.Instance.GetEncoding(0) ?? Encoding.Latin1;
            return ansi.GetString(bytes);
        }
    }
}
