using System.Text.Json;
using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.LiveSession;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Domain.Enums;

namespace GoWithFlow.Application.Services;

public sealed class MistakeService : IMistakeService
{
	private readonly IMistakeRepository _mistakeRepository;

	public MistakeService(IMistakeRepository mistakeRepository)
	{
		_mistakeRepository = mistakeRepository;
	}

	public async Task<ApiResponse<bool>> SaveMistakesFromSessionAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		if (sessionId <= 0 || userId <= 0)
		{
			return ApiResponse<bool>.FailureResult(new[] { "SessionId and UserId must be greater than zero." }, "Validation failed.");
		}

		var voiceAnalyses = await _mistakeRepository.GetVoiceAnalysesBySessionAndUserIdAsync(sessionId, userId, cancellationToken);

		if (voiceAnalyses.Count == 0)
		{
			return ApiResponse<bool>.SuccessResult(true, "No voice analysis found for mistake extraction.");
		}

		var insertedCount = 0;

		foreach (var voiceAnalysis in voiceAnalyses)
		{
			var mistakes = BuildMistakesFromVoiceAnalysis(voiceAnalysis);

			foreach (var mistake in mistakes)
			{
				var alreadyExists = await _mistakeRepository.MistakeExistsAsync(
					mistake.UserId,
					mistake.SessionId,
					mistake.UtteranceId,
					mistake.MistakeType,
					mistake.MistakeDetail,
					cancellationToken);

				if (alreadyExists)
				{
					continue;
				}

				await _mistakeRepository.InsertMistakeAsync(mistake, cancellationToken);
				insertedCount++;
			}
		}

		return ApiResponse<bool>.SuccessResult(true, $"Mistake extraction completed. Inserted {insertedCount} mistake record(s).");
	}

	public async Task<ApiResponse<PagedResult<MistakeResponseDto>>> GetMistakesAsync(MistakeFilterRequestDto dto, long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0 || dto.PageNumber <= 0 || dto.PageSize <= 0)
		{
			return ApiResponse<PagedResult<MistakeResponseDto>>.FailureResult(new[] { "UserId, PageNumber, and PageSize must be greater than zero." }, "Validation failed.");
		}

		var filteredRequest = new MistakeFilterRequestDto
		{
			MistakeType = dto.MistakeType,
			IsResolved = dto.IsResolved,
			PageNumber = dto.PageNumber,
			PageSize = dto.PageSize
		};

		var result = await _mistakeRepository.GetMistakesAsync(filteredRequest, userId, cancellationToken);

		return ApiResponse<PagedResult<MistakeResponseDto>>.SuccessResult(result, "Mistakes retrieved successfully.");
	}

	public async Task<ApiResponse<MistakeSummaryResponseDto>> GetMistakeSummaryAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<MistakeSummaryResponseDto>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var result = await _mistakeRepository.GetMistakeSummaryAsync(userId, cancellationToken);

		return ApiResponse<MistakeSummaryResponseDto>.SuccessResult(result, "Mistake summary retrieved successfully.");
	}

	public async Task<ApiResponse<List<GrammarProgressResponseDto>>> GetGrammarProgressAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<List<GrammarProgressResponseDto>>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var result = await _mistakeRepository.GetGrammarProgressAsync(userId, cancellationToken);

		return ApiResponse<List<GrammarProgressResponseDto>>.SuccessResult(result, "Grammar progress retrieved successfully.");
	}

	private static List<Mistake> BuildMistakesFromVoiceAnalysis(VoiceAnalysis voiceAnalysis)
	{
		var mistakes = new List<Mistake>();
		var grammarErrors = DeserializeJson<List<GrammarErrorDto>>(voiceAnalysis.GrammarErrorsJson) ?? new List<GrammarErrorDto>();
		var pronunciationIssues = DeserializeJson<List<PronunciationIssueDto>>(voiceAnalysis.PronunciationJson) ?? new List<PronunciationIssueDto>();
		var hesitationWords = SplitCsv(voiceAnalysis.HesitationWords);

		if (hesitationWords.Count > 2)
		{
			mistakes.Add(CreateMistake(
				voiceAnalysis,
				MistakeTypeCode.HESITATION,
				$"Used fillers: {string.Join(", ", hesitationWords)}",
				$"Used fillers: {string.Join(", ", hesitationWords)}"));
		}

		foreach (var grammarError in grammarErrors)
		{
			mistakes.Add(CreateMistake(
				voiceAnalysis,
				MistakeTypeCode.GRAMMAR,
				BuildGrammarMistakeDetail(grammarError),
				NormalizeText(grammarError.ExpectedPhrase)));
		}

		if (voiceAnalysis.SpeakingSpeedWpm > 0 && voiceAnalysis.SpeakingSpeedWpm < 60)
		{
			mistakes.Add(CreateMistake(
				voiceAnalysis,
				MistakeTypeCode.SPEED,
				$"Speaking too slowly: {voiceAnalysis.SpeakingSpeedWpm} WPM",
				$"Speaking too slowly: {voiceAnalysis.SpeakingSpeedWpm} WPM"));
		}

		if (IsIncomplete(voiceAnalysis.TranscribedText, voiceAnalysis.ExpectedText))
		{
			mistakes.Add(CreateMistake(
				voiceAnalysis,
				MistakeTypeCode.INCOMPLETE,
				"Sentence was incomplete or skipped",
				NormalizeText(voiceAnalysis.ExpectedText)));
		}

		foreach (var pronunciationIssue in pronunciationIssues)
		{
			mistakes.Add(CreateMistake(
				voiceAnalysis,
				MistakeTypeCode.PRONUNCIATION,
				BuildPronunciationMistakeDetail(pronunciationIssue),
				NormalizeText(pronunciationIssue.ExpectedPhonetic)));
		}

		if (mistakes.Count == 0)
		{
			mistakes.Add(CreateMistake(
				voiceAnalysis,
				MistakeTypeCode.PRONUNCIATION,
				"Needs clearer pronunciation",
				"Needs clearer pronunciation"));
		}

		return mistakes;
	}

	private static Mistake CreateMistake(VoiceAnalysis voiceAnalysis, MistakeTypeCode mistakeType, string mistakeDetail, string? correctionText)
	{
		return new Mistake
		{
			UserId = voiceAnalysis.UserId,
			SessionId = voiceAnalysis.SessionId,
			UtteranceId = voiceAnalysis.UtteranceId,
			ScriptId = voiceAnalysis.Session?.ScriptId ?? 0,
			UtteranceText = NormalizeText(voiceAnalysis.ExpectedText),
			SpokenText = NormalizeNullableText(voiceAnalysis.TranscribedText),
			MistakeType = mistakeType.ToString(),
			MistakeDetail = TrimToLength(mistakeDetail, 256),
			GrammarTag = NormalizeNullableText(voiceAnalysis.Utterance?.GrammarTag),
			ContextTag = NormalizeNullableText(voiceAnalysis.Utterance?.ContextTag),
			CorrectionText = TrimToLength(correctionText, 512),
			CreatedBy = voiceAnalysis.User?.FullName ?? "System",
			IPAddress = voiceAnalysis.IPAddress
		};
	}

	private static string BuildGrammarMistakeDetail(GrammarErrorDto grammarError)
	{
		if (string.IsNullOrWhiteSpace(grammarError.ExpectedPhrase) == false &&
			string.IsNullOrWhiteSpace(grammarError.SpokenPhrase) == false)
		{
			return $"Expected \"{grammarError.ExpectedPhrase}\", heard \"{grammarError.SpokenPhrase}\"";
		}

		if (string.IsNullOrWhiteSpace(grammarError.ExpectedPhrase) == false)
		{
			return $"Missing or incorrect words: {grammarError.ExpectedPhrase}";
		}

		return NormalizeText(grammarError.ErrorType);
	}

	private static string BuildPronunciationMistakeDetail(PronunciationIssueDto pronunciationIssue)
	{
		if (string.IsNullOrWhiteSpace(pronunciationIssue.IssueNote) == false)
		{
			return NormalizeText(pronunciationIssue.IssueNote);
		}

		if (string.IsNullOrWhiteSpace(pronunciationIssue.Word) == false)
		{
			return $"Needs clearer pronunciation for: {pronunciationIssue.Word}";
		}

		return "Needs clearer pronunciation";
	}

	private static bool IsIncomplete(string? transcribedText, string expectedText)
	{
		var expectedWordCount = CountWords(expectedText);

		if (expectedWordCount == 0)
		{
			return false;
		}

		var transcribedWordCount = CountWords(transcribedText);
		return transcribedWordCount < Math.Ceiling(expectedWordCount * 0.5m);
	}

	private static int CountWords(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return 0;
		}

		return value
			.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Length;
	}

	private static T? DeserializeJson<T>(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return default;
		}

		return JsonSerializer.Deserialize<T>(json);
	}

	private static List<string> SplitCsv(string? csv)
	{
		if (string.IsNullOrWhiteSpace(csv))
		{
			return new List<string>();
		}

		return csv
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.ToList();
	}

	private static string NormalizeText(string value)
	{
		return value.Trim();
	}

	private static string? NormalizeNullableText(string? value)
	{
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}

	private static string? TrimToLength(string? value, int maxLength)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		var trimmedValue = value.Trim();
		return trimmedValue.Length <= maxLength ? trimmedValue : trimmedValue[..maxLength];
	}
}
