using GoWithFlow.Application.DTOs.Requests.Admin;

namespace GoWithFlow.Application.Interfaces.Services;

public interface IExcelExportService
{
	Task<byte[]> GenerateUserReportExcelAsync(AdminReportFilterRequestDto filter);

	Task<byte[]> GenerateSampleScriptTemplateAsync();
}
