using System.Text.Json;
using CreatorCompanionPatcher.Patch;

namespace CreatorCompanionPatcher.Models;

public class PatcherConfig
{
    public static PatcherConfig Instance;

    public List<string> EnabledPatches { get; set; } = new()
    {
        nameof(LoggerPatch),
        nameof(PackageInstallTimeoutPatch),
        nameof(DisableTelemetryPatch)
    };

    public static PatcherConfig LoadConfig()
    {
        if (!File.Exists("patcher.json"))
        {
            File.WriteAllText("patcher.json", JsonSerializer.Serialize(new PatcherConfig()));
        }

        var patcherConfig = JsonSerializer.Deserialize<PatcherConfig>(File.ReadAllText("patcher.json"));
        Instance = patcherConfig ?? throw new InvalidOperationException("Failed to load config from patcher.json");

        return patcherConfig;
    }
}