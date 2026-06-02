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
}
