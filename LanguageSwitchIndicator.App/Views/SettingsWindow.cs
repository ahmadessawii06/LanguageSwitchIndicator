using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LanguageSwitchIndicator.Models;
using LanguageSwitchIndicator.Services;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;

namespace LanguageSwitchIndicator.Views;

public class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly PopupManager _popupManager;
    private readonly InputLanguageMonitor _inputLanguageMonitor;
    private readonly SettingsModel _settings;
    private readonly Dictionary<string, FrameworkElement> _sectionHeaders = new();

    public SettingsWindow(SettingsService settingsService, PopupManager popupManager, InputLanguageMonitor inputLanguageMonitor, SettingsModel settings)
    {
        _settingsService = settingsService;
        _popupManager = popupManager;
        _inputLanguageMonitor = inputLanguageMonitor;
        _settings = settings;

        Title = "Language Switch Indicator";
        Icon = System.Windows.Media.Imaging.BitmapFrame.Create(
            new Uri("pack://application:,,,/LanguageSwitchIndicator;component/Assets/Icons/LanguageSwitchIndicator.ico"));
        Width = 760;
        Height = 720;
        ResizeMode = ResizeMode.CanMinimize;
        Background = new SolidColorBrush(MediaColor.FromRgb(244, 246, 250));
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var root = new System.Windows.Controls.DockPanel();
        var sidebar = new Border
        {
            Width = 190,
            Background = new SolidColorBrush(MediaColor.FromRgb(31, 41, 55)),
            Padding = new Thickness(16),
            Child = new System.Windows.Controls.StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Indicator", Foreground = MediaBrushes.White, FontSize = 25, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 24) },
                    CreateNavigationButton("General", true),
                    CreateNavigationButton("Appearance"),
                    CreateNavigationButton("Position"),
                    CreateNavigationButton("Animations"),
                    CreateNavigationButton("Languages"),
                    CreateNavigationButton("Advanced")
                }
            }
        };

        var content = new System.Windows.Controls.ScrollViewer
        {
            Padding = new Thickness(30),
            Background = new SolidColorBrush(MediaColor.FromRgb(244, 246, 250)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = BuildSettingsContent()
        };

        root.Children.Add(sidebar);
        root.Children.Add(content);
        DockPanel.SetDock(sidebar, Dock.Left);
        Content = root;

        Closing += (_, _) =>
        {
            _settingsService.Save(_settings);
        };
    }

    private System.Windows.Controls.StackPanel BuildSettingsContent()
    {
        var section = new System.Windows.Controls.StackPanel { Margin = new Thickness(0, 10, 0, 0) };

        section.Children.Add(CreateSectionHeader("General"));
        section.Children.Add(CreateCheckBox("Enabled", _settings.Enabled, value => _settings.Enabled = value));
        section.Children.Add(CreateCheckBox("Launch at startup", _settings.LaunchAtStartup, value => _settings.LaunchAtStartup = value));
        section.Children.Add(CreateCheckBox("Start minimized", _settings.StartMinimized, value => _settings.StartMinimized = value));
        section.Children.Add(CreateTriggerModeSelector());
        section.Children.Add(CreateCheckBox("Pause indicator", _settings.IsPaused, value => _settings.IsPaused = value));

        section.Children.Add(CreateSectionHeader("Appearance"));
        section.Children.Add(CreateTextField("Theme", _settings.Theme, value => _settings.Theme = value));
        section.Children.Add(CreateTextField("Background color", _settings.BackgroundColor, value => _settings.BackgroundColor = value));
        section.Children.Add(CreateTextField("Text color", _settings.TextColor, value => _settings.TextColor = value));
        section.Children.Add(CreateDoubleField("Width", _settings.PopupWidth, value => _settings.PopupWidth = value));
        section.Children.Add(CreateDoubleField("Height", _settings.PopupHeight, value => _settings.PopupHeight = value));
        section.Children.Add(CreateDoubleField("Font size", _settings.FontSize, value => _settings.FontSize = value));

        section.Children.Add(CreateSectionHeader("Position"));
        var placement = new System.Windows.Controls.ComboBox
        {
            Width = 220,
            SelectedItem = _settings.Placement.ToString(),
            ItemsSource = new[] { "Center", "TopCenter", "BottomCenter", "TopLeft", "TopRight", "BottomLeft", "BottomRight", "NearMouse", "Custom" }
        };
        placement.SelectionChanged += (_, _) =>
        {
            if (placement.SelectedItem is string item)
            {
                if (Enum.TryParse<PopupPlacement>(item, out var placementValue))
                {
                    _settings.Placement = placementValue;
                }
            }
        };
        section.Children.Add(new System.Windows.Controls.StackPanel
        {
            Margin = new Thickness(0, 0, 0, 8),
            Children =
            {
                new TextBlock { Text = "Popup placement" },
                placement
            }
        });

        section.Children.Add(CreateSectionHeader("Animations"));
        section.Children.Add(BuildAnimationOptions());

        section.Children.Add(CreateSectionHeader("Languages"));
        section.Children.Add(new TextBlock
        {
            Text = "Keyboard languages are detected automatically while you type.",
            Margin = new Thickness(0, 0, 0, 12),
            Foreground = System.Windows.Media.Brushes.DimGray
        });

        section.Children.Add(CreateSectionHeader("Advanced"));
        section.Children.Add(CreateCheckBox("Enable debug logging", _settings.DebugLogging, value => _settings.DebugLogging = value));

        var previewButton = new System.Windows.Controls.Button
        {
            Content = "Preview popup",
            Margin = new Thickness(0, 18, 0, 0),
            Padding = new Thickness(8, 6, 8, 6),
            Width = 130,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
        };
        previewButton.Click += (_, _) => _popupManager.ShowLanguage("EN");
        section.Children.Add(previewButton);

        return section;
    }

    private System.Windows.Controls.StackPanel CreateTriggerModeSelector()
    {
        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(0, 4, 0, 12) };
        panel.Children.Add(new TextBlock
        {
            Text = "Show popup when",
            FontSize = 14,
            FontWeight = FontWeights.Medium,
            Margin = new Thickness(0, 0, 0, 5)
        });

        var selector = new System.Windows.Controls.ComboBox
        {
            Width = 300,
            ItemsSource = new[] { "Language changes only", "Caret placed only", "Both" },
            SelectedIndex = (int)_settings.TriggerMode
        };
        selector.SelectionChanged += (_, _) =>
        {
            if (selector.SelectedIndex < 0)
            {
                return;
            }

            _settings.TriggerMode = (PopupTriggerMode)selector.SelectedIndex;
            _settings.ShowOnlyOnLanguageChange = _settings.TriggerMode == PopupTriggerMode.LanguageChangeOnly;
            _settings.ShowPopupOnTyping = _settings.TriggerMode != PopupTriggerMode.LanguageChangeOnly;
            _settingsService.Save(_settings);
        };
        panel.Children.Add(selector);
        return panel;
    }

    private TextBlock CreateSectionHeader(string title)
    {
        var header = new TextBlock
        {
            Text = title,
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, title == "General" ? 0 : 18, 0, 12)
        };
        _sectionHeaders[title] = header;
        return header;
    }

    private void ScrollToSection(string title)
    {
        if (_sectionHeaders.TryGetValue(title, out var header))
        {
            header.BringIntoView();
        }
    }

    private System.Windows.Controls.Button CreateNavigationButton(string title, bool selected = false)
    {
        var button = new System.Windows.Controls.Button
        {
            Content = title,
            HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            Foreground = MediaBrushes.White,
            Background = selected ? new SolidColorBrush(MediaColor.FromRgb(59, 130, 246)) : MediaBrushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(14, 9, 10, 9),
            Margin = new Thickness(0, 0, 0, 6),
            FontSize = 14
        };
        button.MouseEnter += (_, _) => button.Background = new SolidColorBrush(MediaColor.FromRgb(55, 65, 81));
        button.MouseLeave += (_, _) => button.Background = selected ? new SolidColorBrush(MediaColor.FromRgb(59, 130, 246)) : MediaBrushes.Transparent;
        button.Click += (_, _) => ScrollToSection(title);
        return button;
    }

    private System.Windows.Controls.StackPanel BuildAnimationOptions()
    {
        var panel = new System.Windows.Controls.StackPanel();
        panel.Children.Add(new TextBlock { Text = "خيارات الحركة الجاهزة", Margin = new Thickness(0, 0, 0, 6) });
        var names = new List<string> { "مخصص" };
        names.AddRange(AnimationPreset.All.Select(preset => preset.Name));
        var presets = new System.Windows.Controls.ComboBox
        {
            Width = 220,
            ItemsSource = names,
            SelectedIndex = 0,
            Margin = new Thickness(0, 0, 0, 12)
        };
        for (var i = 0; i < AnimationPreset.All.Count; i++)
            if (AnimationPreset.All[i].Matches(_settings))
                presets.SelectedIndex = i + 1;
        panel.Children.Add(presets);
        var timing = new System.Windows.Controls.StackPanel();
        panel.Children.Add(timing);

        void ManualChange(Action change)
        {
            change();
            presets.SelectedIndex = 0;
        }

        void RefreshTiming()
        {
            timing.Children.Clear();
            timing.Children.Add(CreateDoubleField("Popup duration (ms)", _settings.PopupDurationMs, value =>
            {
                if (double.IsFinite(value) && value >= 100 && value <= 10000)
                    ManualChange(() => _settings.PopupDurationMs = value);
            }));
            timing.Children.Add(CreateDoubleField("Fade in (ms)", _settings.FadeInDurationMs, value =>
            {
                if (double.IsFinite(value) && value >= 0 && value <= 5000)
                    ManualChange(() => _settings.FadeInDurationMs = value);
            }));
            timing.Children.Add(CreateDoubleField("Fade out (ms)", _settings.FadeOutDurationMs, value =>
            {
                if (double.IsFinite(value) && value >= 0 && value <= 5000)
                    ManualChange(() => _settings.FadeOutDurationMs = value);
            }));
        }

        RefreshTiming();
        presets.SelectionChanged += (_, _) =>
        {
            if (presets.SelectedIndex <= 0)
                return;
            AnimationPreset.All[presets.SelectedIndex - 1].Apply(_settings);
            RefreshTiming();
            _settingsService.Save(_settings);
            _popupManager.Preview();
        };
        return panel;
    }

    private static System.Windows.Controls.CheckBox CreateCheckBox(string title, bool value, Action<bool> onChanged)
    {
        var checkBox = new System.Windows.Controls.CheckBox
        {
            Content = title,
            IsChecked = value,
            Margin = new Thickness(0, 2, 0, 8),
            FontSize = 14
        };
        checkBox.Checked += (_, _) => onChanged(true);
        checkBox.Unchecked += (_, _) => onChanged(false);
        return checkBox;
    }

    private static System.Windows.Controls.StackPanel CreateTextField(string title, string initialValue, Action<string> onChanged)
    {
        var stack = new System.Windows.Controls.StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        stack.Children.Add(new TextBlock { Text = title, Margin = new Thickness(0, 0, 0, 4) });
        var textBox = new System.Windows.Controls.TextBox { Width = 220, Text = initialValue };
        textBox.TextChanged += (_, _) => onChanged(textBox.Text);
        stack.Children.Add(textBox);
        return stack;
    }

    private static System.Windows.Controls.StackPanel CreateDoubleField(string title, double initialValue, Action<double> onChanged)
    {
        var stack = new System.Windows.Controls.StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        stack.Children.Add(new TextBlock { Text = title, Margin = new Thickness(0, 0, 0, 4) });
        var textBox = new System.Windows.Controls.TextBox { Width = 220, Text = initialValue.ToString() };
        textBox.TextChanged += (_, _) =>
        {
            if (double.TryParse(textBox.Text, out var value))
            {
                onChanged(value);
            }
        };
        stack.Children.Add(textBox);
        return stack;
    }
}
