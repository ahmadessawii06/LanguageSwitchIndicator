using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LanguageSwitchIndicator.Models;

namespace LanguageSwitchIndicator.Views;

public class LanguagePopupWindow : Window
{
    private readonly SettingsModel _settings;
    private readonly Border _border;
    private readonly TextBlock _label;
    private readonly DispatcherTimer _hideTimer;
    private long _displayVersion;
    private readonly ScaleTransform _scale = new(1, 1);

    public LanguagePopupWindow(SettingsModel settings)
    {
        _settings = settings;

        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        Focusable = false;
        IsHitTestVisible = false;
        SizeToContent = SizeToContent.Manual;
        // Reserve transparent space so the soft shadow is not clipped.
        Width = Math.Max(150, settings.PopupWidth) + 48;
        Height = Math.Max(140, settings.PopupHeight) + 48;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;

        _label = new TextBlock
        {
            Text = "EN",
            Foreground = new SolidColorBrush(System.Windows.Media.Colors.White),
            FontSize = Math.Clamp(settings.FontSize * 0.6, 16, 26),
            FontFamily = new System.Windows.Media.FontFamily(settings.FontFamily),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = System.Windows.TextWrapping.NoWrap,
            FontWeight = FontWeights.Medium,
            Margin = new Thickness(4, 13, 4, 0),
            FlowDirection = System.Windows.FlowDirection.LeftToRight,
        };

        var content = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
        };
        content.Children.Add(CreateKeyboardIcon());
        content.Children.Add(_label);

        _border = new Border
        {
            Margin = new Thickness(24),
            CornerRadius = new CornerRadius(Math.Max(22, settings.CornerRadius)),
            Background = new LinearGradientBrush(
                System.Windows.Media.Color.FromArgb((byte)(255 * Math.Clamp(settings.BackgroundOpacity, 0, 1)), 63, 64, 68),
                System.Windows.Media.Color.FromArgb((byte)(255 * Math.Clamp(settings.BackgroundOpacity, 0, 1)), 35, 36, 40), 90),
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(48, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12),
            Child = content,
            RenderTransformOrigin = new System.Windows.Point(0.5, 0.5),
            RenderTransform = _scale,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                ShadowDepth = 6,
                BlurRadius = 22,
                Opacity = Math.Clamp(settings.ShadowIntensity * 0.6, 0, 1),
                Color = Colors.Black
            }
        };

        Content = _border;
        Left = 0;
        Top = 0;

        _hideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(settings.PopupDurationMs)
        };
        _hideTimer.Tick += (_, _) => HidePopup();
    }

    public void ShowLanguage(string languageText)
    {
        if (string.IsNullOrWhiteSpace(languageText))
        {
            return;
        }

        _label.Text = languageText;
        _hideTimer.Stop();
        var wasVisible = IsVisible;
        var currentOpacity = wasVisible ? Opacity : 0;
        _displayVersion++;

        // Remove a previous fade-out before renewing the display period.
        BeginAnimation(OpacityProperty, null);
        Opacity = 1;

        if (!IsVisible)
        {
            if (_settings.AnimationStyle == AnimationStyle.FadeScale)
            {
                var scale = CreateOpacityAnimation(0.92, 1, _settings.FadeInDurationMs);
                _scale.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
                _scale.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
            }
            BeginAnimation(OpacityProperty, CreateOpacityAnimation(0, 1, _settings.FadeInDurationMs));
            PositionPopup();
            Show();
        }
        else
        {
            PositionPopup();
            BeginAnimation(OpacityProperty, CreateOpacityAnimation(currentOpacity, 1, _settings.FadeInDurationMs));
        }

        _hideTimer.Interval = TimeSpan.FromMilliseconds(_settings.PopupDurationMs);
        _hideTimer.Start();
    }

    private static Border CreateKeyboardIcon()
    {
        var keys = new Grid { Margin = new Thickness(5) };
        for (var column = 0; column < 10; column++)
            keys.ColumnDefinitions.Add(new ColumnDefinition());
        for (var row = 0; row < 4; row++)
            keys.RowDefinitions.Add(new RowDefinition());

        for (var row = 0; row < 4; row++)
        {
            for (var column = 0; column < 10; column++)
            {
                if (row == 3 && column > 1 && column < 8 && column != 2)
                    continue;
                var key = new Border
                {
                    Margin = new Thickness(1.2),
                    CornerRadius = new CornerRadius(1),
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 235, 240))
                };
                Grid.SetRow(key, row);
                Grid.SetColumn(key, column);
                if (row == 3 && column == 2)
                    Grid.SetColumnSpan(key, 6);
                keys.Children.Add(key);
            }
        }

        return new Border
        {
            Width = 78,
            Height = 48,
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(2),
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 235, 240)),
            Child = keys
        };
    }

    private void HidePopup()
    {
        _hideTimer.Stop();
        var version = _displayVersion;
        var fade = CreateOpacityAnimation(Opacity, 0, _settings.FadeOutDurationMs);
        fade.Completed += (_, _) =>
        {
            if (version == _displayVersion)
            {
                Hide();
            }
        };
        BeginAnimation(OpacityProperty, fade);
    }

    private void PositionPopup()
    {
        var workArea = SystemParameters.WorkArea;
        var width = Width;
        var height = Height;
        var left = 0d;
        var top = 0d;

        switch (_settings.Placement)
        {
            case PopupPlacement.TopCenter:
                left = workArea.Left + (workArea.Width - width) / 2 + _settings.HorizontalOffset;
                top = workArea.Top + 30 + _settings.VerticalOffset;
                break;
            case PopupPlacement.BottomCenter:
                left = workArea.Left + (workArea.Width - width) / 2 + _settings.HorizontalOffset;
                top = workArea.Bottom - height - 30 + _settings.VerticalOffset;
                break;
            case PopupPlacement.TopLeft:
                left = workArea.Left + 24 + _settings.HorizontalOffset;
                top = workArea.Top + 24 + _settings.VerticalOffset;
                break;
            case PopupPlacement.TopRight:
                left = workArea.Right - width - 24 + _settings.HorizontalOffset;
                top = workArea.Top + 24 + _settings.VerticalOffset;
                break;
            case PopupPlacement.BottomLeft:
                left = workArea.Left + 24 + _settings.HorizontalOffset;
                top = workArea.Bottom - height - 24 + _settings.VerticalOffset;
                break;
            case PopupPlacement.BottomRight:
                left = workArea.Right - width - 24 + _settings.HorizontalOffset;
                top = workArea.Bottom - height - 24 + _settings.VerticalOffset;
                break;
            case PopupPlacement.NearMouse:
                left = System.Windows.Forms.Cursor.Position.X + _settings.HorizontalOffset;
                top = System.Windows.Forms.Cursor.Position.Y + _settings.VerticalOffset;
                break;
            case PopupPlacement.Custom:
                left = workArea.Left + _settings.HorizontalOffset;
                top = workArea.Top + _settings.VerticalOffset;
                break;
            default:
                left = workArea.Left + (workArea.Width - width) / 2 + _settings.HorizontalOffset;
                top = workArea.Top + (workArea.Height - height) / 2 + _settings.VerticalOffset;
                break;
        }

        Left = left;
        Top = top;
    }

    private static DoubleAnimation CreateOpacityAnimation(double from, double to, double durationMs)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop
        };
        return animation;
    }
}
