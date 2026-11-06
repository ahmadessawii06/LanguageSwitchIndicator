using System.IO;
using System.Reflection;
using System.Text.Json;

namespace LanguageSwitchIndicator.Models;

public sealed record DeveloperInfo(string Name, string Role, string? GitHub, string? Website);

public static class ProductInfo
{
    public const string Name = "Language Switch Indicator";
    public const string Repository = "https://github.com/ahmadessawii06/LanguageSwitchIndicator";
    public static string Version => typeof(ProductInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "1.0.0";
    public static IReadOnlyList<DeveloperInfo> Developers
    {
        get
        {
            using var stream = typeof(ProductInfo).Assembly.GetManifestResourceStream("LanguageSwitchIndicator.developers.json");
            return stream == null ? Array.Empty<DeveloperInfo>() : JsonSerializer.Deserialize<DeveloperInfo[]>(stream) ?? Array.Empty<DeveloperInfo>();
        }
    }
}
