namespace GoWithFlow.Application.DTOs.Responses.Script;

public sealed class ExcelParseResultDto
{
	public List<UtteranceParseDto> ValidRows { get; set; } = new();

	public List<ExcelRowError> ErrorRows { get; set; } = new();

	public int TotalRows { get; set; }

	public int ValidCount { get; set; }

	public int ErrorCount { get; set; }

	public bool IsValid { get; set; }
}
