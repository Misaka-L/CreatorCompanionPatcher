using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using CreatorCompanionPatcher.Manager.Windows.ViewModels;
using MahApps.Metro.IconPacks;

namespace CreatorCompanionPatcher.Manager.Windows.Controls;

public partial class InstanceManagerView : UserControl
{
    public static readonly DependencyProperty PatcherProperty = DependencyProperty.Register(nameof(Patcher), typeof(Patcher),
        typeof(InstanceManagerView), new PropertyMetadata(default(Patcher), PropertyChangedCallback));

    private static void PropertyChangedCallback(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        if (obj is not InstanceManagerView view)
            return;

        if (args.NewValue is null)
        {
            view._viewModel.SetPatcherInstance(null);
            return;
        }

        if (args.NewValue is not Patcher vccPatcher)
            return;

        view._viewModel.SetPatcherInstance(vccPatcher);
    }

    public Patcher Patcher
    {
        get => (Patcher)GetValue(PatcherProperty);
        set => SetValue(PatcherProperty, value);
    }

    private readonly InstanceManagerViewModel _viewModel = Ioc.Default.GetRequiredService<InstanceManagerViewModel>();

    public InstanceManagerView()
    {
        _viewModel.SetPatcherInstance(Patcher);
        InitializeComponent();

        DataContext = _viewModel;
    }
}

public record InstanceManagerViewMenuItem(string Title, PackIconMaterialKind Icon, UserControl Control);