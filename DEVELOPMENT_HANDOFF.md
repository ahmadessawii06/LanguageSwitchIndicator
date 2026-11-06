# Development Handoff

## Project Overview
**Language Switch Indicator** is a macOS-inspired keyboard language indicator and typing utility for Windows 10/11 built with .NET 8 (C#) and WPF. It shows a floating, transient visual indicator whenever the user switches keyboard languages, enters an editable text field, or requests typing correction.

- **Target Framework:** .NET 8.0-windows
- **UI Framework:** WPF with [WPF-UI 4.3.0](https://github.com/lepoco/wpfui) (Fluent Design)
- **Architecture:** MVVM-oriented WPF desktop app with Windows Win32 hooks (`SetWindowsHookEx`), UI Automation (`UIAutomationClient` / `UIAutomationTypes`), and System.Windows.Forms (`Screen`, `NotifyIcon`).

---

## Current Project Status
- **Active Branch:** `feature-show-on-focus`
- **Build Status:** Clean build in both Debug and Release (`dotnet build -c Release -p:UseSharedCompilation=false --disable-build-servers`).
- **Test Status:** **81 automated tests passing** in both Debug and Release (`dotnet test -c Release -p:UseSharedCompilation=false --disable-build-servers`).
- **Design Review:** All 8 settings pages verified at 1180, 700, and 620 DIP with 0 data binding errors.
- **Default Shortcut:** `F8` for new installations (existing user configurations preserved).

---

## Completed Functionality

### 1. Fluent Desktop Redesign (WPF-UI 4.3.0)
- Modernized Settings window implemented in `SettingsWindow.xaml` and `SettingsWindow.xaml.cs`.
- 8 fully functional navigation pages:
  1. **General:** Enable/pause indicator, popup trigger mode (`LanguageChangeOnly`, `CaretPlacedOnly`, `Both`), startup launch toggle, start minimized.
  2. **Appearance:** 5 sizes (Extra Small, Small, Medium, Large, Extra Large), Dark/Light/System theme selection, dimensions, custom font/colors, live preview.
  3. **Position:** 9-point alignment grid, near-mouse / near-caret positioning, custom offset controls.
  4. **Animations:** Timing presets (Keyboard, Very Fast, Natural, Slow), custom durations (fade in, fade out, display), reduce motion toggle.
  5. **Languages:** Automatic layout detection and customizable language display labels.
  6. **Typing Correction:** User-configurable global hotkey recorder, toggle layout fix, direction selection, interactive text test box.
  7. **Advanced:** Regional variant change triggers, focus change triggers, diagnostic logging, open data folder shortcut.
  8. **About & Developers:** Project version, verified developer list loaded from embedded `developers.json`, safe external link handlers, GitHub link.
- Reusable `IndicatorView.xaml` user control shared between live popups and settings preview panels.
- Responsive design adapting below 1080 and 800 DIP width.

### 2. Typing Correction Engine (`LanguageSwitchIndicator.App/Typing/`)
- Bidirectional English (US) ↔ Arabic (101) layout text conversion (`TypingLayoutEngine`).
- Handles complex ligatures (`لا`, `لإ`, `لأ`, `لآ`), inverted punctuation, auto-detects direction, rejects mixed text.
- **Support for Simple Shortcuts:**
  - Standalone function keys: `F1`–`F24` (e.g. `F8`, `F9`, `F10`, `F12`) requiring no modifiers.
  - Two-key combinations: `Ctrl+Q`, `Alt+Q`, `Shift+F8`, `Ctrl+Space`, `Win+Q`.
  - Three-key combinations: `Ctrl+Alt+Q`, `Ctrl+Shift+K`.
  - Rejection of plain typing keys (letters, digits, punctuation) when pressed without a modifier.
  - Rejection of Shift-only with non-function keys (`Shift+A`, `Shift+1`).
  - Rejection of Windows-reserved shortcuts (`Win+L`, `Alt+Tab`, `Alt+F4`, `Ctrl+Alt+Del`, etc.).
  - Default shortcut: `F8`. Upgrades preserve previously saved settings from JSON.
- **Reliable Windows Correction Mechanism:**
  - `GlobalCorrectionHotkey`: Invisible helper window configured with `WS_POPUP` and `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW` so it can never activate, be included in alt-tab, or steal focus.
  - `NativeEditTarget`: Directly supports Win32 `Edit` and `RichEdit` controls (including Windows 11 Notepad `RichEditD2DPT`, classic Notepad, WordPad, WinForms). Uses `WM_GETTEXT`, `EM_GETSEL`, `EM_SETSEL`, and `EM_REPLACESEL` with zero clipboard pollution. Eliminates slow cross-process UIA focus verification that caused previous intermittent failures.
  - `AutomationEditTarget`: Supports WPF and modern text fields using UI Automation (`TextPattern` and `ValuePattern`). Uses `ValuePattern.SetValue` when writable, or selects the target word via `TextPatternRange.Select()` and replaces via `SendInput(KEYEVENTF_UNICODE)` with zero clipboard modification.
  - Word capture immediately before the caret (`PreviousSegment`), preserving surrounding text and trailing whitespace.
  - Caret restoration at the end of the replaced segment.

### 3. Structured 7-Stage Diagnostic Logging
- Implemented in `TypingCorrectionService` and `CorrectionExecutor`:
  - **Stage 1:** Hotkey received (Shortcut name).
  - **Stage 2:** Foreground window detected (HWND, Process ID, Window Class).
  - **Stage 3:** Focused control identified (TargetType: `NativeEditTarget` vs `AutomationEditTarget`).
  - **Stage 4:** Text captured (Total length, selection start, selection length).
  - **Stage 5:** Conversion calculated (Direction, word length, converted length, success).
  - **Stage 6:** Replacement attempted (Method, start, length).
  - **Stage 7:** Outcome (Success, error/success message).
- **Privacy Guaranteed:** Diagnostic logs record only integer lengths, offsets, direction, and class names; user text or passwords are never logged.
- Real-time status update in the Settings UI and floating feedback banner upon any correction error.

### 4. Background Services & Diagnostics
- `StartupService`: Manages Windows Run registry key (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`) without requiring elevation.
- `DiagnosticLog`: Size-capped local diagnostics logger in `%LOCALAPPDATA%\LanguageSwitchIndicator\diagnostics.log`.
- `SettingsEditor`: Two-way data binding with validation and automatic persistence.

---

## Work Completed in This Session

1. **Task 1 — Simple Shortcuts & Default F8:**
   - Updated `HotkeyChord.cs` to allow single function keys (`F1`–`F24`), two-key chords (`Ctrl+Q`, `Alt+Q`, `Shift+F8`, `Ctrl+Space`), and existing chords (`Ctrl+Alt+Q`).
   - Added validation enforcing that plain letters/digits/spaces without modifiers and Shift-only chords with non-function keys are rejected.
   - Updated default shortcut in `SettingsModel.cs` and `HotkeyChord.cs` to `F8` while preserving existing JSON user settings upon deserialization.
   - Updated `SettingsWindow.xaml` and `SettingsWindow.xaml.cs` recorder to validate live shortcut combinations and support resetting to `F8`.
2. **Task 2 — Fixed Real-World Text Correction Pipeline:**
   - Modified `GlobalCorrectionHotkey` HWND creation with `WS_POPUP` and `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW` to eliminate focus disruption.
   - Fixed focused handle resolution with `GetGUIThreadInfo` and `AttachThreadInput` fallback.
   - Decoupled `NativeEditTarget` from slow/unreliable UIA cross-process focus comparisons, fixing Windows 11 Notepad (`RichEditD2DPT`) and classic Notepad.
   - Upgraded `AutomationEditTarget` to handle both `TextPattern` and `ValuePattern` with non-clipboard `SendInput(KEYEVENTF_UNICODE)` replacement.
   - Verified that word segmentation, caret position, and trailing spaces are preserved.
3. **Task 3 — Diagnostics:**
   - Implemented 7-stage diagnostic telemetry in `TypingCorrectionService` and `CorrectionExecutor`.
   - Wired user-facing error messages to UI status and `PopupManager.ShowFeedback`.
4. **Task 4 — Regression Testing & Verification:**
   - Added regression unit tests for `F8`, `F9`, `F12`, `Ctrl+Q`, `Alt+Q`, `Shift+F8`, `Ctrl+Space`, rejection of plain letters, Shift-only letters, reserved hotkeys, and `HotkeyChord.Validate`.
   - Verified Debug test suite: **81 passed, 0 failed**.
   - Verified Release test suite: **81 passed, 0 failed**.
   - Verified `DesignReview` across all 8 settings pages with zero binding errors.

---

## Verification & Manual Testing Steps

### Automated Verification
```powershell
# Run complete test suite (81 tests) in Debug
dotnet test -p:UseSharedCompilation=false --disable-build-servers

# Run complete test suite (81 tests) in Release
dotnet test -c Release -p:UseSharedCompilation=false --disable-build-servers

# Run design review tool across all 8 settings pages
dotnet run --project tools/DesignReview -p:UseSharedCompilation=false --disable-build-servers -- tmp/design-review
```

### Manual Verification in Windows
1. Start the application:
   ```powershell
   dotnet run --project LanguageSwitchIndicator.App
   ```
2. **Test Simple Shortcut Customization:**
   - Open **Settings → Typing correction**.
   - Notice the default shortcut displays `F8`.
   - Click the recorder field; observe `"Press your shortcut…"`.
   - Press a single key (e.g. `F12` or `F9`): shortcut updates immediately to `F12`.
   - Press `Ctrl+Q`: shortcut updates immediately to `Ctrl + Q`.
   - Try pressing a plain letter `A`: observe the validation message: `"Plain letters and typing keys cannot be used alone. Choose a function key (like F8) or combine with Ctrl, Alt, or Win."`
   - Click **Reset to default**: shortcut restores to `F8`.
3. **Test Typing Correction in Applications:**
   - Open Notepad (or any text editor).
   - In English layout, type `;dt;` (or in Arabic layout type `hello`).
   - Press `F8` (or your configured shortcut).
   - Observe that the text immediately converts to the correct layout (`كيفك` / `hello`) without touching the clipboard.
   - Verify the caret remains directly after the converted text and trailing spaces are preserved.
4. **Inspect Diagnostics:**
   - Check `%LOCALAPPDATA%\LanguageSwitchIndicator\diagnostics.log`.
   - Verify Stages 1 through 7 are recorded with HWND, PID, and lengths (no raw user text).
