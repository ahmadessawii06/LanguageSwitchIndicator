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

public enum PopupTriggerMode
{
    LanguageChangeOnly = 0,
    CaretPlacedOnly,
    Both
}

public class LanguageMapping
{
    public string InputLanguageId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public LanguageDisplayMode DisplayMode { get; set; } = LanguageDisplayMode.ShortCode;
}

public class SettingsModel
{
    public bool TypingCorrectionEnabled { get; set; } = true;
    public string TypingCorrectionHotkey { get; set; } = "F8";
    public Typing.CorrectionDirection TypingCorrectionDirection { get; set; } = Typing.CorrectionDirection.Auto;
    public bool SwitchLayoutAfterCorrection { get; set; }
    public bool ReduceMotion { get; set; }
    public bool UseCustomColors { get; set; }
    public int IndicatorSize { get; set; } = 2;

    [System.Text.Json.Serialization.JsonIgnore]
    public double IndicatorScale => IndicatorSize switch
    {
        0 => 0.6,
        1 => 0.8,
        3 => 1.2,
        4 => 1.4,
        _ => 1.0
    };

    public bool Enabled { get; set; } = true;
    public bool LaunchAtStartup { get; set; } = false;
    public bool StartMinimized { get; set; } = false;
    public bool ShowOnlyOnLanguageChange { get; set; } = true;
    public bool ShowOnVariantChange { get; set; } = true;
    public bool ShowPopupOnTyping { get; set; } = true;
    public bool ShowPopupOnFocusChange { get; set; } = true;
    public PopupTriggerMode TriggerMode { get; set; } = PopupTriggerMode.Both;
    public bool IsPaused { get; set; } = false;

    public string Theme { get; set; } = "Dark";
    public string AppTheme { get; set; } = "System";
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
    public double PopupDurationMs { get; set; } = 1000;
    public double FadeInDurationMs { get; set; } = 120;
    public double FadeOutDurationMs { get; set; } = 200;

    public bool DebugLogging { get; set; } = false;
    public List<LanguageMapping> LanguageMappings { get; set; } = new();
    public string LastKnownLanguageId { get; set; } = string.Empty;

    public SettingsModel Clone()
    {
        return new SettingsModel
        {
            TypingCorrectionEnabled = TypingCorrectionEnabled,
            TypingCorrectionHotkey = TypingCorrectionHotkey,
            TypingCorrectionDirection = TypingCorrectionDirection,
            SwitchLayoutAfterCorrection = SwitchLayoutAfterCorrection,
            ReduceMotion = ReduceMotion,
            UseCustomColors = UseCustomColors,
            Enabled = Enabled,
            LaunchAtStartup = LaunchAtStartup,
            StartMinimized = StartMinimized,
            ShowOnlyOnLanguageChange = ShowOnlyOnLanguageChange,
            ShowOnVariantChange = ShowOnVariantChange,
            ShowPopupOnTyping = ShowPopupOnTyping,
            ShowPopupOnFocusChange = ShowPopupOnFocusChange,
            TriggerMode = TriggerMode,
            IsPaused = IsPaused,
            Theme = Theme,
            AppTheme = AppTheme,
            BackgroundColor = BackgroundColor,
            TextColor = TextColor,
            FontFamily = FontFamily,
            FontSize = FontSize,
            IndicatorSize = IndicatorSize,
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
