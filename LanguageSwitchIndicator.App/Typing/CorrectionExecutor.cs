using LanguageSwitchIndicator.Services;

namespace LanguageSwitchIndicator.Typing;

public sealed record TextSnapshot(string Text, int SelectionStart, int SelectionLength);

public interface ICorrectionTarget
{
    TextSnapshot Capture();
    bool IsCurrent(TextSnapshot snapshot);
    bool Replace(TextSnapshot snapshot, int start, int length, string replacement);
}

public sealed class UnsafeCorrectionException(string message) : Exception(message);

public sealed record CorrectionOutcome(bool Success, string Message, CorrectionDirection Direction = CorrectionDirection.Auto);

public static class CorrectionExecutor
{
    public static CorrectionOutcome Execute(ICorrectionTarget target, CorrectionDirection direction, string? layout)
    {
        var snapshot = target.Capture();
        DiagnosticLog.Write($"[Correction] Stage 4: Text captured. TotalLength={snapshot.Text.Length}, SelStart={snapshot.SelectionStart}, SelLen={snapshot.SelectionLength}");

        var start = snapshot.SelectionStart;
        var length = snapshot.SelectionLength;

        if (start < 0 || length < 0 || start + length > snapshot.Text.Length)
        {
            DiagnosticLog.Write("[Correction] Stage 4: Invalid selection bounds.");
            return new(false, "Select the text first.");
        }

        if (length == 0)
        {
            var segment = TypingLayoutEngine.PreviousSegment(snapshot.Text, start);
            if (segment == null)
            {
                DiagnosticLog.Write("[Correction] Stage 4: No word segment found before caret.");
                return new(false, "Select the text to fix first.");
            }
            (start, length) = segment.Value;
            DiagnosticLog.Write($"[Correction] Stage 4: Resolved word segment before caret. Start={start}, Length={length}");
        }

        var result = TypingLayoutEngine.Convert(snapshot.Text.Substring(start, length), direction, layout);
        DiagnosticLog.Write($"[Correction] Stage 5: Conversion calculated. Direction={result.Direction}, WordLength={length}, ConvertedLength={result.Text.Length}, Success={result.Success}");

        if (!result.Success)
        {
            return new(false, result.Message);
        }

        if (!target.IsCurrent(snapshot))
        {
            DiagnosticLog.Write("[Correction] Stage 6: Aborted because focus or selection changed.");
            return new(false, "Focus or selection changed. Nothing replaced.");
        }

        DiagnosticLog.Write($"[Correction] Stage 6: Replacement attempted. Start={start}, Length={length}");
        var replaced = target.Replace(snapshot, start, length, result.Text);

        var outcome = replaced
            ? new CorrectionOutcome(true, result.Message, result.Direction)
            : new CorrectionOutcome(false, "Replacement could not be verified. Check the text; no retry was made.");

        DiagnosticLog.Write($"[Correction] Stage 7: Outcome. Success={outcome.Success}, Message='{outcome.Message}'");
        return outcome;
    }
}
