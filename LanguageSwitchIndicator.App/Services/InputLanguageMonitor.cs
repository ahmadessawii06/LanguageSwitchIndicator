using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using LanguageSwitchIndicator.Models;
using LanguageSwitchIndicator.Native;

namespace LanguageSwitchIndicator.Services;

public class InputLanguageChangedEventArgs : EventArgs
{
    public string LanguageId { get; }
    public string Label { get; }
    public string DisplayText { get; }

    public InputLanguageChangedEventArgs(string languageId, string label, string displayText)
    {
        LanguageId = languageId;
        Label = label;
        DisplayText = displayText;
    }
}

public class InputLanguageMonitor : IDisposable
{
    private readonly object _lock = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _monitorTask;
    private string _lastLanguageId = string.Empty;
    private bool _disposed;

    public event EventHandler<InputLanguageChangedEventArgs>? LanguageChanged;

    public InputLanguageMonitor()
    {
        _monitorTask = Task.Run(() => MonitorLoopAsync(_cts.Token));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Cancel();
        _monitorTask.Wait(250);
        _cts.Dispose();
    }

    private async Task MonitorLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var current = GetCurrentLanguageId();
                if (!string.IsNullOrWhiteSpace(current))
                {
                    lock (_lock)
                    {
                        if (!string.Equals(_lastLanguageId, current, StringComparison.OrdinalIgnoreCase))
                        {
                            _lastLanguageId = current;
                            var language = Culturize(current);
                            LanguageChanged?.Invoke(this, new InputLanguageChangedEventArgs(
                                current,
                                language.Name,
                                GetLabel(current, language)));
                        }
                    }
                }

                await Task.Delay(80, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(150, cancellationToken);
            }
        }
    }

    private static CultureInfo Culturize(string languageId)
    {
        try
        {
            if (languageId.Contains("-", StringComparison.Ordinal))
            {
                return CultureInfo.GetCultureInfo(languageId);
            }

            return CultureInfo.GetCultureInfo(languageId);
        }
        catch
        {
            return CultureInfo.InvariantCulture;
        }
    }

    private static string GetLabel(string languageId, CultureInfo culture)
    {
        if (languageId.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
        {
            return "عربي";
        }

        if (languageId.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return "English";
        }

        return culture.TwoLetterISOLanguageName.ToUpperInvariant();
    }

    private static string GetCurrentLanguageId()
    {
        try
        {
            var foregroundWindow = User32.GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                return string.Empty;
            }

            var threadId = User32.GetWindowThreadProcessId(foregroundWindow, out _);
            if (threadId == 0)
            {
                return string.Empty;
            }

            var layout = User32.GetKeyboardLayout(threadId);
            if (layout == IntPtr.Zero)
            {
                return string.Empty;
            }

            var layoutId = (int)(layout.ToInt64() & 0xFFFF);
            return ResolveLanguageByLayoutId(layoutId);
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string ResolveLanguageByLayoutId(int layoutId)
    {
        return layoutId switch
        {
            0x0409 => "en-US",
            0x0401 => "ar-SA",
            0x040C => "fr-FR",
            0x0407 => "de-DE",
            _ => "en-US"
        };
    }

    private static string GuessLanguageByLayoutHex(string hex)
    {
        if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var layoutId))
        {
            return ResolveLanguageByLayoutId(layoutId);
        }

        return "en-US";
    }
}
