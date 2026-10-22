using System;
using System.Windows.Forms;

namespace LanguageSwitchIndicator.Services;

public class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
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

        _notifyIcon = new NotifyIcon
        {
            Text = "Language Switch Indicator",
            Visible = true,
            Icon = SystemIcons.Information,
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open Settings", null, (_, _) => _openSettings());
        contextMenu.Items.Add("Enable/Disable Indicator", null, (_, _) => _toggleEnabled());
        contextMenu.Items.Add("Preview Popup", null, (_, _) => _previewPopup());
        contextMenu.Items.Add("Exit", null, (_, _) => _exit());

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    public void Show()
    {
        _notifyIcon.Visible = true;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
