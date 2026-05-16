namespace GoWithFlow.Application.DTOs.Responses.Script;

public sealed class ExcelValidationResponseDto
{
	public bool IsValid { get; set; }

	public int TotalRows { get; set; }

	public int ValidCount { get; set; }

	public int ErrorCount { get; set; }

	public List<ExcelRowError> ErrorRows { get; set; } = new();

	public List<UtteranceParseDto> ValidRows { get; set; } = new();
}
