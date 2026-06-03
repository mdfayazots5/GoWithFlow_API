using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.Script;
using GoWithFlow.Application.DTOs.Responses.Script;
using GoWithFlow.Application.Helpers;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Application.Settings;
using GoWithFlow.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GoWithFlow.Application.Services;

public sealed class ScriptService : IScriptService
{
	private static readonly ConcurrentDictionary<string, byte> ScriptListCacheKeys = new();

	private readonly IUserRepository _userRepository;
	private readonly IScriptRepository _scriptRepository;
	private readonly IExcelParserService _excelParserService;
	private readonly IMemoryCache _memoryCache;
	private readonly IExcelExportService _excelExportService;
	private readonly IStorageService _storageService;
	private readonly CloudflareR2Settings _r2Settings;

	public ScriptService(
		IUserRepository userRepository,
		IScriptRepository scriptRepository,
		IExcelParserService excelParserService,
		IMemoryCache memoryCache,
		IExcelExportService excelExportService,
		IStorageService storageService,
		IOptions<CloudflareR2Settings> r2Options)
	{
		_userRepository     = userRepository;
		_scriptRepository   = scriptRepository;
		_excelParserService = excelParserService;
		_memoryCache        = memoryCache;
		_excelExportService = excelExportService;
		_storageService     = storageService;
		_r2Settings         = r2Options.Value;
	}

	public async Task<ApiResponse<ExcelValidationResponseDto>> ValidateExcelAsync(IFormFile file, CancellationToken cancellationToken = default)
	{
		var validationErrors = ValidateFile(file);

		if (validationErrors.Count > 0)
		{
			return ApiResponse<ExcelValidationResponseDto>.FailureResult(validationErrors, "Validation failed.");
		}

		await using var stream = file.OpenReadStream();
		var parseResult = await _excelParserService.ParseAndValidateAsync(stream);

		var response = new ExcelValidationResponseDto
		{
			IsValid    = parseResult.IsValid,
			TotalRows  = parseResult.TotalRows,
			ValidCount = parseResult.ValidCount,
			ErrorCount = parseResult.ErrorCount,
			ErrorRows  = parseResult.ErrorRows,
			ValidRows  = parseResult.ValidRows
		};

		return ApiResponse<ExcelValidationResponseDto>.SuccessResult(response, "Excel validation completed successfully.");
	}

	public async Task<ApiResponse<ScriptUploadResponseDto>> UploadScriptAsync(IFormFile file, ScriptUploadRequestDto dto, long uploadedByUserId, CancellationToken cancellationToken = default)
	{
		var validationErrors = ValidateUploadRequest(dto);
		validationErrors.AddRange(ValidateFile(file));

		if (validationErrors.Count > 0)
		{
			return ApiResponse<ScriptUploadResponseDto>.FailureResult(validationErrors, "Validation failed.");
		}

		var uploadedByUser = await _userRepository.GetByUserIdAsync(uploadedByUserId, cancellationToken);

		if (uploadedByUser is null)
		{
			return ApiResponse<ScriptUploadResponseDto>.FailureResult(new[] { "UploadedByUserId is invalid." }, "Script upload failed.");
		}

		// Buffer file into memory so the stream can be read twice (parse + R2 upload)
		byte[] fileBytes;
		await using (var bufferStream = new MemoryStream())
		{
			await file.CopyToAsync(bufferStream, cancellationToken);
			fileBytes = bufferStream.ToArray();
		}

		var parseResult = await _excelParserService.ParseAndValidateAsync(new MemoryStream(fileBytes));

		if (parseResult.IsValid == false)
		{
			var errors = parseResult.ErrorRows
				.Select(error => $"Row {error.RowNumber} / {error.ColumnName}: {error.ErrorMessage}")
				.ToList();

			return ApiResponse<ScriptUploadResponseDto>.FailureResult(errors, "Excel validation failed.");
		}

		var existingScriptId = await _scriptRepository.CheckScriptTitleExistsAsync(dto.ScriptTitle.Trim(), cancellationToken);
		var latestVersion    = await _scriptRepository.GetLatestVersionByTitleAsync(dto.ScriptTitle.Trim(), cancellationToken);
		var versionNumber    = latestVersion + 1;

		var script = new Script
		{
			ScriptTitle     = dto.ScriptTitle.Trim(),
			Category        = dto.Category.Trim(),
			GrammarFocusTag = dto.GrammarFocusTag.Trim(),
			ContextTag      = dto.ContextTag.Trim(),
			ComplexityLevel = dto.ComplexityLevel,
			TargetAgeGroup  = dto.TargetAgeGroup.Trim(),
			HintLanguage    = dto.HintLanguage.Trim(),
			UploadedByUserId = uploadedByUserId,
			Version         = versionNumber,
			CreatedBy       = uploadedByUser.FullName,
			IPAddress       = "127.0.0.1"
		};

		var scriptId = await _scriptRepository.InsertScriptAsync(script, cancellationToken);

		await _scriptRepository.BulkInsertUtterancesAsync(scriptId, parseResult.ValidRows, uploadedByUser.FullName, "127.0.0.1", cancellationToken);
		await _scriptRepository.UpdateScriptUtteranceCountAsync(scriptId, uploadedByUser.FullName, "127.0.0.1", cancellationToken);

		var versionNotes = existingScriptId.HasValue
			? $"Re-upload of existing title. SourceScriptId={existingScriptId.Value}"
			: "Initial upload";

		await _scriptRepository.InsertScriptVersionAsync(
			new ScriptVersion
			{
				ScriptId         = scriptId,
				VersionNumber    = versionNumber,
				VersionNotes     = versionNotes,
				UploadedByUserId = uploadedByUserId,
				CreatedBy        = uploadedByUser.FullName,
				IPAddress        = "127.0.0.1"
			},
			cancellationToken);

		// Upload original Excel to R2 and save the storage key
		var excelKey = StorageKeyBuilder.ScriptExcel(scriptId, versionNumber);
		await using var r2Stream = new MemoryStream(fileBytes);
		await _storageService.UploadAsync(r2Stream, _r2Settings.Buckets.Scripts, excelKey,
			"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", cancellationToken);
		await _scriptRepository.UpdateScriptExcelKeyAsync(scriptId, excelKey, cancellationToken);

		var excelDownloadUrl = await _storageService.GetPresignedUrlAsync(
			_r2Settings.Buckets.Scripts, excelKey, _r2Settings.PresignedUrlExpiryMinutes.Scripts, cancellationToken);

		InvalidateScriptCache();

		return ApiResponse<ScriptUploadResponseDto>.SuccessResult(
			new ScriptUploadResponseDto
			{
				ScriptId         = scriptId,
				ScriptTitle      = script.ScriptTitle,
				Version          = versionNumber,
				UtteranceCount   = parseResult.ValidRows.Count,
				ExcelDownloadUrl = excelDownloadUrl
			},
			"Script uploaded successfully.");
	}

	public async Task<ApiResponse<PagedResult<ScriptListItemResponseDto>>> GetScriptsAsync(ScriptSearchRequestDto dto, CancellationToken cancellationToken = default)
	{
		dto.SearchTerm    = string.IsNullOrWhiteSpace(dto.SearchTerm)    ? null : dto.SearchTerm.Trim();
		dto.Category      = string.IsNullOrWhiteSpace(dto.Category)      ? null : dto.Category.Trim();
		dto.GrammarFocusTag = string.IsNullOrWhiteSpace(dto.GrammarFocusTag) ? null : dto.GrammarFocusTag.Trim();
		dto.TargetAgeGroup  = string.IsNullOrWhiteSpace(dto.TargetAgeGroup)  ? null : dto.TargetAgeGroup.Trim();
		dto.PageNumber = dto.PageNumber <= 0 ? 1 : dto.PageNumber;
		dto.PageSize   = dto.PageSize   <= 0 ? 12 : dto.PageSize;

		var cacheKey = BuildScriptListCacheKey(dto);

		if (_memoryCache.TryGetValue(cacheKey, out PagedResult<ScriptListItemResponseDto>? cachedScripts) && cachedScripts is not null)
		{
			return ApiResponse<PagedResult<ScriptListItemResponseDto>>.SuccessResult(cachedScripts, "Script list retrieved successfully.");
		}

		var scripts = await _scriptRepository.GetScriptsAsync(dto, cancellationToken);
		ScriptListCacheKeys.TryAdd(cacheKey, 0);
		_memoryCache.Set(cacheKey, scripts, TimeSpan.FromMinutes(5));

		return ApiResponse<PagedResult<ScriptListItemResponseDto>>.SuccessResult(scripts, "Script list retrieved successfully.");
	}

	public async Task<ApiResponse<ScriptDetailResponseDto>> GetScriptByIdAsync(long scriptId, CancellationToken cancellationToken = default)
	{
		if (scriptId <= 0)
		{
			return ApiResponse<ScriptDetailResponseDto>.FailureResult(new[] { "ScriptId must be greater than zero." }, "Validation failed.");
		}

		var script = await _scriptRepository.GetScriptByIdAsync(scriptId, cancellationToken);

		if (script is null)
		{
			return ApiResponse<ScriptDetailResponseDto>.FailureResult(new[] { "Script not found." }, "Script detail not found.");
		}

		return ApiResponse<ScriptDetailResponseDto>.SuccessResult(script, "Script detail retrieved successfully.");
	}

	public async Task<ApiResponse<bool>> UpdateScriptStatusAsync(ScriptStatusUpdateRequestDto dto, CancellationToken cancellationToken = default)
	{
		if (dto.ScriptId <= 0)
		{
			return ApiResponse<bool>.FailureResult(new[] { "ScriptId must be greater than zero." }, "Validation failed.");
		}

		await _scriptRepository.UpdateScriptStatusAsync(dto, "Admin", "127.0.0.1", cancellationToken);
		InvalidateScriptCache();

		return ApiResponse<bool>.SuccessResult(true, "Script active status updated successfully.");
	}

	public async Task<ApiResponse<List<ScriptVersionResponseDto>>> GetVersionHistoryAsync(long scriptId, CancellationToken cancellationToken = default)
	{
		if (scriptId <= 0)
		{
			return ApiResponse<List<ScriptVersionResponseDto>>.FailureResult(new[] { "ScriptId must be greater than zero." }, "Validation failed.");
		}

		var versions = await _scriptRepository.GetVersionHistoryAsync(scriptId, cancellationToken);

		return ApiResponse<List<ScriptVersionResponseDto>>.SuccessResult(versions, "Script version history retrieved successfully.");
	}

	public async Task<ApiResponse<byte[]>> DownloadScriptAsync(long scriptId, CancellationToken cancellationToken = default)
	{
		if (scriptId <= 0)
		{
			return ApiResponse<byte[]>.FailureResult(new[] { "ScriptId must be greater than zero." }, "Validation failed.");
		}

		var script = await _scriptRepository.GetScriptByIdAsync(scriptId, cancellationToken);

		if (script is null)
		{
			return ApiResponse<byte[]>.FailureResult(new[] { "Script not found." }, "Script not found.");
		}

		var fileBytes = await _excelExportService.GenerateScriptExcelAsync(script);

		return ApiResponse<byte[]>.SuccessResult(fileBytes, script.ScriptTitle);
	}

	public async Task<ApiResponse<string>> GetExcelDownloadUrlAsync(long scriptId, CancellationToken cancellationToken = default)
	{
		if (scriptId <= 0)
		{
			return ApiResponse<string>.FailureResult(new[] { "ScriptId must be greater than zero." }, "Validation failed.");
		}

		var excelKey = await _scriptRepository.GetScriptExcelKeyAsync(scriptId, cancellationToken);

		if (string.IsNullOrEmpty(excelKey))
		{
			return ApiResponse<string>.FailureResult(new[] { "No Excel file found for this script." }, "Not found.");
		}

		var url = await _storageService.GetPresignedUrlAsync(
			_r2Settings.Buckets.Scripts, excelKey, _r2Settings.PresignedUrlExpiryMinutes.Scripts, cancellationToken);

		return ApiResponse<string>.SuccessResult(url, "Excel download URL generated successfully.");
	}

	public async Task<byte[]> GetSampleTemplateAsync(string? category = null, CancellationToken cancellationToken = default)
	{
		return await _excelExportService.GenerateSampleScriptTemplateAsync(category?.Trim());
	}

	private void InvalidateScriptCache()
	{
		foreach (var cacheKey in ScriptListCacheKeys.Keys)
		{
			_memoryCache.Remove(cacheKey);
			ScriptListCacheKeys.TryRemove(cacheKey, out _);
		}
	}

	private static string BuildScriptListCacheKey(ScriptSearchRequestDto dto)
	{
		var payload = JsonSerializer.Serialize(new
		{
			dto.SearchTerm,
			dto.Category,
			dto.GrammarFocusTag,
			dto.TargetAgeGroup,
			dto.PageNumber,
			dto.PageSize
		});

		var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
		var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

		return $"{CacheKeys.ScriptListPrefix}{hash}";
	}

	private static List<string> ValidateFile(IFormFile file)
	{
		var errors = new List<string>();

		if (file is null || file.Length == 0)
		{
			errors.Add("Excel file is required.");
			return errors;
		}

		if (Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase) == false)
		{
			errors.Add("Only .xlsx files are supported.");
		}

		return errors;
	}

	private static List<string> ValidateUploadRequest(ScriptUploadRequestDto dto)
	{
		var errors = new List<string>();
		var validCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"Grammar Drill",
			"Roleplay",
			"Mock Interview",
			"Interview",
			"Vocabulary Sprint",
			"Vocabulary",
			"Fluency Drill",
			"Repractice Round",
			"Repetition"
		};

		if (string.IsNullOrWhiteSpace(dto.ScriptTitle))
			errors.Add("ScriptTitle is required.");

		if (string.IsNullOrWhiteSpace(dto.Category))
			errors.Add("Category is required.");
		else if (validCategories.Contains(dto.Category.Trim()) == false)
			errors.Add("Category is invalid.");

		if (string.IsNullOrWhiteSpace(dto.GrammarFocusTag))
			errors.Add("GrammarFocusTag is required.");

		if (string.IsNullOrWhiteSpace(dto.ContextTag))
			errors.Add("ContextTag is required.");

		if (dto.ComplexityLevel is < 1 or > 5)
			errors.Add("ComplexityLevel must be between 1 and 5.");

		if (string.IsNullOrWhiteSpace(dto.TargetAgeGroup))
			errors.Add("TargetAgeGroup is required.");

		if (string.IsNullOrWhiteSpace(dto.HintLanguage))
			errors.Add("HintLanguage is required.");

		return errors;
	}

	public async Task<ApiResponse<List<ScriptAnalyticsItemDto>>> GetScriptAnalyticsAsync(string? categoryFilter, CancellationToken cancellationToken = default)
	{
		var result = await _scriptRepository.GetScriptAnalyticsAsync(categoryFilter, cancellationToken);
		return ApiResponse<List<ScriptAnalyticsItemDto>>.SuccessResult(result, "Script analytics retrieved successfully.");
	}

	public async Task<ApiResponse<ScriptPromptDataResponseDto>> GetPromptDataForCategoryAsync(string category, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(category))
			return ApiResponse<ScriptPromptDataResponseDto>.FailureResult(new[] { "Category is required." }, "Validation failed.");

		var result = await _scriptRepository.GetPromptDataForCategoryAsync(category, cancellationToken);
		return ApiResponse<ScriptPromptDataResponseDto>.SuccessResult(result, "Prompt data retrieved successfully.");
	}

	public async Task<ApiResponse<bool>> RollbackScriptVersionAsync(long scriptId, int versionNumber, CancellationToken cancellationToken = default)
	{
		if (scriptId <= 0 || versionNumber <= 0)
			return ApiResponse<bool>.FailureResult(new[] { "Invalid scriptId or versionNumber." }, "Validation failed.");

		await _scriptRepository.RollbackScriptVersionAsync(scriptId, versionNumber, "Admin", cancellationToken);
		return ApiResponse<bool>.SuccessResult(true, "Script rolled back successfully.");
	}

	public async Task<ApiResponse<long>> DuplicateScriptAsync(long scriptId, CancellationToken cancellationToken = default)
	{
		if (scriptId <= 0)
			return ApiResponse<long>.FailureResult(new[] { "Invalid scriptId." }, "Validation failed.");

		var newScriptId = await _scriptRepository.DuplicateScriptAsync(scriptId, "Admin", cancellationToken);
		return ApiResponse<long>.SuccessResult(newScriptId, "Script duplicated successfully.");
	}
}
