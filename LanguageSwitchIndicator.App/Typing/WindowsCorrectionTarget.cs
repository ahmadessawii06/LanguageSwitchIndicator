using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using LanguageSwitchIndicator.Native;
using LanguageSwitchIndicator.Services;

namespace LanguageSwitchIndicator.Typing;

internal static class CorrectionNative
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageTimeoutW", SetLastError = true)]
    internal static extern IntPtr Send(IntPtr window, uint message, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out UIntPtr result);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageTimeoutW", SetLastError = true)]
    internal static extern IntPtr Read(IntPtr window, uint message, IntPtr wParam, StringBuilder text, uint flags, uint timeout, out UIntPtr result);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageTimeoutW", SetLastError = true)]
    internal static extern IntPtr Write(IntPtr window, uint message, IntPtr wParam, string text, uint flags, uint timeout, out UIntPtr result);

    [DllImport("user32.dll")]
    internal static extern bool IsWindowUnicode(IntPtr window);

    [DllImport("user32.dll")]
    internal static extern short GetAsyncKeyState(int key);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, bool inherit, uint id);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(IntPtr token, int information, out int elevation, int size, out int returned);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern IntPtr LoadKeyboardLayout(string id, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    internal static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetFocus();

    [DllImport("user32.dll")]
    internal static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public uint type;
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
        public uint pad1;
        public uint pad2;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);

    internal static bool SendUnicode(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var inputs = new INPUT[text.Length * 2];
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            inputs[i * 2] = new INPUT
            {
                type = 1, // INPUT_KEYBOARD
                ki = new KEYBDINPUT { wVk = 0, wScan = c, dwFlags = 0x0004 } // KEYEVENTF_UNICODE
            };
            inputs[i * 2 + 1] = new INPUT
            {
                type = 1,
                ki = new KEYBDINPUT { wVk = 0, wScan = c, dwFlags = 0x0004 | 0x0002 } // KEYEVENTF_UNICODE | KEYEVENTF_KEYUP
            };
        }
        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        return sent == inputs.Length;
    }

    internal static bool IsAccessibleProcess(uint id)
    {
        var process = OpenProcess(0x1000, false, id);
        if (process == IntPtr.Zero) return false;
        try
        {
            if (!OpenProcessToken(process, 8, out var token)) return true;
            try
            {
                if (GetTokenInformation(token, 20, out var elevation, 4, out _))
                {
                    if (elevation == 0) return true;
                    // Check if our own process is also elevated
                    var myProcess = OpenProcess(0x1000, false, (uint)Process.GetCurrentProcess().Id);
                    if (myProcess != IntPtr.Zero)
                    {
                        try
                        {
                            if (OpenProcessToken(myProcess, 8, out var myToken))
                            {
                                try
                                {
                                    if (GetTokenInformation(myToken, 20, out var myElevation, 4, out _))
                                    {
                                        return myElevation != 0;
                                    }
                                }
                                finally { CloseHandle(myToken); }
                            }
                        }
                        finally { CloseHandle(myProcess); }
                    }
                    return false;
                }
                return true;
            }
            finally { CloseHandle(token); }
        }
        finally { CloseHandle(process); }
    }

    internal static IntPtr FocusedHandle(IntPtr foreground)
    {
        var thread = User32.GetWindowThreadProcessId(foreground, out _);
        if (thread == 0) return IntPtr.Zero;

        var info = new User32.GuiThreadInfo { Size = (uint)Marshal.SizeOf<User32.GuiThreadInfo>() };
        if (User32.GetGUIThreadInfo(thread, ref info) && info.FocusedWindow != IntPtr.Zero)
        {
            return info.FocusedWindow;
        }

        var currentThread = GetCurrentThreadId();
        if (currentThread != thread && AttachThreadInput(currentThread, thread, true))
        {
            try
            {
                var focus = GetFocus();
                if (focus != IntPtr.Zero) return focus;
            }
            finally
            {
                AttachThreadInput(currentThread, thread, false);
            }
        }
        return IntPtr.Zero;
    }
}

internal abstract class WindowsCorrectionTarget : ICorrectionTarget
{
    public CancellationToken Cancellation { get; set; }
    protected readonly IntPtr Foreground;
    protected readonly IntPtr Focus;
    protected readonly AutomationElement? Element;

    protected WindowsCorrectionTarget(IntPtr foreground, IntPtr focus, AutomationElement? element)
    {
        Foreground = foreground;
        Focus = focus;
        Element = element;
    }

    public static WindowsCorrectionTarget Create(IntPtr expectedForeground)
    {
        if (expectedForeground == IntPtr.Zero || User32.GetForegroundWindow() != expectedForeground)
            throw new UnsafeCorrectionException("Focus changed. Try again in the text field.");

        User32.GetWindowThreadProcessId(expectedForeground, out var process);
        if (!CorrectionNative.IsAccessibleProcess(process))
            throw new UnsafeCorrectionException("Elevated or inaccessible apps are not supported.");

        var focus = CorrectionNative.FocusedHandle(expectedForeground);

        AutomationElement? element = null;
        try { element = AutomationElement.FocusedElement; } catch { }

        // If GUIThreadInfo didn't locate a child window, inspect element's HWND
        if (focus == IntPtr.Zero && element != null && element.Current.NativeWindowHandle != 0)
        {
            focus = new IntPtr(element.Current.NativeWindowHandle);
        }

        if (element != null && (element.Current.IsPassword || !element.Current.IsEnabled))
            throw new UnsafeCorrectionException("Protected fields are excluded.");

        // Check if focus is a Win32 Edit or RichEdit control (Notepad, WordPad, Win32 apps)
        if (focus != IntPtr.Zero)
        {
            var className = new StringBuilder(256);
            User32.GetClassName(focus, className, className.Capacity);
            var name = className.ToString();

            if (name.Equals("Edit", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase))
            {
                var style = User32.GetWindowLongPtr(focus, -16).ToInt64();
                var isRich = name.StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase);
                var isReadOnly = (style & 0x0800) != 0;
                var isPassword = !isRich && (style & 0x0020) != 0;

                if (isPassword || isReadOnly || !CorrectionNative.IsWindowUnicode(focus))
                    throw new UnsafeCorrectionException("Protected or read-only fields are excluded.");

                return new NativeEditTarget(expectedForeground, focus, isRich);
            }
        }

        // UIA fallback for WPF, web browsers, and modern document controls
        if (element == null)
        {
            try { if (focus != IntPtr.Zero) element = AutomationElement.FromHandle(focus); } catch { }
        }

        if (element == null || element.Current.ProcessId != process)
            throw new UnsafeCorrectionException("This editor cannot expose a safe replacement range. Select text in a supported plain-text field.");

        var hasText = element.TryGetCurrentPattern(TextPattern.Pattern, out var textPatternObj);
        var hasValue = element.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePatternObj);

        if (!hasText && !hasValue)
            throw new UnsafeCorrectionException("This editor does not support text inspection or replacement.");

        var textPattern = hasText ? (TextPattern)textPatternObj : null;
        var valuePattern = hasValue ? (ValuePattern)valuePatternObj : null;

        if (valuePattern != null && valuePattern.Current.IsReadOnly)
            throw new UnsafeCorrectionException("Protected or read-only fields are excluded.");

        return new AutomationEditTarget(expectedForeground, focus, element, textPattern, valuePattern);
    }

    public abstract TextSnapshot Capture();
    public abstract bool IsCurrent(TextSnapshot snapshot);
    public abstract bool Replace(TextSnapshot snapshot, int start, int length, string replacement);

    private sealed class NativeEditTarget(IntPtr foreground, IntPtr focus, bool isRich)
        : WindowsCorrectionTarget(foreground, focus, null)
    {
        private readonly bool _isRich = isRich;

        private bool HasFocus()
        {
            if (Cancellation.IsCancellationRequested) return false;
            var fg = User32.GetForegroundWindow();
            if (fg != Foreground) return false;
            var currentFocus = CorrectionNative.FocusedHandle(Foreground);
            return currentFocus == Focus || currentFocus == IntPtr.Zero || CorrectionNative.IsChild(Foreground, Focus);
        }

        private string ReadText()
        {
            var buffer = new StringBuilder(60002);
            if (CorrectionNative.Read(Focus, 0xD, new IntPtr(60002), buffer, 2, 400, out _) == IntPtr.Zero)
                throw new UnsafeCorrectionException("The field did not respond. Nothing replaced.");
            var text = buffer.ToString();
            if (text.Length > 60000)
                throw new UnsafeCorrectionException("Select text in a smaller field (maximum 60,000 characters).");
            return text;
        }

        private (int Start, int Length) Selection()
        {
            if (CorrectionNative.Send(Focus, 0xB0, IntPtr.Zero, IntPtr.Zero, 2, 400, out var result) == IntPtr.Zero)
                throw new UnsafeCorrectionException("Cannot read the selection. Nothing replaced.");
            var packed = result.ToUInt64();
            var start = (int)(packed & 0xffff);
            var end = (int)((packed >> 16) & 0xffff);
            if ((uint)packed == uint.MaxValue)
                throw new UnsafeCorrectionException("Selection is too large.");
            return (start, Math.Max(0, end - start));
        }

        public override TextSnapshot Capture()
        {
            if (!HasFocus()) throw new UnsafeCorrectionException("Focus changed. Nothing replaced.");
            var text = ReadText();
            var selection = Selection();
            return new(text, selection.Start, selection.Length);
        }

        public override bool IsCurrent(TextSnapshot snapshot)
        {
            if (!HasFocus() || ReadText() != snapshot.Text) return false;
            var selection = Selection();
            return selection == (snapshot.SelectionStart, snapshot.SelectionLength);
        }

        public override bool Replace(TextSnapshot snapshot, int start, int length, string replacement)
        {
            if (!IsCurrent(snapshot)) return false;

            // If nothing is selected, select the target segment
            if (snapshot.SelectionLength == 0)
            {
                if (CorrectionNative.Send(Focus, 0xB1, new IntPtr(start), new IntPtr(start + length), 2, 400, out _) == IntPtr.Zero)
                    return false;
            }

            if (!HasFocus()) return false;

            // EM_REPLACESEL (0xC2) with wParam = 1 (undoable)
            if (CorrectionNative.Write(Focus, 0xC2, new IntPtr(1), replacement, 2, 600, out _) == IntPtr.Zero)
                return false;

            var updated = ReadText();
            var expected = snapshot.Text[..start] + replacement + snapshot.Text[(start + length)..];
            return updated == expected;
        }
    }

    private sealed class AutomationEditTarget(
        IntPtr foreground,
        IntPtr focus,
        AutomationElement element,
        TextPattern? textPattern,
        ValuePattern? valuePattern)
        : WindowsCorrectionTarget(foreground, focus, element)
    {
        private TextPatternRange? _selection;

        private bool HasFocus()
        {
            if (Cancellation.IsCancellationRequested) return false;
            if (User32.GetForegroundWindow() != Foreground) return false;
            try
            {
                return Element != null && Element.Current.IsEnabled && !Element.Current.IsPassword;
            }
            catch
            {
                return false;
            }
        }

        public override TextSnapshot Capture()
        {
            if (!HasFocus()) throw new UnsafeCorrectionException("Focus changed. Nothing replaced.");

            if (textPattern != null)
            {
                var docRange = textPattern.DocumentRange;
                var fullDocText = docRange.GetText(60001);
                var selRanges = textPattern.GetSelection();

                if (selRanges != null && selRanges.Length > 0)
                {
                    var sel = selRanges[0];
                    _selection = sel.Clone();

                    var prefix = docRange.Clone();
                    prefix.MoveEndpointByRange(TextPatternRangeEndpoint.End, sel, TextPatternRangeEndpoint.Start);
                    var before = prefix.GetText(60001);
                    var selected = sel.GetText(60001);

                    var start = before.Length;
                    var length = selected.Length;
                    var text = valuePattern != null ? valuePattern.Current.Value : fullDocText;

                    if (start <= text.Length)
                    {
                        return new(text, start, Math.Min(length, text.Length - start));
                    }
                }
            }

            if (valuePattern != null)
            {
                var val = valuePattern.Current.Value;
                return new(val, val.Length, 0);
            }

            throw new UnsafeCorrectionException("Cannot determine caret or selection position.");
        }

        public override bool IsCurrent(TextSnapshot snapshot)
        {
            if (!HasFocus()) return false;
            if (valuePattern != null && valuePattern.Current.Value != snapshot.Text) return false;
            return true;
        }

        public override bool Replace(TextSnapshot snapshot, int start, int length, string replacement)
        {
            if (!IsCurrent(snapshot)) return false;

            var expected = snapshot.Text[..start] + replacement + snapshot.Text[(start + length)..];

            // Method A: If ValuePattern is writable, update directly
            if (valuePattern != null && !valuePattern.Current.IsReadOnly)
            {
                valuePattern.SetValue(expected);
                if (valuePattern.Current.Value == expected)
                {
                    // Restore caret at the end of replacement
                    if (textPattern != null)
                    {
                        try
                        {
                            var caret = textPattern.DocumentRange.Clone();
                            caret.MoveEndpointByRange(TextPatternRangeEndpoint.End, caret, TextPatternRangeEndpoint.Start);
                            caret.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, start + replacement.Length);
                            caret.MoveEndpointByRange(TextPatternRangeEndpoint.Start, caret, TextPatternRangeEndpoint.End);
                            caret.Select();
                        }
                        catch { }
                    }
                    return true;
                }
            }

            // Method B: TextPattern selection + Unicode typing (no clipboard used)
            if (textPattern != null)
            {
                try
                {
                    var wordRange = textPattern.DocumentRange.Clone();
                    wordRange.MoveEndpointByRange(TextPatternRangeEndpoint.End, wordRange, TextPatternRangeEndpoint.Start);
                    wordRange.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, start);
                    wordRange.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, start + length);
                    wordRange.Select();

                    if (!CorrectionNative.SendUnicode(replacement)) return false;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}
