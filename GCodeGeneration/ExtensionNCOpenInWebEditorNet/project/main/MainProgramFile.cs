namespace ExtensionNCOpenInWebEditorNet;

/// <summary>
/// Chooses which of the generated NC files goes to the editor
/// </summary>
public static class MainProgramFile
{
    /// <summary>
    /// The first file with the given extension, or the first file when none has it.
    /// Null when the list is empty
    /// </summary>
    public static string? Pick(IReadOnlyList<string> ncFiles, string mainExtension)
    {
        return ncFiles.FirstOrDefault(file =>
                   string.Equals(Path.GetExtension(file), mainExtension, StringComparison.OrdinalIgnoreCase))
               ?? ncFiles.FirstOrDefault();
    }
}
