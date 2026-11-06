using System.Text;

namespace LanguageSwitchIndicator.Typing;

public enum CorrectionDirection { Auto, EnglishToArabic, ArabicToEnglish }
public sealed record ConversionResult(bool Success, string Text, CorrectionDirection Direction, string Message);

public static class TypingLayoutEngine
{
    public const string LayoutPair = "English (US) ↔ Arabic (101)";
    private static readonly List<(string English, string Arabic)> Keys = BuildKeys();
    private static readonly Dictionary<string, string> Forward = Keys.ToDictionary(x => x.English, x => x.Arabic);
    // Prefer the unshifted key when outputs collide. Prefer one physical key over several.
    private static readonly Dictionary<string, string> Reverse = Keys.GroupBy(x => x.Arabic).ToDictionary(x => x.Key, x => x.First().English);
    private static readonly string[] ReverseTokens = Reverse.Keys.OrderByDescending(x => x.Length).ToArray();
    private static readonly HashSet<char> SegmentCharacters = new(Keys.SelectMany(x => x.English + x.Arabic));
    private static List<(string, string)> BuildKeys()
    {
        var keys = new List<(string, string)>();
        void Row(string english, string[] arabic) { for (var i = 0; i < english.Length; i++) keys.Add((english[i].ToString(), arabic[i])); }
        Row("`1234567890-=", new[] { "ذ", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" });
        Row("qwertyuiop[]\\", new[] { "ض", "ص", "ث", "ق", "ف", "غ", "ع", "ه", "خ", "ح", "ج", "د", "\\" });
        Row("asdfghjkl;'", new[] { "ش", "س", "ي", "ب", "ل", "ا", "ت", "ن", "م", "ك", "ط" });
        Row("zxcvbnm,./", new[] { "ئ", "ء", "ؤ", "ر", "لا", "ى", "ة", "و", "ز", "ظ" });
        Row("~!@#$%^&*()_+", new[] { "ّ", "!", "@", "#", "$", "%", "^", "&", "*", ")", "(", "_", "+" });
        Row("QWERTYUIOP{}|", new[] { "َ", "ً", "ُ", "ٌ", "لإ", "إ", "‘", "÷", "×", "؛", "<", ">", "|" });
        Row("ASDFGHJKL:\"", new[] { "ِ", "ٍ", "]", "[", "لأ", "أ", "ـ", "،", "/", ":", "\"" });
        Row("ZXCVBNM<>?", new[] { "~", "ْ", "}", "{", "لآ", "آ", "’", ",", ".", "؟" });
        return keys;
    }
    public static ConversionResult Convert(string text, CorrectionDirection direction, string? currentLayout = null)
    {
        if (string.IsNullOrEmpty(text) || text.Length > 60000) return new(false, text, direction, "Select a smaller piece of text first.");
        if (direction == CorrectionDirection.Auto)
        {
            var latin = text.Any(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z');
            var arabic = text.Any(c => c is >= '\u0600' and <= '\u06ff' && (char.IsLetter(c) || char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark));
            if (latin && arabic) return new(false, text, direction, "Mixed text: choose a correction direction first.");
            direction = latin ? CorrectionDirection.EnglishToArabic : arabic ? CorrectionDirection.ArabicToEnglish
                : currentLayout == "00000409" ? CorrectionDirection.EnglishToArabic
                : currentLayout == "00000401" ? CorrectionDirection.ArabicToEnglish : CorrectionDirection.Auto;
            if (direction == CorrectionDirection.Auto) return new(false, text, direction, "Choose a correction direction for this text.");
        }
        var result = new StringBuilder();
        for (var i = 0; i < text.Length;)
        {
            if (direction == CorrectionDirection.EnglishToArabic)
            {
                result.Append(Forward.TryGetValue(text[i].ToString(), out var value) ? value : text[i].ToString());
                i++;
            }
            else
            {
                var token = ReverseTokens.FirstOrDefault(x => text.AsSpan(i).StartsWith(x, StringComparison.Ordinal));
                if (token == null) { result.Append(text[i++]); }
                else { result.Append(Reverse[token]); i += token.Length; }
            }
        }
        var converted = result.ToString();
        return new(converted != text, converted, direction, converted == text ? "No layout correction needed." : direction == CorrectionDirection.EnglishToArabic ? "EN → AR" : "AR → EN");
    }
    public static (int Start, int Length)? PreviousSegment(string text, int caret)
    {
        if (caret < 1 || caret > text.Length) return null;
        var end = caret;
        while (end > 0 && char.IsWhiteSpace(text[end - 1])) end--;
        if (end == 0) return null;
        var start = end;
        while (start > 0 && SegmentCharacters.Contains(text[start - 1]) && !char.IsWhiteSpace(text[start - 1])) start--;
        return start == end ? null : (start, end - start);
    }
}
