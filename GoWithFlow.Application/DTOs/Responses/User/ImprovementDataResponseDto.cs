namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class ImprovementDataResponseDto
{
	public List<SessionScoreDto> RecentSessions { get; set; } = new();

	public List<WeeklyScoreDto> WeeklyScores { get; set; } = new();

	public List<GrammarProgressResponseDto> GrammarProgress { get; set; } = new();

	public List<RepracticeSessionResponseDto> RepracticeHistory { get; set; } = new();

	public List<UserBadgeDto> BadgesEarned { get; set; } = new();

	public ImprovementStatsHeaderDto StatsHeader { get; set; } = new();
}
