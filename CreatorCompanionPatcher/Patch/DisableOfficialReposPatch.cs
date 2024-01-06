using System.Reflection;
using HarmonyLib;
using Serilog;

namespace CreatorCompanionPatcher.Patch;

public class DisableOfficialReposPatch : IPatch
{
    public int Order => 1;

    public void ApplyPatch(Harmony harmony, Assembly vccAssembly, Assembly vccLibAssembly, Assembly vccCoreLibAssembly)
    {
        var reposType = vccCoreLibAssembly.GetType("VRC.PackageManagement.Core.Repos");
        var vpmPackageProviderType =
            vccCoreLibAssembly.GetType("VRC.PackageManagement.Core.Types.Providers.VPMPackageProvider");
        var vrcRepoListType =
            vccCoreLibAssembly.GetType("VRC.PackageManagement.Core.Types.Packages.VRCRepoList");

        if (reposType is null)
        {
            Log.Error("Failed to find Repos type, DisableOfficialReposPatch patch won't apply");
            return;
        }

        if (vpmPackageProviderType is null)
        {
            Log.Error("Failed to find VPMPackageProvider type, DisableOfficialReposPatch patch won't apply");
            return;
        }

        if (vrcRepoListType is null)
        {
            Log.Error("Failed to find VRCRepoList type, DisableOfficialReposPatch patch won't apply");
            return;
        }

        var refreshMethod = vpmPackageProviderType.GetMethod("Refresh", BindingFlags.Instance | BindingFlags.Public);
        var refreshPrefixMethod = typeof(DisableOfficialReposPatch).GetMethod(nameof(RefreshPrefixMethod),
            BindingFlags.NonPublic | BindingFlags.Static);

        harmony.Patch(refreshMethod, prefix: new HarmonyMethod(refreshPrefixMethod));

        var createMethod = vpmPackageProviderType.GetMethod("Create", BindingFlags.Static | BindingFlags.Public,
            new[] { typeof(string), typeof(string), vrcRepoListType });

        var officialRepoField = reposType.GetField("_official", BindingFlags.Static | BindingFlags.NonPublic);
        var curatedRepoField = reposType.GetField("_curated", BindingFlags.Static | BindingFlags.NonPublic);

        var emptyVPMPackageProvider = createMethod.Invoke(null,
            new[] { Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), null, null });

        officialRepoField.SetValue(null, emptyVPMPackageProvider);
        curatedRepoField.SetValue(null, emptyVPMPackageProvider);
    }

    private static bool RefreshPrefixMethod( ref bool __result, string? ____remoteUrl)
    {
        if (____remoteUrl is not null) return true;

        __result = true;
        return false;
    }
}