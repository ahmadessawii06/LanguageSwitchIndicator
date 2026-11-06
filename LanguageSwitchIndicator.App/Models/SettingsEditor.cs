using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using LanguageSwitchIndicator.Services;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace LanguageSwitchIndicator.Models;

public sealed class SettingsEditor : INotifyPropertyChanged
{
    public SettingsModel Settings { get; }
    private readonly SettingsService _service;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;
    public string SaveStatus { get; private set; } = "All changes saved";
    public string Status => !Settings.Enabled ? "Indicator is off" : Settings.IsPaused ? "Indicator is paused" : "Ready when you type";
    public SettingsEditor(SettingsModel settings, SettingsService service) { Settings = settings; _service = service; }
    private void Set<T>(T value, [CallerMemberName] string name = "")
    {
        var property = typeof(SettingsModel).GetProperty(name)!;
        if (Equals(property.GetValue(Settings), value)) return;
        if (name == nameof(LaunchAtStartup)) StartupService.SetEnabled((bool)(object)value!);
        property.SetValue(Settings, value);
        Persist();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
        Changed?.Invoke();
    }
    public void Persist()
    {
        try { _service.Save(Settings); SaveStatus = "All changes saved"; }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException) { SaveStatus = "Could not save. Check folder permissions."; }
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SaveStatus)));
    }
    public void NotifyAll() { Persist(); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("")); Changed?.Invoke(); }
    private static double Number(double value, double min, double max)
    {
        if (!double.IsFinite(value) || value < min || value > max) throw new ArgumentException($"Use a value from {min} to {max}.");
        return value;
    }
    private static string ColorValue(string value)
    {
        try { _ = (System.Windows.Media.Color)ColorConverter.ConvertFromString(value); return value; }
        catch { throw new ArgumentException("Use a color such as #087F75."); }
    }
    public bool Enabled { get => Settings.Enabled; set => Set(value); }
    public bool IsPaused { get => Settings.IsPaused; set => Set(value); }
    public bool LaunchAtStartup { get => Settings.LaunchAtStartup; set => Set(value); }
    public bool StartMinimized { get => Settings.StartMinimized; set => Set(value); }
    public bool ShowOnVariantChange { get => Settings.ShowOnVariantChange; set => Set(value); }
    public bool ShowPopupOnFocusChange { get => Settings.ShowPopupOnFocusChange; set => Set(value); }
    public bool ShowPopupOnTyping { get => Settings.ShowPopupOnTyping; set => Set(value); }
    public bool ShowOnlyOnLanguageChange { get => Settings.ShowOnlyOnLanguageChange; set => Set(value); }
    public PopupTriggerMode TriggerMode { get => Settings.TriggerMode; set { Settings.ShowOnlyOnLanguageChange = value == PopupTriggerMode.LanguageChangeOnly; Settings.ShowPopupOnTyping = value != PopupTriggerMode.LanguageChangeOnly; Set(value); } }
    public int IndicatorSize { get => Settings.IndicatorSize; set => Set(value); }
    public string Theme { get => Settings.Theme; set => Set(value); }
    public string AppTheme { get => Settings.AppTheme; set => Set(value); }
    public string BackgroundColor { get => Settings.BackgroundColor; set => Set(ColorValue(value)); }
    public string TextColor { get => Settings.TextColor; set => Set(ColorValue(value)); }
    public string FontFamily { get => Settings.FontFamily; set { try { _ = new System.Windows.Media.FontFamily(value); } catch { throw new ArgumentException("Enter a font family name."); } Set(value); } }
    public double FontSize { get => Settings.FontSize; set => Set(Number(value, 8, 200)); }
    public double PopupWidth { get => Settings.PopupWidth; set => Set(Number(value, 60, 1000)); }
    public double PopupHeight { get => Settings.PopupHeight; set => Set(Number(value, 40, 1000)); }
    public double CornerRadius { get => Settings.CornerRadius; set => Set(Number(value, 0, 80)); }
    public double BackgroundOpacity { get => Settings.BackgroundOpacity; set => Set(Number(value, 0.1, 1)); }
    public double ShadowIntensity { get => Settings.ShadowIntensity; set => Set(Number(value, 0, 1)); }
    public bool EnableBlur { get => Settings.EnableBlur; set => Set(value); }
    public PopupPlacement Placement { get => Settings.Placement; set => Set(value); }
    public double HorizontalOffset { get => Settings.HorizontalOffset; set => Set(Number(value, -4000, 4000)); }
    public double VerticalOffset { get => Settings.VerticalOffset; set => Set(Number(value, -4000, 4000)); }
    public AnimationStyle AnimationStyle { get => Settings.AnimationStyle; set => Set(value); }
    public double AnimationSpeed { get => Settings.AnimationSpeed; set => Set(Number(value, 0.25, 4)); }
    public double PopupDurationMs { get => Settings.PopupDurationMs; set => Set(Number(value, 100, 10000)); }
    public double FadeInDurationMs { get => Settings.FadeInDurationMs; set => Set(Number(value, 0, 5000)); }
    public double FadeOutDurationMs { get => Settings.FadeOutDurationMs; set => Set(Number(value, 0, 5000)); }
    public bool DebugLogging { get => Settings.DebugLogging; set => Set(value); }
    public bool ReduceMotion { get => Settings.ReduceMotion; set => Set(value); }
    public bool UseCustomColors { get => Settings.UseCustomColors; set => Set(value); }
    public bool TypingCorrectionEnabled { get => Settings.TypingCorrectionEnabled; set => Set(value); }
    public string TypingCorrectionHotkey { get => Settings.TypingCorrectionHotkey; set => Set(value); }
    public Typing.CorrectionDirection TypingCorrectionDirection { get => Settings.TypingCorrectionDirection; set => Set(value); }
    public bool SwitchLayoutAfterCorrection { get => Settings.SwitchLayoutAfterCorrection; set => Set(value); }
}
