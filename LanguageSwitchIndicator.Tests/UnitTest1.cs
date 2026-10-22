using System;
using System.IO;
using LanguageSwitchIndicator.Services;

namespace LanguageSwitchIndicator.Tests;

public class InputLanguageMonitorTests
{
    [Theory]
    [InlineData(0x0409, "en-US")]
    [InlineData(0x0401, "ar-SA")]
    [InlineData(0x040C, "fr-FR")]
    [InlineData(0x0407, "de-DE")]
    [InlineData(0x1234, "en-US")]
    public void ResolveLanguageByLayoutId_ReturnsExpectedLanguage(int layoutId, string expected)
    {
        var actual = InputLanguageMonitor.ResolveLanguageByLayoutId(layoutId);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SettingsService_CreatesDefaultFileAndLoadsSettings()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"settings-{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(tempPath);
            var settings = service.Load();

            Assert.True(settings.Enabled);
            Assert.NotNull(settings.LanguageMappings);
            Assert.True(File.Exists(tempPath));
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}