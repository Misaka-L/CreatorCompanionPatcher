using System.Diagnostics;
using System.Net;
using System.Reflection;
using HarmonyLib;
using Serilog;

namespace CreatorCompanionPatcher.Patch;

public class DetailHttpServerLoggingPatch : IPatch
{
    public int Order => 0;

    public void ApplyPatch(Harmony harmony, Assembly vccAssembly, Assembly vccLibAssembly, Assembly vccCoreLibAssembly)
    {
        var staticFileServerType = vccAssembly.GetType("VCCApp.Services.StaticFileServer");
        var apiServerType = vccAssembly.GetType("VCCApp.Services.ApiServer");

        if (staticFileServerType is null)
        {
            Log.Error("Failed to find StaticFileServer type, DetailLoggingPatch patch won't apply");
            return;
        }

        if (apiServerType is null)
        {
            Log.Error("Failed to find ApiServer type, DetailLoggingPatch patch won't apply");
            return;
        }

        #region VCCApp.Services.StaticFileServer.Start

        var staticFileServerStartMethod =
            staticFileServerType.GetMethod("Start", BindingFlags.Static | BindingFlags.Public);
        var staticFileServerStartPrefixMethod =
            typeof(DetailHttpServerLoggingPatch).GetMethod(nameof(StaticFileServerStartPrefixMethod),
                BindingFlags.Static | BindingFlags.NonPublic);

        harmony.Patch(staticFileServerStartMethod, prefix: new HarmonyMethod(staticFileServerStartPrefixMethod));

        #endregion

        #region VCCApp.Services.ApiServer..ctor

        var apiServerConstructor = apiServerType.GetConstructor(new[] { typeof(string) });
        var apiServerConstructorPrefixMethod = typeof(DetailHttpServerLoggingPatch).GetMethod(nameof(ApiServerPrefixMethod),
            BindingFlags.Static | BindingFlags.NonPublic);

        harmony.Patch(apiServerConstructor, prefix: new HarmonyMethod(apiServerConstructorPrefixMethod));

        #endregion

        #region VCCApp.Services.ApiServer.HandleHttpRequest

        var handleHttpRequestMethod =
            apiServerType.GetMethod("HandleHttpRequest", BindingFlags.Instance | BindingFlags.NonPublic);
        var handleHttpRequestPrefixMethod = typeof(DetailHttpServerLoggingPatch).GetMethod(nameof(HandleHttpRequestPrefixMethod),
            BindingFlags.Static | BindingFlags.NonPublic);
        var handleHttpRequestPostfixMethod = typeof(DetailHttpServerLoggingPatch).GetMethod(
            nameof(HandleHttpRequestPostfixMethod),
            BindingFlags.Static | BindingFlags.NonPublic);

        harmony.Patch(handleHttpRequestMethod, prefix: new HarmonyMethod(handleHttpRequestPrefixMethod),
            postfix: new HarmonyMethod(handleHttpRequestPostfixMethod));

        #endregion
    }

    #region VCCApp.Services.StaticFileServer.Start

    private static void StaticFileServerStartPrefixMethod(string httpListenerPrefix)
    {
        Log.Information("Static File Server started on {HttpListenerPrefix}", httpListenerPrefix);
    }

    #endregion

    #region VCCApp.Services.ApiServer..ctor

    private static void ApiServerPrefixMethod(string httpListenerPrefix)
    {
        Log.Information("Api Server started on {HttpListenerPrefix}", httpListenerPrefix);
    }

    #endregion

    #region VCCApp.Services.ApiServer.HandleHttpRequest

    private static void HandleHttpRequestPrefixMethod(HttpListenerContext context, out Stopwatch __state)
    {
        var request = context.Request;

        Log.Debug("Request starting {Source} {Method} {Url} HTTP/{Protocol}", request.RemoteEndPoint,
            request.HttpMethod,
            request.Url?.PathAndQuery, request.ProtocolVersion);

        __state = new Stopwatch();
        __state.Start();
    }

    private static void HandleHttpRequestPostfixMethod(HttpListenerContext context, Task __result, Stopwatch __state)
    {
        __result.Wait();
        __state.Stop();

        var request = context.Request;
        var response = context.Response;

        Log.Debug("Request finished {Source} {Method} {Url} HTTP/{Protocol} - {ResponseCode} {Time}ms",
            request.RemoteEndPoint, request.HttpMethod,
            request.Url?.PathAndQuery, request.ProtocolVersion, response.StatusCode, __state.ElapsedMilliseconds);
    }

    #endregion
}