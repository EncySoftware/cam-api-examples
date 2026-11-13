namespace ExtensionNCOpenInWebEditorNet;

/// <summary>
/// The web editor the program goes to. Change these values to point the extension at another editor
/// </summary>
public static class WebEditorSettings
{
    /// <summary>
    /// Editor page. The program is added to it after <c>#</c>
    /// </summary>
    public const string Url = "https://cnc-technik.de/kuka/";

    /// <summary>
    /// Caption of the export target in ENCY
    /// </summary>
    public const string Caption = "Open in CNC-Technik KUKA editor";

    /// <summary>
    /// Link parameter of the main program file, picked first when the postprocessor writes several files
    /// </summary>
    public const string MainFileKey = "src";

    /// <summary>
    /// File extensions the editor accepts. Each one is also the name of the link parameter that carries the file
    /// </summary>
    public static readonly IReadOnlyList<string> FileKeys = ["src", "dat", "sub", "mod", "prg", "sys"];

    /// <summary>
    /// Longest link passed to the browser directly. A longer link goes through a local redirect page
    /// </summary>
    public const int MaxDirectLinkLength = 32000;

    /// <summary>
    /// Folder in the temp folder for the redirect pages
    /// </summary>
    public const string RedirectFolder = "ExtensionNCOpenInWebEditorNet";
}
