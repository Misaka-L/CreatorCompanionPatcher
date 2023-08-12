using System.Reflection;
using HarmonyLib;
using Serilog;

namespace CreatorCompanionPatcher.Patch;

public class PackageInstallTimeoutPatch : IPatch
{
    public void ApplyPatch(Harmony harmony, Assembly vccAssembly, Assembly vccLibAssembly, Assembly vccCoreLibAssembly)
    {
        var httpClientField = vccCoreLibAssembly
            .GetType("VRC.PackageManagement.Core.Types.Providers.VPMPackageProvider")
            ?.GetField("_httpClient", BindingFlags.Static | BindingFlags.NonPublic);

        if (httpClientField?.GetValue(null) is not HttpClient httpClient)
        {
            Log.Error("Failed to patch Package download timeout fix - Field: {@Field}", httpClientField);
            return;
        }
        
        Log.Debug("VPMPackageProvider HttpClient timeout before patched: {Timeout}", httpClient.Timeout);
        httpClient.Timeout = TimeSpan.FromMinutes(30);
        Log.Debug("VPMPackageProvider HttpClient timeout after patched: {Timeout}", httpClient.Timeout);
    }
}