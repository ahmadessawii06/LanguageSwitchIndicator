using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LanguageSwitchIndicator.Models;
using LanguageSwitchIndicator.Services;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfCursors = System.Windows.Input.Cursors;

namespace LanguageSwitchIndicator.Views;

public sealed class SettingsWindow : Window
{
    private static readonly MediaColor PageBackground = MediaColor.FromRgb(246, 248, 252);
    private static readonly MediaColor SidebarBackground = MediaColor.FromRgb(28, 39, 55);
    private static readonly MediaColor Accent = MediaColor.FromRgb(59, 130, 246);
    private static readonly MediaColor BorderColor = MediaColor.FromRgb(218, 224, 233);

    private readonly SettingsService _settingsService;
    private readonly PopupManager _popupManager;
    private readonly InputLanguageMonitor _inputLanguageMonitor;
    private readonly SettingsModel _settings;
    private readonly Dictionary<string, WpfButton> _navigationButtons = new();
    private readonly Dictionary<PopupPlacement, WpfButton> _positionButtons = new();
    private readonly ContentControl _pageHost = new();
    private readonly ScrollViewer _pageScroll = new();
    private Border? _previewBadge;
    private TextBlock? _previewText;

    public SettingsWindow(SettingsService settingsService, PopupManager popupManager, InputLanguageMonitor inputLanguageMonitor, SettingsModel settings)
    {
        _settingsService = settingsService;
        _popupManager = popupManager;
        _inputLanguageMonitor = inputLanguageMonitor;
        _settings = settings;

        Title = "Language Switch Indicator";
        Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/LanguageSwitchIndicator;component/Assets/Icons/LanguageSwitchIndicator.ico"));
        Width = 900;
        Height = 720;
        MinWidth = 640;
        MinHeight = 480;
        ResizeMode = ResizeMode.CanResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(PageBackground);
        FontFamily = new WpfFontFamily("Segoe UI");
        FontSize = 14;

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.Children.Add(BuildSidebar());

        _pageScroll.Padding = new Thickness(36, 30, 36, 36);
        _pageScroll.Background = new SolidColorBrush(PageBackground);
        _pageScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _pageScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        _pageScroll.Content = _pageHost;
        Grid.SetColumn(_pageScroll, 1);
        root.Children.Add(_pageScroll);
        Content = root;

        NavigateTo("General");
        Closing += (_, _) => _settingsService.Save(_settings);
    }

    private Border BuildSidebar()
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = "⌨  Indicator", Foreground = MediaBrushes.White, FontSize = 23, FontWeight = FontWeights.SemiBold, Margin = new Thickness(4, 4, 0, 30) });
        var sections = new[] { ("General", "⚙"), ("Appearance", "◉"), ("Position", "▣"), ("Animations", "✦"), ("Languages", "文"), ("Advanced", "⚒") };
        foreach (var (title, icon) in sections)
        {
            var button = CreateNavigationButton(title, icon);
            _navigationButtons[title] = button;
            stack.Children.Add(button);
        }
        return new Border { Background = new SolidColorBrush(SidebarBackground), Padding = new Thickness(18, 26, 18, 18), Child = stack };
    }

    private WpfButton CreateNavigationButton(string title, string icon)
    {
        var content = new StackPanel { Orientation = WpfOrientation.Horizontal };
        content.Children.Add(new TextBlock { Text = icon, Width = 28, FontSize = 16, VerticalAlignment = VerticalAlignment.Center });
        content.Children.Add(new TextBlock { Text = title, VerticalAlignment = VerticalAlignment.Center });
        var button = new WpfButton
        {
            Content = content,
            Tag = title,
            HorizontalContentAlignment = WpfHorizontalAlignment.Left,
            HorizontalAlignment = WpfHorizontalAlignment.Stretch,
            Foreground = MediaBrushes.White,
            Background = MediaBrushes.Transparent,
            BorderBrush = new SolidColorBrush(Accent),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(12, 11, 10, 11),
            Margin = new Thickness(0, 0, 0, 5),
            FontSize = 14,
            Cursor = WpfCursors.Hand,
            Focusable = true
        };
        button.Click += (_, _) => NavigateTo(title);
        button.MouseEnter += (_, _) => { if ((string?)button.Tag != _activeSection) button.Background = new SolidColorBrush(MediaColor.FromRgb(45, 57, 76)); };
        button.MouseLeave += (_, _) => { if ((string?)button.Tag != _activeSection) button.Background = MediaBrushes.Transparent; };
        return button;
    }

    private string _activeSection = string.Empty;

    private void NavigateTo(string section)
    {
        _activeSection = section;
        foreach (var pair in _navigationButtons)
        {
            var active = pair.Key == section;
            pair.Value.Background = active ? new SolidColorBrush(Accent) : MediaBrushes.Transparent;
            pair.Value.BorderThickness = active ? new Thickness(3, 0, 0, 0) : new Thickness(0);
        }
        _pageHost.Content = section switch
        {
            "Appearance" => BuildAppearancePage(),
            "Position" => BuildPositionPage(),
            "Animations" => BuildAnimationsPage(),
            "Languages" => BuildLanguagesPage(),
            "Advanced" => BuildAdvancedPage(),
            _ => BuildGeneralPage()
        };
        _pageScroll.ScrollToTop();
    }

    private StackPanel CreatePage(string title, string description)
    {
        var page = new StackPanel { MaxWidth = 720 };
        page.Children.Add(new TextBlock { Text = title, FontSize = 28, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(MediaColor.FromRgb(17, 24, 39)), Margin = new Thickness(0, 0, 0, 5) });
        page.Children.Add(new TextBlock { Text = description, Foreground = new SolidColorBrush(MediaColor.FromRgb(107, 114, 128)), Margin = new Thickness(0, 0, 0, 24) });
        return page;
    }

    private StackPanel BuildGeneralPage()
    {
        var page = CreatePage("General", "Control when the language indicator is enabled and displayed.");
        page.Children.Add(CreateCard("Application", "Basic application behavior.",
            CreateCheckBox("Enabled", _settings.Enabled, value => _settings.Enabled = value),
            CreateCheckBox("Launch at startup", _settings.LaunchAtStartup, value => _settings.LaunchAtStartup = value),
            CreateCheckBox("Start minimized", _settings.StartMinimized, value => _settings.StartMinimized = value)));
        page.Children.Add(CreateCard("Indicator behavior", "Choose the events that show the indicator.",
            CreateLabeledRow("Show popup when", CreateComboBox(new[] { "Language changes only", "Caret placed only", "Both" }, (int)_settings.TriggerMode, index =>
            {
                _settings.TriggerMode = (PopupTriggerMode)index;
                _settings.ShowOnlyOnLanguageChange = _settings.TriggerMode == PopupTriggerMode.LanguageChangeOnly;
                _settings.ShowPopupOnTyping = _settings.TriggerMode != PopupTriggerMode.LanguageChangeOnly;
            })),
            CreateCheckBox("Pause indicator", _settings.IsPaused, value => _settings.IsPaused = value)));
        return page;
    }

    private StackPanel BuildAppearancePage()
    {
        var page = CreatePage("Appearance", "Customize the badge and see changes reflected in the preview.");
        var fields = new StackPanel();
        fields.Children.Add(CreateLabeledRow("Indicator size", CreateComboBox(
            new[] { "Extra small", "Small", "Medium", "Large", "Extra large" },
            Math.Clamp(_settings.IndicatorSize, 0, 4), index =>
            {
                _settings.IndicatorSize = index;
                _settingsService.Save(_settings);
                RefreshPreview();
                _popupManager.Preview(_settings.Clone());
            })));
        fields.Children.Add(CreateLabeledRow("Theme", CreateComboBox(new[] { "Dark", "Light", "System" }, ThemeIndex(), index =>
        {
            _settings.Theme = new[] { "Dark", "Light", "System" }[index];
            _settingsService.Save(_settings);
            RefreshPreview();
            _popupManager.Preview(_settings.Clone());
        })));
        fields.Children.Add(CreateColorField("Background color", _settings.BackgroundColor, value => { _settings.BackgroundColor = value; RefreshPreview(); }));
        fields.Children.Add(CreateColorField("Text color", _settings.TextColor, value => { _settings.TextColor = value; RefreshPreview(); }));
        fields.Children.Add(CreateNumericField("Width", _settings.PopupWidth, 60, 1000, "px", value => { _settings.PopupWidth = value; RefreshPreview(); }));
        fields.Children.Add(CreateNumericField("Height", _settings.PopupHeight, 40, 1000, "px", value => { _settings.PopupHeight = value; RefreshPreview(); }));
        fields.Children.Add(CreateNumericField("Font size", _settings.FontSize, 8, 200, "px", value => { _settings.FontSize = value; RefreshPreview(); }));
        var layout = new StackPanel();
        layout.Children.Add(CreateCard("Badge settings", "Colors and dimensions used by the indicator.", fields));
        var preview = CreatePreviewCard();
        layout.Children.Add(preview);
        page.Children.Add(layout);
        page.Children.Add(CreatePreviewButton("Preview popup"));
        return page;
    }

    private Border CreatePreviewCard()
    {
        _previewBadge = new Border { Width = 180, Height = 110, CornerRadius = new CornerRadius(18), HorizontalAlignment = WpfHorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Child = new Grid() };
        var panel = (Grid)_previewBadge.Child;
        _previewText = new TextBlock { Text = "English", FontWeight = FontWeights.SemiBold, HorizontalAlignment = WpfHorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(_previewText);
        var card = new Border { MinHeight = 250, Margin = new Thickness(0, 0, 0, 18), Padding = new Thickness(16), Background = MediaBrushes.White, BorderBrush = new SolidColorBrush(BorderColor), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10) };
        var content = new StackPanel();
        content.Children.Add(new TextBlock { Text = "Live preview", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10) });
        content.Children.Add(_previewBadge);
        content.Children.Add(new TextBlock { Text = "English  /  العربية", HorizontalAlignment = WpfHorizontalAlignment.Center, Foreground = new SolidColorBrush(MediaColor.FromRgb(107, 114, 128)), Margin = new Thickness(0, 10, 0, 0) });
        card.Child = content;
        RefreshPreview();
        return card;
    }

    private StackPanel BuildPositionPage()
    {
        var page = CreatePage("Position", "Choose a supported screen position for the indicator.");
        page.Children.Add(CreateCard("Screen position", "Select a location from the screen map.", BuildPositionMap()));
        page.Children.Add(CreateLabeledRow("Other placement", CreateComboBox(new[] { "Near mouse", "Custom" }, _settings.Placement == PopupPlacement.NearMouse ? 0 : 1, index => _settings.Placement = index == 0 ? PopupPlacement.NearMouse : PopupPlacement.Custom)));
        page.Children.Add(CreatePreviewButton("Preview popup"));
        return page;
    }

    private FrameworkElement BuildPositionMap()
    {
        var grid = new Grid { MaxWidth = 520, Height = 210 };
        for (var i = 0; i < 3; i++) { grid.RowDefinitions.Add(new RowDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition()); }
        var positions = new[] { (PopupPlacement.TopLeft, "Top left", 0, 0), (PopupPlacement.TopCenter, "Top center", 0, 1), (PopupPlacement.TopRight, "Top right", 0, 2), (PopupPlacement.Center, "Center", 1, 1), (PopupPlacement.BottomLeft, "Bottom left", 2, 0), (PopupPlacement.BottomCenter, "Bottom center", 2, 1), (PopupPlacement.BottomRight, "Bottom right", 2, 2) };
        foreach (var (placement, label, row, column) in positions)
        {
            var button = new WpfButton { Content = label, Tag = placement, Margin = new Thickness(4), Padding = new Thickness(4), FontSize = 12, BorderBrush = new SolidColorBrush(BorderColor) };
            button.Click += (_, _) => { _settings.Placement = placement; RefreshPositionMap(); };
            Grid.SetRow(button, row); Grid.SetColumn(button, column); grid.Children.Add(button); _positionButtons[placement] = button;
        }
        RefreshPositionMap();
        return new Border { Height = 250, Padding = new Thickness(20), Background = new SolidColorBrush(MediaColor.FromRgb(250, 251, 253)), BorderBrush = new SolidColorBrush(BorderColor), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Child = grid };
    }

    private void RefreshPositionMap()
    {
        foreach (var pair in _positionButtons) pair.Value.Background = pair.Key == _settings.Placement ? new SolidColorBrush(MediaColor.FromRgb(219, 234, 254)) : MediaBrushes.White;
    }

    private StackPanel BuildAnimationsPage()
    {
        var page = CreatePage("Animations", "Tune how long the indicator stays visible and how it enters and exits.");
        var names = new List<string> { "Custom", "Keyboard", "Very fast", "Natural", "Slow" };
        var preset = new WpfComboBox { ItemsSource = names, SelectedIndex = 0, Width = 320, Height = 32 };
        for (var i = 0; i < AnimationPreset.All.Count; i++) if (AnimationPreset.All[i].Matches(_settings)) preset.SelectedIndex = i + 1;
        preset.SelectionChanged += (_, _) => { if (preset.SelectedIndex > 0) { AnimationPreset.All[preset.SelectedIndex - 1].Apply(_settings); _settingsService.Save(_settings); } };
        page.Children.Add(CreateCard("Animation timing", "Use a preset or edit the values directly.", CreateLabeledRow("Animation preset", preset), CreateNumericField("Popup duration", _settings.PopupDurationMs, 100, 10000, "ms", value => { _settings.PopupDurationMs = value; preset.SelectedIndex = 0; }), CreateNumericField("Fade in", _settings.FadeInDurationMs, 0, 5000, "ms", value => { _settings.FadeInDurationMs = value; preset.SelectedIndex = 0; }), CreateNumericField("Fade out", _settings.FadeOutDurationMs, 0, 5000, "ms", value => { _settings.FadeOutDurationMs = value; preset.SelectedIndex = 0; })));
        page.Children.Add(CreatePreviewButton("Preview animation"));
        return page;
    }

    private StackPanel BuildLanguagesPage()
    {
        var page = CreatePage("Languages", "Keyboard languages are detected automatically while you type.");
        var detected = _inputLanguageMonitor.GetCurrentDisplayText();
        page.Children.Add(CreateCard("Keyboard language detection", "The indicator follows the active Windows keyboard layout. No typed text or keystrokes are collected.", new TextBlock { Text = $"Current detected language: {detected}", Foreground = new SolidColorBrush(MediaColor.FromRgb(55, 65, 81)), Margin = new Thickness(0, 4, 0, 0) }));
        return page;
    }

    private StackPanel BuildAdvancedPage()
    {
        var page = CreatePage("Advanced", "Optional diagnostics for troubleshooting the indicator.");
        page.Children.Add(CreateCard("Diagnostics", "Logging can help investigate focus and keyboard layout detection.", CreateCheckBox("Enable debug logging", _settings.DebugLogging, value => _settings.DebugLogging = value)));
        return page;
    }

    private Border CreateCard(string title, string description, params FrameworkElement[] children)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = title, FontSize = 17, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
        panel.Children.Add(new TextBlock { Text = description, Foreground = new SolidColorBrush(MediaColor.FromRgb(107, 114, 128)), Margin = new Thickness(0, 0, 0, 16) });
        foreach (var child in children) panel.Children.Add(child);
        return new Border { Background = MediaBrushes.White, BorderBrush = new SolidColorBrush(BorderColor), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(22), Margin = new Thickness(0, 0, 0, 18), Child = panel };
    }

    private WpfButton CreatePreviewButton(string text)
    {
        var button = new WpfButton { Content = text, Width = 150, Height = 36, Padding = new Thickness(10, 5, 10, 5), HorizontalAlignment = WpfHorizontalAlignment.Left, Background = new SolidColorBrush(Accent), Foreground = MediaBrushes.White, BorderThickness = new Thickness(0), Cursor = WpfCursors.Hand };
        button.Click += (_, _) => _popupManager.Preview(_settings.Clone());
        return button;
    }

    private void RefreshPreview()
    {
        if (_previewBadge == null || _previewText == null) return;
        try
        {
            _previewBadge.Background = new SolidColorBrush((MediaColor)WpfColorConverter.ConvertFromString(_settings.BackgroundColor)!);
            _previewText.Foreground = new SolidColorBrush((MediaColor)WpfColorConverter.ConvertFromString(_settings.TextColor)!);
        }
        catch { }
        _previewBadge.Width = Math.Clamp(_settings.PopupWidth, 100, 220);
        _previewBadge.Height = Math.Clamp(_settings.PopupHeight, 60, 150);
        _previewText.FontSize = Math.Clamp(_settings.FontSize * 0.55, 14, 34);
        _previewBadge.Width *= _settings.IndicatorScale;
        _previewBadge.Height *= _settings.IndicatorScale;
        _previewText.FontSize *= _settings.IndicatorScale;
        _previewBadge.Background = IndicatorPalette.Background(_settings);
        _previewText.Foreground = IndicatorPalette.Foreground(_settings);
    }

    private static WpfCheckBox CreateCheckBox(string title, bool value, Action<bool> onChanged)
    {
        var box = new WpfCheckBox { Content = title, IsChecked = value, Margin = new Thickness(0, 0, 0, 12), Padding = new Thickness(3), Focusable = true };
        box.Checked += (_, _) => onChanged(true); box.Unchecked += (_, _) => onChanged(false); return box;
    }

    private static StackPanel CreateLabeledRow(string label, FrameworkElement control)
    {
        var row = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
        row.Children.Add(new TextBlock { Text = label, Foreground = new SolidColorBrush(MediaColor.FromRgb(55, 65, 81)), Margin = new Thickness(0, 0, 0, 5) }); row.Children.Add(control); return row;
    }

    private static WpfComboBox CreateComboBox(IReadOnlyList<string> items, int selectedIndex, Action<int> onChanged)
    {
        var combo = new WpfComboBox { ItemsSource = items, SelectedIndex = selectedIndex, Width = 320, Height = 34, Padding = new Thickness(8, 4, 8, 4) };
        combo.SelectionChanged += (_, _) => { if (combo.SelectedIndex >= 0) onChanged(combo.SelectedIndex); }; return combo;
    }

    private StackPanel CreateColorField(string label, string initialValue, Action<string> onChanged)
    {
        var row = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
        row.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 5) });
        var line = new Grid { Width = 320 }; line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) }); line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var swatch = new Border { Width = 22, Height = 22, Margin = new Thickness(0, 6, 8, 0), BorderBrush = new SolidColorBrush(BorderColor), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4) };
        var box = new WpfTextBox { Text = initialValue, Height = 34, Padding = new Thickness(8, 5, 8, 5) }; var error = CreateErrorText();
        Grid.SetColumn(swatch, 0); Grid.SetColumn(box, 1); line.Children.Add(swatch); line.Children.Add(box); row.Children.Add(line); row.Children.Add(error);
        void Validate() { try { swatch.Background = new SolidColorBrush((MediaColor)WpfColorConverter.ConvertFromString(box.Text)!); error.Visibility = Visibility.Collapsed; onChanged(box.Text); } catch { error.Text = "Enter a valid HEX color, for example #252525."; error.Visibility = Visibility.Visible; } }
        Validate(); box.TextChanged += (_, _) => Validate(); return row;
    }

    private static StackPanel CreateNumericField(string label, double initialValue, double minimum, double maximum, string unit, Action<double> onChanged)
    {
        var row = new StackPanel { Margin = new Thickness(0, 0, 0, 14) }; row.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 5) });
        var line = new StackPanel { Orientation = WpfOrientation.Horizontal }; var box = new WpfTextBox { Text = initialValue.ToString(CultureInfo.CurrentCulture), Width = 280, Height = 34, Padding = new Thickness(8, 5, 8, 5) }; var error = CreateErrorText();
        line.Children.Add(box); line.Children.Add(new TextBlock { Text = unit, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(MediaColor.FromRgb(107, 114, 128)) }); row.Children.Add(line); row.Children.Add(error);
        box.TextChanged += (_, _) => { if (double.TryParse(box.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var value) && double.IsFinite(value) && value >= minimum && value <= maximum) { error.Visibility = Visibility.Collapsed; onChanged(value); } else { error.Text = $"Enter a value from {minimum:0} to {maximum:0}."; error.Visibility = Visibility.Visible; } }; return row;
    }

    private static TextBlock CreateErrorText() => new() { Foreground = new SolidColorBrush(MediaColor.FromRgb(185, 28, 28)), Margin = new Thickness(0, 4, 0, 0), Visibility = Visibility.Collapsed };
    private int ThemeIndex() => _settings.Theme switch { "Light" => 1, "System" => 2, _ => 0 };
}
