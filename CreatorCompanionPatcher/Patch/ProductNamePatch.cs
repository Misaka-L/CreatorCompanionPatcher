using System.Reflection;
using HarmonyLib;
using Serilog;

namespace CreatorCompanionPatcher.Patch;

public class ProductNamePatch
{
    public static void PatchAppProductName(Harmony harmony, Assembly vccCoreLibAssembly, Assembly vccAssembly)
    {
        var vccType = vccCoreLibAssembly.GetType("VRC.PackageManagement.Core.VCC");

        if (vccType is not null)
        {
            Log.Information("You are using a old version of vcc, using old AppProductName patch method");

            PatchOldVcc(vccType, harmony);
            return;
        }

        var vccAppProgramType = vccAssembly.GetType("VCCApp.Program");

        if (vccAppProgramType is null)
        {
            Log.Warning("Failed to find VCCApp.Program type, Skip patch AppProductName");
            return;
        }

        var windowTitleField = vccAppProgramType.GetField("WindowTitle", BindingFlags.NonPublic | BindingFlags.Static);

        if (windowTitleField is null)
        {
            Log.Warning("Failed to find WindowTitle field, Skip patch AppProductName");
            return;
        }

        windowTitleField.SetValue(null, "[Patched] Creator Companion");
    }

    private static void PatchOldVcc(Type vccType, Harmony harmony)
    {
        var appProductNameGetMethod = vccType.GetProperty("AppProductName", BindingFlags.Public | BindingFlags.Static)
            .GetGetMethod();
        var appProductNameGetPrefixMethod = typeof(ProductNamePatch).GetMethod(nameof(AppProductNameGetPrefixMethod),
            BindingFlags.Static | BindingFlags.NonPublic);

        harmony.Patch(appProductNameGetMethod, prefix: new HarmonyMethod(appProductNameGetPrefixMethod));
    }

    private static bool AppProductNameGetPrefixMethod(ref string __result)
    {
        __result = "[Patched] CreatorCompanion";
        return false;
    }
}