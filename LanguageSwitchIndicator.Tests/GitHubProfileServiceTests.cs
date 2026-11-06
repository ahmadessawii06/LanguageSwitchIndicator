using System.IO;
using System.Text.Json;
using LanguageSwitchIndicator.Models;
using LanguageSwitchIndicator.Services;

namespace LanguageSwitchIndicator.Tests;

public class GitHubProfileServiceTests : IDisposable
{
    private readonly string _testDir;

    public GitHubProfileServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "LSI_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
        }
        catch { }
    }

    [Fact]
    public void LoadCachedProfile_WhenCacheDoesNotExist_ReturnsDefaultProfile()
    {
        var service = new GitHubProfileService(_testDir);
        var profile = service.LoadCachedProfile();

        Assert.NotNull(profile);
        Assert.Equal("ahmadessawii06", profile.Login);
        Assert.Equal("Ahmad Essawii.", profile.Name);
        Assert.True(profile.IsFromCache);
        Assert.True(profile.PublicRepos > 0);
        Assert.True(profile.Followers > 0);
    }

    [Fact]
    public void LoadCachedProfile_WhenCacheFileExists_ParsesJsonCorrectly()
    {
        var cacheFile = Path.Combine(_testDir, "github_profile_cache.json");
        var custom = new GitHubProfile
        {
            Login = "ahmadessawii06",
            Name = "Ahmad Essawii.",
            Bio = "Custom Test Bio",
            Company = "Ramallah",
            PublicRepos = 42,
            Followers = 15,
            Following = 10
        };
        File.WriteAllText(cacheFile, JsonSerializer.Serialize(custom));

        var service = new GitHubProfileService(_testDir);
        var profile = service.LoadCachedProfile();

        Assert.NotNull(profile);
        Assert.Equal("ahmadessawii06", profile.Login);
        Assert.Equal("Ahmad Essawii.", profile.Name);
        Assert.Equal("Custom Test Bio", profile.Bio);
        Assert.Equal("Ramallah", profile.Company);
        Assert.Equal(42, profile.PublicRepos);
        Assert.Equal(15, profile.Followers);
        Assert.Equal(10, profile.Following);
        Assert.True(profile.IsFromCache);
    }

    [Fact]
    public void LoadCachedProfile_WhenAvatarExists_SetsLocalAvatarPath()
    {
        var avatarFile = Path.Combine(_testDir, "github_avatar.png");
        File.WriteAllBytes(avatarFile, new byte[] { 1, 2, 3 });

        var service = new GitHubProfileService(_testDir);
        var profile = service.LoadCachedProfile();

        Assert.Equal(avatarFile, profile.LocalAvatarPath);
    }

    [Fact]
    public void LoadCachedProfile_WhenCacheIsCorrupt_ReturnsFallbackWithoutThrowing()
    {
        var cacheFile = Path.Combine(_testDir, "github_profile_cache.json");
        File.WriteAllText(cacheFile, "{ invalid json corrupt content !!!");

        var service = new GitHubProfileService(_testDir);
        var profile = service.LoadCachedProfile();

        Assert.NotNull(profile);
        Assert.Equal("ahmadessawii06", profile.Login);
        Assert.True(profile.IsFromCache);
    }

    [Fact]
    public void GitHubProfile_DeserializesRawGitHubApiResponse()
    {
        var rawJson = """
        {
          "login": "ahmadessawii06",
          "id": 233433829,
          "avatar_url": "https://avatars.githubusercontent.com/u/233433829?v=4",
          "html_url": "https://github.com/ahmadessawii06",
          "name": "Ahmad Essawii.",
          "company": "من رام الله الى القدس وبالعكس",
          "blog": "",
          "location": null,
          "email": null,
          "bio": "NNU - CAP ,\r\nSenior C++ Developer.",
          "public_repos": 31,
          "followers": 9,
          "following": 6
        }
        """;

        var profile = JsonSerializer.Deserialize<GitHubProfile>(rawJson);
        Assert.NotNull(profile);
        Assert.Equal("ahmadessawii06", profile.Login);
        Assert.Equal("Ahmad Essawii.", profile.Name);
        Assert.Equal("https://avatars.githubusercontent.com/u/233433829?v=4", profile.AvatarUrl);
        Assert.Equal("NNU - CAP ,\r\nSenior C++ Developer.", profile.Bio);
        Assert.Equal("من رام الله الى القدس وبالعكس", profile.Company);
        Assert.Equal(31, profile.PublicRepos);
        Assert.Equal(9, profile.Followers);
        Assert.Equal(6, profile.Following);
    }
}

