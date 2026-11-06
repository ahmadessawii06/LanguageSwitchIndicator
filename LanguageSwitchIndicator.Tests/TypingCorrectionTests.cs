using LanguageSwitchIndicator.Typing;

namespace LanguageSwitchIndicator.Tests;

public class TypingCorrectionTests
{
    [Theory]
    [InlineData(";dt;", "كيفك")]
    [InlineData("lvpfh", "مرحبا")]
    [InlineData("sbl", "سلام")]
    [InlineData("b", "لا")]
    [InlineData("B", "لآ")]
    [InlineData("G", "لأ")]
    [InlineData("T", "لإ")]
    public void TypingLayoutEngine_ConvertsEnglishToArabic(string english, string expectedArabic)
    {
        var result = TypingLayoutEngine.Convert(english, CorrectionDirection.EnglishToArabic);
        Assert.True(result.Success);
        Assert.Equal(expectedArabic, result.Text);
        Assert.Equal(CorrectionDirection.EnglishToArabic, result.Direction);
    }

    [Theory]
    [InlineData("كيفك", ";dt;")]
    [InlineData("مرحبا", "lvpfh")]
    [InlineData("سلام", "sbl")]
    [InlineData("لا", "b")]
    [InlineData("لآ", "B")]
    [InlineData("لأ", "G")]
    [InlineData("لإ", "T")]
    public void TypingLayoutEngine_ConvertsArabicToEnglish(string arabic, string expectedEnglish)
    {
        var result = TypingLayoutEngine.Convert(arabic, CorrectionDirection.ArabicToEnglish);
        Assert.True(result.Success);
        Assert.Equal(expectedEnglish, result.Text);
        Assert.Equal(CorrectionDirection.ArabicToEnglish, result.Direction);
    }

    [Fact]
    public void TypingLayoutEngine_AutoDirection_DetectsScriptProperly()
    {
        var enResult = TypingLayoutEngine.Convert(";dt;", CorrectionDirection.Auto);
        Assert.True(enResult.Success);
        Assert.Equal("كيفك", enResult.Text);
        Assert.Equal(CorrectionDirection.EnglishToArabic, enResult.Direction);

        var arResult = TypingLayoutEngine.Convert("كيفك", CorrectionDirection.Auto);
        Assert.True(arResult.Success);
        Assert.Equal(";dt;", arResult.Text);
        Assert.Equal(CorrectionDirection.ArabicToEnglish, arResult.Direction);
    }

    [Fact]
    public void TypingLayoutEngine_AutoDirection_RejectsMixedText()
    {
        var mixed = TypingLayoutEngine.Convert("hello مرحبا", CorrectionDirection.Auto);
        Assert.False(mixed.Success);
        Assert.Contains("Mixed text", mixed.Message);
    }

    [Fact]
    public void TypingLayoutEngine_TextExceedingLimit_ReturnsError()
    {
        var hugeText = new string('a', 60001);
        var result = TypingLayoutEngine.Convert(hugeText, CorrectionDirection.EnglishToArabic);
        Assert.False(result.Success);
        Assert.Contains("smaller piece", result.Message);
    }

    [Fact]
    public void TypingLayoutEngine_PreviousSegment_FindsWordBeforeCaret()
    {
        var text = "First ;dt; Second";
        // Caret right after ';dt;' is index 10
        var segment = TypingLayoutEngine.PreviousSegment(text, 10);
        Assert.NotNull(segment);
        Assert.Equal(6, segment.Value.Start);
        Assert.Equal(4, segment.Value.Length);
        Assert.Equal(";dt;", text.Substring(segment.Value.Start, segment.Value.Length));
    }

    [Fact]
    public void TypingLayoutEngine_PreviousSegment_FindsWordBeforeTrailingSpaces()
    {
        var text = "First ;dt;   Second";
        // Caret after trailing spaces of ';dt;   ' is index 13
        var segment = TypingLayoutEngine.PreviousSegment(text, 13);
        Assert.NotNull(segment);
        Assert.Equal(6, segment.Value.Start);
        Assert.Equal(4, segment.Value.Length);
        Assert.Equal(";dt;", text.Substring(segment.Value.Start, segment.Value.Length));
    }

    [Fact]
    public void TypingLayoutEngine_PreviousSegment_ReturnsNullWhenAtStartOrOnlyWhitespace()
    {
        Assert.Null(TypingLayoutEngine.PreviousSegment("hello", 0));
        Assert.Null(TypingLayoutEngine.PreviousSegment("     ", 5));
    }

    [Theory]
    [InlineData("F8", true, 0u, 0x77u)]
    [InlineData("F9", true, 0u, 0x78u)]
    [InlineData("F10", true, 0u, 0x79u)]
    [InlineData("F12", true, 0u, 0x7Bu)]
    [InlineData("Ctrl+Q", true, 2u, (uint)'Q')]
    [InlineData("Alt+Q", true, 1u, (uint)'Q')]
    [InlineData("Shift+F8", true, 4u, 0x77u)]
    [InlineData("Ctrl+Space", true, 2u, 0x20u)]
    [InlineData("Ctrl+Alt+Q", true, 3u, (uint)'Q')]
    [InlineData("ctrl+shift+z", true, 6u, (uint)'Z')]
    [InlineData("Win+Alt+K", true, 9u, (uint)'K')]
    [InlineData("Win+Shift+1", true, 12u, (uint)'1')]
    [InlineData("Alt+F10", true, 1u, 0x79u)]
    [InlineData("Ctrl+1", true, 2u, (uint)'1')]
    [InlineData("Ctrl+Alt+;", true, 3u, 0xBAu)]
    [InlineData("Q", false, 0u, 0u)]
    [InlineData("1", false, 0u, 0u)]
    [InlineData("Space", false, 0u, 0u)]
    [InlineData(";", false, 0u, 0u)]
    [InlineData("Shift+A", false, 0u, 0u)]
    [InlineData("Shift+1", false, 0u, 0u)]
    [InlineData("Ctrl+Ctrl+Q", false, 0u, 0u)]
    [InlineData("Invalid+Hotkey", false, 0u, 0u)]
    [InlineData("Win+L", false, 0u, 0u)]
    [InlineData("Ctrl+Alt+Delete", false, 0u, 0u)]
    [InlineData("Alt+F4", false, 0u, 0u)]
    public void HotkeyChord_TryParse_ValidatesAndParsesChords(string chordText, bool expectedSuccess, uint expectedModifiers, uint expectedKey)
    {
        var success = HotkeyChord.TryParse(chordText, out var chord);
        Assert.Equal(expectedSuccess, success);
        if (expectedSuccess)
        {
            Assert.NotNull(chord);
            Assert.Equal(expectedModifiers, chord.Modifiers);
            Assert.Equal(expectedKey, chord.Key);
        }
        else
        {
            Assert.Null(chord);
        }
    }

    [Theory]
    [InlineData("Win+L", true)]
    [InlineData("Ctrl+Alt+Delete", true)]
    [InlineData("Alt+Tab", true)]
    [InlineData("Alt+F4", true)]
    [InlineData("Ctrl+Shift+Escape", true)]
    [InlineData("Ctrl+Alt+Q", false)]
    [InlineData("Ctrl+Shift+K", false)]
    public void HotkeyChord_IsReserved_IdentifiesOsShortcuts(string chord, bool expectedReserved)
    {
        Assert.Equal(expectedReserved, HotkeyChord.IsReserved(chord));
    }

    [Fact]
    public void HotkeyChord_FormatForDisplay_FormatsNicely()
    {
        Assert.Equal("Ctrl + Alt + Q", HotkeyChord.FormatForDisplay("Ctrl+Alt+Q"));
        Assert.Equal("Win + Shift + K", HotkeyChord.FormatForDisplay("Win+Shift+K"));
        Assert.Equal("None", HotkeyChord.FormatForDisplay(""));
    }

    [Fact]
    public void HotkeyChord_FromKey_BuildsExpectedShortcutString()
    {
        var chord1 = HotkeyChord.FromKey(System.Windows.Input.Key.Q, System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Alt, false);
        Assert.Equal("Ctrl+Alt+Q", chord1);

        var chord2 = HotkeyChord.FromKey(System.Windows.Input.Key.K, System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift, false);
        Assert.Equal("Ctrl+Shift+K", chord2);

        var chord3 = HotkeyChord.FromKey(System.Windows.Input.Key.D5, System.Windows.Input.ModifierKeys.Alt, false);
        Assert.Equal("Alt+5", chord3);

        var chord4 = HotkeyChord.FromKey(System.Windows.Input.Key.F11, System.Windows.Input.ModifierKeys.Control, false);
        Assert.Equal("Ctrl+F11", chord4);

        var chord5 = HotkeyChord.FromKey(System.Windows.Input.Key.Z, System.Windows.Input.ModifierKeys.Shift, true);
        Assert.Equal("Shift+Win+Z", chord5);
    }

    [Fact]
    public void HotkeyChord_FormatModifiersPreview_ShowsLiveModifiers()
    {
        var preview1 = HotkeyChord.FormatModifiersPreview(System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Alt, false);
        Assert.Equal("Ctrl + Alt + …", preview1);

        var preview2 = HotkeyChord.FormatModifiersPreview(System.Windows.Input.ModifierKeys.None, false);
        Assert.Equal("Press your shortcut…", preview2);
    }

    [Fact]
    public void HotkeyChord_Validate_ValidatesCorrectly()
    {
        Assert.True(HotkeyChord.Validate("F8", out var err1));
        Assert.Null(err1);

        Assert.True(HotkeyChord.Validate("F12", out var err2));
        Assert.Null(err2);

        Assert.True(HotkeyChord.Validate("Ctrl+Q", out var err3));
        Assert.Null(err3);

        Assert.True(HotkeyChord.Validate("Shift+F8", out var err4));
        Assert.Null(err4);

        Assert.True(HotkeyChord.Validate("Ctrl+Space", out var err5));
        Assert.Null(err5);

        Assert.True(HotkeyChord.Validate("Ctrl+Alt+Q", out var err6));
        Assert.Null(err6);

        Assert.False(HotkeyChord.Validate("A", out var err7));
        Assert.Contains("Plain letters", err7);

        Assert.False(HotkeyChord.Validate("Shift+A", out var err8));
        Assert.Contains("Shift alone", err8);

        Assert.False(HotkeyChord.Validate("Win+L", out var err9));
        Assert.Contains("reserved", err9);
    }

    [Fact]
    public void GlobalCorrectionHotkey_TryApply_RegistersAndUnregistersProperly()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var hotkey = new GlobalCorrectionHotkey();
                // Test applying default shortcut F8
                var successF8 = hotkey.TryApply(true, "F8", out var msgF8);
                Assert.True(successF8);
                Assert.Contains("Active", msgF8);

                // Test applying valid chord
                var success = hotkey.TryApply(true, "Ctrl+Alt+Q", out var message);
                Assert.True(success);
                Assert.Contains("Active", message);

                // Test applying reserved shortcut fails
                var reserved = hotkey.TryApply(true, "Win+L", out var resMsg);
                Assert.False(reserved);
                Assert.Contains("reserved", resMsg);

                // Previous shortcut remains active
                var sameAgain = hotkey.TryApply(true, "Ctrl+Alt+Q", out var sameMsg);
                Assert.True(sameAgain);
                Assert.Contains("Active", sameMsg);

                // Test disabling
                var disabled = hotkey.TryApply(false, "Ctrl+Alt+Q", out var offMessage);
                Assert.True(disabled);
                Assert.Contains("off", offMessage);

                // Test invalid shortcut rejected
                var invalid = hotkey.TryApply(true, "invalid", out var errMessage);
                Assert.False(invalid);
                Assert.NotNull(errMessage);
            }
            catch (Exception ex) { failure = ex; }
            finally { System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(5)));
        if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    [Fact]
    public void SettingsEditor_TypingCorrectionHotkey_PersistsAndRestores()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"settings-test-{Guid.NewGuid():N}.json");
        try
        {
            var service = new LanguageSwitchIndicator.Services.SettingsService(tempFile);
            var settings = service.Load();
            Assert.Equal("F8", settings.TypingCorrectionHotkey);

            var editor = new LanguageSwitchIndicator.Models.SettingsEditor(settings, service)
            {
                TypingCorrectionHotkey = "Ctrl+Shift+K"
            };

            var reopened = service.Load();
            Assert.Equal("Ctrl+Shift+K", reopened.TypingCorrectionHotkey);
            Assert.Equal(reopened.TypingCorrectionHotkey, editor.TypingCorrectionHotkey);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile);
        }
    }

    private sealed class MockCorrectionTarget(string initialText, int selectionStart, int selectionLength) : ICorrectionTarget
    {
        public string Text { get; set; } = initialText;
        public int SelectionStart { get; set; } = selectionStart;
        public int SelectionLength { get; set; } = selectionLength;
        public bool InvalidateOnNextCheck { get; set; }
        public bool FailReplacement { get; set; }

        public TextSnapshot Capture() => new(Text, SelectionStart, SelectionLength);

        public bool IsCurrent(TextSnapshot snapshot)
        {
            if (InvalidateOnNextCheck) return false;
            return Text == snapshot.Text && SelectionStart == snapshot.SelectionStart && SelectionLength == snapshot.SelectionLength;
        }

        public bool Replace(TextSnapshot snapshot, int start, int length, string replacement)
        {
            if (FailReplacement || !IsCurrent(snapshot)) return false;
            Text = Text.Remove(start, length).Insert(start, replacement);
            SelectionStart = start + replacement.Length;
            SelectionLength = 0;
            return true;
        }
    }

    [Fact]
    public void CorrectionExecutor_WithExplicitSelection_ReplacesText()
    {
        var target = new MockCorrectionTarget("Hello ;dt; world", 6, 4);
        var outcome = CorrectionExecutor.Execute(target, CorrectionDirection.Auto, null);

        Assert.True(outcome.Success);
        Assert.Equal("Hello كيفك world", target.Text);
        Assert.Equal(10, target.SelectionStart);
        Assert.Equal(0, target.SelectionLength);
    }

    [Fact]
    public void CorrectionExecutor_WithCaretAtEndOfWord_ReplacesPreviousSegment()
    {
        var target = new MockCorrectionTarget("Hello ;dt; world", 10, 0);
        var outcome = CorrectionExecutor.Execute(target, CorrectionDirection.Auto, null);

        Assert.True(outcome.Success);
        Assert.Equal("Hello كيفك world", target.Text);
    }

    [Fact]
    public void CorrectionExecutor_WithCaretAfterTrailingSpaces_ReplacesPreviousSegmentAndPreservesSpaces()
    {
        var target = new MockCorrectionTarget("Hello ;dt;   world", 13, 0);
        var outcome = CorrectionExecutor.Execute(target, CorrectionDirection.Auto, null);

        Assert.True(outcome.Success);
        Assert.Equal("Hello كيفك   world", target.Text);
    }

    [Fact]
    public void CorrectionExecutor_WhenFocusOrSelectionChanged_AbortsSafely()
    {
        var target = new MockCorrectionTarget("Hello ;dt; world", 6, 4) { InvalidateOnNextCheck = true };
        var outcome = CorrectionExecutor.Execute(target, CorrectionDirection.Auto, null);

        Assert.False(outcome.Success);
        Assert.Contains("Focus or selection changed", outcome.Message);
        Assert.Equal("Hello ;dt; world", target.Text);
    }

    [Fact]
    public void CorrectionExecutor_WhenReplacementFails_ReturnsFailureMessage()
    {
        var target = new MockCorrectionTarget("Hello ;dt; world", 6, 4) { FailReplacement = true };
        var outcome = CorrectionExecutor.Execute(target, CorrectionDirection.Auto, null);

        Assert.False(outcome.Success);
        Assert.Contains("Replacement could not be verified", outcome.Message);
    }
}
