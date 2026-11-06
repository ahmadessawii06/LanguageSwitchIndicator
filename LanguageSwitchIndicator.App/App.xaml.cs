using System;
using System.Windows;
using LanguageSwitchIndicator.Models;
using LanguageSwitchIndicator.Services;
using LanguageSwitchIndicator.Views;
using LanguageSwitchIndicator.Typing;

namespace LanguageSwitchIndicator.App;

public partial class App : System.Windows.Application
{
    private SettingsService _settingsService = null!;
    private SettingsWindow? _settingsWindow;
    private InputLanguageMonitor _inputLanguageMonitor = null!;
    private CaretTrackingService _caretTrackingService = null!;
    private PopupManager _popupManager = null!;
    private TrayIconService _trayIconService = null!;
    private TypingCorrectionService _typingCorrection = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsService = new SettingsService();
        var settings = _settingsService.Load();

        _popupManager = new PopupManager(settings);
        _typingCorrection = new TypingCorrectionService(settings, _settingsService);
        _typingCorrection.Feedback += message => _popupManager.ShowFeedback(message);
        _inputLanguageMonitor = new InputLanguageMonitor(settings);
        _caretTrackingService = new CaretTrackingService(_inputLanguageMonitor);
        _caretTrackingService.CaretChanged += (_, args) =>
        {
            if (!settings.Enabled || settings.IsPaused ||
                (args.VariantChanged && !settings.ShowOnVariantChange) ||
                (args.FocusChanged && !args.LanguageChanged && !settings.ShowPopupOnFocusChange) ||
                (!args.LanguageChanged && settings.TriggerMode == PopupTriggerMode.LanguageChangeOnly) ||
                (args.LanguageChanged && settings.TriggerMode == PopupTriggerMode.CaretPlacedOnly))
            {
                return;
            }

            var language = _inputLanguageMonitor.GetCurrentDisplayText();
            if (string.IsNullOrWhiteSpace(language))
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                if (args.Activation)
                {
                    if (settings.DebugLogging) DiagnosticLog.Write($"Indicator activated. LanguageChanged={args.LanguageChanged}; FocusChanged={args.FocusChanged}; Placement={settings.Placement}.");
                    _popupManager.ShowLanguageAt(language, args.Position);
                }
                else
                {
                    _popupManager.MoveLanguageTo(args.Position);
                }
            });
        };

        _trayIconService = new TrayIconService(
            () =>
            {
                if (_settingsWindow == null || !_settingsWindow.IsVisible)
                {
                    _settingsWindow = new SettingsWindow(_settingsService, _popupManager, _inputLanguageMonitor, settings, _typingCorrection);
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
                if (!settings.Enabled) _popupManager.Dismiss();
                _settingsService.Save(settings);
                _settingsWindow?.RefreshSettings();
            },
            () => _popupManager.Preview(),
            () => Shutdown());

        _trayIconService.Show();

        if (!settings.StartMinimized)
        {
            var window = new SettingsWindow(_settingsService, _popupManager, _inputLanguageMonitor, settings, _typingCorrection);
            _settingsWindow = window;
            window.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _typingCorrection?.Dispose();
        _popupManager?.Dismiss();
        _inputLanguageMonitor?.Dispose();
        _caretTrackingService?.Dispose();
        _trayIconService?.Dispose();
        base.OnExit(e);
    }
}
