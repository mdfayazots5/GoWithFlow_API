using System.ComponentModel.DataAnnotations;

namespace GoWithFlow.Application.DTOs.Requests.User;

public sealed class SetGoalRequestDto
{
    /// <summary>One of: interview, grammar, vocabulary, fluency</summary>
    [Required]
    public string GoalType { get; set; } = string.Empty;

    /// <summary>One of: 2, 4, 8</summary>
    [Range(1, 52)]
    public int TimelineWeeks { get; set; } = 4;
}
