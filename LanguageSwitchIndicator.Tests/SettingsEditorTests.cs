using System.IO;
using LanguageSwitchIndicator.Models;
using LanguageSwitchIndicator.Services;

namespace LanguageSwitchIndicator.Tests;

public class SettingsEditorTests
{
    [Fact]
    public void OldSettings_LoadEditAndReopen_PreserveExistingPreferences()
    {
        var path = Path.Combine(Path.GetTempPath(), $"indicator-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, """{"Theme":"Light","PopupWidth":180,"IndicatorSize":1,"TriggerMode":0,"StartMinimized":true,"LanguageMappings":[{"InputLanguageId":"ar-SA","Label":"العربية"}]}""");
            var service = new SettingsService(path);
            var settings = service.Load();
            Assert.False(settings.ReduceMotion);
            Assert.False(settings.UseCustomColors);
            var editor = new SettingsEditor(settings, service) { FontSize = 36, IndicatorSize = 4, UseCustomColors = true, BackgroundColor = "#123456" };
            var reopened = service.Load();
            Assert.Equal("Light", reopened.Theme);
            Assert.Equal(180, reopened.PopupWidth);
            Assert.True(reopened.StartMinimized);
            Assert.Equal(PopupTriggerMode.LanguageChangeOnly, reopened.TriggerMode);
            Assert.Equal("العربية", reopened.LanguageMappings[0].Label);
            Assert.Equal(36, reopened.FontSize);
            Assert.Equal(4, reopened.IndicatorSize);
            Assert.True(reopened.UseCustomColors);
            Assert.Equal("#123456", reopened.BackgroundColor);
            Assert.Equal(reopened.IndicatorSize, reopened.Clone().IndicatorSize);
            Assert.True(reopened.Clone().UseCustomColors);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void InvalidInput_DoesNotReplaceLastValidSavedValue()
    {
        var path = Path.Combine(Path.GetTempPath(), $"indicator-{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(path);
            var editor = new SettingsEditor(service.Load(), service) { FontSize = 32, BackgroundColor = "#123456" };
            Assert.Throws<ArgumentException>(() => editor.FontSize = double.NaN);
            Assert.Throws<ArgumentException>(() => editor.BackgroundColor = "not-a-color");
            var restored = service.Load();
            Assert.Equal(32, restored.FontSize);
            Assert.Equal("#123456", restored.BackgroundColor);
        }
        finally { File.Delete(path); }
    }
}
