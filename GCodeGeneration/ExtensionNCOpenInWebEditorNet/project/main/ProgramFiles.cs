namespace ExtensionNCOpenInWebEditorNet;

/// <summary>
/// A generated NC file that goes to the editor
/// </summary>
/// <param name="Key">Link parameter of the file: its extension without the dot, in lower case</param>
/// <param name="Path">Full path of the file</param>
public sealed record ProgramFile(string Key, string Path);

/// <summary>
/// The generated NC files split into the ones that go to the editor and the rest
/// </summary>
/// <param name="Name">Program name: the main file name without extension</param>
/// <param name="Sent">Files that go to the editor, the main file first</param>
/// <param name="NotSent">Generated files that do not go to the editor</param>
public sealed record ProgramFiles(string Name, IReadOnlyList<ProgramFile> Sent, IReadOnlyList<string> NotSent)
{
    /// <summary>
    /// The first file with <paramref name="mainKey"/> or else with any of <paramref name="keys"/>, plus one file per key
    /// with its name. Null when no file has a key from <paramref name="keys"/>
    /// </summary>
    public static ProgramFiles? Pick(IReadOnlyList<string> ncFiles, IReadOnlyCollection<string> keys, string mainKey)
    {
        var mainFile = ncFiles.FirstOrDefault(file => KeyOf(file) == mainKey)
                       ?? ncFiles.FirstOrDefault(file => keys.Contains(KeyOf(file)));
        if (mainFile == null)
            return null;

        var name = Path.GetFileNameWithoutExtension(mainFile);
        var sent = new List<ProgramFile> { new(KeyOf(mainFile), mainFile) };
        foreach (var file in ncFiles)
        {
            var key = KeyOf(file);
            if (!keys.Contains(key) || sent.Any(part => part.Key == key))
                continue;
            if (!string.Equals(Path.GetFileNameWithoutExtension(file), name, StringComparison.OrdinalIgnoreCase))
                continue;
            sent.Add(new ProgramFile(key, file));
        }

        var notSent = ncFiles.Where(file => sent.All(part => part.Path != file)).ToList();
        return new ProgramFiles(name, sent, notSent);
    }

    /// <summary>
    /// Link parameter of a file: its extension without the dot, in lower case
    /// </summary>
    public static string KeyOf(string file)
    {
        return Path.GetExtension(file).TrimStart('.').ToLowerInvariant();
    }
}
