using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CreatorCompanionPatcher.Manager.Models;
using CreatorCompanionPatcher.Models;

namespace CreatorCompanionPatcher.Manager;

public class Patcher
{
    public string PatcherPath { get; private set; }
    public string PatcherVersion { get; private set; }
    public PatcherConfig Config { get; private set; }

    public Task LaunchAsync(bool withNoWindow = true)
    {
        Process.Start(new ProcessStartInfo(PatcherPath)
        {
            WorkingDirectory = Path.GetDirectoryName(PatcherPath),
            UseShellExecute = false,
            CreateNoWindow = withNoWindow
        });

        return Task.CompletedTask;
    }

    public PatcherInstanceData ExportToPatcherInstanceData()
    {
        return new PatcherInstanceData(
            InstancePath: PatcherPath,
            InstanceVersion: PatcherVersion
        );
    }

    public async ValueTask<PatcherConfig> LoadConfigAsync()
    {
        var config = await PatcherConfig.LoadConfigAsync(Path.Combine(GetPatcherFolderPath(), "patcher.json"));

        Config = config;
        return config;
    }

    public async Task SaveConfigAsync()
    {
        await Config.SaveConfigAsync(Path.Combine(GetPatcherFolderPath(), "patcher.json"));
    }

    public void UpdateConfig(PatcherConfig config)
    {
        Config = config;
    }

    public string GetPatcherFolderPath()
    {
        var patcherFolder = Path.GetDirectoryName(PatcherPath);
        if (patcherFolder is null)
            throw new InvalidOperationException("Could not get patcher folder");

        return patcherFolder;
    }

    public static async ValueTask<Patcher> CreateFromInstanceData(PatcherInstanceData patcherInstanceData)
    {
        if (!File.Exists(patcherInstanceData.InstancePath))
            throw new FileNotFoundException("Patcher instance not found", patcherInstanceData.InstancePath);

        if (Path.GetDirectoryName(patcherInstanceData.InstancePath) is not { } patcherFolderPath)
            throw new InvalidOperationException("Could not get patcher folder");

        var config =
            await PatcherConfig.LoadConfigAsync(Path.Combine(patcherFolderPath, "patcher.json"));

        return new Patcher
        {
            PatcherPath = patcherInstanceData.InstancePath,
            PatcherVersion = patcherInstanceData.InstanceVersion,
            Config = config
        };
    }

    public static async ValueTask<Patcher> CreateFromExistInstance(string patcherPath, string patcherVersion)
    {
        if (!File.Exists(patcherPath))
            throw new FileNotFoundException("Patcher instance not found", patcherPath);

        if (Path.GetDirectoryName(patcherPath) is not { } patcherFolderPath)
            throw new InvalidOperationException("Could not get patcher folder");

        var config =
            await PatcherConfig.LoadConfigAsync(Path.Combine(patcherFolderPath, "patcher.json"));

        return new Patcher
        {
            PatcherPath = patcherPath,
            PatcherVersion = patcherVersion,
            Config = config
        };
    }
}