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

	public string? AiVoiceGender { get; set; }   // "Male" | "Female" (legacy; kept for back-compat)

	public string? AiVoiceName { get; set; }     // named Indian voice id: aarav|ananya|vikram|meera|rohan|priya

	public decimal? AiSpeechRate { get; set; }    // 0.75 | 1.00 | 1.25

	public int? AiQuestionDelaySec { get; set; }  // 0 | 1 | 2 | 3 | 5

	// Question & Answer only — "Show Hard Words" practice aid. When true, the Interviewer/listen turn
	// surfaces the turn's key words to the candidate. Ignored (forced false) for other categories.
	public bool ShowHardWords { get; set; }

	// Question & Answer only — keep the question's key words visible while the candidate is ANSWERING
	// (so they can recall and use them). Independent of ShowHardWords. Ignored (forced false) elsewhere.
	public bool ShowHardWordsInAnswer { get; set; }
}
