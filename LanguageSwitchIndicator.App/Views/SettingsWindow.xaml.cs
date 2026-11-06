using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LanguageSwitchIndicator.Models;
using LanguageSwitchIndicator.Services;
using Button = System.Windows.Controls.Button;
using ListBox = System.Windows.Controls.ListBox;
using Brushes = System.Windows.Media.Brushes;
using LanguageSwitchIndicator.Typing;
using System.Windows.Input;

namespace LanguageSwitchIndicator.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsEditor _editor;
    private readonly PopupManager _popupManager;
    private readonly InputLanguageMonitor? _monitor;
    private readonly DispatcherTimer _previewTimer = new();
    private readonly DispatcherTimer _languageTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private string _example = "English";
    private bool _initializing = true;
    private bool _applyingPreset;
    private string _section = "General";
    private readonly TypingCorrectionService? _correction;
    private readonly GitHubProfileService _profileService = new();
    private bool _profileLoadedFromNetwork;
    public SettingsWindow(SettingsService service, PopupManager popupManager, InputLanguageMonitor? monitor, SettingsModel settings, TypingCorrectionService? correction = null)
    {
        InitializeComponent();
        AppThemeChoice.ItemsSource = new[] { "System", "Dark", "Light" };
        AppThemeService.ApplyTheme(settings.AppTheme, this);
        foreach (var item in Navigation.Items.OfType<ListBoxItem>())
            System.Windows.Automation.AutomationProperties.SetName(item, item.Tag?.ToString() ?? "Settings page");
        _editor = new SettingsEditor(settings, service);
        _popupManager = popupManager;
        _monitor = monitor;
        _correction = correction;
        CorrectionDirectionChoice.ItemsSource = new Dictionary<CorrectionDirection, string> { [CorrectionDirection.Auto] = "Auto · refuse mixed text", [CorrectionDirection.EnglishToArabic] = "English → Arabic", [CorrectionDirection.ArabicToEnglish] = "Arabic → English" };
        CorrectionEnabledSwitch.IsChecked = settings.TypingCorrectionEnabled;
        ShortcutDisplay.Text = HotkeyChord.FormatForDisplay(settings.TypingCorrectionHotkey);
        CorrectionShortcutStatus.Text = correction?.Status ?? "Start the tray application to register the global shortcut.";
        if (correction != null) correction.StatusChanged += CorrectionStatusChanged;
        Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/LanguageSwitchIndicator;component/Assets/Icons/LanguageSwitchIndicator.ico"));
        TriggerChoice.ItemsSource = new Dictionary<PopupTriggerMode, string> { [PopupTriggerMode.LanguageChangeOnly] = "When the keyboard language changes", [PopupTriggerMode.CaretPlacedOnly] = "When I enter a text field", [PopupTriggerMode.Both] = "Language changes & entering a text field" };
        SizeChoice.ItemsSource = new[] { "Extra small", "Small", "Medium", "Large", "Extra large" };
        ThemeChoice.ItemsSource = new[] { "Dark", "Light", "System" };
        PlacementChoice.ItemsSource = new Dictionary<PopupPlacement, string> { [PopupPlacement.Center] = "Center", [PopupPlacement.TopCenter] = "Top center", [PopupPlacement.BottomCenter] = "Bottom center", [PopupPlacement.TopLeft] = "Top left", [PopupPlacement.TopRight] = "Top right", [PopupPlacement.BottomLeft] = "Bottom left", [PopupPlacement.BottomRight] = "Bottom right", [PopupPlacement.NearMouse] = "Beside the text caret", [PopupPlacement.Custom] = "Custom offset from top left" };
        AnimationChoice.ItemsSource = new Dictionary<AnimationStyle, string> { [AnimationStyle.Fade] = "Fade", [AnimationStyle.FadeScale] = "Fade & scale", [AnimationStyle.SlideUp] = "Slide up", [AnimationStyle.SlideDown] = "Slide down", [AnimationStyle.None] = "Instant" };
        PresetChoice.ItemsSource = new[] { "Custom", "Keyboard", "Very fast", "Natural", "Slow" };
        DataContext = _editor;
        PresetChoice.SelectedIndex = MatchingPreset();
        VersionLabel.Text = $"Version {ProductInfo.Version}  ·  Windows desktop";
        ApplyDeveloperProfile(_profileService.LoadCachedProfile());
        _editor.Changed += SettingsChanged;
        _previewTimer.Tick += (_, _) => { _previewTimer.Stop(); MotionIndicator.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(IndicatorView.MotionEnabled(settings) ? settings.FadeOutDurationMs / Math.Max(.25, settings.AnimationSpeed) : 0))); };
        _languageTimer.Tick += (_, _) => RefreshDetectedLanguage();
        _languageTimer.Start();
        _initializing = false;
        Navigation.SelectedIndex = 0;
        RefreshPreview();
        SizeChanged += (_, _) => UpdateResponsiveLayout();
        Loaded += (_, _) => UpdateResponsiveLayout();
        Closed += (_, _) => { if (_isRecordingShortcut) CancelRecordingShortcut(); _previewTimer.Stop(); _languageTimer.Stop(); if (_correction != null) _correction.StatusChanged -= CorrectionStatusChanged; _editor.Persist(); };
    }
    private int MatchingPreset()
    {
        for (var i = 0; i < AnimationPreset.All.Count; i++) if (AnimationPreset.All[i].Matches(_editor.Settings)) return i + 1;
        return 0;
    }
    public void RefreshSettings() => _editor.NotifyAll();
    private void SettingsChanged()
    {
        AppThemeService.ApplyTheme(_editor.AppTheme, this);
        RefreshPreview();
        if (!_applyingPreset) { _applyingPreset = true; PresetChoice.SelectedIndex = MatchingPreset(); _applyingPreset = false; }
        if (!_editor.Settings.Enabled || _editor.Settings.IsPaused) _popupManager.Dismiss();
    }
    private void RefreshPreview()
    {
        LiveIndicator.Apply(_editor.Settings, _example);
        MotionIndicator.Apply(_editor.Settings, _example);
        foreach (var child in PositionMap.Children.OfType<Button>())
        {
            var selected = child.Tag?.ToString() == _editor.Placement.ToString();
            child.Background = selected ? (System.Windows.Media.Brush)FindResource("Accent") : (System.Windows.Media.Brush)FindResource("MonitorButtonBg");
            child.BorderBrush = selected ? (System.Windows.Media.Brush)FindResource("AccentBorder") : (System.Windows.Media.Brush)FindResource("MonitorButtonBorder");
            child.BorderThickness = selected ? new Thickness(2) : new Thickness(1);
            child.Opacity = selected ? 1.0 : 0.65;
            System.Windows.Automation.AutomationProperties.SetHelpText(child, selected ? "Current position" : "Select indicator position");
        }
    }
    private void UpdateResponsiveLayout()
    {
        var effectiveWidth = ActualWidth > 0 ? ActualWidth : (double.IsNaN(Width) ? 1180 : Width);
        var wide = effectiveWidth >= 960;
        SidebarColumn.Width = new GridLength(effectiveWidth < 800 ? 184 : 224);
        PageScroll.Padding = new Thickness(effectiveWidth < 800 ? 20 : 32, 24, effectiveWidth < 800 ? 20 : 32, 24);
        EditorColumn.Width = new GridLength(1, GridUnitType.Star);
        PreviewColumn.Width = wide ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        Grid.SetColumn(PreviewPanel, wide ? 1 : 0);
        Grid.SetRow(PreviewPanel, wide ? 0 : 0);
        Grid.SetRow(AppearanceEditor, wide ? 0 : 1);
        PreviewPanel.Margin = wide ? new Thickness(20, 0, 0, 0) : new Thickness(0, 0, 0, 20);
    }
    private void NavigationChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || Navigation.SelectedItem is not ListBoxItem item) return;
        NavigateTo(item.Tag!.ToString()!);
    }
    public void NavigateTo(string section)
    {
        _section = section;
        var pages = new[] { GeneralPage, AppearancePage, PositionPage, AnimationsPage, LanguagesPage, CorrectionPage, AdvancedPage, AboutPage };
        var names = new[] { "General", "Appearance", "Position", "Animations", "Languages", "Typing correction", "Advanced", "About" };
        for (var i = 0; i < pages.Length; i++) pages[i].Visibility = names[i] == section ? Visibility.Visible : Visibility.Collapsed;
        PageTitle.Text = section == "About" ? "About the Developer" : section;
        PageDescription.Text = section switch
        {
            "Appearance" => "A familiar cue. A look that is entirely yours.",
            "Position" => "Put clarity exactly where you need it.",
            "Animations" => "A gentle entrance. A perfectly timed exit.",
            "Languages" => "Different languages. One seamless flow.",
            "Typing correction" => "Keep the words. Fix the keyboard layout.",
            "Advanced" => "A few extra controls, when you need them.",
            "About" => "Meet the developer behind Language Switch Indicator.",
            _ => "Your language, always in view. Make the indicator work your way."
        };
        AboutButton.Background = section == "About" ? (System.Windows.Media.Brush)FindResource("AccentSoft") : (System.Windows.Media.Brush)FindResource("SidebarBackground");
        AboutButton.BorderBrush = section == "About" ? (System.Windows.Media.Brush)FindResource("AccentBorder") : (System.Windows.Media.Brush)FindResource("Line");
        PageScroll.ScrollToTop();
        if (section == "Languages") { RefreshDetectedLanguage(); PopulateLanguages(); }
        if (section == "About") { _ = RefreshDeveloperProfileAsync(isUserInitiated: false); }
        if (!_editor.ReduceMotion && SystemParameters.ClientAreaAnimation)
            PageRoot.BeginAnimation(OpacityProperty, new DoubleAnimation(.65, 1, TimeSpan.FromMilliseconds(140)));
    }
    private void AboutClicked(object sender, RoutedEventArgs e) { Navigation.SelectedIndex = -1; NavigateTo("About"); }
    private void TestSwitchClicked(object sender, RoutedEventArgs e)
    {
        _example = _example == "English" ? "عربي" : "English";
        RefreshPreview();
        LiveIndicator.Animate(_editor.Settings);
        if (IndicatorView.MotionEnabled(_editor.Settings)) LiveIndicator.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(_editor.FadeInDurationMs / _editor.AnimationSpeed)));
    }
    private void PositionClicked(object sender, RoutedEventArgs e) => _editor.Placement = Enum.Parse<PopupPlacement>(((Button)sender).Tag.ToString()!);
    private void PopupPreviewClicked(object sender, RoutedEventArgs e) => _popupManager.Preview(_editor.Settings.Clone());
    private void PlayAnimationClicked(object sender, RoutedEventArgs e)
    {
        _previewTimer.Stop();
        MotionIndicator.BeginAnimation(OpacityProperty, null);
        MotionIndicator.Opacity = 1;
        MotionIndicator.Animate(_editor.Settings);
        if (IndicatorView.MotionEnabled(_editor.Settings)) MotionIndicator.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(_editor.FadeInDurationMs / _editor.AnimationSpeed)));
        _previewTimer.Interval = TimeSpan.FromMilliseconds(_editor.PopupDurationMs);
        _previewTimer.Start();
    }
    private void PresetChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || _applyingPreset || PresetChoice.SelectedIndex <= 0) return;
        _applyingPreset = true;
        AnimationPreset.All[PresetChoice.SelectedIndex - 1].Apply(_editor.Settings);
        _editor.NotifyAll();
        _applyingPreset = false;
    }
    private void RefreshDetectedLanguage()
    {
        if (_section == "Languages") DetectedLanguage.Text = _monitor?.GetCurrentDisplayText() ?? "Detected automatically";
    }
    private void PopulateLanguages()
    {
        var languages = System.Windows.Forms.InputLanguage.InstalledInputLanguages.Cast<System.Windows.Forms.InputLanguage>()
            .Select(language => new LanguageMapping
            {
                InputLanguageId = language.Culture.Name,
                Label = _editor.Settings.LanguageMappings.FirstOrDefault(x => x.InputLanguageId == language.Culture.Name)?.Label
                    ?? (language.Culture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase) ? "عربي" : language.Culture.EnglishName)
            }).ToList();
        LanguageList.ItemsSource = languages;
    }
    private void LanguageSelected(object sender, SelectionChangedEventArgs e)
    {
        LanguageLabelEditor.IsEnabled = LanguageList.SelectedItem is LanguageMapping;
        LanguageLabelEditor.Text = (LanguageList.SelectedItem as LanguageMapping)?.Label ?? "";
    }
    private void SaveLanguageClicked(object sender, RoutedEventArgs e)
    {
        if (LanguageList.SelectedItem is not LanguageMapping selected) { LanguageMessage.Text = "Select a keyboard language first."; return; }
        var label = LanguageLabelEditor.Text.Trim();
        if (label.Length is < 1 or > 32) { LanguageMessage.Text = "Use a label between 1 and 32 characters."; return; }
        lock (_editor.Settings.LanguageMappings)
        {
            var existing = _editor.Settings.LanguageMappings.FirstOrDefault(x => x.InputLanguageId == selected.InputLanguageId);
            if (existing == null) { existing = new LanguageMapping { InputLanguageId = selected.InputLanguageId }; _editor.Settings.LanguageMappings.Add(existing); }
            existing.Label = label;
            existing.DisplayMode = LanguageDisplayMode.CustomLabel;
        }
        _editor.NotifyAll();
        LanguageMessage.Text = "Label saved. It will appear on your next language switch.";
        PopulateLanguages();
    }
    private void ApplyDeveloperProfile(GitHubProfile profile)
    {
        DeveloperName.Text = profile.Name;
        DeveloperUsername.Text = $"@{profile.Login}";
        DeveloperBio.Text = profile.Bio;
        StatRepos.Text = profile.PublicRepos.ToString();
        StatFollowers.Text = profile.Followers.ToString();
        StatFollowing.Text = profile.Following.ToString();

        if (!string.IsNullOrWhiteSpace(profile.Company))
        {
            DeveloperCompany.Text = profile.Company;
            CompanyTag.Visibility = Visibility.Visible;
        }
        else
        {
            CompanyTag.Visibility = Visibility.Collapsed;
        }

        if (!string.IsNullOrWhiteSpace(profile.Location))
        {
            DeveloperLocation.Text = profile.Location;
            LocationTag.Visibility = Visibility.Visible;
        }
        else
        {
            LocationTag.Visibility = Visibility.Collapsed;
        }

        LoadAvatarImage(profile);
    }

    private void LoadAvatarImage(GitHubProfile profile)
    {
        try
        {
            if (!string.IsNullOrEmpty(profile.LocalAvatarPath) && File.Exists(profile.LocalAvatarPath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(profile.LocalAvatarPath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                DeveloperAvatar.Source = bitmap;
                return;
            }

            if (!string.IsNullOrEmpty(profile.AvatarUrl) && Uri.TryCreate(profile.AvatarUrl, UriKind.Absolute, out var avatarUri))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = avatarUri;
                bitmap.EndInit();
                DeveloperAvatar.Source = bitmap;
            }
        }
        catch
        {
            // Fallback initials AE remain visible behind
        }
    }

    private async Task RefreshDeveloperProfileAsync(bool isUserInitiated = false)
    {
        if (!isUserInitiated && _profileLoadedFromNetwork) return;
        try
        {
            ProfileSyncStatus.Text = "Updating profile from GitHub...";
            var profile = await _profileService.FetchProfileAsync();
            _profileLoadedFromNetwork = true;
            Dispatcher.Invoke(() =>
            {
                ApplyDeveloperProfile(profile);
                ProfileSyncStatus.Text = "Live profile from GitHub";
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                ProfileSyncStatus.Text = isUserInitiated ? "Unable to connect to GitHub. Showing cached profile." : "Offline mode · Cached profile";
            });
            DiagnosticLog.Write($"GitHub profile fetch notice: {ex.Message}");
        }
    }

    private void ViewGitHubProfileClicked(object sender, RoutedEventArgs e)
    {
        OpenLink("https://github.com/ahmadessawii06");
    }

    private async void RefreshProfileClicked(object sender, RoutedEventArgs e)
    {
        await RefreshDeveloperProfileAsync(isUserInitiated: true);
    }

    private static bool IsTrustedLink(string? link) => Uri.TryCreate(link, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo);
    private void OpenLink(string url)
    {
        if (!IsTrustedLink(url)) return;
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException) { LinkStatus.Text = "Could not open the browser. Please try again."; LinkStatus.Visibility = Visibility.Visible; }
    }
    private void RepositoryClicked(object sender, RoutedEventArgs e) => OpenLink(ProductInfo.Repository);
    private void OpenDataClicked(object sender, RoutedEventArgs e)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LanguageSwitchIndicator");
        Directory.CreateDirectory(path);
        try { Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true }); }
        catch (System.ComponentModel.Win32Exception) { }
    }
    private bool _isRecordingShortcut;

    private void CorrectionStatusChanged()
    {
        CorrectionShortcutStatus.Text = _correction!.Status;
        CorrectionEnabledSwitch.IsChecked = _editor.Settings.TypingCorrectionEnabled;
        if (!_isRecordingShortcut)
        {
            ShortcutDisplay.Text = HotkeyChord.FormatForDisplay(_editor.Settings.TypingCorrectionHotkey);
        }
    }
    private void CorrectionEnabledClicked(object sender, RoutedEventArgs e)
    {
        if (_correction == null) { CorrectionEnabledSwitch.IsChecked = _editor.Settings.TypingCorrectionEnabled; return; }
        _correction.Apply(CorrectionEnabledSwitch.IsChecked == true, _editor.Settings.TypingCorrectionHotkey);
    }
    private void ShortcutRecorderMouseDown(object sender, MouseButtonEventArgs e)
    {
        StartRecordingShortcut();
        e.Handled = true;
    }
    private void StartRecordingShortcut()
    {
        if (_isRecordingShortcut) return;
        _isRecordingShortcut = true;
        if (_correction != null) _correction.IsSuspended = true;

        ShortcutRecorderBox.BorderBrush = (System.Windows.Media.Brush)FindResource("Accent");
        ShortcutRecorderBox.BorderThickness = new Thickness(2);
        ShortcutRecorderBox.Background = (System.Windows.Media.Brush)FindResource("AccentSoft");
        ShortcutIcon.Foreground = (System.Windows.Media.Brush)FindResource("Accent");
        RecordingDot.Visibility = Visibility.Visible;
        RecordingBadge.Visibility = Visibility.Visible;

        ShortcutDisplay.Text = "Press your shortcut…";
        ShortcutDisplay.Foreground = (System.Windows.Media.Brush)FindResource("Accent");
        CorrectionShortcutStatus.Text = "Press your combination (Ctrl, Alt, or Win + another key). Press Escape to cancel.";
        ShortcutRecorderBox.Focus();
    }
    private void CancelRecordingShortcut(string? message = null)
    {
        if (!_isRecordingShortcut) return;
        _isRecordingShortcut = false;
        if (_correction != null) _correction.IsSuspended = false;

        StopRecordingVisuals();
        ShortcutDisplay.Text = HotkeyChord.FormatForDisplay(_editor.Settings.TypingCorrectionHotkey);
        ShortcutDisplay.Foreground = (System.Windows.Media.Brush)FindResource("Ink");
        CorrectionShortcutStatus.Text = message ?? (_correction?.Status ?? $"Active: {ShortcutDisplay.Text}");
    }
    private void StopRecordingVisuals()
    {
        ShortcutRecorderBox.BorderBrush = (System.Windows.Media.Brush)FindResource("Line");
        ShortcutRecorderBox.BorderThickness = new Thickness(1);
        ShortcutRecorderBox.Background = (System.Windows.Media.Brush)FindResource("InputBackground");
        ShortcutIcon.Foreground = (System.Windows.Media.Brush)FindResource("Muted");
        RecordingDot.Visibility = Visibility.Collapsed;
        RecordingBadge.Visibility = Visibility.Collapsed;
    }
    private void ShortcutRecorderLostFocus(object sender, RoutedEventArgs e)
    {
        if (_isRecordingShortcut)
        {
            CancelRecordingShortcut();
        }
    }
    private void ShortcutRecorderKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_isRecordingShortcut)
        {
            if (e.Key is Key.Enter or Key.Space)
            {
                StartRecordingShortcut();
                e.Handled = true;
            }
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key == Key.Escape)
        {
            CancelRecordingShortcut("Recording cancelled.");
            e.Handled = true;
            return;
        }

        if (key == Key.Tab)
        {
            CancelRecordingShortcut();
            return;
        }

        var modifiers = Keyboard.Modifiers;
        var winDown = Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin);

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            ShortcutDisplay.Text = HotkeyChord.FormatModifiersPreview(modifiers, winDown);
            e.Handled = true;
            return;
        }

        var chord = HotkeyChord.FromKey(key, modifiers, winDown);
        if (chord == null)
        {
            CorrectionShortcutStatus.Text = "Key not supported. Use a letter, number, or function key.";
            e.Handled = true;
            return;
        }

        if (!HotkeyChord.Validate(chord, out var validationError))
        {
            CorrectionShortcutStatus.Text = validationError;
            ShortcutDisplay.Text = HotkeyChord.FormatModifiersPreview(modifiers, winDown);
            e.Handled = true;
            return;
        }

        ApplyRecordedShortcut(chord);
        e.Handled = true;
    }
    private void ShortcutRecorderKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_isRecordingShortcut) return;
        var modifiers = Keyboard.Modifiers;
        var winDown = Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin);
        ShortcutDisplay.Text = HotkeyChord.FormatModifiersPreview(modifiers, winDown);
    }
    private void ApplyRecordedShortcut(string chord)
    {
        _isRecordingShortcut = false;
        if (_correction != null) _correction.IsSuspended = false;
        StopRecordingVisuals();

        var previousShortcut = _editor.Settings.TypingCorrectionHotkey;
        var enabled = CorrectionEnabledSwitch.IsChecked == true;

        if (_correction != null)
        {
            var success = _correction.Apply(enabled, chord);
            if (success)
            {
                _editor.Settings.TypingCorrectionHotkey = chord;
                _editor.Persist();
                ShortcutDisplay.Text = HotkeyChord.FormatForDisplay(chord);
                ShortcutDisplay.Foreground = (System.Windows.Media.Brush)FindResource("Ink");
                CorrectionShortcutStatus.Text = _correction.Status;
            }
            else
            {
                ShortcutDisplay.Text = HotkeyChord.FormatForDisplay(previousShortcut);
                ShortcutDisplay.Foreground = (System.Windows.Media.Brush)FindResource("Ink");
                CorrectionShortcutStatus.Text = _correction.Status;
            }
        }
        else
        {
            _editor.Settings.TypingCorrectionHotkey = chord;
            _editor.Persist();
            ShortcutDisplay.Text = HotkeyChord.FormatForDisplay(chord);
            ShortcutDisplay.Foreground = (System.Windows.Media.Brush)FindResource("Ink");
            CorrectionShortcutStatus.Text = $"Shortcut saved: {ShortcutDisplay.Text}. Start the tray application to register.";
        }
    }
    private void ResetShortcutClicked(object sender, RoutedEventArgs e)
    {
        if (_isRecordingShortcut)
        {
            _isRecordingShortcut = false;
            if (_correction != null) _correction.IsSuspended = false;
            StopRecordingVisuals();
        }

        var defaultShortcut = HotkeyChord.DefaultShortcut;
        var enabled = CorrectionEnabledSwitch.IsChecked == true;
        _editor.Settings.TypingCorrectionHotkey = defaultShortcut;
        _editor.Persist();

        if (_correction != null)
        {
            _correction.Apply(enabled, defaultShortcut);
            CorrectionShortcutStatus.Text = _correction.Status;
        }
        else
        {
            CorrectionShortcutStatus.Text = $"Restored default shortcut ({defaultShortcut}).";
        }

        ShortcutDisplay.Text = HotkeyChord.FormatForDisplay(defaultShortcut);
        ShortcutDisplay.Foreground = (System.Windows.Media.Brush)FindResource("Ink");
    }
    private void FixSampleClicked(object sender, RoutedEventArgs e)
    {
        var outcome = CorrectionExecutor.Execute(new SampleCorrectionTarget(CorrectionDemo), _editor.Settings.TypingCorrectionDirection, "00000409");
        CorrectionDemoStatus.Text = outcome.Message;
        CorrectionDemo.Focus();
    }
    private sealed class SampleCorrectionTarget(System.Windows.Controls.TextBox box) : ICorrectionTarget
    {
        public TextSnapshot Capture() => new(box.Text, box.SelectionStart, box.SelectionLength);
        public bool IsCurrent(TextSnapshot snapshot) => box.Text == snapshot.Text && box.SelectionStart == snapshot.SelectionStart && box.SelectionLength == snapshot.SelectionLength;
        public bool Replace(TextSnapshot snapshot, int start, int length, string replacement)
        {
            if (!IsCurrent(snapshot)) return false;
            box.BeginChange();
            try { box.Select(start, length); box.SelectedText = replacement; box.Select(start + replacement.Length, 0); }
            finally { box.EndChange(); }
            return box.Text == snapshot.Text[..start] + replacement + snapshot.Text[(start + length)..];
        }
    }
}
