using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using CreatorCompanionPatcher.Manager.Windows.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace CreatorCompanionPatcher.Manager.Windows
{
    public partial class App : Application
    {
        public App()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .WriteTo.Debug()
                .WriteTo.File(
                    Path.Combine(PatcherManager.DefaultPatcherManagerDataPath, "logs",
                        $"installer-{DateTimeOffset.Now:yyyy-MM-dd-HH-mm-ss}-{Guid.NewGuid():D}.log"),
                    rollingInterval: RollingInterval.Infinite)
                .CreateLogger();

            Ioc.Default.ConfigureServices(ConfigureServices());

        }

        private static async Task Init()
        {
            var manager = Ioc.Default.GetRequiredService<PatcherManager>();

            Log.Information("Loading Instances...");
            await manager.LoadInstancesAsync();
            Log.Information("Load Instances Complete");

            var mainWindow = Ioc.Default.GetRequiredService<MainWindow>();
            mainWindow.Show();

            Log.Information("Application started");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Init().ConfigureAwait(false);
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Services
            services.AddSingleton<PatcherInstaller>();
            services.AddSingleton<PatcherManager>();

            // Views
            services.AddSingleton<MainWindow>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<InstanceManagerViewModel>();

            return services.BuildServiceProvider();
        }
    }
}