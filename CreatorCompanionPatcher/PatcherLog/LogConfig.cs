﻿namespace CreatorCompanionPatcher.PatcherLog;

public static class LogConfig
{
    public static readonly string LogPath = Path.GetFullPath($"patcher-logs/patcher-{DateTimeOffset.Now:yyyy-MM-dd-HH-mm-ss}-{Guid.NewGuid():D}.log");
}