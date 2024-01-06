using System.Reflection;
using CreatorCompanionPatcher.Patch;
using HarmonyLib;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using SingleFileExtractor.Core;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .WriteTo.Debug()
    .WriteTo.File("patcher-logs/patcher-.log",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

if (!CheckIsPlatformSupport())
{
    Log.Fatal("Only support Windows yet!");
    return;
}

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

// Start the vcc
Log.Information("Done! Starting...");
StartApp(vccAssembly);

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
    var vccExePath = Path.GetFullPath("CreatorCompanion.exe");
    var vccDllPath = Path.GetFullPath(Path.Join(tempPath, "CreatorCompanion.dll"));

    Log.Information("Check is CreatorCompanion.exe needs to be extracted....");
    var reader = new ExecutableReader(vccExePath);

    if (reader.IsSingleFile)
    {
        Log.Information("# Extract the bundle of CreatorCompanion.exe to {Path}...", tempPath);

        foreach (var bundleFile in reader.Bundle.Files)
        {
            var fullPath = Path.GetFullPath(Path.Join(tempPath, bundleFile.RelativePath));

            try
            {
                Log.Debug("- Extract file: {File} => {FullPath}", bundleFile.RelativePath, fullPath);
                await bundleFile.ExtractToFileAsync(fullPath);
            }
#if DEBUG
            catch (IOException ioException)
            {
                if (!ioException.Message.Contains("because it is being used by another process."))
                {
                    Log.Error(ioException, "A IOException was throw during extract the file: {File} => {FullPath}",
                        bundleFile.RelativePath, fullPath);
                }
            }
#endif
            catch (Exception ex)
            {
                Log.Error(ex, "A Exception was throw during extract the file: {File} => {FullPath}",
                    bundleFile.RelativePath, fullPath);
            }
        }

        Log.Information("Extract Done!");
    }

    return !File.Exists(vccDllPath) ? vccExePath : vccDllPath;
}

static void ApplyPatches(Assembly vccAssembly, Assembly vccLibAssembly, Assembly vccCoreLibAssembly)
{
    Log.Information("# Applying patches...");

    var harmony = new Harmony("xyz.misakal.vcc.patch");
    var patches = new List<IPatch>()
    {
        new PackageInstallTimeoutPatch()
    };

    foreach (var patch in patches)
    {
        Log.Information("- Apply patch: {Name}", patch.GetType().Name);
        patch.ApplyPatch(harmony, vccAssembly, vccLibAssembly, vccCoreLibAssembly);
    }
}