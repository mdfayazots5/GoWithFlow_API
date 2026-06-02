using ClosedXML.Excel;
using GoWithFlow.Application.DTOs.Requests.Admin;
using GoWithFlow.Application.DTOs.Responses.Admin;
using GoWithFlow.Application.DTOs.Responses.Script;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

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

	public async Task<byte[]> GenerateSampleScriptTemplateAsync(string? category = null)
	{
		var categoryKey = category?.Trim() ?? string.Empty;

		// Try to pull real utterances from the most recently uploaded active script for this category
		var liveRows = await GetLiveSampleRowsAsync(categoryKey);

		using var workbook = new XLWorkbook();
		var worksheet = workbook.Worksheets.Add("ScriptTemplate");

		WriteScriptTemplateHeader(worksheet, categoryKey);

		if (liveRows.Count > 0)
		{
			WriteLiveSampleRows(worksheet, liveRows);
		}
		else
		{
			WriteScriptTemplateSampleRows(worksheet, categoryKey);
		}

		WriteScriptTemplateGuideSheet(workbook, categoryKey);
		worksheet.Columns().AdjustToContents();

		using var stream = new MemoryStream();
		workbook.SaveAs(stream);
		return stream.ToArray();
	}

	private async Task<List<LiveSampleRow>> GetLiveSampleRowsAsync(string category)
	{
		if (string.IsNullOrWhiteSpace(category)) return new List<LiveSampleRow>();

		// Canonical + legacy aliases
		var aliases = GetCategoryAliases(category);

		// Find the most recently uploaded active script for this category
		var script = await _dbContext.Scripts.AsNoTracking()
			.Where(s => s.IsDeleted == false && s.IsActive && aliases.Contains(s.Category))
			.OrderByDescending(s => s.UploadedDate)
			.Select(s => new { s.ScriptId, s.GrammarFocusTag, s.ContextTag })
			.FirstOrDefaultAsync();

		if (script is null) return new List<LiveSampleRow>();

		// Pull the first 4 utterances from that script
		var utterances = await _dbContext.Utterances.AsNoTracking()
			.Where(u => u.ScriptId == script.ScriptId && u.IsDeleted == false)
			.OrderBy(u => u.SequenceId)
			.Take(4)
			.Select(u => new LiveSampleRow
			{
				SequenceId        = u.SequenceId,
				SpeakerLabel      = u.SpeakerLabel,
				EnglishText       = u.EnglishText,
				HintText          = u.HintText ?? string.Empty,
				GrammarTag        = u.GrammarTag ?? string.Empty,
				ContextTag        = u.ContextTag ?? string.Empty,
				FocusWord         = u.FocusWord ?? string.Empty,
				PronunciationNote = u.PronunciationNote ?? string.Empty
			})
			.ToListAsync();

		return utterances;
	}

	private sealed class LiveSampleRow
	{
		public int    SequenceId        { get; set; }
		public string SpeakerLabel      { get; set; } = string.Empty;
		public string EnglishText       { get; set; } = string.Empty;
		public string HintText          { get; set; } = string.Empty;
		public string GrammarTag        { get; set; } = string.Empty;
		public string ContextTag        { get; set; } = string.Empty;
		public string FocusWord         { get; set; } = string.Empty;
		public string PronunciationNote { get; set; } = string.Empty;
	}

	private static HashSet<string> GetCategoryAliases(string category) => category.ToUpperInvariant() switch
	{
		"MOCK INTERVIEW"    => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Mock Interview", "Interview" },
		"VOCABULARY SPRINT" => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Vocabulary Sprint", "Vocabulary" },
		"REPRACTICE ROUND"  => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Repractice Round", "Repetition" },
		_                   => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { category }
	};

	private static void WriteLiveSampleRows(IXLWorksheet ws, List<LiveSampleRow> rows)
	{
		for (var i = 0; i < rows.Count; i++)
		{
			var r = rows[i];
			AddTemplateRow(ws, i + 2, r.SequenceId, r.SpeakerLabel, r.EnglishText,
				r.HintText, r.GrammarTag, r.ContextTag, r.FocusWord, r.PronunciationNote);
		}
	}

	// ── Color constants for category-specific column headers ──────────────────

	private static readonly XLColor RequiredHeaderColor    = XLColor.FromHtml("#1E5AA8"); // blue  — required all categories
	private static readonly XLColor MandatoryHeaderColor   = XLColor.FromHtml("#E07B39"); // orange — mandatory for this category
	private static readonly XLColor OptionalHeaderColor    = XLColor.FromHtml("#6B7280"); // gray  — optional
	private static readonly XLColor NotUsedHeaderColor     = XLColor.FromHtml("#9CA3AF"); // light gray — not used

	private static void WriteScriptTemplateHeader(IXLWorksheet ws, string category)
	{
		var headers = new[]
		{
			"SequenceId", "SpeakerLabel", "EnglishText", "HintText",
			"GrammarTag", "ContextTag", "FocusWord", "PronunciationNote"
		};

		for (var col = 0; col < headers.Length; col++)
		{
			var cell = ws.Cell(1, col + 1);
			cell.Value = headers[col];
			cell.Style.Font.Bold = true;
			cell.Style.Font.FontColor = XLColor.White;
			cell.Style.Fill.BackgroundColor = ResolveHeaderColor(category, col + 1);
		}

		// Column comments
		ws.Cell(1, 1).CreateComment().AddText("Sequential line number. Must be a positive integer, unique within the file.");
		ws.Cell(1, 2).CreateComment().AddText(ResolveSpeakerLabelComment(category));
		ws.Cell(1, 3).CreateComment().AddText("English sentence the speaker reads aloud. Required. Max 512 characters.");
		ws.Cell(1, 4).CreateComment().AddText(ResolveHintTextComment(category));
		ws.Cell(1, 5).CreateComment().AddText(ResolveGrammarTagComment(category));
		ws.Cell(1, 6).CreateComment().AddText("Context tag matching the session metadata (e.g. Office, Airport, HR Interview). Required.");
		ws.Cell(1, 7).CreateComment().AddText(ResolveFocusWordComment(category));
		ws.Cell(1, 8).CreateComment().AddText(ResolvePronunciationNoteComment(category));
	}

	private static XLColor ResolveHeaderColor(string category, int column)
	{
		// Columns A(1) B(2) C(3) F(6) are required for all categories
		if (column is 1 or 2 or 3 or 6) return RequiredHeaderColor;

		return category.ToUpperInvariant() switch
		{
			"VOCABULARY SPRINT" or "VOCABULARY" => column switch
			{
				4 => MandatoryHeaderColor,  // HintText — mandatory
				5 => OptionalHeaderColor,   // GrammarTag — optional
				7 => MandatoryHeaderColor,  // FocusWord — mandatory
				8 => MandatoryHeaderColor,  // PronunciationNote — mandatory on Tutor rows
				_ => OptionalHeaderColor
			},
			"REPRACTICE ROUND" or "REPETITION" => column switch
			{
				4 => MandatoryHeaderColor,  // HintText — mandatory
				5 => MandatoryHeaderColor,  // GrammarTag — mandatory (same tag all rows)
				7 => OptionalHeaderColor,
				8 => OptionalHeaderColor,
				_ => OptionalHeaderColor
			},
			"MOCK INTERVIEW" or "INTERVIEW" => column switch
			{
				4 => OptionalHeaderColor,
				5 => MandatoryHeaderColor,  // GrammarTag — required (STAR Method / Formal Register)
				7 => MandatoryHeaderColor,  // FocusWord — required (professional vocab)
				8 => OptionalHeaderColor,
				_ => OptionalHeaderColor
			},
			"FLUENCY DRILL" => column switch
			{
				4 => NotUsedHeaderColor,    // HintText — not used
				5 => NotUsedHeaderColor,    // GrammarTag — not used
				7 => NotUsedHeaderColor,    // FocusWord — not used
				8 => NotUsedHeaderColor,    // PronunciationNote — not used
				_ => OptionalHeaderColor
			},
			// Grammar Drill and Roleplay
			_ => column switch
			{
				4 => OptionalHeaderColor,
				5 => column == 5 ? MandatoryHeaderColor : OptionalHeaderColor,
				7 => OptionalHeaderColor,
				8 => OptionalHeaderColor,
				_ => OptionalHeaderColor
			}
		};
	}

	private static void WriteScriptTemplateSampleRows(IXLWorksheet ws, string category)
	{
		switch (category.ToUpperInvariant())
		{
			case "MOCK INTERVIEW":
			case "INTERVIEW":
				AddTemplateRow(ws, 2, 1, "Interviewer", "Tell me about yourself and your professional background.", "", "Formal Register", "HR Interview", "professional", "Pro-FE-ssion-al — stress the second syllable.");
				AddTemplateRow(ws, 3, 2, "Candidate",   "I have been working as a software engineer for three years, focusing on backend development.", "Nenu teen samvatsaraalu software engineer ga panichestunna.", "STAR Method", "HR Interview", "engineer", "EN-gi-neer — three syllables.");
				AddTemplateRow(ws, 4, 3, "Interviewer", "What would you say is your greatest strength?", "", "Formal Register", "HR Interview", "strength", "Stress: STRENGTH — one syllable.");
				AddTemplateRow(ws, 5, 4, "Candidate",   "My greatest strength is my ability to break down complex problems into manageable steps.", "Naa gurinchi cheppukovadam ayithe...", "STAR Method", "HR Interview", "manageable", "MAN-age-a-ble — four syllables.");
				break;

			case "VOCABULARY SPRINT":
			case "VOCABULARY":
				AddTemplateRow(ws, 2, 1, "Tutor",   "Today's word is determined. She is a very determined person who never gives up.", "Neyyi determined ani word nerchukuntaam. Determined ante chaaala committed.", "Present Simple", "General", "determined", "de-TER-mined — stress the second syllable.");
				AddTemplateRow(ws, 3, 2, "Learner", "I agree. She is so determined that she finished the project on time.", "Nenu agree avutunna. Ame chaala determined ga project complete chesindi.", "Present Simple", "General", "determined", "de-TER-mined");
				AddTemplateRow(ws, 4, 3, "Tutor",   "Good! Next word is collaborate. When we collaborate, we work together as a team.", "Collaborate ante kalinchi pani cheyyadam.", "Present Simple", "Workplace", "collaborate", "col-LAB-o-rate — stress the second syllable.");
				AddTemplateRow(ws, 5, 4, "Learner", "At my school, students collaborate on group projects every week.", "", "Present Simple", "Workplace", "collaborate", "col-LAB-o-rate");
				break;

			case "REPRACTICE ROUND":
			case "REPETITION":
				AddTemplateRow(ws, 2, 1, "Coach",   "Last time you said 'I go to the market yesterday.' The correct form is: I went to the market yesterday.", "Meeru prayatinchindi — 'went' vaadhali, 'go' kaadu.", "Past Simple", "General", "went", "WENT — rhymes with bent.");
				AddTemplateRow(ws, 3, 2, "Learner", "I went to the market yesterday and bought some vegetables.", "Nenu yesterday market ki vellanu.", "Past Simple", "General", "went", "");
				AddTemplateRow(ws, 4, 3, "Coach",   "Excellent! Now try: She went to the office early this morning.", "Correct form: went. Past simple of go.", "Past Simple", "General", "went", "");
				AddTemplateRow(ws, 5, 4, "Learner", "She went to the office early this morning to prepare for the meeting.", "", "Past Simple", "General", "went", "");
				break;

			case "FLUENCY DRILL":
				AddTemplateRow(ws, 2, 1, "Speaker A", "What time did you wake up today?", "", "", "Morning Routine", "", "");
				AddTemplateRow(ws, 3, 2, "Speaker B", "I woke up at six. What about you?", "", "", "Morning Routine", "", "");
				AddTemplateRow(ws, 4, 3, "Speaker A", "Same here. Did you have breakfast?", "", "", "Morning Routine", "", "");
				AddTemplateRow(ws, 5, 4, "Speaker B", "Yes, I had toast and coffee. It was quick.", "", "", "Morning Routine", "", "");
				break;

			case "ROLEPLAY":
				AddTemplateRow(ws, 2, 1, "Passenger",     "Excuse me, I'd like to check in for my flight to Mumbai.", "Nenu Mumbai ki check-in cheyyaali.", "Polite Request", "Airport", "check-in", "CHECK-in — stress CHECK.");
				AddTemplateRow(ws, 3, 2, "Check-In Agent", "Of course. Could I see your passport and booking confirmation, please?", "", "Polite Request", "Airport", "confirmation", "con-firm-A-tion — stress third syllable.");
				AddTemplateRow(ws, 4, 3, "Passenger",     "Here you go. I'd prefer a window seat if possible.", "Naku window seat kaavali.", "Conditional", "Airport", "prefer", "pre-FER — stress second syllable.");
				AddTemplateRow(ws, 5, 4, "Check-In Agent", "I've managed to get you a window seat in row twelve. Your gate opens at two-thirty.", "", "Present Perfect", "Airport", "managed", "MAN-aged — stress first syllable.");
				break;

			default: // Grammar Drill
				AddTemplateRow(ws, 2, 1, "Speaker A", "I have been working on this project since Monday.", "Nenu Monday nundi ee project meeda pani chestunna.", "Present Perfect Continuous", "Office", "working", "Stress the first syllable in working.");
				AddTemplateRow(ws, 3, 2, "Speaker B", "How long have you been waiting for the approval?", "Meeru enni rojula nundi approval kosam wait chestunnaru?", "Present Perfect Continuous", "Office", "waiting", "Stress the first syllable in waiting.");
				AddTemplateRow(ws, 4, 3, "Speaker A", "She has been learning English for three years now.", "Ame miru samvatsaraala nundi English nerchukuntundi.", "Present Perfect Continuous", "Office", "learning", "LEARN-ing — two syllables.");
				break;
		}
	}

	private static void WriteScriptTemplateGuideSheet(XLWorkbook workbook, string category)
	{
		var guide = workbook.Worksheets.Add("Guide");
		guide.Cell(1, 1).Value = "Column";
		guide.Cell(1, 2).Value = "Status for this category";
		guide.Cell(1, 3).Value = "Description";
		guide.Range(1, 1, 1, 3).Style.Font.Bold = true;
		guide.Range(1, 1, 1, 3).Style.Fill.BackgroundColor = RequiredHeaderColor;
		guide.Range(1, 1, 1, 3).Style.Font.FontColor = XLColor.White;

		var guideData = ResolveGuideRows(category);
		for (var i = 0; i < guideData.Count; i++)
		{
			guide.Cell(i + 2, 1).Value = guideData[i].Column;
			guide.Cell(i + 2, 2).Value = guideData[i].Status;
			guide.Cell(i + 2, 3).Value = guideData[i].Description;
		}

		guide.Columns().AdjustToContents();
	}

	private record GuideRow(string Column, string Status, string Description);

	private static List<GuideRow> ResolveGuideRows(string category)
	{
		var cat = category.ToUpperInvariant();

		return new List<GuideRow>
		{
			new("A — SequenceId",        "REQUIRED",           "Sequential line number, positive integer, unique per file."),
			new("B — SpeakerLabel",      "REQUIRED",           ResolveSpeakerLabelComment(category)),
			new("C — EnglishText",       "REQUIRED",           "English sentence read aloud. Max 512 characters."),
			new("D — HintText",          ResolveColumnStatus(cat, 4), ResolveHintTextComment(category)),
			new("E — GrammarTag",        ResolveColumnStatus(cat, 5), ResolveGrammarTagComment(category)),
			new("F — ContextTag",        "REQUIRED",           "Context tag matching session metadata (e.g. Office, Airport)."),
			new("G — FocusWord",         ResolveColumnStatus(cat, 7), ResolveFocusWordComment(category)),
			new("H — PronunciationNote", ResolveColumnStatus(cat, 8), ResolvePronunciationNoteComment(category)),
		};
	}

	private static string ResolveColumnStatus(string cat, int col) => cat switch
	{
		"VOCABULARY SPRINT" or "VOCABULARY" => col switch
		{
			4 => "MANDATORY", 5 => "OPTIONAL", 7 => "MANDATORY", 8 => "MANDATORY (Tutor rows)", _ => "OPTIONAL"
		},
		"REPRACTICE ROUND" or "REPETITION" => col switch
		{
			4 => "MANDATORY", 5 => "MANDATORY (same value all rows)", 7 => "OPTIONAL", 8 => "OPTIONAL", _ => "OPTIONAL"
		},
		"MOCK INTERVIEW" or "INTERVIEW" => col switch
		{
			4 => "OPTIONAL", 5 => "MANDATORY", 7 => "MANDATORY", 8 => "OPTIONAL", _ => "OPTIONAL"
		},
		"FLUENCY DRILL" => col switch
		{
			4 => "NOT USED", 5 => "NOT USED", 7 => "NOT USED", 8 => "NOT USED", _ => "OPTIONAL"
		},
		_ => col switch { 4 => "OPTIONAL", 5 => "MANDATORY (same value all rows)", 7 => "OPTIONAL", 8 => "OPTIONAL", _ => "OPTIONAL" }
	};

	private static string ResolveSpeakerLabelComment(string category) => category.ToUpperInvariant() switch
	{
		"MOCK INTERVIEW" or "INTERVIEW"       => "Use exactly 'Interviewer' or 'Candidate'. No other values.",
		"VOCABULARY SPRINT" or "VOCABULARY"   => "Use exactly 'Tutor' or 'Learner'. No other values.",
		"REPRACTICE ROUND" or "REPETITION"    => "Use exactly 'Coach' or 'Learner'. No other values.",
		"FLUENCY DRILL"                        => "Use 'Speaker A' or 'Speaker B'. No other values.",
		"ROLEPLAY"                             => "Use two real-world role names matching your scenario (e.g. Passenger, Check-In Agent). Do not use Speaker A/B.",
		_                                      => "Use 'Speaker A' or 'Speaker B' for Grammar Drill."
	};

	private static string ResolveHintTextComment(string category) => category.ToUpperInvariant() switch
	{
		"VOCABULARY SPRINT" or "VOCABULARY" => "MANDATORY. Native language translation of the sentence. Confirms word comprehension.",
		"REPRACTICE ROUND" or "REPETITION"  => "MANDATORY. Native language translation. Helps learner understand the error being corrected.",
		"FLUENCY DRILL"                      => "NOT USED. Leave blank — translations slow the reading pace for fluency drill.",
		_                                    => "Optional. Native language translation of the sentence."
	};

	private static string ResolveGrammarTagComment(string category) => category.ToUpperInvariant() switch
	{
		"MOCK INTERVIEW" or "INTERVIEW"     => "MANDATORY. Tag the answer structure: STAR Method, Formal Register, Past Simple, etc.",
		"VOCABULARY SPRINT" or "VOCABULARY" => "Optional. Only tag if the sentence demonstrates a specific grammar use.",
		"FLUENCY DRILL"                      => "NOT USED. Leave blank. Fluency Drill does not target grammar structures.",
		"REPRACTICE ROUND" or "REPETITION"  => "MANDATORY. Must be identical on ALL rows — the single error pattern being corrected.",
		_                                    => "MANDATORY. Must be identical on ALL rows — the grammar structure being practiced."
	};

	private static string ResolveFocusWordComment(string category) => category.ToUpperInvariant() switch
	{
		"VOCABULARY SPRINT" or "VOCABULARY" => "MANDATORY. The vocabulary word introduced in this turn. Each Tutor turn introduces exactly one word.",
		"MOCK INTERVIEW" or "INTERVIEW"     => "MANDATORY. Professional or industry-specific term in this turn.",
		"FLUENCY DRILL"                      => "NOT USED. Leave blank.",
		_                                    => "Optional. The key word to emphasize during practice."
	};

	private static string ResolvePronunciationNoteComment(string category) => category.ToUpperInvariant() switch
	{
		"VOCABULARY SPRINT" or "VOCABULARY" => "MANDATORY on Tutor rows. IPA pronunciation for the FocusWord. Optional on Learner rows.",
		"FLUENCY DRILL"                      => "NOT USED. Leave blank.",
		_                                    => "Optional. IPA or plain-English pronunciation coaching note for the FocusWord."
	};

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
