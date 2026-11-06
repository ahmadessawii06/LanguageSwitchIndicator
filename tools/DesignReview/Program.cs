using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using LanguageSwitchIndicator.Models;
using LanguageSwitchIndicator.Services;
using LanguageSwitchIndicator.Views;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--typing-fixture")) { RunTypingFixture(); return; }
        var folder = Path.GetFullPath(args.FirstOrDefault(x => !x.StartsWith("--")) ?? "tmp/design-review");
        Directory.CreateDirectory(folder);
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/LanguageSwitchIndicator;component/Themes/Product.xaml") });
        var settings = new SettingsModel { ReduceMotion = true };
        var service = new SettingsService(Path.Combine(folder, "review-settings.json"));
        var manager = new PopupManager(settings);
        var window = new SettingsWindow(service, manager, null, settings);
        var trace = new StringWriter();
        PresentationTraceSources.DataBindingSource.Listeners.Add(new TextWriterTraceListener(trace));
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        var live = args.Contains("--live");
        if (live) { window.Show(); Pump(); }
        foreach (var width in new[] { 1180, 700, 620 })
        {
            window.Width = width;
            window.Height = 820;
            var root = (FrameworkElement)window.Content;
            root.Measure(new Size(width, 780));
            root.Arrange(new Rect(0, 0, width, 780));
            root.UpdateLayout();
            window.GetType().GetMethod("UpdateResponsiveLayout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(window, null);
            foreach (var page in new[] { "General", "Appearance", "Position", "Animations", "Languages", "Typing correction", "Advanced", "About" })
            {
                window.NavigateTo(page);
                var navigation = (ListBox)window.FindName("Navigation");
                navigation.SelectedItem = navigation.Items.Cast<ListBoxItem>().FirstOrDefault(x => x.Tag?.ToString() == page);
                if (live) Pump();
                Capture(root, width, 780, 1, Path.Combine(folder, $"{page.ToLowerInvariant()}-{width}.png"));
                if (page is "General" or "Appearance" or "About")
                {
                    Capture(root, width, 780, 1.25, Path.Combine(folder, $"{page.ToLowerInvariant()}-{width}-125.png"));
                    Capture(root, width, 780, 1.5, Path.Combine(folder, $"{page.ToLowerInvariant()}-{width}-150.png"));
                }
                if (width == 620)
                {
                    var scroll = (ScrollViewer)window.FindName("PageScroll");
                    scroll.ScrollToEnd();
                    Pump();
                    Capture(root, width, 780, 1, Path.Combine(folder, $"{page.ToLowerInvariant()}-{width}-scrolled.png"));
                }
            }
        }
        // Exercise bound editing, validation, immediate persistence, and reopening.
        window.NavigateTo("Appearance");
        var editor = (SettingsEditor)window.DataContext;
        editor.IndicatorSize = 4;
        editor.Theme = "Light";
        editor.FontSize = 34;
        editor.AppTheme = "Dark";
        Pump();
        var saved = service.Load();
        if (saved.IndicatorSize != 4 || saved.Theme != "Light" || saved.FontSize != 34 || saved.AppTheme != "Dark") throw new Exception("Settings did not persist.");
        
        // Capture dark mode samples
        var rootDark = (FrameworkElement)window.Content;
        window.NavigateTo("Appearance");
        Pump();
        Capture(rootDark, 1180, 780, 1.25, Path.Combine(folder, "dark-appearance.png"));
        window.NavigateTo("Position");
        Pump();
        Capture(rootDark, 1180, 780, 1.25, Path.Combine(folder, "dark-position.png"));
        window.NavigateTo("General");
        Pump();
        Capture(rootDark, 1180, 780, 1.25, Path.Combine(folder, "dark-general.png"));
        window.NavigateTo("Appearance");
        var fontBox = Descendants((DependencyObject)window.Content).OfType<TextBox>().First(x => System.Windows.Automation.AutomationProperties.GetName(x) == "Text size");
        fontBox.Text = "not a number";
        fontBox.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        if (!Validation.GetHasError(fontBox)) throw new Exception("Invalid number was not reported.");
        if (service.Load().FontSize != 34) throw new Exception("Invalid input replaced saved data.");
        fontBox.Text = "34";
        fontBox.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        if (live)
        {
            window.NavigateTo("General");
            var focusable = Descendants(window).OfType<Control>().First(x => x.IsVisible && x.Focusable && x.IsEnabled);
            focusable.Focus();
            if (!window.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next))) throw new Exception("Keyboard traversal failed.");
            var trigger = (ComboBox)window.FindName("TriggerChoice");
            trigger.IsDropDownOpen = true;
            Pump();
            if (!trigger.IsDropDownOpen) throw new Exception("Dropdown did not open.");
            trigger.SelectedIndex = 0;
            trigger.IsDropDownOpen = false;
            if (service.Load().TriggerMode != PopupTriggerMode.LanguageChangeOnly) throw new Exception("Dropdown selection did not persist.");
            using var tray = new TrayIconService(() => { }, () => { }, () => { }, () => { });
            tray.Show();
            manager.Preview(settings.Clone());
            Pump();
            if (!Application.Current.Windows.OfType<LanguagePopupWindow>().Any(x => x.IsVisible)) throw new Exception("Popup did not show.");
            manager.Dismiss();
            var preview = (IndicatorView)window.FindName("LiveIndicator");
            var oldLabel = ((TextBlock)preview.FindName("LanguageLabel")).Text;
            window.GetType().GetMethod("TestSwitchClicked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(window, new object[] { window, new RoutedEventArgs() });
            if (((TextBlock)preview.FindName("LanguageLabel")).Text == oldLabel) throw new Exception("Test switch did not update the preview.");
            if (Application.Current.Windows.OfType<LanguagePopupWindow>().Any(x => x.IsVisible)) throw new Exception("Inline preview displayed a system popup.");
            Console.WriteLine("Live window, keyboard traversal, tray creation, and popup display passed.");
        }
        window.Close();
        var reopened = new SettingsWindow(service, manager, null, service.Load());
        if (((SettingsEditor)reopened.DataContext).FontSize != 34) throw new Exception("Reopening did not restore settings.");
        reopened.Close();
        if (live && args.Contains("--typing-check")) CheckTyping(manager, settings);
        if (args.Contains("--export-docs"))
        {
            var docsImages = Path.GetFullPath("docs/images");
            ExportDocsImages(window, docsImages);
            RenderThemesImage(Path.Combine(docsImages, "themes.png"));
            RenderSizesImage(Path.Combine(docsImages, "sizes.png"));
            Console.WriteLine("Exported all documentation screenshots to docs/images/");
        }
        File.WriteAllText(Path.Combine(folder, "binding-errors.log"), trace.ToString());
        if (trace.ToString().Contains("System.Windows.Data Error")) throw new Exception("Binding errors were logged.");
        app.Shutdown();
        Console.WriteLine("All eight pages rendered at 1180, 700, and 620 DIP; 125% and 150% raster checks captured.");
        Console.WriteLine("Bound editing, invalid-input validation, saving and reopening passed.");
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr LoadKeyboardLayout(string layout, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr ActivateKeyboardLayout(IntPtr layout, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr GetKeyboardLayout(uint thread);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    private static void RunTypingFixture()
    {
        var app = new Application();
        var text = new TextBox { Text = "Keyboard layout integration fixture", Margin = new Thickness(24), FontSize = 20 };
        var window = new Window { Title = "Language Indicator — typing check", Width = 650, Height = 200, Content = text, WindowStartupLocation = WindowStartupLocation.CenterScreen };
        var original = GetKeyboardLayout(0);
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
        var step = 0;
        window.Loaded += (_, _) => { window.Activate(); text.Focus(); ActivateKeyboardLayout(LoadKeyboardLayout("00000409", 0), 0); timer.Start(); };
        timer.Tick += (_, _) =>
        {
            if (step++ == 0) ActivateKeyboardLayout(LoadKeyboardLayout("00000401", 0), 0);
            else { timer.Stop(); ActivateKeyboardLayout(original, 0); window.Close(); }
        };
        app.Run(window);
    }
    private static void CheckTyping(PopupManager manager, SettingsModel settings)
    {
        if (GetForegroundWindow() == IntPtr.Zero)
        {
            Console.WriteLine("SKIPPED real keyboard-layout check: Windows exposes no foreground window in this execution desktop.");
            return;
        }
        using var monitor = new InputLanguageMonitor(settings);
        using var caret = new CaretTrackingService(monitor);
        var english = false;
        var arabic = false;
        caret.CaretChanged += (_, e) =>
        {
            var id = monitor.GetCurrentLanguageIdentifier();
            english |= id.StartsWith("en");
            arabic |= id.StartsWith("ar");
            if (e.Activation) manager.ShowLanguageAt(monitor.GetCurrentDisplayText(), e.Position);
            if (Application.Current.Windows.OfType<LanguagePopupWindow>().Count(x => x.IsVisible) > 1) throw new Exception("Multiple popup windows were visible.");
        };
        using var fixture = Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "--typing-fixture") { UseShellExecute = false, CreateNoWindow = true })!;
        var frame = new DispatcherFrame();
        var deadline = Stopwatch.StartNew();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += (_, _) =>
        {
            if (!fixture.HasExited)
            {
                fixture.Refresh();
                if (fixture.MainWindowHandle != IntPtr.Zero) SetForegroundWindow(fixture.MainWindowHandle);
            }
            if (fixture.HasExited || deadline.Elapsed.TotalSeconds > 8) frame.Continue = false;
        };
        timer.Start();
        Dispatcher.PushFrame(frame);
        timer.Stop();
        if (!fixture.HasExited) fixture.Kill();
        manager.Dismiss();
        if (!english || !arabic)
        {
            GetWindowThreadProcessId(GetForegroundWindow(), out var foregroundProcess);
            throw new Exception($"Typing integration incomplete: English={english}; Arabic={arabic}; ForegroundProcess={foregroundProcess}; FixtureProcess={fixture.Id}.");
        }
        Console.WriteLine("External text-field focus, English-to-Arabic layout change, and single-popup routing passed.");
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
    private static void Capture(FrameworkElement root, int width, int height, double scale, string file)
    {
        root.Measure(new Size(width, height));
        root.Arrange(new Rect(0, 0, width, height));
        root.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)(width * scale), (int)(height * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(file);
        encoder.Save(stream);
    }
    private static void ExportDocsImages(SettingsWindow window, string docsImagesDir)
    {
        Directory.CreateDirectory(docsImagesDir);
        var root = (FrameworkElement)window.Content;
        window.Width = 1180;
        window.Height = 820;
        root.Measure(new Size(1180, 780));
        root.Arrange(new Rect(0, 0, 1180, 780));
        root.UpdateLayout();
        window.GetType().GetMethod("UpdateResponsiveLayout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(window, null);

        var pages = new[]
        {
            ("General", "settings-general.png"),
            ("Appearance", "settings-appearance.png"),
            ("Position", "settings-position.png"),
            ("Animations", "settings-animations.png"),
            ("Languages", "settings-languages.png"),
            ("Typing correction", "settings-typing-correction.png"),
            ("Advanced", "settings-advanced.png"),
            ("About", "settings-about.png")
        };

        foreach (var (pageName, fileName) in pages)
        {
            window.NavigateTo(pageName);
            var navigation = (ListBox)window.FindName("Navigation");
            navigation.SelectedItem = navigation.Items.Cast<ListBoxItem>().FirstOrDefault(x => x.Tag?.ToString() == pageName);
            Pump();
            Capture(root, 1180, 780, 1.25, Path.Combine(docsImagesDir, fileName));
        }
    }
    private static void RenderThemesImage(string file)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        
        var darkSettings = new SettingsModel { Theme = "Dark", IndicatorSize = 2, PopupWidth = 170, PopupHeight = 150 };
        var darkView = new IndicatorView();
        darkView.Apply(darkSettings, "English");
        darkView.Margin = new Thickness(30, 20, 30, 20);

        var lightSettings = new SettingsModel { Theme = "Light", IndicatorSize = 2, PopupWidth = 170, PopupHeight = 150 };
        var lightView = new IndicatorView();
        lightView.Apply(lightSettings, "عربي");
        lightView.Margin = new Thickness(30, 20, 30, 20);

        panel.Children.Add(darkView);
        panel.Children.Add(lightView);

        var canvas = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF7, 0xF9)),
            Width = 680,
            Height = 320,
            Child = panel
        };

        Capture(canvas, 680, 320, 1.5, file);
    }
    private static void RenderSizesImage(string file)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

        var labels = new[] { "Extra small", "Small", "Medium", "Large", "Extra large" };
        for (int size = 0; size < 5; size++)
        {
            var col = new Grid { Margin = new Thickness(-14, 0, -14, 0) };
            col.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            col.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var settings = new SettingsModel { Theme = "Dark", IndicatorSize = size, PopupWidth = 170, PopupHeight = 150 };
            var view = new IndicatorView
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            view.Apply(settings, "English");
            Grid.SetRow(view, 0);
            col.Children.Add(view);

            var title = new TextBlock
            {
                Text = labels[size],
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x55, 0x68)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(title, 1);
            col.Children.Add(title);

            panel.Children.Add(col);
        }

        var canvas = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF7, 0xF9)),
            CornerRadius = new CornerRadius(14),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xDF, 0xE5, 0xE8)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(20, 16, 20, 16),
            Width = 980,
            Height = 350,
            Child = panel
        };

        Capture(canvas, 980, 350, 1.5, file);
    }
}
