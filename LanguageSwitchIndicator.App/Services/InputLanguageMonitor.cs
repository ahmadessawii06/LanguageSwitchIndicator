using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LanguageSwitchIndicator.Models;
using LanguageSwitchIndicator.Native;

namespace LanguageSwitchIndicator.Services;

public class InputLanguageChangedEventArgs : EventArgs
{
    public string LanguageId { get; }
    public string Label { get; }
    public string DisplayText { get; }

    public InputLanguageChangedEventArgs(string languageId, string label, string displayText)
    {
        LanguageId = languageId;
        Label = label;
        DisplayText = displayText;
    }
}

public class InputLanguageMonitor : IDisposable
{
    private readonly SettingsModel? _settings;
    public string GetCurrentLanguageIdentifier() => GetCurrentLanguageId();
    private readonly object _lock = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _monitorTask;
    private string _lastLanguageId = string.Empty;
    private IntPtr _lastFocusedWindow;
    private IntPtr _lastCaretWindow;
    private User32.Rect _lastCaretRect;
    private bool _hasLastCaret;
    private readonly User32.HookProc _keyboardHookCallback;
    private readonly User32.HookProc _mouseHookCallback;
    private IntPtr _keyboardHookHandle;
    private IntPtr _mouseHookHandle;
    private DateTime _lastTypingNotificationUtc = DateTime.MinValue;
    private DateTime _lastCaretNotificationUtc = DateTime.MinValue;
    private bool _disposed;

    public event EventHandler<InputLanguageChangedEventArgs>? LanguageChanged;
    public event EventHandler<InputLanguageChangedEventArgs>? FocusChanged;
    public event EventHandler<InputLanguageChangedEventArgs>? Typing;

    public string GetCurrentDisplayText()
    {
        var languageId = GetCurrentLanguageId();
        if (string.IsNullOrWhiteSpace(languageId))
        {
            return string.Empty;
        }

        var language = Culturize(languageId);
        if (_settings != null)
        {
            lock (_settings.LanguageMappings)
            {
                var mapping = _settings.LanguageMappings.FirstOrDefault(x => x.InputLanguageId == languageId);
                if (mapping != null && !string.IsNullOrWhiteSpace(mapping.Label)) return mapping.Label;
            }
        }
        return GetLabel(languageId, language);
    }

    public InputLanguageMonitor(SettingsModel? settings = null)
    {
        _settings = settings;
        _keyboardHookCallback = KeyboardHookCallback;
        _mouseHookCallback = MouseHookCallback;
        _keyboardHookHandle = User32.SetWindowsHookEx(
            13,
            _keyboardHookCallback,
            User32.GetModuleHandle(null),
            0);
        _mouseHookHandle = User32.SetWindowsHookEx(
            14,
            _mouseHookCallback,
            User32.GetModuleHandle(null),
            0);
        _monitorTask = Task.Run(() => MonitorLoopAsync(_cts.Token));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Cancel();
        _monitorTask.Wait(250);
        if (_keyboardHookHandle != IntPtr.Zero)
        {
            User32.UnhookWindowsHookEx(_keyboardHookHandle);
            _keyboardHookHandle = IntPtr.Zero;
        }
        if (_mouseHookHandle != IntPtr.Zero)
        {
            User32.UnhookWindowsHookEx(_mouseHookHandle);
            _mouseHookHandle = IntPtr.Zero;
        }
        _cts.Dispose();
    }

    private IntPtr KeyboardHookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            const int keyDown = 0x0100;
            const int systemKeyDown = 0x0104;
            if (code >= 0 && (wParam.ToInt64() == keyDown || wParam.ToInt64() == systemKeyDown))
            {
                var key = Marshal.PtrToStructure<User32.KeyboardHookData>(lParam).VirtualKeyCode;
                var now = DateTime.UtcNow;
                if (IsTextKey(key) && now - _lastTypingNotificationUtc >= TimeSpan.FromMilliseconds(500))
                {
                    _lastTypingNotificationUtc = now;
                    var languageId = GetCurrentLanguageId();
                    if (!string.IsNullOrWhiteSpace(languageId))
                    {
                        var language = Culturize(languageId);
                        Typing?.Invoke(this, new InputLanguageChangedEventArgs(
                            languageId,
                            language.Name,
                            GetLabel(languageId, language)));
                    }
                }
            }
        }
        catch
        {
            // Keyboard hooks must never prevent the keystroke from reaching its target.
        }

        return User32.CallNextHookEx(_keyboardHookHandle, code, wParam, lParam);
    }

    private IntPtr MouseHookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            const int leftButtonDown = 0x0201;
            if (code >= 0 && wParam.ToInt64() == leftButtonDown && GetFocusSnapshot().HasCaret)
            {
                var now = DateTime.UtcNow;
                if (now - _lastCaretNotificationUtc >= TimeSpan.FromMilliseconds(250))
                {
                    _lastCaretNotificationUtc = now;
                    NotifyCaretPlaced();
                }
            }
        }
        catch
        {
            // Mouse hooks must never prevent the click from reaching its target.
        }

        return User32.CallNextHookEx(_mouseHookHandle, code, wParam, lParam);
    }

    private void NotifyCaretPlaced()
    {
        var languageId = GetCurrentLanguageId();
        if (string.IsNullOrWhiteSpace(languageId))
        {
            return;
        }

        var language = Culturize(languageId);
        FocusChanged?.Invoke(this, new InputLanguageChangedEventArgs(
            languageId,
            language.Name,
            GetLabel(languageId, language)));
    }

    private static bool IsTextKey(uint key)
    {
        return key is 0x08 or 0x09 or 0x0D or 0x20 ||
               key is >= 0x30 and <= 0x39 ||
               key is >= 0x41 and <= 0x5A ||
               key is >= 0x60 and <= 0x69 ||
               key is >= 0xBA and <= 0xC0 ||
               key is >= 0xDB and <= 0xDF;
    }

    private async Task MonitorLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var current = GetCurrentLanguageId();
                if (!string.IsNullOrWhiteSpace(current))
                {
                    var language = Culturize(current);
                    var args = new InputLanguageChangedEventArgs(
                        current,
                        language.Name,
                        GetLabel(current, language));
                    var languageChanged = false;
                    var focusChanged = false;
                    var focusSnapshot = GetFocusSnapshot();
                    var focusedWindow = focusSnapshot.FocusedWindow;

                    lock (_lock)
                    {
                        languageChanged = !string.Equals(_lastLanguageId, current, StringComparison.OrdinalIgnoreCase);
                        if (languageChanged)
                        {
                            _lastLanguageId = current;
                        }

                        if (focusSnapshot.HasCaret && focusedWindow != IntPtr.Zero &&
                            _lastFocusedWindow != IntPtr.Zero &&
                            _lastFocusedWindow != focusedWindow)
                        {
                            focusChanged = true;
                        }

                        if (focusSnapshot.HasCaret &&
                            (!_hasLastCaret ||
                             _lastCaretWindow != focusSnapshot.CaretWindow ||
                             !AreRectsEqual(_lastCaretRect, focusSnapshot.CaretRect)))
                        {
                            focusChanged = true;
                        }

                        if (focusedWindow != IntPtr.Zero)
                        {
                            _lastFocusedWindow = focusedWindow;
                        }

                        if (focusSnapshot.HasCaret)
                        {
                            _lastCaretWindow = focusSnapshot.CaretWindow;
                            _lastCaretRect = focusSnapshot.CaretRect;
                            _hasLastCaret = true;
                        }
                        else
                        {
                            _hasLastCaret = false;
                        }
                    }

                    if (languageChanged)
                    {
                        LanguageChanged?.Invoke(this, args);
                    }

                    if (focusChanged)
                    {
                        FocusChanged?.Invoke(this, args);
                    }
                }

                await Task.Delay(80, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(150, cancellationToken);
            }
        }
    }

    private static CultureInfo Culturize(string languageId)
    {
        try
        {
            if (languageId.Contains("-", StringComparison.Ordinal))
            {
                return CultureInfo.GetCultureInfo(languageId);
            }

            return CultureInfo.GetCultureInfo(languageId);
        }
        catch
        {
            return CultureInfo.InvariantCulture;
        }
    }

    private static string GetLabel(string languageId, CultureInfo culture)
    {
        if (languageId.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
        {
            return "عربي";
        }

        if (languageId.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return "English";
        }

        return culture.TwoLetterISOLanguageName.ToUpperInvariant();
    }

    private static string GetCurrentLanguageId()
    {
        try
        {
            var foregroundWindow = User32.GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                return string.Empty;
            }

            var threadId = User32.GetWindowThreadProcessId(foregroundWindow, out _);
            if (threadId == 0)
            {
                return string.Empty;
            }

            var layout = User32.GetKeyboardLayout(threadId);
            if (layout == IntPtr.Zero)
            {
                return string.Empty;
            }

            var layoutId = (int)(layout.ToInt64() & 0xFFFF);
            return ResolveLanguageByLayoutId(layoutId);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static FocusSnapshot GetFocusSnapshot()
    {
        var foregroundWindow = User32.GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            return default;
        }

        var threadId = User32.GetWindowThreadProcessId(foregroundWindow, out _);
        if (threadId == 0)
        {
            return new FocusSnapshot(foregroundWindow, IntPtr.Zero, default, false);
        }

        var info = new User32.GuiThreadInfo
        {
            Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<User32.GuiThreadInfo>()
        };

        if (!User32.GetGUIThreadInfo(threadId, ref info))
        {
            return new FocusSnapshot(foregroundWindow, IntPtr.Zero, default, false);
        }

        return new FocusSnapshot(
            info.FocusedWindow != IntPtr.Zero ? info.FocusedWindow : foregroundWindow,
            info.CaretWindow,
            info.CaretRect,
            info.CaretWindow != IntPtr.Zero);
    }

    private static bool AreRectsEqual(User32.Rect first, User32.Rect second)
    {
        return first.Left == second.Left &&
               first.Top == second.Top &&
               first.Right == second.Right &&
               first.Bottom == second.Bottom;
    }

    private readonly record struct FocusSnapshot(
        IntPtr FocusedWindow,
        IntPtr CaretWindow,
        User32.Rect CaretRect,
        bool HasCaret);

    public static string ResolveLanguageByLayoutId(int layoutId)
    {
        try { return CultureInfo.GetCultureInfo(layoutId).Name; }
        catch (CultureNotFoundException) { return "en-US"; }
    }

    private static string GuessLanguageByLayoutHex(string hex)
    {
        if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var layoutId))
        {
            return ResolveLanguageByLayoutId(layoutId);
        }

        return "en-US";
    }
}
