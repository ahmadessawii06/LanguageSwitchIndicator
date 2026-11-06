using System;
using System.Windows;
using System.Windows.Threading;
using LanguageSwitchIndicator.Models;
using LanguageSwitchIndicator.Views;
using WpfPoint = System.Windows.Point;

namespace LanguageSwitchIndicator.Services;

public class PopupManager
{
    private readonly SettingsModel _settings;
    private LanguagePopupWindow? _currentPopup;
    private LanguagePopupWindow? _previewPopup;
    private DateTime _feedbackUntil;

    public void ShowFeedback(string message)
    {
        _feedbackUntil = DateTime.UtcNow.AddMilliseconds(Math.Max(1800, _settings.PopupDurationMs));
        _previewPopup?.Close();
        var feedbackSettings = _settings.Clone();
        feedbackSettings.PopupDurationMs = Math.Max(1800, _settings.PopupDurationMs);
        _previewPopup = new LanguagePopupWindow(feedbackSettings);
        _previewPopup.ShowLanguage(message);
    }

    public PopupManager(SettingsModel settings)
    {
        _settings = settings;
    }

    public void ShowLanguage(string languageText)
    {
        if (DateTime.UtcNow < _feedbackUntil) return;
        if (string.IsNullOrWhiteSpace(languageText))
        {
            return;
        }

        _previewPopup?.Dismiss();
        var popup = _currentPopup ??= new LanguagePopupWindow(_settings);
        popup.ShowLanguage(languageText);
        _currentPopup = popup;
    }

    public void ShowLanguageAt(string languageText, WpfPoint position)
    {
        if (DateTime.UtcNow < _feedbackUntil) return;
        if (string.IsNullOrWhiteSpace(languageText))
        {
            return;
        }

        _previewPopup?.Dismiss();
        var popup = _currentPopup ??= new LanguagePopupWindow(_settings);
        popup.SetPosition(position);
        popup.ShowLanguage(languageText);
        _currentPopup = popup;
    }

    public void MoveLanguageTo(WpfPoint position)
    {
        _currentPopup?.SetPosition(position);
    }

    public void Preview()
    {
        Preview(_settings.Clone());
    }

    public void Preview(SettingsModel previewSettings)
    {
        _currentPopup?.Dismiss();
        _previewPopup?.Close();
        _previewPopup = new LanguagePopupWindow(previewSettings);
        _previewPopup.ShowLanguage("English");
    }

    public void Dismiss()
    {
        _currentPopup?.Dismiss();
        _previewPopup?.Dismiss();
    }
}
