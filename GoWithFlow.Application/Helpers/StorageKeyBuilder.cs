namespace GoWithFlow.Application.Helpers;

/// <summary>
/// Centralised R2 object key construction. Always use this — never build keys inline.
/// </summary>
public static class StorageKeyBuilder
{
    public static string VoiceRecording(long sessionId, int turnIndex, long userId, string extension = "ogg")
        => $"sessions/{sessionId}/turns/{turnIndex}/{userId}.{extension}";

    public static string UserAvatar(long userId, string extension = "jpg")
        => $"avatars/{userId}/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.{extension}";

    public static string ScriptExcel(long scriptId, int version)
        => $"scripts/{scriptId}/v{version}.xlsx";

    public static string ReportExport(long requestedByUserId)
        => $"exports/{requestedByUserId}/{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

    public static string SampleTemplate()
        => "scripts/sample/template_v1.xlsx";

    public static string AudioArchiveClip(long sessionId, int turnIndex, long userId)
        => $"sessions/{sessionId}/turns/{turnIndex}/{userId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.webm";

    /// <summary>
    /// Final consolidated session recording (one per session). Produced by the merge worker
    /// from the ordered per-turn segments stored under sessions/{sessionId}/turns/...
    /// </summary>
    public static string SessionRecordingFinal(long sessionId, string extension = "m4a")
        => $"sessions/{sessionId}/recording/session_{sessionId}.{extension}";

    public static bool IsR2Key(string? value)
        => !string.IsNullOrEmpty(value)
           && !value.StartsWith('/')
           && !value.StartsWith("http", StringComparison.OrdinalIgnoreCase);
}
