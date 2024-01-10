using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CreatorCompanionPatcher.Manager.Windows.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private IReadOnlyList<Patcher> _patchers = new List<Patcher>().AsReadOnly();

    private readonly PatcherManager _patcherManager;

    public MainWindowViewModel(PatcherManager patcherManager)
    {
        _patcherManager = patcherManager;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        await _patcherManager.LoadInstancesAsync();

        Patchers = _patcherManager.PatcherInstances;
    }
}