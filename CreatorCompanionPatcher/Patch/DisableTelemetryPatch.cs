using System.Reflection;
using HarmonyLib;

namespace CreatorCompanionPatcher.Patch;

public class DisableTelemetryPatch : IPatch
{
    public int Order => 0;
    public void ApplyPatch(Harmony harmony, Assembly vccAssembly, Assembly vccLibAssembly, Assembly vccCoreLibAssembly)
    {
        var telemetryType = vccAssembly.GetType("VCCApp.Analytics.Amplitude");
        var sendEventMethod = telemetryType.GetMethod("PostEvents", BindingFlags.Instance | BindingFlags.NonPublic);
        harmony.Patch(sendEventMethod, prefix: new HarmonyMethod(typeof(DisableTelemetryPatch), nameof(SendEventPrefix)));
    }

    private static bool SendEventPrefix()
    {
        return false;
    }
}