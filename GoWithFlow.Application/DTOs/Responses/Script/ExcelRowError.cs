namespace GoWithFlow.Application.DTOs.Responses.Script;

public sealed class ExcelRowError
{
	public int RowNumber { get; set; }

	public string ColumnName { get; set; } = string.Empty;

	public string ErrorMessage { get; set; } = string.Empty;
}
