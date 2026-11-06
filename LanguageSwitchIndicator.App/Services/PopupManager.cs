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

    public PopupManager(SettingsModel settings)
    {
        _settings = settings;
    }

    public void ShowLanguage(string languageText)
    {
        if (string.IsNullOrWhiteSpace(languageText))
        {
            return;
        }

        var popup = _currentPopup ??= new LanguagePopupWindow(_settings);
        popup.ShowLanguage(languageText);
        _currentPopup = popup;
    }

    public void ShowLanguageAt(string languageText, WpfPoint position)
    {
        if (string.IsNullOrWhiteSpace(languageText))
        {
            return;
        }

        var popup = _currentPopup ??= new LanguagePopupWindow(_settings);
        if (_settings.Placement == PopupPlacement.NearMouse)
        {
            popup.SetPosition(position);
        }
        else
        {
            popup.ClearPosition();
        }
        popup.ShowLanguage(languageText);
        _currentPopup = popup;
    }

    public void MoveLanguageTo(WpfPoint position)
    {
        if (_settings.Placement == PopupPlacement.NearMouse)
        {
            _currentPopup?.SetPosition(position);
        }
    }

    public void Preview()
    {
        ShowLanguage("EN");
    }

    public void Preview(SettingsModel previewSettings)
    {
        _previewPopup?.Close();
        _previewPopup = new LanguagePopupWindow(previewSettings);
        _previewPopup.ShowLanguage("EN");
    }
}
