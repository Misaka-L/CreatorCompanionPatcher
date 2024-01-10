using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using CreatorCompanionPatcher.Manager.Models;

namespace CreatorCompanionPatcher.Manager;

public class PatcherInstaller
{
    public static readonly string
        ReleasesUrl = "https://api.github.com/repos/Misaka-L/CreatorCompanionPatcher/releases";

    private readonly HttpClient _httpClient;

    public PatcherInstaller()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CreatorCompanionPatcher.Manager",
            Assembly.GetExecutingAssembly().GetName().Version?.ToString()));
    }

    public async ValueTask<GithubRelease[]> GetAllReleases()
    {
        var releases = await _httpClient.GetFromJsonAsync<GithubRelease[]>(ReleasesUrl);

        if (releases is null)
            throw new InvalidOperationException("Could not get releases from GitHub");

        return releases;
    }

    public async Task InstallFromUrl(string vccPath, string url)
    {
        await using var stream = await _httpClient.GetStreamAsync(url);

        var tempFileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        await using var tempFileStream = File.Create(tempFileName);

        await stream.CopyToAsync(tempFileStream);
        tempFileStream.Close();

        await InstallFromFile(vccPath, tempFileName);
    }

    public Task InstallFromFile(string vccPath, string patcherPath)
    {
        File.Copy(patcherPath,  Path.Combine(vccPath, "CreatorCompanionPatcher.exe"), true);

        return Task.CompletedTask;
    }
}