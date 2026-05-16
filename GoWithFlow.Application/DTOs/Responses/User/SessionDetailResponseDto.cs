using GoWithFlow.Application.DTOs.Responses.LiveSession;

namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class SessionDetailResponseDto
{
	public SessionHeaderDto SessionHeader { get; set; } = new();

	public PerformanceSummaryDto MyPerformance { get; set; } = new();

	public List<MistakeResponseDto> MyMistakes { get; set; } = new();

	public List<FeedbackCountDto> ListenerFeedbackReceived { get; set; } = new();

	public List<MemberScoreDto> AllMemberScores { get; set; } = new();
}
