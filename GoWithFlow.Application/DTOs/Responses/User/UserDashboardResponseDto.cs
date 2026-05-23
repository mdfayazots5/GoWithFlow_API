using GoWithFlow.Application.DTOs.Responses.Session;

namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class UserDashboardResponseDto
{
	public string UserName { get; set; } = string.Empty;

	public int CurrentStreak { get; set; }

	public DateTime TodayDate { get; set; }

	public int PendingRepracticeCount { get; set; }

	public List<SessionListItemResponseDto> RecentSessions { get; set; } = new();

	public List<MistakeResponseDto> PendingMistakes { get; set; } = new();
}
