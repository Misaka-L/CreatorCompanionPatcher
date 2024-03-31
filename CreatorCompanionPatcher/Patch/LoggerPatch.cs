using System.Reflection;
using CreatorCompanionPatcher.PatcherLog;
using HarmonyLib;
using Serilog;
using Serilog.Core;

namespace CreatorCompanionPatcher.Patch;

public class LoggerPatch : IPatch
{
    public int Order => -2;

    private static object? _apiServerObject;
    private static FieldInfo? _apiServerField;
    private static MethodInfo? _sendWsMessageMethod;
    private static Type? _wsMessageType;

    private static PropertyInfo? _wsMessageTypeProperty;
    private static PropertyInfo? _wsMessageDataProperty;

    private static bool _useOldWebsocketLogFormat = false;

    public void ApplyPatch(Harmony harmony, Assembly vccAssembly, Assembly vccLibAssembly, Assembly vccCoreLibAssembly)
    {
        if (!PatchVRCLoggerLib(vccCoreLibAssembly, harmony))
            return;

        if (!SetupWSMessage(vccAssembly))
            return;

        _apiServerField = vccAssembly.GetType("VCCApp.Program")
            ?.GetField("Server", BindingFlags.Public | BindingFlags.Static);
        if (_apiServerField is null)
        {
            Log.Warning("Failed to get field VCCApp.Services.ApiServer, WebSocket Logger maybe go down");
            return;
        }

        if (vccAssembly.GetType("VCCApp.Models.LogEvent") is null)
        {
            _useOldWebsocketLogFormat = true;
            Log.Warning("Failed to get type VCCApp.Models.LogEvent, WebSocket Logger will use old format");
        }

        LogListener.Instance.LogEventEmitted += (_, logEvent) =>
        {
            Task.Run(() =>
            {
                if (_apiServerObject is null)
                {
                    _apiServerObject = GetApiServerObject();
                    if (_apiServerObject is null)
                        return;
                }

                var logContent = logEvent.RenderMessage();

                if (_useOldWebsocketLogFormat)
                {
                    var oldWsMessage = CreateWSMessage("log", logContent);

                    _sendWsMessageMethod.Invoke(_apiServerObject, new[] { oldWsMessage });
                    return;
                }

                var wsMessage = CreateWSMessage("log", new
                {
                    Message = logContent,
                    logEvent.Level,
                    Timstamp = logEvent.Timestamp
                });

                _sendWsMessageMethod.Invoke(_apiServerObject, new[] { wsMessage });
            }).ConfigureAwait(false);
        };
    }

    private static string GetLogPathInternal()
    {
        return LogConfig.LogPath;
    }

    #region Prefix Methods

    private static bool SetupLogger(ref Logger ____log)
    {
        ____log = (Logger)Log.Logger;
        return false;
    }

    private static bool DeleteOldLogFiles()
    {
        return false;
    }

    private static bool GetLogPath(ref string __result)
    {
        __result = GetLogPathInternal();
        return false;
    }

    private static bool SaveStringAsLog(string value, string name)
    {
        File.WriteAllText(GetLogPathInternal(), value);
        return false;
    }

    #endregion

    #region Relfction

    private static object? GetApiServerObject()
    {
        return _apiServerField.GetValue(null);
    }

    private static Type? GetWSMessageType(Assembly vccAssembly)
    {
        return vccAssembly.GetType("VCCApp.Models.WSMessage");
    }

    private static MethodInfo? GetApiServerSendWSMessageMethod(Assembly vccAssembly, Type wsMessageType)
    {
        return vccAssembly.GetType("VCCApp.Services.ApiServer")?.GetMethod("SendWSMessage",
            BindingFlags.Instance | BindingFlags.Public, new[] { wsMessageType });
    }

    private static object? CreateWSMessage(string messageType, object data)
    {
        if (_sendWsMessageMethod is null || _wsMessageType is null || _wsMessageTypeProperty is null ||
            _wsMessageDataProperty is null)
            return null;

        var wsMessage = Activator.CreateInstance(_wsMessageType);

        _wsMessageTypeProperty.SetValue(wsMessage, messageType);
        _wsMessageDataProperty.SetValue(wsMessage, data);

        return wsMessage;
    }

    #endregion

    #region Setup

    private static bool PatchVRCLoggerLib(Assembly vccCoreLibAssembly, Harmony harmony)
    {
        var vrcLibLoggerType = vccCoreLibAssembly.GetType("VRC.PackageManagement.VRCLibLogger");
        if (vrcLibLoggerType is null)
        {
            Log.Error("Failed to patch Logger because get type VRC.PackageManagement.VRCLibLogger failed");
            return false;
        }

        var setLoggerDirectlyMethod =
            vrcLibLoggerType.GetMethod("SetLoggerDirectly", BindingFlags.Static | BindingFlags.Public);
        var setupNullLoggerMethod =
            vrcLibLoggerType.GetMethod("SetupNullLogger", BindingFlags.Static | BindingFlags.Public);
        var setupDefaultLoggerMethod =
            vrcLibLoggerType.GetMethod("SetupDefaultLogger", BindingFlags.Static | BindingFlags.Public);
        var deleteOldLogFilesMethod =
            vrcLibLoggerType.GetMethod("DeleteOldLogFiles", BindingFlags.Static | BindingFlags.Public);
        var getLogPathMethod =
            vrcLibLoggerType.GetMethod("GetLogPath", BindingFlags.Static | BindingFlags.NonPublic);
        var saveStringAsLogPathMethod =
            vrcLibLoggerType.GetMethod("SaveStringAsLog", BindingFlags.Static | BindingFlags.Public);

        if (setLoggerDirectlyMethod is null)
        {
            Log.Error(
                "Failed to patch Logger because get method VRC.PackageManagement.VRCLibLogger.SetLoggerDirectly failed");
            return false;
        }

        if (setupNullLoggerMethod is null)
            Log.Warning(
                "Failed to get method VRC.PackageManagement.VRCLibLogger.SetupNullLogger, but the patch may be still work");
        if (setupDefaultLoggerMethod is null)
            Log.Warning(
                "Failed to get method VRC.PackageManagement.VRCLibLogger.SetupDefaultLogger, but the patch may be still work");
        if (deleteOldLogFilesMethod is null)
            Log.Warning(
                "Failed to get method VRC.PackageManagement.VRCLibLogger.DeleteOldLogFiles, but the patch may be still work");
        if (getLogPathMethod is null)
            Log.Warning(
                "Failed to get method VRC.PackageManagement.VRCLibLogger.GetLogPath, but the patch may be still work");
        if (saveStringAsLogPathMethod is null)
            Log.Warning(
                "Failed to get method VRC.PackageManagement.VRCLibLogger.SaveStringAsLog, but the patch may be still work");

        var setupLoggerPrefixMethod =
            typeof(LoggerPatch).GetMethod(nameof(SetupLogger), BindingFlags.NonPublic | BindingFlags.Static);
        var deleteOldLogFilesPrefixMethod =
            typeof(LoggerPatch).GetMethod(nameof(DeleteOldLogFiles), BindingFlags.NonPublic | BindingFlags.Static);
        var getLogPathPrefixMethod =
            typeof(LoggerPatch).GetMethod(nameof(GetLogPath), BindingFlags.NonPublic | BindingFlags.Static);
        var saveStringAsLogPrefixMethod =
            typeof(LoggerPatch).GetMethod(nameof(SaveStringAsLog), BindingFlags.NonPublic | BindingFlags.Static);

        harmony.Patch(setLoggerDirectlyMethod, prefix: new HarmonyMethod(setupLoggerPrefixMethod));

        if (setupNullLoggerMethod is not null)
            harmony.Patch(setupNullLoggerMethod, prefix: new HarmonyMethod(setupLoggerPrefixMethod));
        if (setupDefaultLoggerMethod is not null)
            harmony.Patch(setupNullLoggerMethod, prefix: new HarmonyMethod(setupLoggerPrefixMethod));
        if (deleteOldLogFilesMethod is not null)
            harmony.Patch(deleteOldLogFilesMethod, prefix: new HarmonyMethod(deleteOldLogFilesPrefixMethod));
        if (getLogPathMethod is not null)
            harmony.Patch(getLogPathMethod, prefix: new HarmonyMethod(getLogPathPrefixMethod));
        if (saveStringAsLogPathMethod is not null)
            harmony.Patch(saveStringAsLogPathMethod, prefix: new HarmonyMethod(saveStringAsLogPrefixMethod));

        return true;
    }

    private static bool SetupWSMessage(Assembly vccAssembly)
    {
        _wsMessageType = GetWSMessageType(vccAssembly);
        if (_wsMessageType is null)
        {
            Log.Warning("Failed to get type VCCApp.Models.WSMessage, WebSocket Logger maybe go down");
            return false;
        }

        _wsMessageTypeProperty = _wsMessageType.GetProperty("messageType", BindingFlags.Public | BindingFlags.Instance);
        _wsMessageDataProperty = _wsMessageType.GetProperty("data", BindingFlags.Public | BindingFlags.Instance);

        if (_wsMessageTypeProperty is null)
        {
            Log.Warning(
                "Failed to get field VCCApp.Models.WSMessage.messageType, WebSocket Logger maybe go down");
            return false;
        }

        if (_wsMessageDataProperty is null)
        {
            Log.Warning(
                "Failed to get field VCCApp.Models.WSMessage.data, WebSocket Logger maybe go down");
            return false;
        }

        _sendWsMessageMethod = GetApiServerSendWSMessageMethod(vccAssembly, _wsMessageType);
        if (_sendWsMessageMethod is null)
        {
            Log.Warning(
                "Failed to get field VCCApp.Services.ApiServer.SendWSMessage(), WebSocket Logger maybe go down");
            return false;
        }

        return true;
    }

    #endregion
}