using System.Drawing;
using LanguageSwitchIndicator.Services;

namespace LanguageSwitchIndicator.Tests;

public class TrayIconTests
{
    [Theory]
    [InlineData("white", 16)]
    [InlineData("dark", 16)]
    [InlineData("white", 32)]
    [InlineData("dark", 32)]
    public void EmbeddedTrayIcons_CanBeLoadedByWindows(string theme, int size)
    {
        using var stream = typeof(TrayIconService).Assembly.GetManifestResourceStream(
            $"LanguageSwitchIndicator.Assets.Icons.tray_{theme}.ico");
        Assert.NotNull(stream);
        using var icon = new Icon(stream, new Size(size, size));
        using var bitmap = icon.ToBitmap();
        Assert.Equal(size, bitmap.Width);
        Assert.Equal(size, bitmap.Height);
    }
}
