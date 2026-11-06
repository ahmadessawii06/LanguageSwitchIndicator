using System;
using System.Collections.Generic;
using System.Windows.Input;
using WpfKey = System.Windows.Input.Key;

namespace LanguageSwitchIndicator.Typing;

public sealed record HotkeyChord(uint Modifiers, uint VirtualKey)
{
    public uint Key => VirtualKey;
    public const string DefaultShortcut = "F8";

    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "CTRL+ALT+DELETE",
        "CTRL+ALT+DEL",
        "WIN+L",
        "WIN+D",
        "WIN+E",
        "WIN+R",
        "WIN+I",
        "WIN+X",
        "WIN+S",
        "WIN+A",
        "WIN+TAB",
        "ALT+TAB",
        "ALT+F4",
        "CTRL+SHIFT+ESCAPE",
        "CTRL+SHIFT+ESC",
        "CTRL+ESCAPE",
        "CTRL+ESC",
        "PRINTSCREEN",
        "WIN+PRINTSCREEN"
    };

    public static bool IsReserved(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var normalized = Normalize(text);
        return Reserved.Contains(normalized);
    }

    public static string Normalize(string text)
    {
        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var mods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? key = null;
        for (var i = 0; i < parts.Length; i++)
        {
            var p = parts[i].ToUpperInvariant();
            if (p is "CTRL" or "CONTROL") mods.Add("Ctrl");
            else if (p is "ALT") mods.Add("Alt");
            else if (p is "SHIFT") mods.Add("Shift");
            else if (p is "WIN" or "WINDOWS") mods.Add("Win");
            else
            {
                // Format key nicely (e.g. F8, Space, or uppercase single letter)
                if (p.StartsWith('F') && int.TryParse(p[1..], out var fn) && fn is >= 1 and <= 24)
                    key = $"F{fn}";
                else if (p == "SPACE")
                    key = "Space";
                else if (p.Length == 1)
                    key = char.ToUpperInvariant(p[0]).ToString();
                else
                    key = p;
            }
        }
        var ordered = new List<string>();
        if (mods.Contains("Ctrl")) ordered.Add("Ctrl");
        if (mods.Contains("Alt")) ordered.Add("Alt");
        if (mods.Contains("Shift")) ordered.Add("Shift");
        if (mods.Contains("Win")) ordered.Add("Win");
        if (!string.IsNullOrEmpty(key)) ordered.Add(key);
        return string.Join('+', ordered);
    }

    public static string FormatForDisplay(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "None";
        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" + ", parts);
    }

    public static string FormatModifiersPreview(ModifierKeys modifiers, bool winDown)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (winDown || modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        return parts.Count > 0 ? string.Join(" + ", parts) + " + …" : "Press your shortcut…";
    }

    public static string? FromKey(WpfKey key, ModifierKeys modifiers, bool winDown)
    {
        var rawKey = key == WpfKey.System ? key : key;
        string? keyStr = rawKey switch
        {
            >= WpfKey.A and <= WpfKey.Z => rawKey.ToString(),
            >= WpfKey.D0 and <= WpfKey.D9 => ((char)('0' + (rawKey - WpfKey.D0))).ToString(),
            >= WpfKey.NumPad0 and <= WpfKey.NumPad9 => ((char)('0' + (rawKey - WpfKey.NumPad0))).ToString(),
            >= WpfKey.F1 and <= WpfKey.F24 => rawKey.ToString(),
            WpfKey.Space => "Space",
            WpfKey.OemSemicolon => ";",
            WpfKey.OemPlus => "=",
            WpfKey.OemComma => ",",
            WpfKey.OemMinus => "-",
            WpfKey.OemPeriod => ".",
            WpfKey.OemQuestion => "/",
            WpfKey.OemTilde => "`",
            WpfKey.OemOpenBrackets => "[",
            WpfKey.OemPipe => "\\",
            WpfKey.OemCloseBrackets => "]",
            WpfKey.OemQuotes => "'",
            _ => null
        };
        if (keyStr == null) return null;

        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (winDown || modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(keyStr);
        return string.Join('+', parts);
    }

    public static bool Validate(string text, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Please enter a shortcut.";
            return false;
        }
        if (IsReserved(text))
        {
            error = "This shortcut is reserved by Windows. Choose a different combination.";
            return false;
        }
        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            error = "Invalid shortcut format.";
            return false;
        }

        var keyPart = parts[^1].ToUpperInvariant();
        var isFunctionKey = keyPart.StartsWith('F') && int.TryParse(keyPart[1..], out var fn) && fn is >= 1 and <= 24;

        if (parts.Length == 1)
        {
            if (isFunctionKey)
            {
                error = null;
                return true;
            }
            error = "Plain letters and typing keys cannot be used alone. Choose a function key (like F8) or combine with Ctrl, Alt, or Win.";
            return false;
        }

        var hasPrimary = false;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            var p = parts[i].ToUpperInvariant();
            if (p is "CTRL" or "CONTROL" or "ALT" or "WIN" or "WINDOWS")
            {
                hasPrimary = true;
            }
        }

        if (!hasPrimary)
        {
            // Only Shift was pressed
            if (!isFunctionKey)
            {
                error = "Shift alone can only be combined with function keys (like Shift+F8). Use Ctrl, Alt, or Win with typing keys.";
                return false;
            }
        }

        if (!TryParse(text, out _))
        {
            error = "Invalid shortcut combination. Choose a different combination.";
            return false;
        }

        error = null;
        return true;
    }

    public static bool TryParse(string text, out HotkeyChord? chord)
    {
        chord = null;
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (IsReserved(text)) return false;

        uint modifiers = 0;
        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        for (var i = 0; i < parts.Length - 1; i++)
        {
            var flag = parts[i].ToUpperInvariant() switch
            {
                "CTRL" or "CONTROL" => 2u, // MOD_CONTROL
                "ALT" => 1u,               // MOD_ALT
                "SHIFT" => 4u,             // MOD_SHIFT
                "WIN" or "WINDOWS" => 8u,  // MOD_WIN
                _ => 0u
            };
            if (flag == 0 || (modifiers & flag) != 0) return false;
            modifiers |= flag;
        }

        var key = parts[^1].ToUpperInvariant();
        var isFunctionKey = key.StartsWith('F') && int.TryParse(key[1..], out var fn) && fn is >= 1 and <= 24;

        // Validation rules:
        // 1. Single key (modifiers == 0): MUST be a function key F1..F24.
        // 2. Shift only (modifiers == 4u): MUST be a function key F1..F24.
        // 3. Otherwise, must include at least Ctrl (2), Alt (1), or Win (8).
        if (modifiers == 0)
        {
            if (!isFunctionKey) return false;
        }
        else if (modifiers == 4u)
        {
            if (!isFunctionKey) return false;
        }
        else if ((modifiers & (1u | 2u | 8u)) == 0)
        {
            return false;
        }

        uint vk = key.StartsWith('F') && int.TryParse(key[1..], out var fnVal) && fnVal is >= 1 and <= 24
            ? (uint)(0x70 + fnVal - 1)
            : key.Length == 1 && char.IsAsciiLetterOrDigit(key[0]) ? (uint)key[0]
            : key switch
            {
                ";" => 0xBA, "=" or "+" => 0xBB, "," or "<" => 0xBC, "-" or "_" => 0xBD,
                "." or ">" => 0xBE, "/" or "?" => 0xBF, "`" or "~" => 0xC0,
                "[" or "{" => 0xDB, "\\" or "|" => 0xDC, "]" or "}" => 0xDD, "'" or "\"" => 0xDE,
                "SPACE" => 0x20,
                _ => 0
            };

        if (vk == 0) return false;
        chord = new(modifiers, vk);
        return true;
    }
}
