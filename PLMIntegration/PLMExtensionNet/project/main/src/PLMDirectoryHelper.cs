using System.IO.Compression;

/// <summary>
/// Helper class for performing directory operations.
/// </summary>
public class PLMDirectoryHelper
{
    private string _basePath = string.Empty;
    
    /// <summary>
    /// Gets the base directory path.
    /// </summary>
    public string BaseDirectory 
    { 
        get => _basePath;
        private set => _basePath = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PLMDirectoryHelper"/> class with the specified base path.
    /// </summary>
    /// <param name="basePath">The base directory path to work with.</param>
    public PLMDirectoryHelper(string basePath)
    {
        UpdateBasePath(basePath);
    }

    /// <summary>
    /// Updates a base path of the <see cref="PLMDirectoryHelper"/> class object.
    /// </summary>
    /// <param name="newBasePath">The base directory path to work with.</param>
    public void UpdateBasePath(string newBasePath)
    {
        if (!Directory.Exists(newBasePath))
            Directory.CreateDirectory(newBasePath);

        _basePath = newBasePath;
    }
    
    /// <summary>
    /// Gets immediate subdirectories of a specified directory.
    /// If <paramref name="directoryName"/> is empty, returns subdirectories of the base path.
    /// If <paramref name="directoryName"/> is provided, finds that directory and returns its subdirectories.
    /// </summary>
    /// <param name="directoryName">Optional. The exact name of the directory to search under (case-sensitive).</param>
    /// <returns>An array of full paths to the subdirectories.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown if the specified directory is not found.</exception>
    public string[] GetSubdirectories(string directoryName = "")
    {
        string targetPath = _basePath;

        if (!string.IsNullOrEmpty(directoryName) && !IsBaseDirectory(directoryName))
        {
            string foundDir = FindSubdirectoryByExactName(directoryName);
            if (foundDir == string.Empty)
                throw new DirectoryNotFoundException($"Directory '{directoryName}' not found.");

            targetPath = foundDir;
        }

        return Directory.GetDirectories(targetPath);
    }
    
    /// <summary>
    /// Creates a new directory with the specified name in the given parent directory.
    /// </summary>
    /// <param name="parentDirectory">The parent directory where the new directory should be created.</param>
    /// <param name="directoryName">The name of the new directory to create.</param>
    /// <exception cref="DirectoryNotFoundException">Thrown if the specified parent directory does not exist under the base directory.</exception>
    public void CreateDirectory(string parentDirectory, string directoryName)
    {
        string targetDir;
        if (string.IsNullOrEmpty(parentDirectory) || IsBaseDirectory(parentDirectory))
            targetDir = _basePath;
        else
        {
            targetDir = FindSubdirectoryByExactName(parentDirectory);
            if (!Directory.Exists(targetDir))
                throw new DirectoryNotFoundException($"Target directory '{parentDirectory}' not found.");
        }

        string newDirectoryPath = Path.Combine(targetDir, directoryName);
        Directory.CreateDirectory(newDirectoryPath);
    }

    /// <summary>
    /// Copies a file or extracts a ZIP archive into a uniquely named subdirectory inside the base directory.
    /// If a subdirectory with the same name already exists, a timestamp is appended to ensure uniqueness.
    /// </summary>
    /// <param name="sourceFilePath">The full path to the source file or ZIP archive.</param>
    /// <param name="targetDirectoryName">The target directory name where the file or extracted content will be placed.</param>
    /// <param name="replace">Specifies whether to replace an existing item if one is found.</param>
    /// <exception cref="DirectoryNotFoundException">Thrown if a directory does not exist.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the source file is not found.</exception>
    public void CopyFileToPLMDirectory(string sourceFilePath, string targetDirectoryName, bool replace = false)
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException($"Source file not found: {sourceFilePath}");
        
        var targetDir = FindSubdirectoryByExactName(targetDirectoryName);
        if (!Directory.Exists(targetDir))
            throw new DirectoryNotFoundException($"Target directory '{targetDirectoryName}' not found.");

        try
        {
            var files = Directory.GetFiles(targetDir);
            if (files.Length > 0)
                if (replace)
                {
                    Directory.Delete(targetDir, true);
                    Directory.CreateDirectory(targetDir);
                }
                else return;
        }
        catch (Exception ex)
        {
            throw new Exception($"Cannot replace directory {targetDirectoryName}: {ex.Message}.");
        }

        try
        {
            if (Path.GetExtension(sourceFilePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                ZipFile.ExtractToDirectory(sourceFilePath, targetDir);
            else
            {
                string destFilePath = Path.Combine(targetDir, Path.GetFileName(sourceFilePath));

                File.Copy(sourceFilePath, destFilePath, replace);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Cannot copy files to directory {targetDirectoryName}: {ex.Message}.");
        }
    }
    
    /// <summary>
    /// Copies content of the given source directory to the destination folder.
    /// If the source contains only a single file, the file is copied directly.
    /// If the source contains multiple files or subdirectories, it is zipped and the archive is copied.
    /// </summary>
    /// <param name="sourceDirectoryName">The name of the source directory.</param>
    /// <param name="destinationDirectory">Full path to the destination directory.</param>
    /// <param name="zipIfMultiple">If true and multiple files exist, they are zipped before copying.</param>
    /// <exception cref="DirectoryNotFoundException">Thrown if the specified directory is not found.</exception>
    public IEnumerable<string> CopyFilesFromPLMDirectory(string sourceDirectoryName, string destinationDirectory, bool zipIfMultiple = true)
    {
        var sourceDirectory = FindSubdirectoryByExactName(sourceDirectoryName);
        if (sourceDirectory == string.Empty)
            throw new DirectoryNotFoundException($"Source directory '{sourceDirectory}' does not exist.");

        if (!Directory.Exists(destinationDirectory))
            throw new DirectoryNotFoundException($"Source directory '{destinationDirectory}' does not exist.");

        var files = Directory.GetFiles(sourceDirectory);
        var subDirs = Directory.GetDirectories(sourceDirectory);

        List<string> destFilePaths = [];
        if (files.Length == 1 && subDirs.Length == 0 || !zipIfMultiple)
        {
            foreach (var file in files)
            {
                var destFilePath = Path.Combine(destinationDirectory, Path.GetFileName(file));
                File.Copy(file, destFilePath, overwrite: true);
                destFilePaths.Add(destFilePath);
            }
        }
        else
        {
            var zipFileName = $"{Path.GetFileName(sourceDirectory)}.zip";
            var destFilePath = Path.Combine(destinationDirectory, zipFileName);

            if (File.Exists(destFilePath))
                File.Delete(destFilePath);

            ZipFile.CreateFromDirectory(sourceDirectory, destFilePath, CompressionLevel.Fastest, includeBaseDirectory: false);
            destFilePaths.Add(destFilePath);
        }
        
        return destFilePaths;
    }
    
    /// <summary>
    /// Copies the specified files into a subdirectory inside the base directory.
    /// </summary>
    /// <param name="targetDirectoryName">The target directory name where files will be placed.</param>
    /// <param name="filePaths">Array of full file paths to copy.</param>
    /// <param name="replace">Specifies whether to replace an existing item if one is found.</param>
    /// <exception cref="DirectoryNotFoundException">Thrown if a directory does not exist.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the source file is not found.</exception>
    public void CopyFilesToPLMDirectory(string targetDirectoryName, IEnumerable<string> filePaths, bool replace = false)
    {
        var targetDir = FindSubdirectoryByExactName(targetDirectoryName);
        if (!Directory.Exists(targetDir))
            throw new DirectoryNotFoundException($"Target directory '{targetDirectoryName}' not found.");

        try
        {
            var files = Directory.GetFiles(targetDir);
            if (files.Length > 0)
                if (replace)
                {
                    Directory.Delete(targetDir, true);
                    Directory.CreateDirectory(targetDir);
                }
                else return;
        }
        catch (Exception ex)
        {
            throw new Exception($"Cannot replace directory {targetDirectoryName}: {ex.Message}.");
        }

        foreach (var filePath in filePaths)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            string destFilePath = Path.Combine(targetDir, Path.GetFileName(filePath));
            File.Copy(filePath, destFilePath, overwrite: true);
        }
    }

    /// <summary>
    /// Adds a JSON file to a specified subdirectory inside the base directory.
    /// </summary>
    /// <param name="targetDirectoryName">The subdirectory inside the base directory.</param>
    /// <param name="jsonFileName">The name of the JSON file (without .json extension).</param>
    /// <param name="jsonContent">The JSON string content to write.</param>
    public void AddJsonFile(string targetDirectoryName, string jsonFileName, string jsonContent)
    {
        var targetDir = FindSubdirectoryByExactName(targetDirectoryName);
        if (!Directory.Exists(targetDir))
            throw new DirectoryNotFoundException($"Target directory '{targetDirectoryName}' not found.");

        string jsonFilePath = Path.Combine(targetDir, jsonFileName + ".json");

        File.WriteAllText(jsonFilePath, jsonContent);
    }

    /// <summary>
    /// Determines whether the specified directory represents a group or element.
    /// </summary>
    /// <param name="directoryName">The exact name of the directory (case-sensitive).</param>
    /// <returns>
    /// <c>true</c> if the directory is a group (contains only subdirectories, no files);
    /// <c>false</c> if the directory is an element (contains at least one file).
    /// </returns>
    public bool IsElementGroup(string directoryName)
    {
        var dirPath = FindSubdirectoryByExactName(directoryName);
        if (string.IsNullOrEmpty(dirPath))
            return true;
        if (!Directory.Exists(dirPath))
            throw new DirectoryNotFoundException($"Directory '{directoryName}' not found.");
        return !Directory.EnumerateFiles(dirPath).Any();
    }
    
    /// <summary>
    /// Determines whether the specified folder name matches the base path.
    /// </summary>
    /// <param name="directoryName">The name of the directory to check. Case-insensitive comparison is used.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="directoryName"/> matches the base path;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool IsBaseDirectory(string directoryName)
    {
        string lastFolderName = Path.GetFileName(_basePath);
        return string.Equals(directoryName, lastFolderName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Searches all subdirectories for names containing the specified substring (case-insensitive).
    /// </summary>
    /// <param name="nameSubstring">The substring to search for within directory names.</param>
    /// <returns>A list of full paths to matching directories.</returns>
    public string[] FindSubdirectoriesByPartialName(string nameSubstring)
    {
        return Directory.GetDirectories(_basePath, "*", SearchOption.AllDirectories)
                        .Where(dir => Path.GetFileName(dir)
                            .Contains(nameSubstring, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
    }

    /// <summary>
    /// Recursively searches for the first subdirectory with an exact, case-sensitive name match.
    /// </summary>
    /// <param name="directoryName">The exact name of the directory to find (case-sensitive).</param>
    /// <returns>The full path of the directory if found; otherwise, empty string.</returns>
    public string FindSubdirectoryByExactName(string directoryName)
    {
        return FindExactRecursive(_basePath, directoryName) ??  string.Empty;
    }

    private string? FindExactRecursive(string currentPath, string directoryName)
    {
        foreach (var dir in Directory.GetDirectories(currentPath))
        {
            if (string.Equals(Path.GetFileName(dir), directoryName))
                return dir;

            var result = FindExactRecursive(dir, directoryName);
            if (result != null)
                return result;
        }
        return null;
    }

}