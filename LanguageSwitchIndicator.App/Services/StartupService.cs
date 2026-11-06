using Microsoft.Win32;

namespace LanguageSwitchIndicator.Services;

public static class StartupService
{
    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (enabled)
        {
            var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate application executable.");
            key.SetValue("LanguageSwitchIndicator", $"\"{executable}\"");
        }
        else key.DeleteValue("LanguageSwitchIndicator", false);
    }
}
