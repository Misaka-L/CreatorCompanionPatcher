using System.Reflection;
using HarmonyLib;

namespace CreatorCompanionPatcher.Patch;

public class ProductNamePatch
{
    public static void PatchAppProductName(Harmony harmony, Assembly vccCoreLibAssembly)
    {
        var vccType = vccCoreLibAssembly.GetType("VRC.PackageManagement.Core.VCC");
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