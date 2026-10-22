using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Threading;
using LanguageSwitchIndicator.Models;
using LanguageSwitchIndicator.Views;

namespace LanguageSwitchIndicator.Tests;

public class LanguagePopupWindowTests
{
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
