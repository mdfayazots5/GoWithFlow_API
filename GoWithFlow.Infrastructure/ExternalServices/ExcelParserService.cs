using ClosedXML.Excel;
using GoWithFlow.Application.DTOs.Responses.Script;
using GoWithFlow.Application.Interfaces.Services;

namespace GoWithFlow.Infrastructure.ExternalServices;

public sealed class ExcelParserService : IExcelParserService
{
	public Task<ExcelParseResultDto> ParseAndValidateAsync(Stream fileStream)
	{
		var result = new ExcelParseResultDto();
		var sequenceIds = new HashSet<int>();

		using var workbook = new XLWorkbook(fileStream);
		var worksheet = workbook.Worksheets.First();
		var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

		for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
		{
			var rowErrors = new List<ExcelRowError>();
			var row = worksheet.Row(rowNumber);

			if (IsRowEmpty(row))
			{
				continue;
			}

			result.TotalRows++;

			var sequenceText = GetCellValue(row.Cell(1));
			var speakerLabel = GetCellValue(row.Cell(2));
			var englishText = GetCellValue(row.Cell(3));
			var hintText = GetNullableCellValue(row.Cell(4));
			var grammarTag = GetNullableCellValue(row.Cell(5));
			var contextTag = GetNullableCellValue(row.Cell(6));
			var focusWord = GetNullableCellValue(row.Cell(7));
			var pronunciationNote = GetNullableCellValue(row.Cell(8));

			var sequenceId = ValidateSequenceId(rowNumber, sequenceText, sequenceIds, rowErrors);

			if (string.IsNullOrWhiteSpace(speakerLabel))
			{
				rowErrors.Add(CreateError(rowNumber, "SpeakerLabel", "SpeakerLabel must not be empty."));
			}

			if (string.IsNullOrWhiteSpace(englishText))
			{
				rowErrors.Add(CreateError(rowNumber, "EnglishText", "EnglishText must not be empty."));
			}
			else if (englishText.Length > 512)
			{
				rowErrors.Add(CreateError(rowNumber, "EnglishText", "EnglishText cannot exceed 512 characters."));
			}

			if (hintText?.Length > 512)
			{
				rowErrors.Add(CreateError(rowNumber, "HintText", "HintText cannot exceed 512 characters."));
			}

			if (rowErrors.Count > 0)
			{
				result.ErrorRows.AddRange(rowErrors);
				continue;
			}

			result.ValidRows.Add(new UtteranceParseDto
			{
				SequenceId = sequenceId,
				SpeakerLabel = speakerLabel,
				EnglishText = englishText,
				HintText = hintText,
				GrammarTag = grammarTag,
				ContextTag = contextTag,
				FocusWord = focusWord,
				PronunciationNote = pronunciationNote
			});
		}

		result.ValidCount = result.ValidRows.Count;
		result.ErrorCount = result.ErrorRows.Count;
		result.IsValid = result.ErrorCount == 0;

		return Task.FromResult(result);
	}

	private static int ValidateSequenceId(int rowNumber, string sequenceText, ISet<int> sequenceIds, ICollection<ExcelRowError> rowErrors)
	{
		if (int.TryParse(sequenceText, out var sequenceId) == false || sequenceId <= 0)
		{
			rowErrors.Add(CreateError(rowNumber, "SequenceId", "SequenceId must be a positive integer."));
			return 0;
		}

		if (sequenceIds.Add(sequenceId) == false)
		{
			rowErrors.Add(CreateError(rowNumber, "SequenceId", "SequenceId must be unique within the file."));
		}

		return sequenceId;
	}

	private static string GetCellValue(IXLCell cell)
	{
		return cell.GetString().Trim();
	}

	private static string? GetNullableCellValue(IXLCell cell)
	{
		var value = cell.GetString().Trim();
		return string.IsNullOrWhiteSpace(value) ? null : value;
	}

	private static bool IsRowEmpty(IXLRow row)
	{
		return row.Cells(1, 8).All(cell => string.IsNullOrWhiteSpace(cell.GetString()));
	}

	private static ExcelRowError CreateError(int rowNumber, string columnName, string errorMessage)
	{
		return new ExcelRowError
		{
			RowNumber = rowNumber,
			ColumnName = columnName,
			ErrorMessage = errorMessage
		};
	}
}
