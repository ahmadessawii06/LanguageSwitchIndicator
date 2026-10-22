using System;
using System.IO;
using System.Text.Json;
using LanguageSwitchIndicator.Models;

namespace LanguageSwitchIndicator.Services;

public class SettingsService
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public SettingsService(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LanguageSwitchIndicator",
            "settings.json");

        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath) ?? Environment.CurrentDirectory);
    }

    public SettingsModel Load()
    {
        if (!File.Exists(_settingsPath))
        {
            var defaultSettings = new SettingsModel();
            Save(defaultSettings);
            return defaultSettings;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<SettingsModel>(json, _jsonOptions);
            return settings ?? new SettingsModel();
        }
        catch
        {
            var fallback = new SettingsModel();
            Save(fallback);
            return fallback;
        }
    }

    public void Save(SettingsModel settings)
    {
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        File.WriteAllText(_settingsPath, json);
    }
}
