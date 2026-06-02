namespace GoWithFlow.Application.DTOs.Responses.Script;

public sealed class ScriptPromptDataResponseDto
{
    /// <summary>Distinct GrammarFocusTag values used in this category across active scripts in the DB.</summary>
    public List<string> GrammarTagsInUse { get; set; } = new();

    /// <summary>Distinct ContextTag values used in this category across active scripts in the DB.</summary>
    public List<string> ContextTagsInUse { get; set; } = new();

    /// <summary>Full approved GrammarTag list from ExcelTemplateStandard §9.3.</summary>
    public List<string> ApprovedGrammarTags { get; set; } = new();

    /// <summary>Default speaker labels for this category.</summary>
    public string SpeakerLabels { get; set; } = string.Empty;

    public int MinRows { get; set; }
    public int MaxRows { get; set; }

    public string MandatoryColumns { get; set; } = string.Empty;

    /// <summary>Total active scripts in this category currently in the DB.</summary>
    public int ActiveScriptCount { get; set; }

    /// <summary>
    /// Complete ready-to-copy Claude prompt built from ExcelTemplateStandard.md rules
    /// (§6 category rules, §8.3 row counts, §9 content standards, §10 output contract)
    /// enriched with live DB tag values.
    /// </summary>
    public string FullPrompt { get; set; } = string.Empty;
}
