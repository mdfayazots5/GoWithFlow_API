using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.Script;
using GoWithFlow.Application.DTOs.Responses.Script;
using Microsoft.AspNetCore.Http;

namespace GoWithFlow.Application.Interfaces.Services;

public interface IScriptService
{
	Task<ApiResponse<ExcelValidationResponseDto>> ValidateExcelAsync(IFormFile file, CancellationToken cancellationToken = default);

	Task<ApiResponse<ScriptUploadResponseDto>> UploadScriptAsync(IFormFile file, ScriptUploadRequestDto dto, long uploadedByUserId, CancellationToken cancellationToken = default);

	Task<ApiResponse<PagedResult<ScriptListItemResponseDto>>> GetScriptsAsync(ScriptSearchRequestDto dto, CancellationToken cancellationToken = default);

	Task<ApiResponse<ScriptDetailResponseDto>> GetScriptByIdAsync(long scriptId, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> UpdateScriptStatusAsync(ScriptStatusUpdateRequestDto dto, CancellationToken cancellationToken = default);

	Task<ApiResponse<List<ScriptVersionResponseDto>>> GetVersionHistoryAsync(long scriptId, CancellationToken cancellationToken = default);

	Task<ApiResponse<byte[]>> GetSampleTemplateAsync(CancellationToken cancellationToken = default);

	Task<ApiResponse<byte[]>> DownloadScriptAsync(long scriptId, CancellationToken cancellationToken = default);
}
