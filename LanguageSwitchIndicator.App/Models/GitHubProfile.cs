using System.Text.Json.Serialization;

namespace LanguageSwitchIndicator.Models;

public sealed class GitHubProfile
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = "ahmadessawii06";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Ahmad Essawii.";

    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; } = "https://avatars.githubusercontent.com/u/233433829?v=4";

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = "https://github.com/ahmadessawii06";

    [JsonPropertyName("bio")]
    public string? Bio { get; set; } = "NNU - CAP ,\r\nSenior C++ Developer.";

    [JsonPropertyName("company")]
    public string? Company { get; set; } = "من رام الله الى القدس وبالعكس";

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("public_repos")]
    public int PublicRepos { get; set; } = 31;

    [JsonPropertyName("followers")]
    public int Followers { get; set; } = 9;

    [JsonPropertyName("following")]
    public int Following { get; set; } = 6;

    [JsonIgnore]
    public bool IsFromCache { get; set; }

    [JsonIgnore]
    public string? LocalAvatarPath { get; set; }
}

