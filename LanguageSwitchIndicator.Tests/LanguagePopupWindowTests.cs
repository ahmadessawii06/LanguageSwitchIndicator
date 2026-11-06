using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Threading;
using LanguageSwitchIndicator.Models;
using LanguageSwitchIndicator.Views;

namespace LanguageSwitchIndicator.Tests;

public class LanguagePopupWindowTests
{
    [Fact]
    public void Popup_AllSizesFitContent_AndThemeUpdatesOnExistingWindow()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            LanguagePopupWindow? popup = null;
            try
            {
                var settings = new SettingsModel { FadeInDurationMs = 0 };
                popup = new LanguagePopupWindow(settings);
                double previousWidth = 0;
                for (var size = 0; size < 5; size++)
                {
                    settings.IndicatorSize = size;
                    popup.ShowLanguage("EN");
                    popup.UpdateLayout();
                    var border = (System.Windows.Controls.Border)popup.Content;
                    var content = (System.Windows.Controls.StackPanel)border.Child;
                    var label = (System.Windows.Controls.TextBlock)content.Children[1];
                    Assert.Null(border.LayoutTransform as System.Windows.Media.ScaleTransform);
                    Assert.True(popup.Width > previousWidth);
                    Assert.True(content.ActualHeight + border.Padding.Top + border.Padding.Bottom
                        + border.BorderThickness.Top + border.BorderThickness.Bottom <= border.ActualHeight + 1);
                    Assert.True(label.ActualHeight >= label.FontSize);
                    previousWidth = popup.Width;
                }
                settings.Theme = "Light";
                popup.ShowLanguage("EN");
                var badge = (System.Windows.Controls.Border)popup.Content;
                var light = (System.Windows.Media.LinearGradientBrush)badge.Background;
                Assert.True(light.GradientStops[0].Color.R > 200);
                settings.Theme = "Dark";
                popup.ShowLanguage("EN");
                var dark = (System.Windows.Media.LinearGradientBrush)badge.Background;
                Assert.True(dark.GradientStops[0].Color.R < 100);
            }
            catch (Exception exception) { failure = exception; }
            finally
            {
                popup?.Close();
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Popup test timed out.");
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    [Fact]
    public void Popup_RemainsOpaqueAfterFadeIn_AndRenewsDuringFadeOut()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            LanguagePopupWindow? popup = null;
            try
            {
                popup = new LanguagePopupWindow(new SettingsModel
                {
                    FadeInDurationMs = 30,
                    PopupDurationMs = 500,
                    FadeOutDurationMs = 400
                });

                popup.ShowLanguage("English");
                PumpFor(200);
                Assert.True(popup.IsVisible);
                Assert.Equal(1, popup.Opacity, precision: 2);

                // Renew while the first display is fading out.
                PumpFor(450);
                popup.ShowLanguage("العربية");
                PumpFor(400);
                Assert.True(popup.IsVisible);
                Assert.Equal(1, popup.Opacity, precision: 2);

                PumpFor(700);
                Assert.False(popup.IsVisible);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                popup?.Close();
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Popup test timed out.");
        if (failure != null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static void PumpFor(int milliseconds)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(milliseconds)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            frame.Continue = false;
        };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }
}
