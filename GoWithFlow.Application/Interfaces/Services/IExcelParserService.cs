using GoWithFlow.Application.DTOs.Responses.Script;

namespace GoWithFlow.Application.Interfaces.Services;

public interface IExcelParserService
{
	Task<ExcelParseResultDto> ParseAndValidateAsync(Stream fileStream);
}
