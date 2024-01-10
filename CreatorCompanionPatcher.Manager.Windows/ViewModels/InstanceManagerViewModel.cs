using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CreatorCompanionPatcher.Manager.Windows.Controls;
using CreatorCompanionPatcher.Manager.Windows.Controls.ManagerViews;
using MahApps.Metro.IconPacks;

namespace CreatorCompanionPatcher.Manager.Windows.ViewModels;

public partial class InstanceManagerViewModel : ObservableObject
{
    private readonly PatcherManager _patcherManager;

    private Patcher? _patcherInstance;

    [ObservableProperty] private string? _patcherPath;

    [ObservableProperty]
    private IReadOnlyList<InstanceManagerViewMenuItem>
        _menuItems = new List<InstanceManagerViewMenuItem>().AsReadOnly();

    public InstanceManagerViewModel(PatcherManager patcherManager)
    {
        _patcherManager = patcherManager;
    }

    public void SetPatcherInstance(Patcher? patcher)
    {
        _patcherInstance = patcher;

        PatcherPath = _patcherInstance?.PatcherPath;

        if (_patcherInstance is null)
        {
            MenuItems = new List<InstanceManagerViewMenuItem>().AsReadOnly();
            return;
        }

        MenuItems = new List<InstanceManagerViewMenuItem>()
        {
            new("Settings", PackIconMaterialKind.CogOutline, new InstanceManagerSettings()),
            new("Logs", PackIconMaterialKind.FileDocument,  new InstanceManagerLogs()),
            new("Releases", PackIconMaterialKind.Download, new InstanceManagerReleases())
        }.AsReadOnly();
    }

    [RelayCommand]
    private async Task LaunchInstanceAsync()
    {
        if (_patcherInstance is null)
            return;

        await _patcherInstance.LaunchAsync();
    }

    [RelayCommand]
    private async Task LaunchDebugInstanceAsync()
    {
        if (_patcherInstance is null)
            return;

        await _patcherInstance.LaunchAsync(false);
    }

    [RelayCommand]
    private void OpenInExplorer()
    {
        if (_patcherInstance is null)
            return;

        Process.Start("explorer.exe", $"/select , \"{_patcherInstance.PatcherPath}\"");
    }
}