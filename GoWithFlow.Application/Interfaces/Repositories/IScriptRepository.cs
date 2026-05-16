using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.Script;
using GoWithFlow.Application.DTOs.Responses.Script;
using GoWithFlow.Domain.Entities;

namespace GoWithFlow.Application.Interfaces.Repositories;

public interface IScriptRepository
{
	Task<long> InsertScriptAsync(Script script, CancellationToken cancellationToken = default);

	Task InsertUtteranceAsync(Utterance utterance, CancellationToken cancellationToken = default);

	Task BulkInsertUtterancesAsync(long scriptId, IEnumerable<UtteranceParseDto> utterances, string createdBy, string ipAddress, CancellationToken cancellationToken = default);

	Task UpdateScriptUtteranceCountAsync(long scriptId, string updatedBy, string ipAddress, CancellationToken cancellationToken = default);

	Task<PagedResult<ScriptListItemResponseDto>> GetScriptsAsync(ScriptSearchRequestDto dto, CancellationToken cancellationToken = default);

	Task<ScriptDetailResponseDto?> GetScriptByIdAsync(long scriptId, CancellationToken cancellationToken = default);

	Task<List<ScriptVersionResponseDto>> GetVersionHistoryAsync(long scriptId, CancellationToken cancellationToken = default);

	Task UpdateScriptStatusAsync(ScriptStatusUpdateRequestDto dto, string updatedBy, string ipAddress, CancellationToken cancellationToken = default);

	Task<long?> CheckScriptTitleExistsAsync(string scriptTitle, CancellationToken cancellationToken = default);

	Task<int> GetLatestVersionByTitleAsync(string scriptTitle, CancellationToken cancellationToken = default);

	Task<long> InsertScriptVersionAsync(ScriptVersion scriptVersion, CancellationToken cancellationToken = default);

	Task SoftDeleteScriptAsync(long scriptId, string deletedBy, string ipAddress, CancellationToken cancellationToken = default);
}
