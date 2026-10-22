using System;
using System.Collections.Generic;
using System.Linq;

namespace LanguageSwitchIndicator.Models;

public enum PopupPlacement
{
    Center = 0,
    TopCenter,
    BottomCenter,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    NearMouse,
    Custom
}

public enum AnimationStyle
{
    Fade = 0,
    FadeScale,
    SlideUp,
    SlideDown,
    None
}

public enum LanguageDisplayMode
{
    FullName = 0,
    ShortCode,
    NativeName,
    CustomLabel
}

public class LanguageMapping
{
    public string InputLanguageId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public LanguageDisplayMode DisplayMode { get; set; } = LanguageDisplayMode.ShortCode;
}

public class SettingsModel
{
    public bool Enabled { get; set; } = true;
    public bool LaunchAtStartup { get; set; } = false;
    public bool StartMinimized { get; set; } = false;
    public bool ShowOnlyOnLanguageChange { get; set; } = true;
    public bool ShowOnVariantChange { get; set; } = true;
    public bool ShowPopupOnFocusChange { get; set; } = false;
    public bool IsPaused { get; set; } = false;

    public string Theme { get; set; } = "Dark";
    public string BackgroundColor { get; set; } = "#252525";
    public string TextColor { get; set; } = "#FFFFFF";
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 30;
    public double PopupWidth { get; set; } = 130;
    public double PopupHeight { get; set; } = 75;
    public double CornerRadius { get; set; } = 18;
    public double BackgroundOpacity { get; set; } = 0.9;
    public double ShadowIntensity { get; set; } = 0.7;
    public bool EnableBlur { get; set; } = true;
    public PopupPlacement Placement { get; set; } = PopupPlacement.Center;
    public double HorizontalOffset { get; set; } = 0;
    public double VerticalOffset { get; set; } = 0;

    public AnimationStyle AnimationStyle { get; set; } = AnimationStyle.FadeScale;
    public double AnimationSpeed { get; set; } = 1.0;
    public double PopupDurationMs { get; set; } = 700;
    public double FadeInDurationMs { get; set; } = 120;
    public double FadeOutDurationMs { get; set; } = 200;

    public bool DebugLogging { get; set; } = false;
    public List<LanguageMapping> LanguageMappings { get; set; } = new();
    public string LastKnownLanguageId { get; set; } = string.Empty;

    public SettingsModel Clone()
    {
        return new SettingsModel
        {
            Enabled = Enabled,
            LaunchAtStartup = LaunchAtStartup,
            StartMinimized = StartMinimized,
            ShowOnlyOnLanguageChange = ShowOnlyOnLanguageChange,
            ShowOnVariantChange = ShowOnVariantChange,
            ShowPopupOnFocusChange = ShowPopupOnFocusChange,
            IsPaused = IsPaused,
            Theme = Theme,
            BackgroundColor = BackgroundColor,
            TextColor = TextColor,
            FontFamily = FontFamily,
            FontSize = FontSize,
            PopupWidth = PopupWidth,
            PopupHeight = PopupHeight,
            CornerRadius = CornerRadius,
            BackgroundOpacity = BackgroundOpacity,
            ShadowIntensity = ShadowIntensity,
            EnableBlur = EnableBlur,
            Placement = Placement,
            HorizontalOffset = HorizontalOffset,
            VerticalOffset = VerticalOffset,
            AnimationStyle = AnimationStyle,
            AnimationSpeed = AnimationSpeed,
            PopupDurationMs = PopupDurationMs,
            FadeInDurationMs = FadeInDurationMs,
            FadeOutDurationMs = FadeOutDurationMs,
            DebugLogging = DebugLogging,
            LastKnownLanguageId = LastKnownLanguageId,
            LanguageMappings = LanguageMappings.Select(x => new LanguageMapping
            {
                InputLanguageId = x.InputLanguageId,
                Label = x.Label,
                DisplayMode = x.DisplayMode
            }).ToList()
        };
    }
}
