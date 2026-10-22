using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LanguageSwitchIndicator.Services;

public class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _lightTrayIcon;
    private readonly Icon _darkTrayIcon;
    private readonly Action _openSettings;
    private readonly Action _toggleEnabled;
    private readonly Action _previewPopup;
    private readonly Action _exit;

    public TrayIconService(Action openSettings, Action toggleEnabled, Action previewPopup, Action exit)
    {
        _openSettings = openSettings;
        _toggleEnabled = toggleEnabled;
        _previewPopup = previewPopup;
        _exit = exit;

        _lightTrayIcon = LoadIcon("tray_white.ico");
        _darkTrayIcon = LoadIcon("tray_dark.ico");
        _notifyIcon = new NotifyIcon
        {
            Text = "Language Switch Indicator",
            Visible = true,
            Icon = GetTrayIcon(),
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open Settings", null, (_, _) => _openSettings());
        contextMenu.Items.Add("Enable/Disable Indicator", null, (_, _) => _toggleEnabled());
        contextMenu.Items.Add("Preview Popup", null, (_, _) => _previewPopup());
        contextMenu.Items.Add("Exit", null, (_, _) => _exit());

        _notifyIcon.ContextMenuStrip = contextMenu;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    private static Icon LoadIcon(string name)
    {
        using var stream = typeof(TrayIconService).Assembly.GetManifestResourceStream(
            $"LanguageSwitchIndicator.Assets.Icons.{name}")
            ?? throw new InvalidOperationException($"Missing tray icon: {name}");
        using var icon = new Icon(stream, SystemInformation.SmallIconSize);
        return (Icon)icon.Clone();
    }

    private Icon GetTrayIcon()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("SystemUsesLightTheme") is int value && value == 1
            ? _darkTrayIcon
            : _lightTrayIcon;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        _notifyIcon.Icon = GetTrayIcon();
    }

    public void Show()
    {
        _notifyIcon.Visible = true;
    }

    public void Dispose()
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _lightTrayIcon.Dispose();
        _darkTrayIcon.Dispose();
    }
}
