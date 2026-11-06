using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LanguageSwitchIndicator.Models;
using LanguageSwitchIndicator.Native;
using WpfPoint = System.Windows.Point;

namespace LanguageSwitchIndicator.Views;

public class LanguagePopupWindow : Window
{
    private readonly SettingsModel _settings;
    public IndicatorView Indicator { get; } = new();
    private readonly DispatcherTimer _hideTimer;
    private long _displayVersion;
    private WpfPoint? _targetPosition;

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
        UseLayoutRounding = true;
        Content = Indicator;
        Indicator.Apply(settings, "English");
        Width = Indicator.Width;
        Height = Indicator.Height;
        _hideTimer = new DispatcherTimer();
        _hideTimer.Tick += (_, _) => HidePopup();
        Closed += (_, _) => _hideTimer.Stop();
    }

    public void ShowLanguage(string languageText)
    {
        if (string.IsNullOrWhiteSpace(languageText)) return;
        Indicator.Apply(_settings, languageText);
        Width = Indicator.Width;
        Height = Indicator.Height;
        _hideTimer.Stop();
        _displayVersion++;
        var currentOpacity = IsVisible ? Opacity : 0;
        BeginAnimation(OpacityProperty, null);
        Opacity = 1;
        PositionPopup();
        if (!IsVisible) Show();
        // A presentation source now exists, so physical coordinates can use the actual DPI.
        PositionPopup();
        Indicator.Animate(_settings);
        if (IndicatorView.MotionEnabled(_settings))
            BeginAnimation(OpacityProperty, Fade(currentOpacity, 1, _settings.FadeInDurationMs));
        _hideTimer.Interval = TimeSpan.FromMilliseconds(Math.Clamp(_settings.PopupDurationMs, 100, 10000));
        _hideTimer.Start();
    }

    public void SetPosition(WpfPoint position)
    {
        _targetPosition = position;
        if (IsVisible) PositionPopup();
    }
    public void ClearPosition() => _targetPosition = null;
    public void Dismiss()
    {
        _displayVersion++;
        _hideTimer.Stop();
        BeginAnimation(OpacityProperty, null);
        Hide();
    }
    private void HidePopup()
    {
        _hideTimer.Stop();
        if (!IndicatorView.MotionEnabled(_settings)) { Hide(); return; }
        var version = _displayVersion;
        var fade = Fade(Opacity, 0, _settings.FadeOutDurationMs);
        fade.Completed += (_, _) => { if (version == _displayVersion) Hide(); };
        BeginAnimation(OpacityProperty, fade);
    }
    private DoubleAnimation Fade(double from, double to, double milliseconds) => new(from, to,
        TimeSpan.FromMilliseconds(Math.Clamp(milliseconds / Math.Max(0.25, _settings.AnimationSpeed), 0, 20000)))
    {
        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        FillBehavior = FillBehavior.Stop
    };
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        if (IsVisible) PositionPopup();
    }

    private void PositionPopup()
    {
        // Caret tracking reports physical desktop pixels, including negative monitor coordinates.
        var screen = _targetPosition.HasValue
            ? System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(
                (int)_targetPosition.Value.X, (int)_targetPosition.Value.Y))
            : System.Windows.Forms.Screen.FromHandle(User32.GetForegroundWindow());
        screen ??= System.Windows.Forms.Screen.PrimaryScreen ?? System.Windows.Forms.Screen.AllScreens.FirstOrDefault();
        if (screen == null) return;
        var fromDevice = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice
            ?? Matrix.Identity;
        var bounds = screen.WorkingArea;
        var origin = fromDevice.Transform(new WpfPoint(bounds.Left, bounds.Top));
        var bottomRight = fromDevice.Transform(new WpfPoint(bounds.Right, bounds.Bottom));
        var workArea = new System.Windows.Rect(origin, bottomRight);
        var width = Width;
        var height = Height;
        var left = 0d;
        var top = 0d;

        if (_targetPosition.HasValue && _settings.Placement == PopupPlacement.NearMouse)
        {
            var caret = fromDevice.Transform(_targetPosition.Value);
            left = caret.X + _settings.HorizontalOffset;
            top = caret.Y + _settings.VerticalOffset;
        }
        else switch (_settings.Placement)
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
                var mouse = fromDevice.Transform(new WpfPoint(
                    System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y));
                left = mouse.X + _settings.HorizontalOffset;
                top = mouse.Y + _settings.VerticalOffset;
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

        Left = Math.Clamp(left, workArea.Left, Math.Max(workArea.Left, workArea.Right - width));
        Top = Math.Clamp(top, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - height));
    }

}
