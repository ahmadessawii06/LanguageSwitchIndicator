using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Wpf.Ui.Appearance;

namespace LanguageSwitchIndicator.Services;

public static class AppThemeService
{
    public static bool IsWindowsDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }

    public static void ApplyTheme(string themePreference, Window? window = null)
    {
        bool isDark = themePreference switch
        {
            "Dark" => true,
            "Light" => false,
            _ => IsWindowsDarkTheme()
        };

        try
        {
            ApplicationThemeManager.Apply(isDark ? ApplicationTheme.Dark : ApplicationTheme.Light);
        }
        catch
        {
            // Fallback for headless, test, or designer environments
        }

        var appDict = System.Windows.Application.Current?.Resources;
        if (appDict != null) ApplyTokens(appDict, isDark);

        if (window != null)
        {
            ApplyTokens(window.Resources, isDark);
        }
    }

    private static void ApplyTokens(ResourceDictionary dict, bool isDark)
    {
        SetBrush(dict, "Canvas", isDark ? "#0B0F19" : "#F8FAFC");
        SetBrush(dict, "SurfaceBrush", isDark ? "#161F30" : "#FFFFFF");
        SetBrush(dict, "SurfaceSecondary", isDark ? "#0F172A" : "#F1F5F9");
        SetBrush(dict, "SurfaceHover", isDark ? "#1E293B" : "#F8FAFC");
        SetBrush(dict, "SidebarBackground", isDark ? "#0F172A" : "#FFFFFF");
        SetBrush(dict, "InputBackground", isDark ? "#0F172A" : "#FFFFFF");
        SetBrush(dict, "Line", isDark ? "#283548" : "#E2E8F0");
        SetBrush(dict, "LineSubtle", isDark ? "#1E293B" : "#F1F5F9");
        SetBrush(dict, "Ink", isDark ? "#F8FAFC" : "#0F172A");
        SetBrush(dict, "InkSecondary", isDark ? "#CBD5E1" : "#334155");
        SetBrush(dict, "Muted", isDark ? "#94A3B8" : "#64748B");
        SetBrush(dict, "MutedLight", isDark ? "#64748B" : "#94A3B8");

        SetBrush(dict, "Accent", "#0D9488");
        SetBrush(dict, "AccentHover", isDark ? "#14B8A6" : "#0F766E");
        SetBrush(dict, "AccentPressed", isDark ? "#2DD4BF" : "#115E59");
        SetBrush(dict, "AccentSoft", isDark ? "#132E35" : "#F0FDFA");
        SetBrush(dict, "AccentSoftHover", isDark ? "#164E4D" : "#CCFBF1");
        SetBrush(dict, "AccentBorder", isDark ? "#0D9488" : "#5EEAD4");

        SetBrush(dict, "HeroBackground", isDark ? "#0F172A" : "#0F172A");
        SetBrush(dict, "HeroBorder", isDark ? "#283548" : "#1E293B");

        SetBrush(dict, "MonitorButtonBg", isDark ? "#0F172A" : "#1E293B");
        SetBrush(dict, "MonitorButtonBorder", isDark ? "#283548" : "#334155");
    }

    private static void SetBrush(ResourceDictionary dict, string key, string hexColor)
    {
        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        dict[key] = brush;
    }
}
