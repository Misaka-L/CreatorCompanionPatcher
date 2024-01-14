using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using CreatorCompanionPatcher.Core;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

const string newVersionDownloadUrl = "https://cn-sy1.rains3.com/vcc-patcher/app/CreatorCompanionPatcher.exe";
const string patcherVersionDataFile = "patcher-version";


var logPath = Path.Combine(Path.GetTempPath(), "vcc-patcher-installer-logs/");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .WriteTo.Debug()
    .WriteTo.File(logPath + $"installer-{DateTimeOffset.Now:yyyy-MM-dd-HH-mm-ss}-{Guid.NewGuid():D}.log",
        rollingInterval: RollingInterval.Infinite)
    .CreateLogger();

if (!OperatingSystem.IsWindows())
{
    Log.Error("This installer is only supported on Windows. Press any key to exit");
    Console.ReadLine();
    return;
}

Log.Information("VRChat Creator Companion Installer v{Version}", Assembly.GetExecutingAssembly().GetName().Version);

var creatorCompanionPath = LookupCreatorCompanion();
if (creatorCompanionPath is null)
{
    Log.Information("VRChat Creator Companion not found. Please install it first. Press any key to exit");
    Console.ReadLine();
    return;
}

Log.Information("VRChat Creator Companion found at: {VccPath}", creatorCompanionPath);

if (LookupCreatorCompanionPatcher() is not { } patcherPath)
{
    var installedPatcherPath = await InstallPatcherAsync();
    StartApp(installedPatcherPath);
    return;
}

Log.Information("VRChat Creator Companion Patcher found at: {PatcherPath}", patcherPath);

var patcherVersion = GetPatcherVersion(patcherPath);

Log.Information("Patcher version: {PatcherVersion}", patcherVersion);

Log.Information("Checking Patcher Update...");

if (await UpdateChecker.CheckIsNewerReleaseAvailableAsync(patcherVersion) is { } release)
{
    Log.Information("Newer release available: {Version}", release.TagName);
    Log.Information("Installing New Version...");
    await InstallPatcherByUrlAsync(newVersionDownloadUrl, release.TagName);
    Log.Information("Patcher Installed");
}
else
{
    Log.Information("Already installed latest version");
}

StartApp(patcherPath);

return;

static string? LookupCreatorCompanion()
{
    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var creatorCompanionPath = Path.Combine(localAppData, "Programs/VRChat Creator Companion/CreatorCompanion.exe");

    return File.Exists(creatorCompanionPath) ? creatorCompanionPath : null;
}

static string? LookupCreatorCompanionPatcher()
{
    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var patcherPath = Path.Combine(localAppData, "Programs/VRChat Creator Companion/CreatorCompanionPatcher.exe");

    return File.Exists(patcherPath) ? patcherPath : null;
}

static string GetPatcherVersion(string path)
{
    var vccPath = Path.GetDirectoryName(path);
    if (vccPath is null)
        throw new InvalidOperationException("Could not get patcher directory path.");

    var patcherVersionPath = Path.Combine(vccPath, patcherVersionDataFile);

    if (!File.Exists(patcherVersionPath))
        throw new FileNotFoundException("Could not find patcher version file.", patcherVersionPath);

    return File.ReadAllText(patcherVersionPath);
}

static void StartApp(string patcherPath)
{
    Log.Information("Starting VRChat CreatorCompanion...");

    Process.Start(new ProcessStartInfo(patcherPath)
    {
        WorkingDirectory = Path.GetDirectoryName(patcherPath),
        UseShellExecute = true
    });
}

static async Task<string> InstallPatcherByUrlAsync(string patcherFileUrl, string version)
{
    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var vccPath = Path.Combine(localAppData, "Programs/VRChat Creator Companion");
    var patcherPath = Path.Combine(vccPath, "CreatorCompanionPatcher.exe");
    var versionDataPath = Path.Combine(vccPath, patcherVersionDataFile);

    Log.Information("Patcher will be install at {PatcherPath}", patcherPath);
    Log.Information("Downloading patcher from {PatcherFileUrl}...", patcherFileUrl);

    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CreatorCompanionPatcherInstaller",
        Assembly.GetExecutingAssembly().GetName().Version?.ToString()));
    httpClient.Timeout = TimeSpan.FromHours(1);

    await using var downloadStream = await httpClient.GetStreamAsync(patcherFileUrl);

    if (File.Exists(patcherPath))
        File.Delete(patcherPath);

    await using var fileStream = File.Create(patcherPath);

    await downloadStream.CopyToAsync(fileStream);

    await fileStream.FlushAsync();

    await File.WriteAllTextAsync(versionDataPath, version);

    Log.Information("Install Completed!");

    return patcherPath;
}

static async Task<string> InstallPatcherAsync()
{
    Log.Information("Fetching latest release from GitHub...");
    var release = await UpdateChecker.GetLatestReleaseAsync();

    return await InstallPatcherByUrlAsync(newVersionDownloadUrl, release.TagName);
}