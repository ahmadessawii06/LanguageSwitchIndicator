using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using LanguageSwitchIndicator.Models;

namespace LanguageSwitchIndicator.Views;

public partial class IndicatorView : System.Windows.Controls.UserControl
{
    public IndicatorView()
    {
        InitializeComponent();
        for (var column = 0; column < 10; column++) KeyboardKeys.ColumnDefinitions.Add(new ColumnDefinition());
        for (var row = 0; row < 4; row++) KeyboardKeys.RowDefinitions.Add(new RowDefinition());
        for (var row = 0; row < 4; row++)
        for (var column = 0; column < 10; column++)
        {
            if (row == 3 && column > 2 && column < 8) continue;
            var key = new Border();
            Grid.SetRow(key, row);
            Grid.SetColumn(key, column);
            if (row == 3 && column == 2) Grid.SetColumnSpan(key, 6);
            KeyboardKeys.Children.Add(key);
        }
    }
    public void Apply(SettingsModel settings, string text)
    {
        var scale = settings.IndicatorScale;
        LanguageLabel.Text = text;
        LanguageLabel.FontFamily = new System.Windows.Media.FontFamily(settings.FontFamily);
        LanguageLabel.FontSize = Math.Clamp(settings.FontSize * 0.6, 8, 120) * scale;
        Keyboard.Width = 78 * scale;
        Keyboard.Height = 48 * scale;
        Keyboard.CornerRadius = new CornerRadius(6 * scale);
        Keyboard.BorderThickness = new Thickness(2 * scale);
        KeyboardKeys.Margin = new Thickness(5 * scale);
        Badge.Padding = new Thickness(16 * scale);
        Badge.CornerRadius = new CornerRadius(Math.Max(22, settings.CornerRadius) * scale);
        LanguageLabel.Margin = new Thickness(4 * scale, 13 * scale, 4 * scale, 0);
        var content = (StackPanel)Badge.Child;
        content.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        Width = Math.Max(Math.Max(150, settings.PopupWidth) * scale,
            MeasureLabel() + 32 * scale + 2) + 48;
        Height = Math.Max(Math.Max(140, settings.PopupHeight) * scale,
            content.DesiredSize.Height + 32 * scale + 4) + 48;
        Badge.Background = IndicatorPalette.Background(settings);
        var foreground = IndicatorPalette.Foreground(settings);
        LanguageLabel.Foreground = Keyboard.BorderBrush = foreground;
        foreach (Border key in KeyboardKeys.Children)
        {
            key.Background = foreground;
            key.Margin = new Thickness(1.2 * scale);
            key.CornerRadius = new CornerRadius(scale);
        }
        Badge.BorderBrush = new SolidColorBrush(IndicatorPalette.IsLight(settings)
            ? System.Windows.Media.Color.FromArgb(35, 0, 0, 0) : System.Windows.Media.Color.FromArgb(45, 255, 255, 255));
        ((DropShadowEffect)Badge.Effect).Opacity = Math.Clamp(settings.ShadowIntensity * 0.6, 0, 1);
    }
    private double MeasureLabel()
    {
        LanguageLabel.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        return LanguageLabel.DesiredSize.Width;
    }
    public static bool MotionEnabled(SettingsModel settings) => !settings.ReduceMotion && SystemParameters.ClientAreaAnimation && settings.AnimationStyle != AnimationStyle.None;
    public void Animate(SettingsModel settings)
    {
        BadgeScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        BadgeScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        BadgeTranslation.BeginAnimation(TranslateTransform.YProperty, null);
        if (!MotionEnabled(settings)) return;
        var time = TimeSpan.FromMilliseconds(settings.FadeInDurationMs / Math.Max(0.25, settings.AnimationSpeed));
        if (settings.AnimationStyle == AnimationStyle.FadeScale)
        {
            var animation = new DoubleAnimation(0.92, 1, time) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            BadgeScale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            BadgeScale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }
        if (settings.AnimationStyle is AnimationStyle.SlideUp or AnimationStyle.SlideDown)
            BadgeTranslation.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(settings.AnimationStyle == AnimationStyle.SlideUp ? 12 : -12, 0, time) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
    }
}
