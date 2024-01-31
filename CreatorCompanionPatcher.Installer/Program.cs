using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using CreatorCompanionPatcher.Core;
using Microsoft.Win32;
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
    Log.Information("VRChat Creator Companion Path not found. Please install it first. Press any key to exit");
    Console.ReadLine();
    return;
}

var creatorCompanionExecutablePath = GetCreatorCompanionExecutablePath(creatorCompanionPath);
if (creatorCompanionExecutablePath is null)
{
    Log.Information("VRChat Creator Companion Executable not found. Please install it first. Press any key to exit");
    Console.ReadLine();
    return;
}

Log.Information("VRChat Creator Companion found at: {VccPath}", creatorCompanionExecutablePath);

if (LookupCreatorCompanionPatcher(creatorCompanionPath) is not { } patcherPath)
{
    var installedPatcherPath = await InstallPatcherAsync(creatorCompanionPath);
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
    await InstallPatcherByUrlAsync(newVersionDownloadUrl, release.TagName, creatorCompanionPath);
    Log.Information("Patcher Installed");
}
else
{
    Log.Information("Already installed latest version");
}

StartApp(patcherPath);

return;

#region Usefull or useless methods

#region Lookup CreatorCompanion & Patcher

static string? GetCreatorCompanionExecutablePath(string installLocation)
{
    return Path.Combine(installLocation, "CreatorCompanion.exe");
}

static string? LookupCreatorCompanion()
{
    if (LookupCreatorCompanionInRegistry() is { } installLocation)
        return installLocation;

    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var creatorCompanionPath = Path.Combine(localAppData, "Programs/VRChat Creator Companion");

    return Directory.Exists(creatorCompanionPath) ? creatorCompanionPath : null;
}

static string? LookupCreatorCompanionInRegistry()
{
    const string installedAppsRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    if (LookupCreatorCompanionPathInRegistry(Registry.LocalMachine, installedAppsRegistryKey) is
        { } globalInstallLocation)
        return globalInstallLocation;

    if (LookupCreatorCompanionPathInRegistry(Registry.CurrentUser, installedAppsRegistryKey) is { } userInstallLocation)
        return userInstallLocation;

    return null;
}

static string? LookupCreatorCompanionPathInRegistry(RegistryKey registryKey, string installedAppsRegistryKey)
{
    using var key = registryKey.OpenSubKey(installedAppsRegistryKey);

    if (key is null)
        throw new InvalidOperationException("Unable to find registry key: " + installedAppsRegistryKey +
                                            ", maybe there is something wrong in your system?");

    foreach (var appId in key.GetSubKeyNames())
    {
        using var appKey = key.OpenSubKey(appId);

        if (appKey?.GetValue("DisplayName") is not string displayName) continue;

        if (!displayName.Contains("VRChat Creator Companion")) continue;
        if (appKey.GetValue("InstallLocation") is not string installLocation) continue;

        return installLocation;
    }

    return null;
}


static string? LookupCreatorCompanionPatcher(string creatorCompanionInstallLocation)
{
    var patcherPath = Path.Combine(creatorCompanionInstallLocation,
        "CreatorCompanionPatcher.exe");

    return File.Exists(patcherPath) ? patcherPath : null;
}

#endregion

#region Install Patcher

static async Task<string> InstallPatcherByUrlAsync(string patcherFileUrl, string version, string vccPath)
{
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

static async Task<string> InstallPatcherAsync(string vccPath)
{
    Log.Information("Fetching latest release from GitHub...");
    var release = await UpdateChecker.GetLatestReleaseAsync();

    return await InstallPatcherByUrlAsync(newVersionDownloadUrl, release.TagName, vccPath);
}

#endregion

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

#endregion