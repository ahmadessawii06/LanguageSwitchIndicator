using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace LanguageSwitchIndicator.Typing;

public sealed class GlobalCorrectionHotkey : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr window, int id);

    private readonly HwndSource _source = new(new HwndSourceParameters("Typing correction hotkey")
    {
        Width = 0,
        Height = 0,
        WindowStyle = unchecked((int)0x80000000), // WS_POPUP
        ExtendedWindowStyle = 0x08000080          // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW
    });

    private int _id;
    private int _nextId = 100;
    private HotkeyChord? _chord;

    public event Action? Pressed;

    public GlobalCorrectionHotkey()
    {
        _source.AddHook(Hook);
    }

    public bool TryApply(bool enabled, string shortcut, out string message)
    {
        if (!enabled)
        {
            if (_id != 0) UnregisterHotKey(_source.Handle, _id);
            _id = 0;
            _chord = null;
            message = "Typing correction is off.";
            return true;
        }

        if (!HotkeyChord.Validate(shortcut, out var validationError))
        {
            message = validationError!;
            return false;
        }

        if (!HotkeyChord.TryParse(shortcut, out var candidate))
        {
            message = "Invalid shortcut combination. Choose a different combination.";
            return false;
        }

        if (_id != 0 && candidate == _chord)
        {
            message = $"Active: {HotkeyChord.FormatForDisplay(shortcut)}";
            return true;
        }

        var id = ++_nextId;
        // Register first: a conflicting replacement must not disable the previous shortcut.
        // 0x4000 = MOD_NOREPEAT
        if (!RegisterHotKey(_source.Handle, id, candidate!.Modifiers | 0x4000, candidate.Key))
        {
            var win32Err = Marshal.GetLastWin32Error();
            message = $"Shortcut unavailable (Windows error {win32Err}). Another application may be using this shortcut. Previous shortcut remains active.";
            return false;
        }

        if (_id != 0) UnregisterHotKey(_source.Handle, _id);
        _id = id;
        _chord = candidate;
        message = $"Active: {HotkeyChord.FormatForDisplay(shortcut)}";
        return true;
    }

    private IntPtr Hook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == 0x312 && wParam.ToInt32() == _id && _id != 0)
        {
            handled = true;
            Pressed?.Invoke();
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_id != 0) UnregisterHotKey(_source.Handle, _id);
        _source.RemoveHook(Hook);
        _source.Dispose();
    }
}
