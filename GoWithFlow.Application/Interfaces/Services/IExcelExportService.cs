using GoWithFlow.Application.DTOs.Requests.Admin;
using GoWithFlow.Application.DTOs.Responses.Script;

namespace GoWithFlow.Application.Interfaces.Services;

public interface IExcelExportService
{
	Task<byte[]> GenerateUserReportExcelAsync(AdminReportFilterRequestDto filter);

	Task<byte[]> GenerateSampleScriptTemplateAsync();

	Task<byte[]> GenerateScriptExcelAsync(ScriptDetailResponseDto script);
}
