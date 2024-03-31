using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Nodes;
using CreatorCompanionPatcher;
using CreatorCompanionPatcher.Models;
using CreatorCompanionPatcher.Patch;
using CreatorCompanionPatcher.PatcherLog;
using HarmonyLib;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using SingleFileExtractor.Core;

var commandLineArgs = Environment.GetCommandLineArgs().ToList();

// Set current directory
var currentDirectory = commandLineArgs.Count > 1 ? commandLineArgs[^1] : AppDomain.CurrentDomain.BaseDirectory;
Directory.SetCurrentDirectory(currentDirectory);

// Logger Configuration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .WriteTo.Debug()
    .WriteTo.File(LogConfig.LogPath, rollingInterval: RollingInterval.Infinite)
    .WriteTo.LogListener()
    .CreateLogger();

Log.Information("Current Directory: {CurrentDirectory}", Directory.GetCurrentDirectory());

if (!CheckIsPlatformSupport())
{
    Log.Fatal("Only support Windows yet!");
    return;
}

// Check is patcher need to be extracted
if (commandLineArgs.FindIndex(arg => arg == "--start") == -1 && !await CheckAndExtraItself())
    return;

Log.Warning(
    "If you meet any bugs/problems, try to launch vcc without patcher. If problem disappear, please create a issues on github");

PatcherApp.Config =
    await PatcherConfig.LoadConfigAsync();

// Extra bundle
var tempPath = GetTempPath();

var vccDllPath = await GetCreatorCompanionAssemblyFile(tempPath);
var vccLibPath = Path.GetFullPath(Path.Join(tempPath, "vcc-lib.dll"));
var vccCoreLibPath = Path.GetFullPath(Path.Join(tempPath, "vpm-core-lib"));

// Load Assembly and apply patch
Log.Information("Load CreatorCompanion assembly ({VccDllPath})", vccDllPath);
var vccAssembly = Assembly.LoadFrom(vccDllPath);
var vccLibAssembly = Assembly.LoadFrom(vccLibPath);
var vccCoreLibAssembly = Assembly.LoadFrom(vccCoreLibPath);

ApplyPatches(vccAssembly, vccLibAssembly, vccCoreLibAssembly);

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

#region Locate CreatorCompanion Files

static string FindCreatorCompanionAssemblyFile(string path)
{
    const string vccBetaDllFileName = "CreatorCompanionBeta.dll";
    const string vccBetaExeFileName = "CreatorCompanionBeta.exe";
    const string vccDllFileName = "CreatorCompanion.dll";
    const string vccExeFileName = "CreatorCompanion.exe";

    if (File.Exists(Path.Join(path, vccBetaDllFileName)))
        return Path.Join(path, vccBetaDllFileName);

    if (File.Exists(Path.Join(path, vccDllFileName)))
        return Path.Join(path, vccDllFileName);

    if (File.Exists(Path.Join(path, vccBetaExeFileName)))
        return Path.Join(path, vccBetaExeFileName);

    if (File.Exists(Path.Join(path, vccExeFileName)))
        return Path.Join(path, vccExeFileName);

    throw new InvalidOperationException("Can't found CreatorCompanion File");
}

static async ValueTask<string> GetCreatorCompanionAssemblyFile(string tempPath)
{
    var vccAssemblyFilePath = FindCreatorCompanionAssemblyFile(Directory.GetCurrentDirectory());

    Log.Information("Check is CreatorCompanion.exe needs to be extracted....");

    if (await ExtraSingleFileExeAsync(vccAssemblyFilePath, tempPath))
        return FindCreatorCompanionAssemblyFile(tempPath);

    // If the file isn't a single file publish file
    return vccAssemblyFilePath;
}

#endregion

static async Task<bool> ExtraSingleFileExeAsync(string exePath, string tempPath)
{
    if (Path.GetExtension(exePath) != ".exe")
        return false;

    var reader = new ExecutableReader(exePath);

    // If the file isn't a single file publish file, skip extra
    if (!reader.IsSingleFile)
        return false;

    Log.Information("# Extract the bundle of {ExePath} to {Path}...", exePath, tempPath);

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

    return true;
}

static string GetTempPath()
{
    return Path.Join(Path.GetTempPath(), Path.GetRandomFileName(), "/");
}

static void ApplyPatches(Assembly vccAssembly, Assembly vccLibAssembly, Assembly vccCoreLibAssembly)
{
    Log.Information("# Applying patches...");

    var harmony = new Harmony("xyz.misakal.vcc.patch");

    AppDomainBaseDirectoryPatch.ApplyPatch(harmony, Directory.GetCurrentDirectory());
    ProductNamePatch.PatchAppProductName(harmony, vccCoreLibAssembly, vccAssembly);

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

static async ValueTask<bool> CheckAndExtraItself()
{
    var appExePath = Process.GetCurrentProcess().MainModule?.FileName;

    var tempPatcherPath = GetTempPath();
    if (!await ExtraSingleFileExeAsync(appExePath, tempPatcherPath))
        return true;

    var patcherAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
    var patcherAssemblyPath = Path.Join(tempPatcherPath, patcherAssemblyName + ".dll");
    var patcherRuntimeConfigPath = Path.Join(tempPatcherPath, patcherAssemblyName + ".runtimeconfig.json");

    var runtimeConfig = JsonNode.Parse(await File.ReadAllTextAsync(patcherRuntimeConfigPath));
    runtimeConfig["runtimeOptions"].AsObject().Remove("includedFrameworks");
    runtimeConfig["runtimeOptions"]["framework"] = new JsonObject{
        ["name"] = "Microsoft.NETCore.App",
        ["version"] = "6.0.27"
    };

    await File.WriteAllTextAsync(patcherRuntimeConfigPath, runtimeConfig.ToJsonString());

    Process.Start(new ProcessStartInfo
    {
        FileName = "dotnet",
        ArgumentList =
        {
            patcherAssemblyPath,
            "--start",
            Directory.GetCurrentDirectory()
        },
        CreateNoWindow = false,
        UseShellExecute = true,
        WorkingDirectory = Directory.GetCurrentDirectory(),
    });

    return false;
}

#endregion