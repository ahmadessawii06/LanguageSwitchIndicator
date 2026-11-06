using System.Windows.Media;
using LanguageSwitchIndicator.Models;
using Microsoft.Win32;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace LanguageSwitchIndicator.Views;

internal static class IndicatorPalette
{
    public static bool IsLight(SettingsModel settings)
    {
        if (settings.Theme != "System") return settings.Theme == "Light";
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value && value == 1;
    }

    public static Brush Background(SettingsModel settings)
    {
        var light = IsLight(settings);
        var alpha = (byte)(255 * Math.Clamp(settings.BackgroundOpacity, 0, 1));
        return new LinearGradientBrush(
            light ? Color.FromArgb(alpha, 252, 252, 254) : Color.FromArgb(alpha, 63, 64, 68),
            light ? Color.FromArgb(alpha, 230, 232, 237) : Color.FromArgb(alpha, 35, 36, 40), 90);
    }

    public static Brush Foreground(SettingsModel settings) => new SolidColorBrush(
        IsLight(settings) ? Color.FromRgb(38, 40, 46) : Color.FromRgb(235, 235, 240));
}
