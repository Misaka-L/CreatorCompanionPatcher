using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using CreatorCompanionPatcher.Core.Models;

namespace CreatorCompanionPatcher.Core;

public static class UpdateChecker
{
    private static readonly HttpClient _httpClient = new();

    private static readonly string _checkUpdateUrl = "https://api.github.com/repos/Misaka-L/CreatorCompanionPatcher/releases";

    static UpdateChecker()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("CreatorCompanionPatcherUpdateChecker", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.4.0"));
    }

    public static async Task<GithubRelease[]> GetReleasesAsync()
    {
        var releases = await _httpClient.GetFromJsonAsync<GithubRelease[]>(_checkUpdateUrl);

        if (releases is null)
            throw new InvalidOperationException("Failed to get releases from GitHub.");

        return releases;
    }

    public static async Task<GithubRelease> GetLatestReleaseAsync()
    {
        var releases = await GetReleasesAsync();

        if (releases.Length == 0)
            throw new InvalidOperationException("No releases available.");

        return releases[0];
    }

    public static async Task<GithubRelease?> CheckIsNewerReleaseAvailableAsync(string currentVersion)
    {
        var release = await GetLatestReleaseAsync();

        return "v" + currentVersion == release.TagName ? null : release;
    }
}