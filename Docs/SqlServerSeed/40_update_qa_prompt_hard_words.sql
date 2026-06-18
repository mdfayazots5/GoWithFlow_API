-- SQL Server seed 40: update the "Question & Answer" prompt to generate the Hard Words aid (2026-06-18).
-- PostgreSQL equivalent: Backend/Docs/PostgreSQLMigration/40_update_qa_prompt_hard_words.sql.
-- PromptText is identical to the PostgreSQL migration; the only difference is doubled apostrophes ('').
-- Idempotent: an UPDATE keyed on Category. UNAPPLIED unless SQL Server becomes the active provider.

UPDATE dbo.tblScriptPromptTemplate
SET SourceRef = N'ExcelTemplateStandard.md §6.7, §7.1, §9, §10',
    PromptText = N'
Generate a GoWithFlow script for the category: Question & Answer.

Output strict JSON only — no explanation, no markdown, no code fences.

PURPOSE (ExcelTemplateStandard.md §6.7):
AI-driven mock interview. An AI voice reads each Interviewer question ALOUD; the candidate listens and
answers OUT LOUD in their OWN words — they never see the question or the model answer on screen during
the session. This builds real interview confidence, spoken fluency, and the ability to explain answers
under pressure (designed for learners in Hyderabad / Telangana / Andhra Pradesh and similar regions).

KEY DIFFERENCE FROM MOCK INTERVIEW:
- The "Candidate" rows are MODEL answers used only as a hidden reference (post-session report comparison
  and AI evaluation). They are NEVER shown to the candidate live. Write them as strong example answers.
- During the session the candidate hears the question (AI TTS) and speaks freely; the recognizer captures
  their actual words. So make Interviewer questions clear, self-contained, and easy to follow BY EAR.

SPEAKER LABELS (§6.7 — mandatory):
- Interviewer — asks one clear question per turn, read aloud by the AI (FACILITATOR turn — not scored).
- Candidate   — model answer, hidden on screen; reference only (PERFORMANCE turn — the candidate''s own
                spoken answer is what gets scored, not this text).
Always use exactly "Interviewer" and "Candidate". No aliases.

COLUMN RULES (§6.7):
- A (SequenceId):       REQUIRED — sequential integers; alternate Interviewer then Candidate.
- B (SpeakerLabel):     REQUIRED — strictly "Interviewer" or "Candidate".
- C (EnglishText):      REQUIRED — Interviewer: one clear spoken question, NO contractions, easy to follow
                        by ear. Candidate: a strong model answer of 2–4 sentences (hidden reference).
- D (HintText):         OPTIONAL — native-language translation (helps the report review, not shown live).
- E (GrammarTag):       REQUIRED — e.g. "STAR Method", "Formal Register", "Question Forms", "Past Simple",
                        "Present Perfect", "Conditionals".
- F (ContextTag):       REQUIRED — interview type, consistent throughout: "HR Interview", "Tech Interview",
                        "Behavioural Interview", "Sales Interview", "Management Interview".
- G (FocusWord):        OPTIONAL — a professional/industry term relevant to the question or answer.
- H (PronunciationNote):OPTIONAL — IPA for the FocusWord.
- I (HardWords):        OPTIONAL — Question & Answer ONLY. On INTERVIEWER rows, 2–4 hard/important words the
                        candidate should understand and try to USE in the answer. Format as
                        ''word:short meaning'' pairs separated by '' | ''
                        (e.g. "mitigate:to reduce harm | leverage:to make use of | stakeholder:person with an interest").
                        Keep meanings to 2–5 plain words. Leave BLANK on Candidate rows.

CONTENT RULES (§6.7):
1. Script length: minimum 16 rows, maximum 40 rows (≈ 8–20 question/answer pairs).
2. Every Interviewer turn is ONE complete, self-contained question — answerable without seeing it written.
3. Keep Interviewer questions to a single idea each (no compound "and also..." questions) — they are heard, not read.
4. Each Candidate model answer is 2–4 sentences; behavioural questions should model the STAR structure.
5. No contractions in Interviewer turns (formal register, clear for TTS).
6. At most 2 opening small-talk turns; then straight into substance.
7. Include at least one challenging follow-up probe by the Interviewer.
8. ContextTag identical on every row (same interview type).
9. Close with a natural interview ending (candidate-style closing answer or mutual farewell).
10. Strictly alternate Interviewer → Candidate; no speaker has 2 consecutive turns.
11. HardWords (Column I): provide 2–4 ''word:meaning'' pairs on EVERY Interviewer row where useful vocabulary
    applies; pick words that are genuinely relevant to that question/answer. Leave blank on Candidate rows.

METADATA DEFAULTS (§7.1):
- category: "Question & Answer"
- grammarFocusTag: "STAR Method" / "Formal Register" / "Question Forms" / "Past Simple" / "Present Perfect"
- contextTag: "HR Interview" / "Tech Interview" / "Behavioural Interview"
- complexityLevel: 3–5 (Intermediate to Advanced)
- targetAgeGroup: "Adult"
- hintLanguage: "Telugu" / "Hindi" / "Tamil" / "Kannada" / "None"

JSON OUTPUT CONTRACT (§10.1):
{
  "metadata": {
    "scriptTitle": "[interview title e.g. HR Interview — Self Introduction & Strengths]",
    "category": "Question & Answer",
    "grammarFocusTag": "[one tag e.g. STAR Method]",
    "contextTag": "[interview type e.g. HR Interview]",
    "complexityLevel": 4,
    "targetAgeGroup": "Adult",
    "hintLanguage": "Telugu"
  },
  "rows": [
    {
      "sequenceId": 1,
      "speakerLabel": "Interviewer",
      "englishText": "[one clear spoken question — no contractions]",
      "hintText": "[translation or empty]",
      "grammarTag": "[grammar/method tag]",
      "contextTag": "[same as metadata contextTag — every row]",
      "focusWord": "[professional term or empty]",
      "pronunciationNote": "[IPA or empty]",
      "hardWords": "[2–4 ''word:meaning'' pairs separated by '' | '', or empty]"
    },
    {
      "sequenceId": 2,
      "speakerLabel": "Candidate",
      "englishText": "[strong 2–4 sentence MODEL answer — hidden reference, not shown live]",
      "hintText": "[translation or empty]",
      "grammarTag": "[grammar/method tag]",
      "contextTag": "[same as metadata contextTag]",
      "focusWord": "[professional term or empty]",
      "pronunciationNote": "[IPA or empty]",
      "hardWords": ""
    }
  ]
}

SELF-VALIDATION BEFORE OUTPUT (§10.2):
1. SequenceId: starts at 1, sequential, no duplicates.
2. SpeakerLabel: only "Interviewer" or "Candidate", strictly alternating.
3. No contractions in any Interviewer turn.
4. Each Interviewer turn is one self-contained question, clear when heard aloud.
5. Each Candidate model answer is 2–4 sentences.
6. GrammarTag (E) present on all rows; ContextTag identical on every row.
7. FocusWord (if used) appears verbatim in the same row''s EnglishText.
8. HardWords (if used) only on Interviewer rows, formatted as ''word:meaning'' pairs separated by '' | '';
   blank on Candidate rows.
9. Row count: 16–40.
10. Output is valid JSON — no markdown, no code fences, no trailing commas.
', N'ExcelTemplateStandard.md §6.7, §7.1, §9, §10'
WHERE Category = N'Question & Answer';
