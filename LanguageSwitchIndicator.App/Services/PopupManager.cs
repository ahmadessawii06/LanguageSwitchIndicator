using System;
using System.Windows;
using System.Windows.Threading;
using LanguageSwitchIndicator.Models;
using LanguageSwitchIndicator.Views;

namespace LanguageSwitchIndicator.Services;

public class PopupManager
{
    private readonly SettingsModel _settings;
    private LanguagePopupWindow? _currentPopup;

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

    public void Preview()
    {
        ShowLanguage("EN");
    }
}
