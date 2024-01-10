using CreatorCompanionPatcher.Manager.Windows.ViewModels;

namespace CreatorCompanionPatcher.Manager.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = viewModel;
        }

        // private async void LoadAllInstancesButton_OnClick(object sender, RoutedEventArgs e)
        // {
        //     await _patcherManager.LoadInstancesAsync();
        //     InstanceList.ItemsSource = _patcherManager.PatcherInstances;
        // }
        //
        // private async void SaveAllInstancesButton_OnClick(object sender, RoutedEventArgs e)
        // {
        //     await _patcherManager.SaveInstancesDataAsync();
        // }
        //
        // private async void ImportExistInstanceButton_OnClick(object sender, RoutedEventArgs e)
        // {
        //     var openFileDialog = new OpenFileDialog
        //     {
        //         Multiselect = false,
        //         Filter = "EXE|*.exe"
        //     };
        //
        //     if (openFileDialog.ShowDialog() == true)
        //     {
        //         await _patcherManager.ImportExistInstance(openFileDialog.FileName);
        //     }
        // }
        //
        // private async void LaunchInstanceButton_OnClick(object sender, RoutedEventArgs e)
        // {
        //     if (sender is not Button button)
        //         return;
        //
        //     if (button.Tag is not Patcher vccPatcher)
        //         return;
        //
        //     await vccPatcher.LaunchAsync();
        // }
        //
        // private async void FetchVersionsButton_OnClick(object sender, RoutedEventArgs e)
        // {
        //     var releases = await Ioc.Default.GetRequiredService<PatcherInstaller>().GetAllReleases();
        //     VersionsList.ItemsSource = releases;
        // }
        //
        // private async void InstallButton_OnClick(object sender, RoutedEventArgs e)
        // {
        //     if (sender is not Button button)
        //         return;
        //
        //     if (button.Tag is not string downloadUrl)
        //         return;
        //
        //     await Ioc.Default.GetRequiredService<PatcherInstaller>().InstallFromUrl("C:\\Users\\lipww\\AppData\\Local\\Programs\\VRChat Creator Companion", downloadUrl);
        //     await _patcherManager.ImportExistInstance("C:\\Users\\lipww\\AppData\\Local\\Programs\\VRChat Creator Companion\\CreatorCompanionPatcher.exe");
        //     await _patcherManager.SaveInstancesDataAsync();
        //
        //     LoadAllInstancesButton_OnClick(null, null);
        // }
    }
}