namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class LearningPathRecommendationDto
{
	public long ScriptId { get; set; }

	public string ScriptTitle { get; set; } = string.Empty;

	public string Category { get; set; } = string.Empty;

	public int ComplexityLevel { get; set; }

	/// <summary>Short reason shown to the user explaining why this session was recommended.</summary>
	public string ReasonText { get; set; } = string.Empty;

	/// <summary>Recommendation priority: repractice > low_score > level_up > variety</summary>
	public string RecommendationType { get; set; } = string.Empty;
}

public sealed class GuidedLearningPathResponseDto
{
	public List<LearningPathRecommendationDto> Recommendations { get; set; } = new();
}
