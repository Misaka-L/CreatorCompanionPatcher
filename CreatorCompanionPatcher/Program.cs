using System.Diagnostics;
using System.Reflection;
using CreatorCompanionPatcher;
using CreatorCompanionPatcher.Models;
using CreatorCompanionPatcher.Patch;
using CreatorCompanionPatcher.PatcherLog;
using HarmonyLib;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using SingleFileExtractor.Core;

var commandLineArgs = Environment.GetCommandLineArgs().ToList();
var cleanupArgIndex = commandLineArgs.FindIndex(arg => arg == "-cleanup");
if (cleanupArgIndex != -1)
{
    if (cleanupArgIndex >= commandLineArgs.Count)
        return;

    var cleanupPath = commandLineArgs[cleanupArgIndex + 1];

    Directory.Delete(cleanupPath, true);
    return;
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .WriteTo.Debug()
    .WriteTo.File(LogConfig.LogPath, rollingInterval: RollingInterval.Infinite)
    .WriteTo.LogListener()
    .CreateLogger();

if (!CheckIsPlatformSupport())
{
    Log.Fatal("Only support Windows yet!");
    return;
}

PatcherApp.Config = await PatcherConfig.LoadConfigAsync(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "patcher.json"));

// Extra bundle
var tempPath = Path.Join(Path.GetTempPath(), Path.GetRandomFileName(), "/");

var vccDllPath = await ExtraSingleFileExe(tempPath);
var vccLibPath = Path.GetFullPath(Path.Join(tempPath, "vcc-lib.dll"));
var vccCoreLibPath = Path.GetFullPath(Path.Join(tempPath, "vpm-core-lib"));

// Load Assembly and apply patch

Log.Information("Load CreatorCompanion assembly ({VccDllPath})", vccDllPath);
var vccAssembly = Assembly.LoadFrom(vccDllPath);
var vccLibAssembly = Assembly.LoadFrom(vccLibPath);
var vccCoreLibAssembly = Assembly.LoadFrom(vccCoreLibPath);

ApplyPatches(vccAssembly, vccLibAssembly, vccCoreLibAssembly);

AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    Log.Information("Starting Cleanup Temp files...");
    var selfPath = Process.GetCurrentProcess().MainModule?.FileName;

    if (selfPath is null)
    {
        Log.Error("Unable to cleanup Temp files, can't found self executable path");
        return;
    }

    Process.Start(selfPath, $"-cleanup {tempPath}");
};

// Start the vcc
Log.Information("Done! Starting...");
StartApp(vccAssembly);

#region Methods

static void StartApp(Assembly vccAssembly)
{
    var vccProgramType = vccAssembly.GetType("VCCApp.Program");
    var mainMethod = vccProgramType.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static);
    var thread = new Thread(() => mainMethod.Invoke(null, new object?[] { Array.Empty<string>() }));

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
}

static bool CheckIsPlatformSupport()
{
    return OperatingSystem.IsWindows();
}

static async ValueTask<string> ExtraSingleFileExe(string tempPath)
{
    var vccExePath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "CreatorCompanion.exe");
    var vccDllPath = Path.GetFullPath(Path.Join(tempPath, "CreatorCompanion.dll"));

    Log.Information("Check is CreatorCompanion.exe needs to be extracted....");
    var reader = new ExecutableReader(vccExePath);

    if (!reader.IsSingleFile) return !File.Exists(vccDllPath) ? vccExePath : vccDllPath;

    Log.Information("# Extract the bundle of CreatorCompanion.exe to {Path}...", tempPath);

    foreach (var bundleFile in reader.Bundle.Files)
    {
        var fullPath = Path.GetFullPath(Path.Join(tempPath, bundleFile.RelativePath));

        try
        {
            Log.Debug("- Extract file: {File} => {FullPath}", bundleFile.RelativePath, fullPath);
            await bundleFile.ExtractToFileAsync(fullPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "A Exception was throw during extract the file: {File} => {FullPath}",
                bundleFile.RelativePath, fullPath);
        }
    }

    Log.Information("Extract Done!");

    return !File.Exists(vccDllPath) ? vccExePath : vccDllPath;
}


static void ApplyPatches(Assembly vccAssembly, Assembly vccLibAssembly, Assembly vccCoreLibAssembly)
{
    Log.Information("# Applying patches...");

    var harmony = new Harmony("xyz.misakal.vcc.patch");

    ProductNamePatch.PatchAppProductName(harmony, vccCoreLibAssembly);

    var patches = Assembly.GetExecutingAssembly().GetTypes()
        .Where(type => type.IsAssignableTo(typeof(IPatch)))
        .Where(type => PatcherApp.Config.EnabledPatches.Contains(type.Name))
        .Select(type => Activator.CreateInstance(type) as IPatch)
        .OrderBy(patch => patch?.Order)
        .ToArray();

    foreach (var patch in patches)
    {
        Log.Information("- Apply patch: {Name}", patch?.GetType().Name);
        patch?.ApplyPatch(harmony, vccAssembly, vccLibAssembly, vccCoreLibAssembly);
    }
}

#endregion