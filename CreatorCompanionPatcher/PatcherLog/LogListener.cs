using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace CreatorCompanionPatcher.PatcherLog;

public sealed class LogListener : ILogEventSink
{
    public static readonly LogListener Instance = new();

    public event EventHandler<LogEvent>? LogEventEmitted;

    public void Emit(LogEvent logEvent)
    {
        LogEventEmitted?.Invoke(this, logEvent);
    }
}

public static class LogListenerExtensions
{
    public static LoggerConfiguration LogListener(this LoggerSinkConfiguration loggerConfiguration)
    {
        return loggerConfiguration.Sink(PatcherLog.LogListener.Instance);
    }
}