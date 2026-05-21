namespace GoWithFlow.Application.DTOs.Requests.Admin;

public sealed class AdminCreateUserRequestDto
{
	public string FullName { get; set; } = string.Empty;

	public string MobileNumber { get; set; } = string.Empty;

	public string? Email { get; set; }

	/// <summary>Accepted values: "Child (6-12)", "Teen (13-17)", "Adult (18+)"</summary>
	public string AgeGroup { get; set; } = string.Empty;

	/// <summary>Accepted values: "Telugu", "Hindi", "Tamil", "Kannada", "None"</summary>
	public string PreferredHintLanguage { get; set; } = string.Empty;

	/// <summary>Optional. When provided, sets the user's login password.</summary>
	public string? Password { get; set; }
}
