using System.Reflection;
using HarmonyLib;
using Serilog;

namespace CreatorCompanionPatcher.Patch;

public class AppDomainBaseDirectoryPatch
{
    public static void ApplyPatch(Harmony harmony, string? baseDirectory = null)
    {
        typeof(AppContext).GetMethod("SetData", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object?[] { "APP_CONTEXT_BASE_DIRECTORY", baseDirectory });
    }
}