namespace ExtensionNCOpenInWebEditorNet;

/// <summary>
/// The web editor the program goes to. Change these values to point the extension at another editor
/// </summary>
public static class WebEditorSettings
{
    /// <summary>
    /// Page opened in the default browser
    /// </summary>
    public const string Url = "https://cnc-technik.de/kuka/";

    /// <summary>
    /// Caption of the export target in ENCY
    /// </summary>
    public const string Caption = "Open in CNC-Technik KUKA editor";

    /// <summary>
    /// Extension of the main program file, picked first when the postprocessor writes several files
    /// </summary>
    public const string MainFileExtension = ".src";

    /// <summary>
    /// What the user does in the editor to load the program
    /// </summary>
    public const string PasteHint = "Press \"Read from clipboard\" in the editor.";
}
