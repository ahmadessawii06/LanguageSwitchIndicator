using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LanguageSwitchIndicator.Models;
using LanguageSwitchIndicator.Native;
using LanguageSwitchIndicator.Services;

namespace LanguageSwitchIndicator.Typing;

public sealed class TypingCorrectionService : IDisposable
{
    private readonly SettingsModel _settings;
    private readonly SettingsService _store;
    private readonly GlobalCorrectionHotkey _hotkey = new();
    private CancellationTokenSource _cancellation = new();
    private bool _busy;
    private bool _disposed;

    public string Status { get; private set; } = "";
    public string LastError { get; private set; } = "";
    public bool IsSuspended { get; set; }

    public event Action? StatusChanged;
    public event Action<string>? Feedback;

    public TypingCorrectionService(SettingsModel settings, SettingsService store)
    {
        _settings = settings;
        _store = store;
        _hotkey.Pressed += OnPressed;
        _hotkey.TryApply(settings.TypingCorrectionEnabled, settings.TypingCorrectionHotkey, out var status);
        Status = status;
    }

    public bool Apply(bool enabled, string shortcut)
    {
        var success = _hotkey.TryApply(enabled, shortcut.Trim(), out var status);
        if (success)
        {
            _settings.TypingCorrectionEnabled = enabled;
            _settings.TypingCorrectionHotkey = shortcut.Trim();
            if (!enabled)
            {
                _cancellation.Cancel();
                _cancellation.Dispose();
                _cancellation = new();
            }
            try
            {
                _store.Save(_settings);
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
            {
                status += " Changes could not be saved.";
            }
        }
        Status = status;
        StatusChanged?.Invoke();
        return success;
    }

    private async void OnPressed()
    {
        if (_disposed || _busy || !_settings.TypingCorrectionEnabled || IsSuspended) return;
        _busy = true;

        DiagnosticLog.Write($"[Correction] Stage 1: Hotkey received. Shortcut='{_settings.TypingCorrectionHotkey}'");

        var foreground = User32.GetForegroundWindow();
        var thread = User32.GetWindowThreadProcessId(foreground, out var processId);
        var className = new StringBuilder(256);
        User32.GetClassName(foreground, className, className.Capacity);
        DiagnosticLog.Write($"[Correction] Stage 2: Foreground window detected. HWND={foreground}, PID={processId}, Class='{className}'");

        var token = _cancellation.Token;
        var direction = _settings.TypingCorrectionDirection;
        var switchLayout = _settings.SwitchLayoutAfterCorrection;

        try
        {
            var result = await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                var target = WindowsCorrectionTarget.Create(foreground);
                target.Cancellation = token;

                DiagnosticLog.Write($"[Correction] Stage 3: Focused control identified. TargetType={target.GetType().Name}");

                var layout = (uint)(User32.GetKeyboardLayout(thread).ToInt64() & 0xffffffff);
                var primaryLang = (layout & 0xFFFF) & 0x3FF;
                var id = primaryLang == 0x01 ? "00000401" : primaryLang == 0x09 ? "00000409" : null;
                return CorrectionExecutor.Execute(target, direction, id);
            }, token);

            if (_disposed || token.IsCancellationRequested) return;

            Feedback?.Invoke(result.Message);
            if (!result.Success)
            {
                LastError = result.Message;
                Status = $"Correction failed: {result.Message}";
                StatusChanged?.Invoke();
            }

            if (result.Success && switchLayout && User32.GetForegroundWindow() == foreground)
            {
                var desiredKlid = result.Direction == CorrectionDirection.EnglishToArabic ? "00000401" : "00000409";
                var desired = CorrectionNative.LoadKeyboardLayout(desiredKlid, 1);
                if (desired == IntPtr.Zero || !CorrectionNative.PostMessage(foreground, 0x50, IntPtr.Zero, desired))
                {
                    DiagnosticLog.Write("[Correction] Keyboard layout switch request failed or not supported by foreground window.");
                    Feedback?.Invoke("Text fixed. Keyboard layout could not be switched.");
                }
                else
                {
                    DiagnosticLog.Write($"[Correction] Keyboard layout switched to {desiredKlid}.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            DiagnosticLog.Write("[Correction] Operation cancelled.");
        }
        catch (UnsafeCorrectionException ex)
        {
            DiagnosticLog.Write($"[Correction] Unsafe correction aborted: {ex.Message}");
            LastError = ex.Message;
            Status = $"Correction failed: {ex.Message}";
            StatusChanged?.Invoke();
            if (!_disposed) Feedback?.Invoke(ex.Message);
        }
        catch (Exception ex) when (ex is System.Windows.Automation.ElementNotAvailableException or System.Windows.Automation.ElementNotEnabledException or InvalidOperationException or UnauthorizedAccessException or COMException or System.ComponentModel.Win32Exception)
        {
            DiagnosticLog.Write($"[Correction] Target access exception: {ex.GetType().Name}: {ex.Message}");
            LastError = "Field could not be corrected safely.";
            Status = "Correction failed: field could not be corrected safely.";
            StatusChanged?.Invoke();
            if (!_disposed) Feedback?.Invoke("This field could not be corrected safely. Nothing else was attempted.");
        }
        finally
        {
            _busy = false;
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _cancellation.Cancel();
        _hotkey.Dispose();
        _cancellation.Dispose();
    }
}
