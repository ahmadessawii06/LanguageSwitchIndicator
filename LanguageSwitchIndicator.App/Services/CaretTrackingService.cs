using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Threading;
using LanguageSwitchIndicator.Native;
using WpfPoint = System.Windows.Point;

namespace LanguageSwitchIndicator.Services;

public sealed class CaretEventArgs : EventArgs
{
    public WpfPoint Position { get; }
    public bool Activation { get; }
    public bool LanguageChanged { get; }

    public CaretEventArgs(WpfPoint position, bool activation, bool languageChanged)
    {
        Position = position;
        Activation = activation;
        LanguageChanged = languageChanged;
    }
}

public sealed class CaretTrackingService : IDisposable
{
    private readonly InputLanguageMonitor _languageMonitor;
    private readonly DispatcherTimer _timer;
    private readonly User32.WinEventProc _winEventCallback;
    private readonly User32.HookProc _mouseCallback;
    private readonly IntPtr _focusHook;
    private readonly IntPtr _foregroundHook;
    private readonly IntPtr _mouseHook;
    private IntPtr _lastWindow;
    private IntPtr _lastFocus;
    private string _lastLanguage = string.Empty;
    private bool _activationPending;
    private bool _disposed;

    public event EventHandler<CaretEventArgs>? CaretChanged;

    public CaretTrackingService(InputLanguageMonitor languageMonitor)
    {
        _languageMonitor = languageMonitor;
        _winEventCallback = (_, _, _, _, _, _, _) =>
        {
            _activationPending = true;
            Dispatcher.CurrentDispatcher.BeginInvoke(Refresh);
        };
        _mouseCallback = (code, message, data) =>
        {
            if (code >= 0 && message.ToInt32() == 0x202)
            {
                _activationPending = true;
                Dispatcher.CurrentDispatcher.BeginInvoke(Refresh);
            }

            return User32.CallNextHookEx(IntPtr.Zero, code, message, data);
        };

        _focusHook = User32.SetWinEventHook(0x8005, 0x8005, IntPtr.Zero, _winEventCallback, 0, 0, 2);
        _foregroundHook = User32.SetWinEventHook(3, 3, IntPtr.Zero, _winEventCallback, 0, 0, 2);
        _mouseHook = User32.SetWindowsHookEx(14, _mouseCallback, IntPtr.Zero, 0);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
    }

    private void Refresh()
    {
        if (_disposed)
        {
            return;
        }

        var foreground = User32.GetForegroundWindow();
        var thread = User32.GetWindowThreadProcessId(foreground, out var processId);
        if (foreground == IntPtr.Zero || processId == Environment.ProcessId || thread == 0)
        {
            _lastWindow = IntPtr.Zero;
            _lastFocus = IntPtr.Zero;
            _activationPending = false;
            return;
        }

        var info = new User32.GuiThreadInfo
        {
            Size = (uint)Marshal.SizeOf<User32.GuiThreadInfo>()
        };
        if (!User32.GetGUIThreadInfo(thread, ref info) || info.FocusedWindow == IntPtr.Zero || !IsEditable(info.FocusedWindow))
        {
            _activationPending = false;
            return;
        }

        var position = ReadCaretPosition(info);
        if (position == null && _activationPending && User32.GetCursorPos(out var mouse))
        {
            position = new WpfPoint(mouse.X, mouse.Y);
        }

        if (position == null)
        {
            return;
        }

        var language = _languageMonitor.GetCurrentDisplayText();
        var languageChanged = !string.IsNullOrWhiteSpace(language) && !string.Equals(language, _lastLanguage, StringComparison.Ordinal);
        var focusChanged = foreground != _lastWindow || info.FocusedWindow != _lastFocus;
        var activation = _activationPending || focusChanged || languageChanged;
        _lastWindow = foreground;
        _lastFocus = info.FocusedWindow;
        _lastLanguage = language;
        _activationPending = false;
        CaretChanged?.Invoke(this, new CaretEventArgs(position.Value, activation, languageChanged));
    }

    private static bool IsEditable(IntPtr focus)
    {
        var className = new StringBuilder(256);
        User32.GetClassName(focus, className, className.Capacity);
        if (className.ToString().Equals("Edit", StringComparison.OrdinalIgnoreCase) ||
            className.ToString().StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase))
        {
            return (User32.GetWindowLongPtr(focus, -16).ToInt64() & 0x800) == 0;
        }

        try
        {
            var element = AutomationElement.FocusedElement;
            if (element == null || !element.Current.IsEnabled || element.Current.ProcessId == Environment.ProcessId)
            {
                return false;
            }

            if (element.Current.IsPassword)
            {
                return true;
            }

            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePattern))
            {
                return !((ValuePattern)valuePattern).Current.IsReadOnly;
            }

            if (element.TryGetCurrentPattern(TextPattern.Pattern, out var textPattern))
            {
                var readOnly = ((TextPattern)textPattern).DocumentRange.GetAttributeValue(TextPattern.IsReadOnlyAttribute);
                return readOnly is bool flag && !flag;
            }
        }
        catch (ElementNotAvailableException) { }
        catch (InvalidOperationException) { }
        catch (COMException) { }

        return false;
    }

    private static WpfPoint? ReadCaretPosition(User32.GuiThreadInfo info)
    {
        if (info.CaretWindow != IntPtr.Zero)
        {
            var point = new User32.Point { X = info.CaretRect.Left, Y = info.CaretRect.Bottom };
            if (User32.ClientToScreen(info.CaretWindow, ref point))
            {
                return new WpfPoint(point.X, point.Y);
            }
        }

        try
        {
            var element = AutomationElement.FocusedElement;
            if (element != null && element.TryGetCurrentPattern(TextPattern.Pattern, out var pattern))
            {
                var selection = ((TextPattern)pattern).GetSelection();
                if (selection.Length > 0)
                {
                    var rectangles = selection[0].GetBoundingRectangles();
                    if (rectangles.Length > 0 && rectangles[0].Height > 0)
                    {
                        return new WpfPoint(rectangles[0].Left, rectangles[0].Bottom);
                    }
                }
            }
        }
        catch (ElementNotAvailableException) { }
        catch (InvalidOperationException) { }
        catch (COMException) { }

        return null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        if (_focusHook != IntPtr.Zero) User32.UnhookWinEvent(_focusHook);
        if (_foregroundHook != IntPtr.Zero) User32.UnhookWinEvent(_foregroundHook);
        if (_mouseHook != IntPtr.Zero) User32.UnhookWindowsHookEx(_mouseHook);
    }
}
