using System.Text.Json;

namespace ExtensionNCOpenInWebEditorNet;

/// <summary>
/// What the browser is started with: the editor link, or a local page that opens it
/// </summary>
public static class BrowserTarget
{
    /// <summary>
    /// The link itself when it is not longer than <see cref="WebEditorSettings.MaxDirectLinkLength"/>.
    /// Otherwise a redirect page written to the temp folder, and its path
    /// </summary>
    public static string For(string link, string programName)
    {
        if (link.Length <= WebEditorSettings.MaxDirectLinkLength)
            return link;

        var folder = Path.Combine(Path.GetTempPath(), WebEditorSettings.RedirectFolder);
        Directory.CreateDirectory(folder);
        var page = Path.Combine(folder, programName + ".html");
        File.WriteAllText(page, RedirectPage(link));
        return page;
    }

    /// <summary>
    /// HTML page that replaces itself with <paramref name="url"/>
    /// </summary>
    public static string RedirectPage(string url)
    {
        return $"""
                <!DOCTYPE html>
                <html>
                <head>
                <meta charset="utf-8">
                <script>location.replace({JsonSerializer.Serialize(url)});</script>
                </head>
                <body></body>
                </html>
                """;
    }
}
