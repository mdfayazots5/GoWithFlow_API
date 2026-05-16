namespace GoWithFlow.Domain.Entities;

public abstract class BaseAuditEntity
{
	public string? Tag { get; set; }

	public string? Comments { get; set; }

	public int SortOrder { get; set; }

	public string IPAddress { get; set; } = "127.0.0.1";

	public string CreatedBy { get; set; } = "Admin";

	public DateTime DateCreated { get; set; }

	public string? UpdatedBy { get; set; }

	public DateTime? LastUpdated { get; set; }

	public string? DeletedBy { get; set; }

	public DateTime? DateDeleted { get; set; }

	public bool IsDeleted { get; set; }
}
