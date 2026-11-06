using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using LanguageSwitchIndicator.Models;

namespace LanguageSwitchIndicator.Services;

public class GitHubProfileService
{
    private const string GitHubUser = "ahmadessawii06";
    private const string ApiUrl = $"https://api.github.com/users/{GitHubUser}";
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private readonly string _cacheDirectory;
    private readonly string _cacheFilePath;
    private readonly string _avatarCacheFilePath;

    public GitHubProfileService(string? cacheDirectory = null)
    {
        _cacheDirectory = cacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LanguageSwitchIndicator");
        _cacheFilePath = Path.Combine(_cacheDirectory, "github_profile_cache.json");
        _avatarCacheFilePath = Path.Combine(_cacheDirectory, "github_avatar.png");
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LanguageSwitchIndicator-Desktop", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        return client;
    }

    public GitHubProfile LoadCachedProfile()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = File.ReadAllText(_cacheFilePath);
                var cached = JsonSerializer.Deserialize<GitHubProfile>(json);
                if (cached != null)
                {
                    cached.IsFromCache = true;
                    if (File.Exists(_avatarCacheFilePath))
                    {
                        cached.LocalAvatarPath = _avatarCacheFilePath;
                    }
                    return cached;
                }
            }
        }
        catch
        {
            // Fall back to built-in defaults
        }

        var fallback = new GitHubProfile { IsFromCache = true };
        if (File.Exists(_avatarCacheFilePath))
        {
            fallback.LocalAvatarPath = _avatarCacheFilePath;
        }
        return fallback;
    }

    public async Task<GitHubProfile> FetchProfileAsync(CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.GetAsync(ApiUrl, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var profile = JsonSerializer.Deserialize<GitHubProfile>(json) ?? throw new InvalidOperationException("Failed to deserialize GitHub profile.");
        profile.IsFromCache = false;

        try
        {
            Directory.CreateDirectory(_cacheDirectory);
            await File.WriteAllTextAsync(_cacheFilePath, json, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(profile.AvatarUrl))
            {
                var imageBytes = await HttpClient.GetByteArrayAsync(profile.AvatarUrl, cancellationToken).ConfigureAwait(false);
                await File.WriteAllBytesAsync(_avatarCacheFilePath, imageBytes, cancellationToken).ConfigureAwait(false);
                profile.LocalAvatarPath = _avatarCacheFilePath;
            }
        }
        catch
        {
            // Caching failures should not prevent returning the live profile
        }

        return profile;
    }
}

