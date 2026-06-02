using System.ComponentModel.DataAnnotations;

namespace GoWithFlow.Application.DTOs.Requests.Admin;

public sealed class CreateCohortRequestDto
{
	[Required]
	[MaxLength(128)]
	public string CohortName { get; set; } = string.Empty;

	[MaxLength(256)]
	public string? Description { get; set; }
}

public sealed class AssignUserToCohortRequestDto
{
	[Required]
	public long UserId { get; set; }

	public long? CohortId { get; set; }
}
