-- Migration 19: Add tblscriptprompttemplate
-- Stores the Claude prompt per category, seeded from ExcelTemplateStandard.md.
-- Admins can update prompts directly in this table without code changes.

CREATE TABLE IF NOT EXISTS public.tblscriptprompttemplate (
    prompttemplateid    BIGSERIAL       NOT NULL,
    category            VARCHAR(64)     NOT NULL,
    prompttext          TEXT            NOT NULL,
    sourceref           VARCHAR(256)    NOT NULL DEFAULT 'ExcelTemplateStandard.md',
    version             INT             NOT NULL DEFAULT 1,
    isactive            BOOLEAN         NOT NULL DEFAULT TRUE,
    sortorder           INT             NOT NULL DEFAULT 0,
    ipaddress           VARCHAR(64)     NOT NULL DEFAULT '127.0.0.1',
    createdby           VARCHAR(128)    NOT NULL DEFAULT 'System',
    datecreated         TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updatedby           VARCHAR(128)    NULL,
    lastupdated         TIMESTAMPTZ     NULL,
    deletedby           VARCHAR(128)    NULL,
    datedeleted         TIMESTAMPTZ     NULL,
    isdeleted           BOOLEAN         NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_tblscriptprompttemplate PRIMARY KEY (prompttemplateid),
    CONSTRAINT uk_tblscriptprompttemplate_category UNIQUE (category)
);

CREATE INDEX IF NOT EXISTS idx_tblscriptprompttemplate_category
    ON public.tblscriptprompttemplate (category);

-- ─── SEED: one prompt per category from ExcelTemplateStandard.md ────────────

INSERT INTO public.tblscriptprompttemplate (category, prompttext, sourceref) VALUES

-- ── Grammar Drill (§6.1) ─────────────────────────────────────────────────────
('Grammar Drill', $PROMPT$
Generate a GoWithFlow script for the category: Grammar Drill.

Output strict JSON only — no explanation, no markdown, no code fences.

PURPOSE (ExcelTemplateStandard.md §6.1):
Structured grammar practice through dialogue. Each script isolates ONE grammar structure
and demonstrates it through realistic conversation between two speakers.

SPEAKER LABELS (§6.1 — mandatory):
- Speaker A  — initiates the grammar-focused dialogue
- Speaker B  — responds and mirrors the grammar pattern
No other speaker labels are valid for Grammar Drill.

COLUMN RULES (§6.1):
- A (SequenceId):       REQUIRED — sequential integers 1, 2, 3, …
- B (SpeakerLabel):     REQUIRED — strictly "Speaker A" or "Speaker B"
- C (EnglishText):      REQUIRED — must contain the target grammar structure; ≥ 60% of turns
- D (HintText):         OPTIONAL — full sentence translation in target language (not word-by-word)
- E (GrammarTag):       REQUIRED — must be IDENTICAL on ALL rows (same structure as grammarFocusTag)
- F (ContextTag):       REQUIRED — must be CONSISTENT on all rows (same scene as contextTag)
- G (FocusWord):        OPTIONAL — one word per row that carries the grammar focus
- H (PronunciationNote):OPTIONAL — IPA notation for the FocusWord

CONTENT RULES (§6.1):
1. Script length: minimum 12 rows, maximum 30 rows
2. Grammar target structure must appear at least once every 3 turns
3. All GrammarTag values in column E must be IDENTICAL across all rows
4. ContextTag in column F must be CONSISTENT (same scene throughout)
5. HintText must be full sentence translation, not word-by-word
6. FocusWord must appear verbatim in the same row's EnglishText
7. Do not introduce unexplained new vocabulary — grammar-focused, not vocab-focused
8. Avoid contractions in the first 3 rows to establish formal grammar exposure

METADATA DEFAULTS (§7.1):
- category: "Grammar Drill"
- grammarFocusTag: one specific structure e.g. "Present Perfect", "Past Simple", "Passive Voice", "Modal Verbs", "Conditionals", "Reported Speech", "Future Perfect", "Gerunds", "Infinitives"
- contextTag: scene e.g. "Office", "Hospital", "Airport", "Home", "School", "Restaurant", "Bank", "Shopping"
- complexityLevel: 1–5
- targetAgeGroup: "All" / "Teen" / "Adult"
- hintLanguage: "Telugu" / "Hindi" / "Tamil" / "Kannada" / "None"

JSON OUTPUT CONTRACT (§10.1):
{
  "metadata": {
    "scriptTitle": "[descriptive title]",
    "category": "Grammar Drill",
    "grammarFocusTag": "[one grammar structure — same on all rows]",
    "contextTag": "[scene — same on all rows]",
    "complexityLevel": 3,
    "targetAgeGroup": "Adult",
    "hintLanguage": "Telugu"
  },
  "rows": [
    {
      "sequenceId": 1,
      "speakerLabel": "Speaker A",
      "englishText": "[sentence demonstrating the grammar structure]",
      "hintText": "[Telugu translation of the full sentence]",
      "grammarTag": "[same as grammarFocusTag — identical on every row]",
      "contextTag": "[same as metadata contextTag — identical on every row]",
      "focusWord": "[word carrying the grammar target, or empty]",
      "pronunciationNote": "[IPA or empty]"
    }
  ]
}

SELF-VALIDATION BEFORE OUTPUT (§10.2):
1. SequenceId: starts at 1, sequential, no duplicates
2. SpeakerLabel: only "Speaker A" or "Speaker B"
3. GrammarTag: identical value on EVERY row
4. ContextTag: identical value on EVERY row
5. FocusWord appears verbatim in the same row's EnglishText
6. Row count: 12–30
7. No speaker has 3+ consecutive turns
8. Output is valid JSON — no markdown, no code fences, no trailing commas
$PROMPT$, 'ExcelTemplateStandard.md §6.1, §7.1, §9, §10'),

-- ── Roleplay (§6.2) ──────────────────────────────────────────────────────────
('Roleplay', $PROMPT$
Generate a GoWithFlow script for the category: Roleplay.

Output strict JSON only — no explanation, no markdown, no code fences.

PURPOSE (ExcelTemplateStandard.md §6.2):
Simulated real-world conversation scenarios. Users take on characters.
Focuses on contextual fluency, social language, and turn-taking.
Allows multiple grammar structures — prioritises natural conversation flow.

SPEAKER LABELS (§6.2 — mandatory):
Use exactly two role-based names matching the scenario. Examples:
  Airport:      Passenger / Check-In Agent
  Hotel:        Guest / Receptionist
  Doctor:       Patient / Doctor
  Restaurant:   Customer / Waiter
  Bank:         Customer / Bank Teller
  Shopping:     Customer / Sales Associate
  Pharmacy:     Customer / Pharmacist
  General:      Person A / Person B
DO NOT use "Speaker A / Speaker B" for Roleplay. Always two distinct role names.

COLUMN RULES (§6.2):
- A (SequenceId):       REQUIRED — sequential integers
- B (SpeakerLabel):     REQUIRED — character role name (see above)
- C (EnglishText):      REQUIRED — natural conversational English; contractions allowed
- D (HintText):         OPTIONAL — full sentence native language equivalent
- E (GrammarTag):       OPTIONAL — only if a specific structure is intentionally prominent
- F (ContextTag):       REQUIRED — scenario location; must match contextTag metadata
- G (FocusWord):        OPTIONAL — key scenario-specific vocabulary (e.g. boarding pass, prescription)
- H (PronunciationNote):OPTIONAL — pronunciation note for FocusWord

CONTENT RULES (§6.2):
1. Script length: minimum 16 rows, maximum 40 rows
2. Must have: beginning (greeting/problem), middle (interaction), end (resolution/farewell)
3. At least one polite phrase per 5 turns (please, thank you, excuse me, I'm sorry, certainly)
4. Vary sentence openings — avoid repetitive structures
5. GrammarTag optional — only when grammar is demonstrably intentional
6. HintText must translate the full utterance, not isolated words
7. FocusWord: scenario-specific vocabulary (e.g. boarding pass, invoice, prescription)
8. Natural conversation flow — no abrupt endings

METADATA DEFAULTS (§7.1):
- category: "Roleplay"
- grammarFocusTag: "General" or loose tag — "Politeness", "Requests", "Negotiations", "Small Talk"
- contextTag: scenario location — "Airport", "Hotel", "Doctor", "Bank", "Restaurant", "Job Office", "Police Station", "Pharmacy"
- complexityLevel: 1–5
- targetAgeGroup: "All" / "Teen" / "Adult"
- hintLanguage: "Telugu" / "Hindi" / "Tamil" / "Kannada" / "None"

JSON OUTPUT CONTRACT (§10.1):
{
  "metadata": {
    "scriptTitle": "[scenario title e.g. Airport Check-In Practice]",
    "category": "Roleplay",
    "grammarFocusTag": "[loose tag or General]",
    "contextTag": "[scenario location]",
    "complexityLevel": 3,
    "targetAgeGroup": "Adult",
    "hintLanguage": "Telugu"
  },
  "rows": [
    {
      "sequenceId": 1,
      "speakerLabel": "[role name — e.g. Passenger]",
      "englishText": "[natural conversational sentence]",
      "hintText": "[Telugu translation or empty]",
      "grammarTag": "[grammar tag or empty]",
      "contextTag": "[same as metadata contextTag]",
      "focusWord": "[scenario vocabulary word or empty]",
      "pronunciationNote": "[IPA or empty]"
    }
  ]
}

SELF-VALIDATION BEFORE OUTPUT (§10.2):
1. SequenceId: starts at 1, sequential, no duplicates
2. SpeakerLabel: exactly two distinct role names used throughout
3. No "Speaker A / Speaker B" — use real role names
4. ContextTag: identical on every row
5. FocusWord appears verbatim in the same row's EnglishText
6. Row count: 16–40
7. No speaker has 3+ consecutive turns
8. Clear beginning, middle, and end in the conversation arc
9. Output is valid JSON — no markdown, no code fences, no trailing commas
$PROMPT$, 'ExcelTemplateStandard.md §6.2, §7.1, §9, §10'),

-- ── Mock Interview (§6.3) ────────────────────────────────────────────────────
('Mock Interview', $PROMPT$
Generate a GoWithFlow script for the category: Mock Interview.

Output strict JSON only — no explanation, no markdown, no code fences.

PURPOSE (ExcelTemplateStandard.md §6.3):
Simulated interview practice for professional English.
Covers HR rounds, technical screenings, and behavioural interviews.
Users practice formal register, structured answers (STAR method), and professional vocabulary.

SPEAKER LABELS (§6.3 — mandatory):
- Interviewer — asks questions, probes for detail, challenges responses (FACILITATOR turn — not scored)
- Candidate   — answers questions using professional English (PERFORMANCE turn — scored)
Always use exactly "Interviewer" and "Candidate". No aliases.

COLUMN RULES (§6.3):
- A (SequenceId):       REQUIRED — sequential integers
- B (SpeakerLabel):     REQUIRED — strictly "Interviewer" or "Candidate"
- C (EnglishText):      REQUIRED — formal English; NO contractions in Interviewer turns
- D (HintText):         OPTIONAL — native language translation
- E (GrammarTag):       REQUIRED — "STAR Method", "Past Simple", "Conditionals", "Formal Register", "Present Perfect", "Future Simple", "Modal Verbs"
- F (ContextTag):       REQUIRED — interview type; consistent throughout: "HR Interview", "Tech Interview", "Behavioural Interview", "Sales Interview", "Management Interview"
- G (FocusWord):        REQUIRED — professional or industry-specific vocabulary term
- H (PronunciationNote):OPTIONAL — pronunciation for professional terms

CONTENT RULES (§6.3):
1. Script length: minimum 20 rows, maximum 50 rows
2. Each Interviewer turn: complete, formal question or probing statement
3. Candidate answers: ≥ 2 sentences — no single-word or single-clause answers
4. At least 40% of Candidate turns must use STAR format (Situation, Task, Action, Result)
5. FocusWord (G) must be professional or industry-specific
6. No small talk beyond 2 opening turns
7. At least one challenging follow-up probe by the Interviewer
8. All Interviewer turns in formal English — NO contractions
9. ContextTag consistent throughout (same interview type on every row)
10. Close with natural interview ending (candidate question or mutual farewell)

METADATA DEFAULTS (§7.1):
- category: "Mock Interview"
- grammarFocusTag: "Formal Register" / "STAR Method" / "Past Simple" / "Present Perfect" / "Conditional"
- contextTag: "HR Interview" / "Tech Interview" / "Behavioural Interview"
- complexityLevel: 3–5 (always Intermediate to Advanced)
- targetAgeGroup: "Adult"
- hintLanguage: "Telugu" / "Hindi" / "Tamil" / "Kannada" / "None"

JSON OUTPUT CONTRACT (§10.1):
{
  "metadata": {
    "scriptTitle": "[interview title e.g. HR Interview — Software Engineer]",
    "category": "Mock Interview",
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
      "englishText": "[formal question — no contractions]",
      "hintText": "[Telugu translation or empty]",
      "grammarTag": "[grammar/method tag]",
      "contextTag": "[same as metadata contextTag — every row]",
      "focusWord": "[professional vocabulary term]",
      "pronunciationNote": "[IPA or empty]"
    }
  ]
}

SELF-VALIDATION BEFORE OUTPUT (§10.2):
1. SequenceId: starts at 1, sequential, no duplicates
2. SpeakerLabel: only "Interviewer" or "Candidate"
3. No contractions in any Interviewer turn
4. Candidate answers ≥ 2 sentences each
5. GrammarTag (E): present on all rows
6. FocusWord (G): professional term present on all rows
7. ContextTag identical on every row
8. FocusWord appears verbatim in the same row's EnglishText
9. Row count: 20–50
10. Output is valid JSON — no markdown, no code fences, no trailing commas
$PROMPT$, 'ExcelTemplateStandard.md §6.3, §7.1, §9, §10'),

-- ── Vocabulary Sprint (§6.4) ─────────────────────────────────────────────────
('Vocabulary Sprint', $PROMPT$
Generate a GoWithFlow script for the category: Vocabulary Sprint.

Output strict JSON only — no explanation, no markdown, no code fences.

PURPOSE (ExcelTemplateStandard.md §6.4):
Rapid vocabulary exposure through short, high-frequency exchanges.
Each script introduces a thematic set of vocabulary words (10–20 distinct words) in context-rich sentences.
Designed for quick vocabulary building within 5–8 minutes.

SPEAKER LABELS (§6.4 — mandatory):
- Tutor   — introduces and uses the vocabulary word in context (FACILITATOR turn — not scored)
- Learner — responds using the same word or a related word (PERFORMANCE turn — scored)
DO NOT use Speaker A/B. Always "Tutor" and "Learner".

COLUMN RULES (§6.4):
- A (SequenceId):       REQUIRED — sequential integers
- B (SpeakerLabel):     REQUIRED — strictly "Tutor" or "Learner"
- C (EnglishText):      REQUIRED — sentence using FocusWord in natural context
- D (HintText):         REQUIRED (MANDATORY) — native language translation of the full sentence
- E (GrammarTag):       OPTIONAL — only if sentence demonstrates specific grammar
- F (ContextTag):       REQUIRED — vocabulary theme; matches contextTag metadata
- G (FocusWord):        REQUIRED (MANDATORY) — the vocabulary word introduced in this turn
- H (PronunciationNote):REQUIRED on all Tutor rows; optional on Learner rows — IPA pronunciation

CONTENT RULES (§6.4):
1. Script length: minimum 20 rows, maximum 40 rows
2. Each Tutor turn introduces EXACTLY ONE FocusWord — never two new words in one turn
3. Each Learner turn uses the SAME FocusWord from the preceding Tutor turn
4. A word may appear again for reinforcement but must NOT be re-introduced as new
5. HintText (D) is REQUIRED for ALL rows — mandatory without exception
6. PronunciationNote (H) is REQUIRED for all Tutor turns
7. FocusWord (G) is REQUIRED for all rows
8. Each FocusWord must appear verbatim in the same row's EnglishText
9. Vocabulary progression: simple words first, complex later
10. Avoid idioms unless the idiom is the intended FocusWord

METADATA DEFAULTS (§7.1):
- category: "Vocabulary Sprint"
- grammarFocusTag: "Vocabulary" or theme: "Business Vocabulary", "Medical Vocabulary", "Travel Vocabulary", "Technology Vocabulary"
- contextTag: theme: "Corporate", "Healthcare", "Travel", "Technology", "Legal", "Academic", "Social"
- complexityLevel: 1–5 (Basic=1-2, Professional=3-4, Expert=5)
- targetAgeGroup: "All" / "Teen" / "Adult"
- hintLanguage: "Telugu" / "Hindi" / "Tamil" / "Kannada" / "None"

JSON OUTPUT CONTRACT (§10.1):
{
  "metadata": {
    "scriptTitle": "[vocabulary title e.g. Business Vocabulary — Corporate Setting]",
    "category": "Vocabulary Sprint",
    "grammarFocusTag": "[vocabulary theme]",
    "contextTag": "[theme — same on all rows]",
    "complexityLevel": 3,
    "targetAgeGroup": "Adult",
    "hintLanguage": "Telugu"
  },
  "rows": [
    {
      "sequenceId": 1,
      "speakerLabel": "Tutor",
      "englishText": "[sentence using FocusWord in natural context]",
      "hintText": "[Telugu translation — REQUIRED]",
      "grammarTag": "[grammar tag or empty]",
      "contextTag": "[same as metadata contextTag]",
      "focusWord": "[vocabulary word — REQUIRED on every row]",
      "pronunciationNote": "[IPA — REQUIRED on Tutor rows]"
    }
  ]
}

SELF-VALIDATION BEFORE OUTPUT (§10.2):
1. SequenceId: starts at 1, sequential, no duplicates
2. SpeakerLabel: only "Tutor" or "Learner"
3. HintText: NOT EMPTY on any row — this is mandatory
4. FocusWord: NOT EMPTY on any row — this is mandatory
5. PronunciationNote: NOT EMPTY on all Tutor rows
6. Each Tutor introduces exactly one NEW word per turn
7. Following Learner turn uses the SAME word as the preceding Tutor turn
8. FocusWord appears verbatim in the same row's EnglishText
9. Row count: 20–40
10. Output is valid JSON — no markdown, no code fences, no trailing commas
$PROMPT$, 'ExcelTemplateStandard.md §6.4, §7.1, §9, §10'),

-- ── Fluency Drill (§6.5) ─────────────────────────────────────────────────────
('Fluency Drill', $PROMPT$
Generate a GoWithFlow script for the category: Fluency Drill.

Output strict JSON only — no explanation, no markdown, no code fences.

PURPOSE (ExcelTemplateStandard.md §6.5):
High-speed, high-volume conversation practice for building speaking fluency and reducing hesitation.
Scripts are natural, conversational, and fast-paced. Short sentences. Goal is volume of output, not grammatical precision.

SPEAKER LABELS (§6.5 — mandatory):
- Speaker A — initiates fast-paced dialogue turns
- Speaker B — responds quickly, mirrors energy
Always "Speaker A" and "Speaker B". No other labels.

COLUMN RULES (§6.5):
- A (SequenceId):       REQUIRED — sequential integers
- B (SpeakerLabel):     REQUIRED — strictly "Speaker A" or "Speaker B"
- C (EnglishText):      REQUIRED — short sentences, 5–15 words; natural contractions OK
- D (HintText):         OPTIONAL — translations optional; they slow reading pace for fluency drill
- E (GrammarTag):       DO NOT USE — leave blank (Fluency Drill does not target grammar)
- F (ContextTag):       REQUIRED — topic/setting; matches contextTag metadata
- G (FocusWord):        DO NOT USE — leave blank (no vocabulary focus)
- H (PronunciationNote):DO NOT USE — leave blank

CONTENT RULES (§6.5):
1. Script length: minimum 30 rows, maximum 60 rows — HIGH VOLUME is required
2. Average sentence length: 5–15 words per turn — NO long paragraphs
3. Alternate questions and statements freely
4. Natural contractions REQUIRED: I'm, don't, can't, it's, we're, won't
5. No grammar explanation — natural conversation only
6. Topic stays consistent (no topic drift)
7. HintText optional — speed over translation
8. GrammarTag MUST be left blank (empty string)
9. FocusWord MUST be left blank (empty string)
10. No speaker has more than 2 consecutive turns

METADATA DEFAULTS (§7.1):
- category: "Fluency Drill"
- grammarFocusTag: "General Fluency" / "Question Fluency" / "Response Fluency"
- contextTag: topic — "Daily Life", "Opinions", "Social Events", "Shopping", "Travel", "Work Life", "Hobbies"
- complexityLevel: 1–3 (Fluency Drill stays Beginner to Intermediate — speed over complexity)
- targetAgeGroup: "All" / "Teen" / "Adult"
- hintLanguage: "Telugu" / "Hindi" / "Tamil" / "Kannada" / "None"

JSON OUTPUT CONTRACT (§10.1):
{
  "metadata": {
    "scriptTitle": "[fluency title e.g. Daily Life Fluency Drill]",
    "category": "Fluency Drill",
    "grammarFocusTag": "General Fluency",
    "contextTag": "[topic — same on all rows]",
    "complexityLevel": 2,
    "targetAgeGroup": "All",
    "hintLanguage": "Telugu"
  },
  "rows": [
    {
      "sequenceId": 1,
      "speakerLabel": "Speaker A",
      "englishText": "[short natural sentence with contractions — 5–15 words]",
      "hintText": "",
      "grammarTag": "",
      "contextTag": "[same as metadata contextTag]",
      "focusWord": "",
      "pronunciationNote": ""
    }
  ]
}

SELF-VALIDATION BEFORE OUTPUT (§10.2):
1. SequenceId: starts at 1, sequential, no duplicates
2. SpeakerLabel: only "Speaker A" or "Speaker B"
3. grammarTag: EMPTY on all rows
4. focusWord: EMPTY on all rows
5. pronunciationNote: EMPTY on all rows
6. EnglishText: 5–15 words per turn — no long paragraphs
7. Contractions present in most turns
8. No speaker has 3+ consecutive turns
9. Row count: 30–60 (high volume mandatory)
10. Output is valid JSON — no markdown, no code fences, no trailing commas
$PROMPT$, 'ExcelTemplateStandard.md §6.5, §7.1, §9, §10'),

-- ── Repractice Round (§6.6) ──────────────────────────────────────────────────
('Repractice Round', $PROMPT$
Generate a GoWithFlow script for the category: Repractice Round.

Output strict JSON only — no explanation, no markdown, no code fences.

PURPOSE (ExcelTemplateStandard.md §6.6):
Targeted mistake correction and reinforcement.
Scripts address ONE specific grammar error pattern previously made by a user.
Provides corrected practice through dialogue. Single-error focused — no blending.

SPEAKER LABELS (§6.6 — mandatory):
- Coach   — models the correct form; corrects errors; provides brief explanation (FACILITATOR — not scored)
- Learner — attempts the corrected form; confirms understanding (PERFORMANCE — scored)
Always "Coach" and "Learner". Different from GrammarDrill's "Speaker A/B".

COLUMN RULES (§6.6):
- A (SequenceId):       REQUIRED — sequential integers
- B (SpeakerLabel):     REQUIRED — strictly "Coach" or "Learner"
- C (EnglishText):      REQUIRED — contains the corrected form of the previously-errored grammar pattern
- D (HintText):         REQUIRED (MANDATORY) — native translation helps comprehension of the error
- E (GrammarTag):       REQUIRED (MANDATORY) — the EXACT grammar error tag; IDENTICAL on ALL rows
- F (ContextTag):       REQUIRED — scene context; matches the original error's context
- G (FocusWord):        OPTIONAL — the specific word that was misused originally
- H (PronunciationNote):OPTIONAL — pronunciation if pronunciation was part of the error

CONTENT RULES (§6.6):
1. Script length: minimum 14 rows, maximum 28 rows — concise and focused
2. Address ONLY ONE grammar error pattern — no mixing of error types
3. Coach must explicitly model the correct form in the FIRST 2 turns
4. At least 50% of Learner turns must produce the corrected grammar form
5. One Coach turn per 4 turns should provide a brief reinforcement note
6. GrammarTag (E): SAME VALUE on ALL rows — the error being corrected
7. HintText (D): REQUIRED on all rows
8. FocusWord: the specific word where the original error occurred
9. Script ends with the Learner successfully producing the correct form independently
10. Do NOT introduce new grammar concepts — single-error focused only

METADATA DEFAULTS (§7.1):
- category: "Repractice Round"
- grammarFocusTag: the specific grammar error — "Present Perfect vs Past Simple", "Articles (a/an/the)", "Subject-Verb Agreement", "Preposition Use", "Tense Consistency"
- contextTag: same context as the original session where the mistake occurred — "Office", "Travel", "Daily Life", etc.
- complexityLevel: match the original session's complexity level
- targetAgeGroup: match the original session's target age group
- hintLanguage: "Telugu" / "Hindi" / "Tamil" / "Kannada" / "None"

JSON OUTPUT CONTRACT (§10.1):
{
  "metadata": {
    "scriptTitle": "[correction title e.g. Present Perfect vs Past Simple — Office]",
    "category": "Repractice Round",
    "grammarFocusTag": "[the ONE error being corrected — same on all rows]",
    "contextTag": "[original error context — same on all rows]",
    "complexityLevel": 3,
    "targetAgeGroup": "Adult",
    "hintLanguage": "Telugu"
  },
  "rows": [
    {
      "sequenceId": 1,
      "speakerLabel": "Coach",
      "englishText": "[model sentence demonstrating the correct form]",
      "hintText": "[Telugu translation — REQUIRED]",
      "grammarTag": "[error tag — REQUIRED, SAME on every row]",
      "contextTag": "[same as metadata contextTag — every row]",
      "focusWord": "[the specific misused word or empty]",
      "pronunciationNote": "[IPA or empty]"
    }
  ]
}

SELF-VALIDATION BEFORE OUTPUT (§10.2):
1. SequenceId: starts at 1, sequential, no duplicates
2. SpeakerLabel: only "Coach" or "Learner"
3. GrammarTag (E): IDENTICAL value on EVERY row — no variation
4. HintText (D): NOT EMPTY on any row — mandatory
5. Coach models correct form explicitly in first 2 turns
6. At least 50% of Learner turns produce the corrected grammar form
7. Script ends with Learner success
8. Row count: 14–28
9. Only one grammar error addressed — no blending
10. Output is valid JSON — no markdown, no code fences, no trailing commas
$PROMPT$, 'ExcelTemplateStandard.md §6.6, §7.1, §9, §10')

ON CONFLICT (category) DO NOTHING;
