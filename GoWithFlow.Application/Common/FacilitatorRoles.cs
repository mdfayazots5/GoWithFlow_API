namespace GoWithFlow.Application.Common;

/// <summary>
/// Determines whether a given speaker label is a facilitator role for its category.
/// Facilitator roles (Interviewer, Tutor, Coach) are not performance participants —
/// they guide or question the learner but are not scored as session performers.
/// </summary>
public static class FacilitatorRoles
{
	// Categories that have role-asymmetry: one speaker is a facilitator, the other is a performer.
	// Each entry maps category name (case-insensitive) → facilitator speaker label (case-insensitive).
	// Both legacy short names and full display names are registered to handle existing DB data.
	private static readonly Dictionary<string, string> FacilitatorLabelByCategory =
		new(StringComparer.OrdinalIgnoreCase)
		{
			// MockInterview / Interview
			["Mock Interview"] = "Interviewer",
			["Interview"]      = "Interviewer",
			// VocabularySprint / Vocabulary
			["Vocabulary Sprint"] = "Tutor",
			["Vocabulary"]        = "Tutor",
			// RepracticeRound / Repetition
			["Repractice Round"] = "Coach",
			["Repetition"]       = "Coach",
			// Question & Answer — AI Interviewer asks (facilitator, read-only), Candidate answers (scored)
			["Question & Answer"] = "Interviewer",
		};

	/// <summary>
	/// Returns true if the given speakerLabel is the facilitator role for the given category.
	/// Returns false for all parity categories (Grammar Drill, Roleplay, Fluency Drill).
	/// </summary>
	public static bool IsFacilitator(string category, string speakerLabel)
	{
		if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(speakerLabel))
			return false;

		return FacilitatorLabelByCategory.TryGetValue(category.Trim(), out var facilitatorLabel)
			&& string.Equals(facilitatorLabel, speakerLabel.Trim(), StringComparison.OrdinalIgnoreCase);
	}
}
