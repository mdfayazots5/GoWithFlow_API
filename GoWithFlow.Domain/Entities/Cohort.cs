namespace GoWithFlow.Domain.Entities;

public sealed class Cohort : BaseAuditEntity
{
	public long CohortId { get; set; }

	public string CohortName { get; set; } = string.Empty;

	public string? Description { get; set; }

	public bool IsActive { get; set; } = true;

	public ICollection<User> Members { get; set; } = new List<User>();
}
