namespace GoWithFlow.Application.Common;

public sealed class PagedResult<T>
{
	public List<T> Items { get; init; } = new();

	public int TotalCount { get; init; }

	public int PageNumber { get; init; }

	public int PageSize { get; init; }

	public int TotalPages => PageSize <= 0
		? 0
		: (int)Math.Ceiling(TotalCount / (double)PageSize);
}
