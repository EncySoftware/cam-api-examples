using System.IO;
using System.Text.Json;
using PartCalibrationWorkflowNet.Model;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Loads / saves <see cref="WizardSettings"/> as a JSON file next to the extension DLL.
/// </summary>
internal sealed class SettingsRepository
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { WriteIndented = true };

    private readonly string _filePath;

    public SettingsRepository(string pluginDir)
    {
        _filePath = Path.Combine(pluginDir, "wizard.user.json");
    }

    public WizardSettings Load()
    {
        if (!File.Exists(_filePath))
            return new WizardSettings();
        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<WizardSettings>(json) ?? new WizardSettings();
        }
        catch
        {
            // Corrupted user settings — fall back to defaults silently.
            return new WizardSettings();
        }
    }

    public void Save(WizardSettings settings)
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, JsonOpts));
    }
}
