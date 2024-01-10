using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CreatorCompanionPatcher.Manager.Models;

namespace CreatorCompanionPatcher.Manager;

public class PatcherManager
{
    public static readonly string DefaultPatcherManagerDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CreatorCompanionPatcherManager");

    public static readonly string DefaultInstancesDataPath = Path.Combine(DefaultPatcherManagerDataPath, "instances.json");

    private List<Patcher> _patcherInstances = new();
    public IReadOnlyList<Patcher> PatcherInstances => _patcherInstances.AsReadOnly();

    public async Task LoadInstancesAsync(string? instancesDataPath = null)
    {
        _patcherInstances = new List<Patcher>();
        instancesDataPath ??= DefaultInstancesDataPath;

        if (!File.Exists(instancesDataPath))
            return;

        var jsonData = await File.ReadAllTextAsync(instancesDataPath);
        var instancesData = JsonSerializer.Deserialize<List<PatcherInstanceData>>(jsonData);

        if (instancesData is null)
            throw new InvalidOperationException("Failed to load instances data: Deserialize Failed.");

        foreach (var instanceData in instancesData)
        {
            _patcherInstances.Add(await Patcher.CreateFromInstanceData(instanceData));
        }
    }

    public async Task SaveInstancesDataAsync(string? instancesDataPath = null)
    {
        instancesDataPath ??= DefaultInstancesDataPath;
        var instancesDataDirectoryPath = Path.GetDirectoryName(instancesDataPath);

        var instancesData = _patcherInstances.Select(patcher => patcher.ExportToPatcherInstanceData());
        var jsonData = JsonSerializer.Serialize(instancesData);

        if (instancesDataDirectoryPath is not null && !Directory.Exists(instancesDataDirectoryPath))
            Directory.CreateDirectory(instancesDataDirectoryPath);

        await File.WriteAllTextAsync(instancesDataPath, jsonData);
    }

    public async Task ImportExistInstance(string instancePath, string instanceVersion)
    {
        _patcherInstances.Add(await Patcher.CreateFromExistInstance(instancePath, instanceVersion));
    }
}