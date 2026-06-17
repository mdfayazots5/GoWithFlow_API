using GoWithFlow.Domain.Enums;

namespace GoWithFlow.Application.DTOs.Requests.Session;

public sealed class CreateSessionRequestDto
{
	public string SessionName { get; set; } = string.Empty;

	public SessionModeType SessionMode { get; set; }

	public byte MaxMembers { get; set; }

	public int SessionDuration { get; set; }

	public long ScriptId { get; set; }

	public int RoomExpiryMinutes { get; set; }

	// AI Voice Participant (Phase 17). When enabled, the AI fills every non-host slot so the
	// candidate can practice solo. The three settings below are required only when AiEnabled.
	public bool AiEnabled { get; set; }

	public string? AiVoiceGender { get; set; }   // "Male" | "Female"

	public decimal? AiSpeechRate { get; set; }    // 0.75 | 1.00 | 1.25

	public int? AiQuestionDelaySec { get; set; }  // 0 | 1 | 2 | 3 | 5
}
