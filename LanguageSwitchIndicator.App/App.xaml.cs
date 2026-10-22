using System;
using System.Windows;
using LanguageSwitchIndicator.Services;
using LanguageSwitchIndicator.Views;

namespace LanguageSwitchIndicator.App;

public partial class App : System.Windows.Application
{
    private SettingsService _settingsService = null!;
    private SettingsWindow? _settingsWindow;
    private InputLanguageMonitor _inputLanguageMonitor = null!;
    private PopupManager _popupManager = null!;
    private TrayIconService _trayIconService = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsService = new SettingsService();
        var settings = _settingsService.Load();

        _popupManager = new PopupManager(settings);
        _inputLanguageMonitor = new InputLanguageMonitor();
        _inputLanguageMonitor.LanguageChanged += (_, args) =>
        {
            if (!settings.Enabled || settings.IsPaused)
            {
                return;
            }

            Dispatcher.Invoke(() => _popupManager.ShowLanguage(args.DisplayText));
        };

        _trayIconService = new TrayIconService(
            () =>
            {
                if (_settingsWindow == null || !_settingsWindow.IsVisible)
                {
                    _settingsWindow = new SettingsWindow(_settingsService, _popupManager, _inputLanguageMonitor, settings);
                    _settingsWindow.Show();
                    _settingsWindow.Activate();
                }
                else
                {
                    _settingsWindow.Activate();
                }
            },
            () =>
            {
                settings.Enabled = !settings.Enabled;
                _settingsService.Save(settings);
            },
            () => _popupManager.ShowLanguage("EN"),
            () => Shutdown());

        _trayIconService.Show();

        if (!settings.StartMinimized)
        {
            var window = new SettingsWindow(_settingsService, _popupManager, _inputLanguageMonitor, settings);
            _settingsWindow = window;
            window.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _inputLanguageMonitor?.Dispose();
        _trayIconService?.Dispose();
        base.OnExit(e);
    }
}
