using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CreatorCompanionPatcher.Models;

public record PatcherConfig
{
    public List<string> EnabledPatches { get; set; } = new()
    {
        "LoggerPatch",
        "PackageInstallTimeoutPatch",
        "DisableTelemetryPatch"
    };

    public string? ReplaceOfficialReposUrl { get; set; }
    public string? ReplaceCuratedReposUrl { get; set; }

    public static async ValueTask<PatcherConfig> LoadConfigAsync(string path = "patcher.json")
    {
        if (!File.Exists(path))
        {
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new PatcherConfig()));
        }

        var patcherConfig = JsonSerializer.Deserialize<PatcherConfig>(await File.ReadAllTextAsync(path));
        return patcherConfig ?? throw new InvalidOperationException("Failed to load config from patcher.json");
    }

    public async Task SaveConfigAsync(string path = "patcher.json")
    {
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(this));
    }
}