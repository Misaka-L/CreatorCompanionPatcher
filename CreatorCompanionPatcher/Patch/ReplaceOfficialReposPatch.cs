using System.Reflection;
using HarmonyLib;
using Serilog;

namespace CreatorCompanionPatcher.Patch;

public class ReplaceOfficialReposPatch : IPatch
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
            Log.Error("Failed to find Repos type, ReplaceOfficialReposPatch patch won't apply");
            return;
        }

        if (vpmPackageProviderType is null)
        {
            Log.Error("Failed to find VPMPackageProvider type, ReplaceOfficialReposPatch patch won't apply");
            return;
        }

        if (vrcRepoListType is null)
        {
            Log.Error("Failed to find VRCRepoList type, ReplaceOfficialReposPatch patch won't apply");
            return;
        }

        var refreshMethod = vpmPackageProviderType.GetMethod("Refresh", BindingFlags.Instance | BindingFlags.Public);
        var refreshPrefixMethod = typeof(ReplaceOfficialReposPatch).GetMethod(nameof(RefreshPrefixMethod),
            BindingFlags.NonPublic | BindingFlags.Static);

        harmony.Patch(refreshMethod, prefix: new HarmonyMethod(refreshPrefixMethod));

        var providersCacheDir = GetProvidersCacheDir(reposType, vccCoreLibAssembly);

        var createMethod = vpmPackageProviderType.GetMethod("Create", BindingFlags.Static | BindingFlags.Public,
            new[] { typeof(string), typeof(string), vrcRepoListType });

        var officialRepoField = reposType.GetField("_official", BindingFlags.Static | BindingFlags.NonPublic);
        var curatedRepoField = reposType.GetField("_curated", BindingFlags.Static | BindingFlags.NonPublic);

        var emptyVPMPackageProvider = createMethod.Invoke(null,
            new[] { Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), null, null });

        officialRepoField.SetValue(null, emptyVPMPackageProvider);
        curatedRepoField.SetValue(null, emptyVPMPackageProvider);

        if (PatcherApp.Config.ReplaceOfficialReposUrl is {} vccReplaceOfficialReposUrl)
        {
            var replaceOfficialRepoProvider = createMethod.Invoke(null,
                new[] { Path.Combine(providersCacheDir, "replace-vrc-official.json"), vccReplaceOfficialReposUrl, null });

            officialRepoField.SetValue(null, replaceOfficialRepoProvider);
        }

        if (PatcherApp.Config.ReplaceCuratedReposUrl is {} vccReplaceCuratedReposUrl)
        {
            var replaceCuratedRepoProvider = createMethod.Invoke(null,
                new[] { Path.Combine(providersCacheDir, "replace-vrc-curated.json"), vccReplaceCuratedReposUrl, null });

            curatedRepoField.SetValue(null, replaceCuratedRepoProvider);
        }
    }

    private static bool RefreshPrefixMethod( ref bool __result, string? ____remoteUrl)
    {
        if (____remoteUrl is "https://packages.vrchat.com/official" or "https://packages.vrchat.com/curated")
        {
            __result = true;
            return false;
        }

        if (____remoteUrl is not null) return true;

        __result = true;
        return false;
    }

    private static string? GetProvidersCacheDir(Type reposType, Assembly vpmCoreLibAssembly)
    {
        var providersCacheDirField = reposType.GetField("ProvidersCacheDir", BindingFlags.Static | BindingFlags.Public);

        if (providersCacheDirField is not null)
        {
            Log.Information("you are using a old version of vcc, we will get the providersCacheDir using old patch method");
            return providersCacheDirField.GetValue(null) as string;
        }

        var constantsType = vpmCoreLibAssembly.GetType("VRC.PackageManagement.Core.Constants");
        var reposProvidersCacheDirField = constantsType.GetField("ReposProvidersCacheDir", BindingFlags.Public | BindingFlags.Static);

        return reposProvidersCacheDirField.GetValue(null) as string;
    }
}