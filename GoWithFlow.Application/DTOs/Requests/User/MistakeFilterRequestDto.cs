namespace GoWithFlow.Application.DTOs.Requests.User;

public sealed class MistakeFilterRequestDto
{
	public string? MistakeType { get; set; }

	public bool? IsResolved { get; set; }

	public int PageNumber { get; set; } = 1;

	public int PageSize { get; set; } = 20;
}
