using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using Serilog;

namespace CreatorCompanionPatcher.Patch;

public class AppLocalizationPatch : IPatch
{
    private static readonly string _webAssetsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebApp", "Dist");

    public int Order => 0;
    public void ApplyPatch(Harmony harmony, Assembly vccAssembly, Assembly vccLibAssembly, Assembly vccCoreLibAssembly)
    {
        var staticFileServerType = vccAssembly.GetType("VCCApp.Services.StaticFileServer");

        if (staticFileServerType is null)
        {
            Log.Error("Failed to find VCCApp.Services.StaticFileServer type, skip localization patch");
            return;
        }

        var getRequestedWebAppFileMethod = staticFileServerType.GetMethod("GetRequestedWebAppFile", BindingFlags.NonPublic | BindingFlags.Static);
        var getRequestedWebAppFilePrefixMethod =
            typeof(AppLocalizationPatch).GetMethod(nameof(GetRequestedWebAppFilePrefix),
                BindingFlags.Static | BindingFlags.NonPublic);

        harmony.Patch(getRequestedWebAppFileMethod, prefix: new HarmonyMethod(getRequestedWebAppFilePrefixMethod));
    }

    private static bool GetRequestedWebAppFilePrefix(ref FileStream __result, string path)
    {
        if (path != Path.Combine(_webAssetsRoot, "index.html"))
            return true;

        var indexHtml = File.ReadAllText(path);

        var jsContent = File.ReadAllText("script-loader.js");

        var regex = new Regex("<script.+src=\"(\\/assets\\/index-.+)\".+<\\/script>");
        var jsPath = regex.Match(indexHtml).Groups[1].Value;
        var result = regex.Replace(indexHtml, $"<meta name=\"index-module\" content=\"{jsPath}\"/>");

        result = result.Replace("</body>", $"</body><script type=\"module\">{jsContent}</script>");

        var resultHtmlPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(resultHtmlPath, result);

        __result = File.OpenRead(resultHtmlPath);

        return false;
    }
}