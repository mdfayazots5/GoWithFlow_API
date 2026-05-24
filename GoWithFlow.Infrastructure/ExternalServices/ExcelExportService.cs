using ClosedXML.Excel;
using GoWithFlow.Application.DTOs.Requests.Admin;
using GoWithFlow.Application.DTOs.Responses.Admin;
using GoWithFlow.Application.DTOs.Responses.Script;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoWithFlow.Infrastructure.ExternalServices;

public sealed class ExcelExportService : IExcelExportService
{
	private readonly GoWithFlowDbContext _dbContext;

	public ExcelExportService(GoWithFlowDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<byte[]> GenerateUserReportExcelAsync(AdminReportFilterRequestDto filter)
	{
		// ── Session detail rows (EF Core — provider-agnostic) ──────────────────
		var sessionDetailRows = await (
			from sessionMember in _dbContext.SessionMembers.AsNoTracking()
			join user    in _dbContext.Users.AsNoTracking()    on sessionMember.UserId    equals user.UserId
			join session in _dbContext.Sessions.AsNoTracking() on sessionMember.SessionId equals session.SessionId
			where sessionMember.IsDeleted == false
				&& user.IsDeleted    == false
				&& session.IsDeleted == false
				&& (filter.UserId.HasValue    == false || filter.UserId.Value    == 0 || user.UserId == filter.UserId.Value)
				&& (filter.FromDate.HasValue  == false || (session.StartedDate ?? session.DateCreated) >= filter.FromDate.Value)
				&& (filter.ToDate.HasValue    == false || (session.StartedDate ?? session.DateCreated) <  filter.ToDate.Value.Date.AddDays(1))
			orderby user.UserId, session.SessionId
			select new UserReportExportSessionDetailDto
			{
				UserId      = user.UserId,
				FullName    = user.FullName,
				SessionId   = session.SessionId,
				SessionName = session.SessionName,
				SessionDate = session.StartedDate ?? session.DateCreated,
				FluencyScore = _dbContext.VoiceAnalyses
					.Where(va => va.SessionId == session.SessionId && va.UserId == user.UserId && va.IsDeleted == false)
					.Average(va => (decimal?)va.FluencyScore) ?? 0m,
				MistakeCount = _dbContext.Mistakes
					.Count(m => m.SessionId == session.SessionId && m.UserId == user.UserId && m.IsDeleted == false),
				Status = session.Status
			})
			.ToListAsync();

		// ── Resolved mistakes per user (needed for ImprovementPercent) ─────────
		var resolvedByUser = await (
			from m in _dbContext.Mistakes.AsNoTracking()
			join s in _dbContext.Sessions.AsNoTracking() on m.SessionId equals s.SessionId
			where m.IsDeleted    == false
				&& m.IsResolved  == true
				&& s.IsDeleted   == false
				&& (filter.UserId.HasValue   == false || filter.UserId.Value   == 0 || m.UserId == filter.UserId.Value)
				&& (filter.FromDate.HasValue == false || (s.StartedDate ?? s.DateCreated) >= filter.FromDate.Value)
				&& (filter.ToDate.HasValue   == false || (s.StartedDate ?? s.DateCreated) <  filter.ToDate.Value.Date.AddDays(1))
			group m by m.UserId into g
			select new { UserId = g.Key, ResolvedCount = g.Count() })
			.ToDictionaryAsync(x => x.UserId, x => x.ResolvedCount);

		// ── Summary: aggregate session detail rows in-memory ──────────────────
		var summaryRows = sessionDetailRows
			.GroupBy(r => new { r.UserId, r.FullName })
			.OrderBy(g => g.Key.FullName).ThenBy(g => g.Key.UserId)
			.Select(g =>
			{
				var totalMistakes    = g.Sum(r => r.MistakeCount);
				var resolvedMistakes = resolvedByUser.TryGetValue(g.Key.UserId, out var rc) ? rc : 0;
				return new UserReportExportSummaryDto
				{
					UserId    = g.Key.UserId,
					FullName  = g.Key.FullName,
					TotalSessions     = g.Count(),
					AvgScore          = g.Any(r => r.FluencyScore > 0)
						? Math.Round(g.Average(r => r.FluencyScore), 2)
						: 0m,
					TotalMistakes     = totalMistakes,
					ImprovementPercent = totalMistakes == 0 ? 0m
						: Math.Round((decimal)resolvedMistakes * 100m / totalMistakes, 2)
				};
			})
			.ToList();

		using var workbook = new XLWorkbook();
		WriteUserSummarySheet(workbook, summaryRows);
		WriteSessionDetailSheet(workbook, sessionDetailRows);

		using var stream = new MemoryStream();
		workbook.SaveAs(stream);

		return stream.ToArray();
	}

	public Task<byte[]> GenerateSampleScriptTemplateAsync()
	{
		using var workbook = new XLWorkbook();
		var worksheet = workbook.Worksheets.Add("ScriptTemplate");

		var headers = new[]
		{
			"SequenceId",
			"SpeakerLabel",
			"EnglishText",
			"HintText",
			"GrammarTag",
			"ContextTag",
			"FocusWord",
			"PronunciationNote"
		};

		WriteHeaderRow(worksheet, headers);
		AddTemplateRow(worksheet, 2, 1, "Person A", "I have been waiting here since morning.", "Nenu poddununchi ikkade wait chesthunna.", "Present Perfect Continuous", "Office", "waiting", "Stress the first syllable in waiting.");
		AddTemplateRow(worksheet, 3, 2, "Person B", "The manager will arrive in ten minutes.", "Manager inko padi nimishallo vastaru.", "Future", "Office", "manager", "Pronounce manager with a soft g.");
		AddTemplateRow(worksheet, 4, 3, "Person A", "Could you please call me after lunch?", "Lunch taruvata naku call chesthara?", "Polite Request", "Phone", "please", "Stretch the ee vowel in please.");

		AddHeaderComment(worksheet, 1, 1, "Sequential line number from the source script.");
		AddHeaderComment(worksheet, 1, 2, "Speaker label shown in the session lobby and turn order.");
		AddHeaderComment(worksheet, 1, 3, "Original English sentence the learner reads aloud.");
		AddHeaderComment(worksheet, 1, 4, "Optional native-language hint shown to the learner.");
		AddHeaderComment(worksheet, 1, 5, "Grammar focus for progress and reporting.");
		AddHeaderComment(worksheet, 1, 6, "Conversation context, such as Office or Home.");
		AddHeaderComment(worksheet, 1, 7, "Word to emphasize during speaking practice.");
		AddHeaderComment(worksheet, 1, 8, "Pronunciation coaching note for the target word or phrase.");

		worksheet.Columns().AdjustToContents();

		using var stream = new MemoryStream();
		workbook.SaveAs(stream);

		return Task.FromResult(stream.ToArray());
	}

	private static void WriteUserSummarySheet(XLWorkbook workbook, IReadOnlyCollection<UserReportExportSummaryDto> rows)
	{
		var worksheet = workbook.Worksheets.Add("User Summary");
		WriteHeaderRow(worksheet, new[]
		{
			"UserId",
			"FullName",
			"Sessions",
			"AvgFluencyScore",
			"MistakeCount",
			"ImprovementPercent"
		});

		var rowIndex = 2;

		foreach (var row in rows)
		{
			worksheet.Cell(rowIndex, 1).Value = row.UserId;
			worksheet.Cell(rowIndex, 2).Value = row.FullName;
			worksheet.Cell(rowIndex, 3).Value = row.TotalSessions;
			worksheet.Cell(rowIndex, 4).Value = row.AvgScore;
			worksheet.Cell(rowIndex, 5).Value = row.TotalMistakes;
			worksheet.Cell(rowIndex, 6).Value = row.ImprovementPercent;
			rowIndex++;
		}

		worksheet.Columns().AdjustToContents();
	}

	private static void WriteSessionDetailSheet(XLWorkbook workbook, IReadOnlyCollection<UserReportExportSessionDetailDto> rows)
	{
		var worksheet = workbook.Worksheets.Add("Session Detail");
		WriteHeaderRow(worksheet, new[]
		{
			"UserId",
			"FullName",
			"SessionId",
			"SessionName",
			"SessionDate",
			"FluencyScore",
			"MistakeCount",
			"Status"
		});

		var rowIndex = 2;

		foreach (var row in rows)
		{
			worksheet.Cell(rowIndex, 1).Value = row.UserId;
			worksheet.Cell(rowIndex, 2).Value = row.FullName;
			worksheet.Cell(rowIndex, 3).Value = row.SessionId;
			worksheet.Cell(rowIndex, 4).Value = row.SessionName;
			worksheet.Cell(rowIndex, 5).Value = row.SessionDate.ToString("yyyy-MM-dd HH:mm:ss");
			worksheet.Cell(rowIndex, 6).Value = row.FluencyScore;
			worksheet.Cell(rowIndex, 7).Value = row.MistakeCount;
			worksheet.Cell(rowIndex, 8).Value = row.Status;
			rowIndex++;
		}

		worksheet.Columns().AdjustToContents();
	}

	private static void WriteHeaderRow(IXLWorksheet worksheet, IReadOnlyList<string> headers)
	{
		for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
		{
			worksheet.Cell(1, columnIndex + 1).Value = headers[columnIndex];
		}

		var headerRange = worksheet.Range(1, 1, 1, headers.Count);
		headerRange.Style.Font.Bold = true;
		headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E5AA8");
		headerRange.Style.Font.FontColor = XLColor.White;
	}

	private static void AddTemplateRow(
		IXLWorksheet worksheet,
		int rowIndex,
		int sequenceId,
		string speakerLabel,
		string englishText,
		string hintText,
		string grammarTag,
		string contextTag,
		string focusWord,
		string pronunciationNote)
	{
		worksheet.Cell(rowIndex, 1).Value = sequenceId;
		worksheet.Cell(rowIndex, 2).Value = speakerLabel;
		worksheet.Cell(rowIndex, 3).Value = englishText;
		worksheet.Cell(rowIndex, 4).Value = hintText;
		worksheet.Cell(rowIndex, 5).Value = grammarTag;
		worksheet.Cell(rowIndex, 6).Value = contextTag;
		worksheet.Cell(rowIndex, 7).Value = focusWord;
		worksheet.Cell(rowIndex, 8).Value = pronunciationNote;
	}

	private static void AddHeaderComment(IXLWorksheet worksheet, int rowIndex, int columnIndex, string commentText)
	{
		worksheet.Cell(rowIndex, columnIndex).CreateComment().AddText(commentText);
	}

	public Task<byte[]> GenerateScriptExcelAsync(ScriptDetailResponseDto script)
	{
		using var workbook = new XLWorkbook();
		var worksheet = workbook.Worksheets.Add("Script");

		var headers = new[]
		{
			"SequenceId",
			"SpeakerLabel",
			"EnglishText",
			"HintText",
			"GrammarTag",
			"ContextTag",
			"FocusWord",
			"PronunciationNote"
		};

		WriteHeaderRow(worksheet, headers);

		var rowIndex = 2;
		foreach (var u in script.Utterances.OrderBy(u => u.SequenceId))
		{
			worksheet.Cell(rowIndex, 1).Value = u.SequenceId;
			worksheet.Cell(rowIndex, 2).Value = u.SpeakerLabel;
			worksheet.Cell(rowIndex, 3).Value = u.EnglishText;
			worksheet.Cell(rowIndex, 4).Value = u.HintText ?? string.Empty;
			worksheet.Cell(rowIndex, 5).Value = u.GrammarTag ?? string.Empty;
			worksheet.Cell(rowIndex, 6).Value = u.ContextTag ?? string.Empty;
			worksheet.Cell(rowIndex, 7).Value = u.FocusWord ?? string.Empty;
			worksheet.Cell(rowIndex, 8).Value = u.PronunciationNote ?? string.Empty;
			rowIndex++;
		}

		worksheet.Columns().AdjustToContents();

		using var stream = new MemoryStream();
		workbook.SaveAs(stream);

		return Task.FromResult(stream.ToArray());
	}

}
