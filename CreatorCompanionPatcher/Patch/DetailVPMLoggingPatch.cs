using System.Reflection;
using HarmonyLib;
using Serilog;

namespace CreatorCompanionPatcher.Patch;

public class DetailVPMLoggingPatch : IPatch
{
    public int Order => 0;

    public void ApplyPatch(Harmony harmony, Assembly vccAssembly, Assembly vccLibAssembly, Assembly vccCoreLibAssembly)
    {
        var reposType = vccCoreLibAssembly.GetType("VRC.PackageManagement.Core.Repos");
        var vpmPackageProviderType =
            vccCoreLibAssembly.GetType("VRC.PackageManagement.Core.Types.Providers.VPMPackageProvider");
        var vrcRepoListType =
            vccCoreLibAssembly.GetType("VRC.PackageManagement.Core.Types.Packages.VRCRepoList");

        if (reposType is null)
        {
            Log.Error("Failed to find Repos type, DetailLoggingPatch patch won't apply");
            return;
        }

        if (vpmPackageProviderType is null)
        {
            Log.Error("Failed to find VPMPackageProvider type, DetailLoggingPatch patch won't apply");
            return;
        }

        if (vrcRepoListType is null)
        {
            Log.Error("Failed to find VRCRepoList type, DetailLoggingPatch patch won't apply");
            return;
        }

        #region VRC.PackageManagement.Core.Types.Providers.VPMPackageProvider.Refresh

        var refreshMethod = vpmPackageProviderType.GetMethod("Refresh", BindingFlags.Instance | BindingFlags.Public);
        var refreshPrefixMethod = typeof(DetailVPMLoggingPatch).GetMethod(nameof(RefreshPrefixMethod),
            BindingFlags.NonPublic | BindingFlags.Static);

        harmony.Patch(refreshMethod, prefix: new HarmonyMethod(refreshPrefixMethod));

        #endregion

        #region VRC.PackageManagement.Core.Types.Providers.VPMPackageProvider.GetRepoData

        var getRepoDataMethod =
            vpmPackageProviderType.GetMethod("GetRepoData", BindingFlags.Static | BindingFlags.NonPublic);
        var getRepoDataPrefixMethod =
            typeof(DetailVPMLoggingPatch).GetMethod(nameof(GetRepoDataPrefixMethod),
                BindingFlags.Static | BindingFlags.NonPublic);

        harmony.Patch(getRepoDataMethod, prefix: new HarmonyMethod(getRepoDataPrefixMethod));

        #endregion

        #region VRC.PackageManagement.Core.Types.Providers.VPMPackageProvider

        var downloadAndCachePackageMethod = vpmPackageProviderType.GetMethod("DownloadAndCachePackage",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var downloadAndCachePackagePrefixMethod = typeof(DetailVPMLoggingPatch).GetMethod(
            nameof(DownloadAndCachePackagePrefixMethod),
            BindingFlags.NonPublic | BindingFlags.Static);
        var downloadAndCachePackagePostfixMethod = typeof(DetailVPMLoggingPatch).GetMethod(
            nameof(DownloadAndCachePackagePostfixMethod),
            BindingFlags.NonPublic | BindingFlags.Static);

        harmony.Patch(downloadAndCachePackageMethod, prefix: new HarmonyMethod(downloadAndCachePackagePrefixMethod),
            postfix: new HarmonyMethod(downloadAndCachePackagePostfixMethod));

        #endregion
    }

    #region VRC.PackageManagement.Core.Types.Providers.VPMPackageProvider.Refresh

    private static void RefreshPrefixMethod(string? ____remoteUrl)
    {
        if (____remoteUrl is null)
            return;

        Log.Debug("Refreshing repository from {RemoteUrl}", ____remoteUrl);
    }

    #endregion

    #region VRC.PackageManagement.Core.Types.Providers.VPMPackageProvider.GetRepoData

    private static void GetRepoDataPrefixMethod(Uri url)
    {
        Log.Debug("Get repository data from {RemoteUrl}", url);
    }

    #endregion

    #region VRC.PackageManagement.Core.Types.Providers.VPMPackageProvider

    private static void DownloadAndCachePackagePrefixMethod(object manifest)
    {
        var manifestType = manifest.GetType();

        if (manifestType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)?.GetValue(manifest) is not
            string packageId)
            return;
        if (manifestType.GetProperty("Version", BindingFlags.Public | BindingFlags.Instance)?.GetValue(manifest) is not
            string version)
            return;
        if (manifestType.GetProperty("Url", BindingFlags.Public | BindingFlags.Instance)?.GetValue(manifest) is not
            string url)
            return;

        Log.Information("Downloading and caching package {PackageId}@{Version} from {RemoteUrl}", packageId, version,
            url);
    }

    private static void DownloadAndCachePackagePostfixMethod(object manifest, Task __result)
    {
        __result.Wait();

        var manifestType = manifest.GetType();

        if (manifestType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)?.GetValue(manifest) is not
            string packageId)
            return;
        if (manifestType.GetProperty("Version", BindingFlags.Public | BindingFlags.Instance)?.GetValue(manifest) is not
            string version)
            return;
        if (manifestType.GetProperty("Url", BindingFlags.Public | BindingFlags.Instance)?.GetValue(manifest) is not
            string url)
            return;

        Log.Information("Finishing Downloading and caching package {PackageId}@{Version} from {RemoteUrl}", packageId,
            version,
            url);
    }

    #endregion
}