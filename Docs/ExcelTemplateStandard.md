# GoWithFlow — Excel Template Master Standard
### Version 1.0 | Author: Project AI Engineer | Date: 2026-05-22
### Status: PERMANENT REFERENCE — Do Not Modify Without Architecture Review

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [Core Parsing Contract](#2-core-parsing-contract)
3. [Naming Convention Standards](#3-naming-convention-standards)
4. [Universal Sheet Structure](#4-universal-sheet-structure)
5. [Universal Column Specification](#5-universal-column-specification)
6. [Category-Specific Template Standards](#6-category-specific-template-standards)
   - 6.1 GrammarDrill
   - 6.2 Roleplay
   - 6.3 MockInterview
   - 6.4 VocabularySprint
   - 6.5 FluencyDrill
   - 6.6 RepracticeRound
7. [Metadata Upload Standards](#7-metadata-upload-standards)
8. [Validation Rules Guide](#8-validation-rules-guide)
9. [Content Writing Standards](#9-content-writing-standards)
10. [AI Generation Protocol](#10-ai-generation-protocol)
11. [Upload Compatibility Rules](#11-upload-compatibility-rules)
12. [Quick Reference Card](#12-quick-reference-card)

---

## 1. System Overview

### 1.1 Purpose

This document defines the permanent, category-wise Excel template standard for GoWithFlow content uploads. Every Excel file produced by admins, content teams, or AI generation must conform exactly to these specifications.

### 1.2 Architecture Context

- **Parser Engine**: ClosedXML (C# backend, `ExcelParserService`)
- **Upload API**: `POST /api/v1/scripts/validate` → `POST /api/v1/scripts/upload`
- **Storage Tables**: `tblScript` (metadata) + `tblUtterance` (rows)
- **File Format**: `.xlsx` only (Excel 2007+), max 5 MB
- **Sheet Read**: First worksheet only (index 0); additional sheets are ignored

### 1.3 Registered Categories (Exact DB Values)

| Display Name        | DB Category Value    | Excel Category Code |
|---------------------|----------------------|---------------------|
| Grammar Drill       | `Grammar Drill`      | `GrammarDrill`      |
| Roleplay            | `Roleplay`           | `Roleplay`          |
| Mock Interview      | `Mock Interview`     | `MockInterview`     |
| Vocabulary Sprint   | `Vocabulary Sprint`  | `VocabularySprint`  |
| Fluency Drill       | `Fluency Drill`      | `FluencyDrill`      |
| Repractice Round    | `Repractice Round`   | `RepracticeRound`   |

> **Rule**: The `category` field sent to the upload API must exactly match the DB Category Value column above.

---

## 2. Core Parsing Contract

The backend parser reads **Row 1 as the header row** and data from **Row 2 onwards**. Column positions are **positional, not name-based** — column A is always `SequenceId` regardless of header text.

### 2.1 Fixed Column Map

| Column | Field             | Type        | Required | Max Length | Validation Rule                                           |
|--------|-------------------|-------------|----------|------------|-----------------------------------------------------------|
| A      | `SequenceId`      | Integer     | YES      | —          | Positive integer; unique within the file; sequential from 1|
| B      | `SpeakerLabel`    | String      | YES      | 64 chars   | Non-empty; one of the registered speaker roles per category|
| C      | `EnglishText`     | String      | YES      | 512 chars  | Non-empty; the utterance content                          |
| D      | `HintText`        | String      | NO       | 512 chars  | Translation hint or native language equivalent            |
| E      | `GrammarTag`      | String      | NO       | 64 chars   | Grammar structure tag; e.g. `Present Perfect`             |
| F      | `ContextTag`      | String      | NO       | 64 chars   | Scene context; e.g. `Office`, `Airport`, `Hospital`       |
| G      | `FocusWord`       | String      | NO       | 64 chars   | Highlighted vocabulary word from EnglishText              |
| H      | `PronunciationNote`| String     | NO       | 256 chars  | IPA or phonetic guide for the focus word                  |

### 2.2 Parser Behaviour

- Header row (Row 1) is **skipped** during data parsing — it must exist for human readability but is never validated
- Empty rows between data rows cause a `SequenceId` gap error — **no blank rows allowed inside the data range**
- Trailing empty rows after the last data row are silently ignored
- All string fields are trimmed of leading/trailing whitespace before storage
- `SequenceId` uniqueness is checked within the file scope only (not across scripts)

---

## 3. Naming Convention Standards

### 3.1 File Naming Pattern

```
[CategoryCode]_[ScriptTitle-Slug]_v[Version]_[YYYY-MM-DD].xlsx
```

**Rules:**
- `CategoryCode`: Use the Excel Category Code from §1.3
- `ScriptTitle-Slug`: Title in kebab-case, max 40 characters, alphanumeric and hyphens only
- `Version`: Integer starting from 1; increment on each re-upload of the same script
- `Date`: ISO 8601 date of file creation

**Examples:**
```
GrammarDrill_present-perfect-office-dialogue_v1_2026-05-22.xlsx
Roleplay_airport-checkin-simulation_v1_2026-05-22.xlsx
MockInterview_software-engineer-hr-round_v2_2026-06-01.xlsx
VocabularySprint_business-vocabulary-set-1_v1_2026-05-22.xlsx
FluencyDrill_daily-conversation-fluency-1_v1_2026-05-22.xlsx
RepracticeRound_present-perfect-mistakes-set-3_v1_2026-05-22.xlsx
```

### 3.2 ScriptTitle Naming (Sent in Upload Form)

```
[Readable Title] — [Context Tag] — [Complexity Hint]
```

**Examples:**
```
Present Perfect in the Office — Business — Intermediate
Airport Check-In Roleplay — Travel — Beginner
Software Engineer HR Round — Tech Interview — Advanced
Business Vocabulary Sprint Set 1 — Corporate — Intermediate
Daily Conversation Fluency Pack 1 — General — Beginner
Present Perfect Mistake Repractice — Grammar Drill — Intermediate
```

**Rules:**
- Max 128 characters (DB constraint on `tblScript.ScriptTitle`)
- Human-readable; used in admin dashboard and session selection
- Must be unique; the system checks `uspCheckScriptTitleExists` before insert

---

## 4. Universal Sheet Structure

Every Excel file must follow this structure exactly.

### 4.1 Workbook Structure

| Sheet Position | Sheet Name        | Purpose                          | Required |
|----------------|-------------------|----------------------------------|----------|
| Sheet 1        | `Content`         | The script utterances (parsed)   | YES      |
| Sheet 2        | `Metadata`        | Human-readable metadata summary  | NO       |
| Sheet 3+       | Any               | Ignored by parser                | —        |

> Sheet 1 must be named `Content`. The parser reads by index (sheet 0), but naming enforces clarity for human editors.

### 4.2 Sheet 1 — Content — Row Layout

| Row       | Content              |
|-----------|----------------------|
| Row 1     | Header row (fixed labels — see §5) |
| Row 2+    | Data rows (utterances) |

### 4.3 Formatting Rules

| Property           | Rule                                                              |
|--------------------|-------------------------------------------------------------------|
| Row 1 fill colour  | `#1E3A5F` (Dark Navy) — bold white text, 11pt Calibri            |
| Data rows          | Alternating `#FFFFFF` / `#F0F4F8` (white / light blue-grey)      |
| Column A width     | 14                                                                |
| Column B width     | 22                                                                |
| Column C width     | 60                                                                |
| Column D width     | 50                                                                |
| Column E width     | 24                                                                |
| Column F width     | 20                                                                |
| Column G width     | 20                                                                |
| Column H width     | 40                                                                |
| Row height (data)  | 20pt                                                              |
| Text wrap          | Enabled on columns C, D, H                                        |
| Cell borders       | Thin border on all cells (data range only)                        |
| Freeze panes       | Row 1 frozen                                                      |

> Formatting does not affect parsing. It is required only for human review readability and template distribution.

---

## 5. Universal Column Specification

### 5.1 Header Row — Exact Labels (Row 1)

| Column | Exact Header Text     |
|--------|-----------------------|
| A      | `SequenceId`          |
| B      | `SpeakerLabel`        |
| C      | `EnglishText`         |
| D      | `HintText`            |
| E      | `GrammarTag`          |
| F      | `ContextTag`          |
| G      | `FocusWord`           |
| H      | `PronunciationNote`   |

> Header text must be exactly as shown above (case-sensitive). The parser skips row 1, but validation tooling and AI generation pipelines rely on exact header names.

---

## 6. Category-Specific Template Standards

---

### 6.1 GrammarDrill

#### Purpose
Structured grammar practice through dialogue. Each script isolates one grammar structure (e.g., Present Perfect, Passive Voice) and demonstrates it through realistic conversation between two speakers. Users practice by speaking the target grammar pattern in a controlled context.

#### Upload Metadata Defaults

| Field              | Typical Value(s)                                              |
|--------------------|---------------------------------------------------------------|
| `category`         | `Grammar Drill`                                               |
| `grammarFocusTag`  | The specific structure: `Present Perfect`, `Past Simple`, `Passive Voice`, `Modal Verbs`, `Conditionals`, `Reported Speech`, `Future Perfect`, `Gerunds`, `Infinitives` |
| `contextTag`       | Scene of dialogue: `Office`, `Hospital`, `Airport`, `Home`, `School`, `Restaurant`, `Bank`, `Shopping` |
| `complexityLevel`  | 1–5 (1=Beginner, 3=Intermediate, 5=Advanced)                 |
| `targetAgeGroup`   | `All` / `Teen` / `Adult`                                      |
| `hintLanguage`     | `Telugu` / `Hindi` / `Tamil` / `Kannada` / `None`            |

#### Speaker Labels (Required Set)

| SpeakerLabel   | Role Description                             |
|----------------|----------------------------------------------|
| `Speaker A`    | Initiates the grammar-focused dialogue        |
| `Speaker B`    | Responds and mirrors the grammar pattern      |

> GrammarDrill must always use exactly `Speaker A` and `Speaker B`. No other labels are valid for this category.

#### Column Usage Rules

| Column | Required | GrammarDrill-Specific Rule                                              |
|--------|----------|-------------------------------------------------------------------------|
| A      | YES      | Sequential integers 1, 2, 3, …                                          |
| B      | YES      | Strictly `Speaker A` or `Speaker B` only                                |
| C      | YES      | Contains the grammar target structure — must appear ≥ 60% of turns      |
| D      | NO       | Native language translation of the utterance                            |
| E      | YES      | Must match the `grammarFocusTag` sent in upload metadata                |
| F      | YES      | Must match the `contextTag` sent in upload metadata                     |
| G      | NO       | One vocabulary word per row that is the grammar-focus bearer            |
| H      | NO       | IPA notation for the FocusWord                                          |

#### Content Rules

1. Script length: **minimum 12 rows, maximum 30 rows**
2. The grammar target structure must appear in EnglishText at least once every 3 turns
3. All GrammarTag values in column E must be identical (same structure across all rows)
4. ContextTag in column F must be consistent across all rows (same scene)
5. HintText in column D must be the full sentence translation, not word-for-word
6. FocusWord in column G must be a word that appears verbatim in the same row's EnglishText
7. Do not introduce new vocabulary unexplained — scripts are grammar-focused, not vocab-focused
8. Avoid contractions in the first 3 rows to establish formal grammar exposure

#### Sample Data — GrammarDrill (Present Perfect / Office)

| SequenceId | SpeakerLabel | EnglishText                                                          | HintText                                          | GrammarTag       | ContextTag | FocusWord  | PronunciationNote |
|------------|--------------|----------------------------------------------------------------------|---------------------------------------------------|------------------|------------|------------|-------------------|
| 1          | Speaker A    | Have you finished the project report yet?                            | మీరు ప్రాజెక్ట్ రిపోర్ట్ పూర్తి చేశారా?       | Present Perfect  | Office     | finished   | /ˈfɪnɪʃt/         |
| 2          | Speaker B    | Yes, I have just sent it to the manager.                             | అవును, నేను ఇప్పుడే మేనేజర్కు పంపాను.           | Present Perfect  | Office     | sent       | /sɛnt/            |
| 3          | Speaker A    | Great. Have you also updated the client database?                    | చాలా బాగుంది. క్లయింట్ డేటాబేస్ కూడా అప్డేట్ చేశారా? | Present Perfect | Office  | updated    | /ˈʌpdeɪtɪd/       |
| 4          | Speaker B    | Not yet. I have been working on it since this morning.               | ఇంకా లేదు. నేను ఈ ఉదయం నుండి దానిపై పని చేస్తున్నాను. | Present Perfect | Office | working    | /ˈwɜːrkɪŋ/        |
| 5          | Speaker A    | Have you spoken to the client about the delay?                       | మీరు జాప్యం గురించి క్లయింట్తో మాట్లాడారా?     | Present Perfect  | Office     | spoken     | /ˈspoʊkən/        |
| 6          | Speaker B    | Yes, I have already called them and explained the situation.         | అవును, నేను ఇప్పటికే వారికి ఫోన్ చేసి పరిస్థితి వివరించాను. | Present Perfect | Office | called  | /kɔːld/           |
| 7          | Speaker A    | Have we received their confirmation email?                           | మేము వారి నిర్ధారణ ఇమెయిల్ అందుకున్నామా?       | Present Perfect  | Office     | received   | /rɪˈsiːvd/        |
| 8          | Speaker B    | Yes, it has just arrived in the shared inbox.                        | అవును, అది ఇప్పుడే షేర్డ్ ఇన్‌బాక్స్‌కు వచ్చింది. | Present Perfect | Office  | arrived    | /əˈraɪvd/         |
| 9          | Speaker A    | Has the team lead reviewed the final version?                        | టీమ్ లీడ్ తుది వెర్షన్ సమీక్షించారా?           | Present Perfect  | Office     | reviewed   | /rɪˈvjuːd/        |
| 10         | Speaker B    | She has not reviewed it yet, but she has confirmed the meeting time. | ఆమె ఇంకా సమీక్షించలేదు, కానీ మీటింగ్ సమయం నిర్ధారించింది. | Present Perfect | Office | confirmed | /kənˈfɜːrmd/ |
| 11         | Speaker A    | Have you prepared the slide deck for the presentation?               | మీరు ప్రెజెంటేషన్ కోసం స్లైడ్ డెక్ సిద్ధం చేశారా? | Present Perfect | Office | prepared  | /prɪˈpɛrd/        |
| 12         | Speaker B    | Yes, I have prepared everything. We are ready to begin.              | అవును, నేను అన్నీ సిద్ధం చేశాను. మేము ప్రారంభించడానికి సిద్ధంగా ఉన్నాం. | Present Perfect | Office | prepared | /prɪˈpɛrd/ |

---

### 6.2 Roleplay

#### Purpose
Simulated real-world conversation scenarios where users take on characters. Focuses on contextual fluency, social language, and turn-taking. Unlike GrammarDrill, Roleplay allows multiple grammar structures and prioritises natural conversation flow.

#### Upload Metadata Defaults

| Field              | Typical Value(s)                                               |
|--------------------|----------------------------------------------------------------|
| `category`         | `Roleplay`                                                     |
| `grammarFocusTag`  | `General` or a loose tag: `Politeness`, `Requests`, `Negotiations`, `Small Talk` |
| `contextTag`       | The scenario location: `Airport`, `Hotel`, `Doctor`, `Bank`, `Restaurant`, `Job Office`, `Police Station`, `Pharmacy` |
| `complexityLevel`  | 1–5                                                            |
| `targetAgeGroup`   | `All` / `Teen` / `Adult`                                       |
| `hintLanguage`     | `Telugu` / `Hindi` / `Tamil` / `Kannada` / `None`             |

#### Speaker Labels (Required Set — Roleplay)

Speaker labels must reflect character roles, not generic labels. Use role-based names matching the scenario.

| Scenario Type     | Valid SpeakerLabels                        |
|-------------------|--------------------------------------------|
| Airport           | `Passenger`, `Check-In Agent`              |
| Hotel             | `Guest`, `Receptionist`                    |
| Doctor            | `Patient`, `Doctor`                        |
| Restaurant        | `Customer`, `Waiter`                       |
| Bank              | `Customer`, `Bank Teller`                  |
| Job Interview     | `Candidate`, `Interviewer`                 |
| Police            | `Citizen`, `Police Officer`                |
| Shopping          | `Customer`, `Sales Associate`              |
| Pharmacy          | `Customer`, `Pharmacist`                   |
| General           | `Person A`, `Person B`                     |

> Custom role names are acceptable if they clearly reflect the scenario. Always use exactly two distinct speaker labels per script.

#### Column Usage Rules

| Column | Required | Roleplay-Specific Rule                                               |
|--------|----------|----------------------------------------------------------------------|
| A      | YES      | Sequential integers 1, 2, 3, …                                       |
| B      | YES      | Character role name (see speaker label table above)                  |
| C      | YES      | Natural conversational English; contractions allowed                 |
| D      | NO       | Native language equivalent of the utterance                          |
| E      | NO       | Optional grammar tag if a specific structure is prominent            |
| F      | YES      | Scene location — must match `contextTag` in upload metadata          |
| G      | NO       | Key vocabulary word from the utterance (social/contextual word)      |
| H      | NO       | Pronunciation note for FocusWord                                     |

#### Content Rules

1. Script length: **minimum 16 rows, maximum 40 rows**
2. Conversation must have a clear beginning (greeting/problem statement), middle (interaction), and end (resolution/farewell)
3. At least one polite phrase per 5 turns (`please`, `thank you`, `excuse me`, `I'm sorry`, `certainly`)
4. Avoid repetitive sentence structures — vary sentence openings across turns
5. GrammarTag (column E) is optional; if used, it should only tag rows where the grammar is demonstrably intentional
6. HintText must translate the full utterance, not isolated words
7. FocusWord should be scenario-specific vocabulary (e.g., `boarding pass`, `prescription`, `invoice`)
8. Scripts must be completable within a natural conversation flow — no abrupt endings

#### Sample Data — Roleplay (Airport Check-In)

| SequenceId | SpeakerLabel    | EnglishText                                                           | HintText                                          | GrammarTag | ContextTag | FocusWord    | PronunciationNote |
|------------|-----------------|-----------------------------------------------------------------------|---------------------------------------------------|------------|------------|--------------|-------------------|
| 1          | Passenger       | Good morning. I'm here to check in for my flight to London.          | శుభోదయం. నేను లండన్ విమానానికి చెక్-ఇన్ చేయడానికి వచ్చాను. |            | Airport    | check in     | /tʃɛk ɪn/         |
| 2          | Check-In Agent  | Good morning! May I see your passport and booking reference, please?  | శుభోదయం! దయచేసి మీ పాస్‌పోర్ట్ మరియు బుకింగ్ రిఫరెన్స్ చూపించగలరా? | Modal Verbs | Airport | passport    | /ˈpæspɔːrt/       |
| 3          | Passenger       | Of course. Here you go. My booking reference is AB1234.               | తప్పకుండా. ఇక్కడ ఉంది. నా బుకింగ్ రిఫరెన్స్ AB1234. |           | Airport    | reference    | /ˈrɛfərəns/       |
| 4          | Check-In Agent  | Thank you. Are you checking in any luggage today?                     | ధన్యవాదాలు. మీరు ఈరోజు ఏమైనా లగేజ్ చెక్-ఇన్ చేస్తున్నారా? |  | Airport  | luggage      | /ˈlʌɡɪdʒ/         |
| 5          | Passenger       | Yes, I have one suitcase to check in and one carry-on bag.            | అవును, నాకు చెక్-ఇన్ చేయడానికి ఒక సూట్‌కేస్ మరియు ఒక క్యారీ-ఆన్ బ్యాగ్ ఉన్నాయి. | | Airport | suitcase | /ˈsuːtkeɪs/  |
| 6          | Check-In Agent  | Could you please place your suitcase on the scale?                    | మీరు దయచేసి మీ సూట్‌కేస్ స్కేల్‌పై ఉంచగలరా?   | Modal Verbs | Airport   | scale        | /skeɪl/           |
| 7          | Passenger       | Sure. Is it within the weight limit?                                  | తప్పకుండా. ఇది బరువు పరిమితిలో ఉందా?            |            | Airport    | weight limit | /weɪt ˈlɪmɪt/     |
| 8          | Check-In Agent  | It is 22 kilograms, which is within the 23-kilogram limit. You're fine. | ఇది 22 కిలోగ్రాములు, ఇది 23-కిలోగ్రాముల పరిమితిలో ఉంది. మీరు సరే. | | Airport | kilogram | /ˈkɪləɡræm/  |
| 9          | Passenger       | That's a relief. Do you have a window seat available?                 | అది సంతోషదాయకం. మీకు విండో సీట్ అందుబాటులో ఉందా? | Present Simple | Airport | window seat | /ˈwɪndoʊ siːt/ |
| 10         | Check-In Agent  | Let me check. Yes, I can assign you seat 14A — that's a window seat. | నేను చెక్ చేస్తాను. అవును, నేను మీకు సీట్ 14A ని కేటాయించగలను — అది విండో సీట్. | Modal Verbs | Airport | assign | /əˈsaɪn/ |
| 11         | Passenger       | That's perfect. Thank you very much.                                  | అది సరైనది. చాలా ధన్యవాదాలు.                    |            | Airport    | perfect      | /ˈpɜːrfɪkt/       |
| 12         | Check-In Agent  | You're welcome. Your boarding pass will be printed shortly.           | మీకు స్వాగతం. మీ బోర్డింగ్ పాస్ త్వరలో ప్రింట్ అవుతుంది. | Future Simple | Airport | boarding pass | /ˈbɔːrdɪŋ pæs/ |
| 13         | Passenger       | Wonderful. Which gate do I need to go to?                             | అద్భుతం. నేను ఏ గేట్‌కు వెళ్ళాలి?               |            | Airport    | gate         | /ɡeɪt/            |
| 14         | Check-In Agent  | Please proceed to Gate B7. Boarding begins in 45 minutes.            | దయచేసి గేట్ B7 వైపు వెళ్ళండి. బోర్డింగ్ 45 నిమిషాల్లో ప్రారంభమవుతుంది. | | Airport | proceed | /prəˈsiːd/ |
| 15         | Passenger       | Thank you. Have a great day!                                          | ధన్యవాదాలు. మీకు మంచి రోజు కావాలని ఆశిస్తున్నాను! | | Airport | | |
| 16         | Check-In Agent  | You too! Enjoy your flight.                                           | మీకు కూడా! మీ విమాన ప్రయాణం ఆనందంగా గడవాలని ఆశిస్తున్నాను. | | Airport | | |

---

### 6.3 MockInterview

#### Purpose
Simulated interview practice for professional English. Covers HR rounds, technical screenings, and behavioural interviews. Users practice formal register, structured answers (STAR method), and professional vocabulary.

#### Upload Metadata Defaults

| Field              | Typical Value(s)                                                |
|--------------------|-----------------------------------------------------------------|
| `category`         | `Mock Interview`                                                |
| `grammarFocusTag`  | `Formal Register` / `STAR Method` / `Past Simple` / `Present Perfect` / `Conditional` |
| `contextTag`       | `HR Interview` / `Tech Interview` / `Behavioural Interview` / `Sales Interview` / `Management Interview` |
| `complexityLevel`  | 3–5 (MockInterview is always Intermediate to Advanced)          |
| `targetAgeGroup`   | `Adult`                                                         |
| `hintLanguage`     | `Telugu` / `Hindi` / `Tamil` / `Kannada` / `None`              |

#### Speaker Labels (Required Set — MockInterview)

| SpeakerLabel   | Role Description                                       |
|----------------|--------------------------------------------------------|
| `Interviewer`  | Asks questions, probes for detail, challenges responses|
| `Candidate`    | Answers questions using professional English           |

> Always use exactly `Interviewer` and `Candidate`. Do not use aliases.

#### Column Usage Rules

| Column | Required | MockInterview-Specific Rule                                           |
|--------|----------|-----------------------------------------------------------------------|
| A      | YES      | Sequential integers 1, 2, 3, …                                        |
| B      | YES      | Strictly `Interviewer` or `Candidate`                                 |
| C      | YES      | Formal English; no contractions in Interviewer turns                  |
| D      | NO       | Native language translation of the utterance                          |
| E      | YES      | Grammar or method tag: `STAR Method`, `Past Simple`, `Conditionals`   |
| F      | YES      | Interview type: `HR Interview`, `Tech Interview`, etc.                |
| G      | YES      | Professional vocabulary word prominent in the utterance               |
| H      | NO       | Pronunciation note for FocusWord (especially for professional terms)  |

#### Content Rules

1. Script length: **minimum 20 rows, maximum 50 rows**
2. Each Interviewer turn must be a complete, formal question or probing statement
3. Candidate answers must be ≥ 2 sentences — no single-word or single-clause answers
4. At least 40% of Candidate turns must use the STAR format (Situation, Task, Action, Result) for behavioural questions
5. FocusWord (column G) must be a professional or industry-specific term
6. Do not include small talk beyond 2 opening turns
7. The script must include at least one challenging follow-up probe by the Interviewer
8. All Interviewer turns must be in formal English — no contractions
9. ContextTag must remain consistent throughout the script (same interview type)
10. Close with a natural interview ending (candidate asking a question or mutual farewell)

#### Sample Data — MockInterview (HR Interview / Software Engineer)

| SequenceId | SpeakerLabel | EnglishText                                                                                      | HintText                                        | GrammarTag   | ContextTag   | FocusWord     | PronunciationNote  |
|------------|--------------|--------------------------------------------------------------------------------------------------|-------------------------------------------------|--------------|--------------|---------------|--------------------|
| 1          | Interviewer  | Good afternoon. Thank you for coming in today. Please take a seat and make yourself comfortable. | శుభ సాయంత్రం. ఈరోజు వచ్చినందుకు ధన్యవాదాలు. | Formal Greet | HR Interview | comfortable   | /ˈkʌmftəbəl/       |
| 2          | Candidate    | Thank you. I'm pleased to be here. I have been looking forward to this opportunity.              | ధన్యవాదాలు. నేను ఇక్కడ ఉన్నందుకు సంతోషంగా ఉన్నాను. | Present Perfect | HR Interview | pleased   | /pliːzd/           |
| 3          | Interviewer  | Could you please begin by telling me a little about yourself and your professional background?   | మీ గురించి మరియు మీ వృత్తిపరమైన నేపథ్యం గురించి కొంచెం చెప్పగలరా? | Modal Verbs | HR Interview | background | /ˈbækɡraʊnd/   |
| 4          | Candidate    | Certainly. I am a software engineer with five years of experience in backend development using .NET and SQL Server. I have led two major product migrations and consistently delivered projects on time. | తప్పకుండా. నేను .NET మరియు SQL Server ఉపయోగించి బ్యాకెండ్ డెవలప్‌మెంట్‌లో ఐదు సంవత్సరాల అనుభవం ఉన్న సాఫ్ట్‌వేర్ ఇంజనీర్‌ని. | STAR Method | HR Interview | migrations | /maɪˈɡreɪʃənz/ |
| 5          | Interviewer  | That is impressive. Could you describe a situation where you had to work under significant pressure? | అది ఆకట్టుకుంటోంది. మీరు గణనీయమైన ఒత్తిడిలో పని చేయాల్సి వచ్చిన పరిస్థితిని వివరించగలరా? | STAR Method | HR Interview | pressure | /ˈprɛʃər/ |
| 6          | Candidate    | In my previous role, our team was given a two-week deadline to rebuild a critical API that had caused a production outage. I coordinated daily stand-ups, delegated tasks clearly, and we successfully deployed the fix on the thirteenth day. | నా మునుపటి పాత్రలో, మా టీమ్‌కు ఉత్పత్తి అంతరాయం కలిగించిన క్రిటికల్ API ని పునర్నిర్మించడానికి రెండు వారాల గడువు ఇచ్చారు. | STAR Method | HR Interview | deployed | /dɪˈplɔɪd/ |
| 7          | Interviewer  | What specific steps did you take to ensure the team remained focused during that period?         | ఆ కాలంలో టీమ్ దృష్టిని కొనసాగించడానికి మీరు ఏ నిర్దిష్ట చర్యలు తీసుకున్నారు? | Past Simple | HR Interview | ensure | /ɪnˈʃʊər/ |
| 8          | Candidate    | I broke the problem into daily milestones, held brief check-ins each morning, and removed blockers immediately rather than waiting for weekly reviews. This kept momentum and visibility high across the team. | నేను సమస్యను రోజువారీ మైలురాళ్ళగా విభజించాను, ప్రతి ఉదయం సంక్షిప్త చెక్-ఇన్‌లు నిర్వహించాను. | STAR Method | HR Interview | milestones | /ˈmaɪlstoʊnz/ |
| 9          | Interviewer  | Where do you see yourself in the next three to five years professionally?                        | వృత్తిపరంగా రాబోయే మూడు నుండి ఐదు సంవత్సరాలలో మీరు మిమ్మల్ని ఎక్కడ చూస్తున్నారు? | Future Simple | HR Interview | professionally | /prəˈfɛʃənəli/ |
| 10         | Candidate    | I aspire to move into a lead architect role where I can shape system design decisions and mentor junior engineers. I am also keen to deepen my expertise in cloud infrastructure over the next two years. | నేను సిస్టమ్ డిజైన్ నిర్ణయాలను రూపొందించగలిగే లీడ్ ఆర్కిటెక్ట్ పాత్రలోకి వెళ్ళాలని ఆకాంక్షిస్తున్నాను. | Future Simple | HR Interview | architect | /ˈɑːrkɪtɛkt/ |
| 11         | Interviewer  | Do you have any questions for us about the role or the organisation?                             | పాత్ర లేదా సంస్థ గురించి మీకు మాకు ఏమైనా ప్రశ్నలు ఉన్నాయా? | Present Simple | HR Interview | organisation | /ˌɔːrɡənəˈzeɪʃən/ |
| 12         | Candidate    | Yes, thank you. Could you tell me more about the team structure and how success is measured in this role during the first six months? | అవును, ధన్యవాదాలు. టీమ్ నిర్మాణం మరియు మొదటి ఆరు నెలల్లో ఈ పాత్రలో విజయం ఎలా కొలుస్తారు అనే దాని గురించి మీరు మరింత చెప్పగలరా? | Modal Verbs | HR Interview | structure | /ˈstrʌktʃər/ |

---

### 6.4 VocabularySprint

#### Purpose
Rapid vocabulary exposure through short, high-frequency exchanges. Each script introduces a thematic set of vocabulary words (10–20 distinct words) in context-rich sentences. Designed for quick vocabulary building within 5–8 minutes of session time.

#### Upload Metadata Defaults

| Field              | Typical Value(s)                                                |
|--------------------|-----------------------------------------------------------------|
| `category`         | `Vocabulary Sprint`                                             |
| `grammarFocusTag`  | `Vocabulary` or the theme: `Business Vocabulary`, `Medical Vocabulary`, `Travel Vocabulary`, `Technology Vocabulary`, `Legal Vocabulary` |
| `contextTag`       | The vocabulary theme: `Corporate`, `Healthcare`, `Travel`, `Technology`, `Legal`, `Academic`, `Social` |
| `complexityLevel`  | 1–5 (match vocabulary tier: Basic=1-2, Professional=3-4, Expert=5) |
| `targetAgeGroup`   | `All` / `Teen` / `Adult`                                        |
| `hintLanguage`     | `Telugu` / `Hindi` / `Tamil` / `Kannada` / `None`              |

#### Speaker Labels (Required Set — VocabularySprint)

| SpeakerLabel   | Role Description                                          |
|----------------|-----------------------------------------------------------|
| `Tutor`        | Introduces and uses the vocabulary word in context        |
| `Learner`      | Responds using the same word or a related word            |

> VocabularySprint uses `Tutor` and `Learner` to signal the instructional nature. Do not use Speaker A/B.

#### Column Usage Rules

| Column | Required | VocabularySprint-Specific Rule                                      |
|--------|----------|---------------------------------------------------------------------|
| A      | YES      | Sequential integers 1, 2, 3, …                                      |
| B      | YES      | Strictly `Tutor` or `Learner`                                       |
| C      | YES      | Sentence that uses the FocusWord in natural context                 |
| D      | YES      | Native language translation of EnglishText — REQUIRED for vocab drills |
| E      | NO       | Grammar tag if the sentence demonstrates a specific grammar use      |
| F      | YES      | Vocabulary theme — must match `contextTag`                          |
| G      | YES      | REQUIRED — the vocabulary word being introduced in that turn        |
| H      | YES      | REQUIRED — IPA pronunciation for every FocusWord                   |

#### Content Rules

1. Script length: **minimum 20 rows, maximum 40 rows**
2. Each Tutor turn introduces exactly one FocusWord — never introduce two new words in one turn
3. Each Learner turn uses the same FocusWord from the preceding Tutor turn
4. A word may appear again later in the script for reinforcement but must not be re-introduced as new
5. HintText (column D) is REQUIRED for all rows in VocabularySprint — this is mandatory, not optional
6. PronunciationNote (column H) is REQUIRED for all Tutor turns; optional for Learner turns
7. FocusWord (column G) is REQUIRED for all rows
8. Each FocusWord must appear verbatim in the same row's EnglishText
9. Vocabulary progression: simple words first, complex words later in the script
10. Avoid idioms unless the idiom itself is the intended FocusWord

#### Sample Data — VocabularySprint (Business Vocabulary / Corporate)

| SequenceId | SpeakerLabel | EnglishText                                                                | HintText                                         | GrammarTag     | ContextTag | FocusWord    | PronunciationNote |
|------------|--------------|----------------------------------------------------------------------------|--------------------------------------------------|----------------|------------|--------------|-------------------|
| 1          | Tutor        | The company decided to outsource its customer support to a third-party firm. | కంపెనీ తన కస్టమర్ సపోర్ట్‌ను మూడవ పక్ష సంస్థకు అవుట్‌సోర్స్ చేయాలని నిర్ణయించింది. | Past Simple | Corporate | outsource | /ˌaʊtˈsɔːrs/ |
| 2          | Learner      | I see. So they chose to outsource rather than hiring new employees.        | అర్థమైంది. కాబట్టి వారు కొత్త ఉద్యోగులను నియమించుకోవడానికి బదులు అవుట్‌సోర్స్ చేయడాన్ని ఎంచుకున్నారు. | Past Simple | Corporate | outsource | |
| 3          | Tutor        | Exactly. Now, when two companies work together on a shared project, we call it a collaboration. | సరిగ్గా. ఇప్పుడు, రెండు కంపెనీలు ఒక పంచుకున్న ప్రాజెక్ట్‌పై కలిసి పని చేస్తే, మేము దానిని కోలాబరేషన్ అంటాము. | Present Simple | Corporate | collaboration | /kəˌlæbəˈreɪʃən/ |
| 4          | Learner      | Is a collaboration similar to a partnership?                               | కోలాబరేషన్ పార్ట్‌నర్‌షిప్‌కు సమానమా?           | Present Simple | Corporate | collaboration | |
| 5          | Tutor        | Good question. A partnership is more formal, while a collaboration can be temporary and project-specific. | మంచి ప్రశ్న. పార్ట్‌నర్‌షిప్ మరింత అధికారికమైనది, కోలాబరేషన్ తాత్కాలికంగా మరియు ప్రాజెక్ట్-నిర్దిష్టంగా ఉంటుంది. | Contrast | Corporate | partnership | /ˈpɑːrtnərʃɪp/ |
| 6          | Learner      | I understand. So a startup might seek a collaboration before formalising a partnership. | అర్థమైంది. కాబట్టి పార్ట్‌నర్‌షిప్‌ను అధికారికం చేయడానికి ముందు స్టార్టప్ కోలాబరేషన్ కోసం చూడవచ్చు. | Modal Verbs | Corporate | startup | |
| 7          | Tutor        | Precisely. The budget allocated for the project directly impacts its scope and deliverables. | ఖచ్చితంగా. ప్రాజెక్ట్‌కు కేటాయించిన బడ్జెట్ దాని స్కోప్ మరియు డెలివరబుల్స్‌పై నేరుగా ప్రభావం చూపిస్తుంది. | Present Simple | Corporate | allocated | /ˈæləkeɪtɪd/ |
| 8          | Learner      | So the funds allocated determine how many features we can build?           | కాబట్టి కేటాయించిన నిధులు మేము ఎన్ని ఫీచర్లు నిర్మించగలమో నిర్ణయిస్తాయా? | Present Simple | Corporate | allocated | |
| 9          | Tutor        | Correct. When a company grows rapidly, we say it is scaling its operations. | సరైనది. ఒక కంపెనీ వేగంగా వృద్ధి చెందినప్పుడు, అది తన కార్యకలాపాలను స్కేలింగ్ చేస్తుందని చెప్తాము. | Present Continuous | Corporate | scaling | /ˈskeɪlɪŋ/ |
| 10         | Learner      | So a business scaling means it is expanding to handle more demand?         | కాబట్టి వ్యాపారం స్కేలింగ్ అంటే అది ఎక్కువ డిమాండ్‌ను నిర్వహించడానికి విస్తరిస్తుందా? | Present Simple | Corporate | scaling | |

---

### 6.5 FluencyDrill

#### Purpose
High-speed, high-volume conversation practice designed to build speaking fluency and reduce hesitation. Scripts are natural, conversational, and fast-paced. Sentences are shorter. The goal is volume of correct output, not grammatical precision. Used for building speaking confidence and pacing.

#### Upload Metadata Defaults

| Field              | Typical Value(s)                                                |
|--------------------|-----------------------------------------------------------------|
| `category`         | `Fluency Drill`                                                 |
| `grammarFocusTag`  | `General Fluency` or speed target: `Question Fluency`, `Response Fluency`, `Topic Transition Fluency` |
| `contextTag`       | Topic or setting: `Daily Life`, `Opinions`, `Social Events`, `Shopping`, `Travel`, `Work Life`, `Hobbies` |
| `complexityLevel`  | 1–3 (FluencyDrill stays Beginner to Intermediate — speed over complexity) |
| `targetAgeGroup`   | `All` / `Teen` / `Adult`                                        |
| `hintLanguage`     | `Telugu` / `Hindi` / `Tamil` / `Kannada` / `None`              |

#### Speaker Labels (Required Set — FluencyDrill)

| SpeakerLabel   | Role Description                                              |
|----------------|---------------------------------------------------------------|
| `Speaker A`    | Initiates fast-paced dialogue turns                           |
| `Speaker B`    | Responds quickly, mirrors energy                              |

#### Column Usage Rules

| Column | Required | FluencyDrill-Specific Rule                                           |
|--------|----------|----------------------------------------------------------------------|
| A      | YES      | Sequential integers 1, 2, 3, …                                       |
| B      | YES      | Strictly `Speaker A` or `Speaker B`                                  |
| C      | YES      | Short sentences — target 5–15 words per turn; natural contractions OK|
| D      | NO       | Native language translation (optional for fluency drills)            |
| E      | NO       | Rarely used — only if a specific structure emerges naturally         |
| F      | YES      | Topic context — must match `contextTag`                              |
| G      | NO       | Optional — only when a specific word is the fluency target           |
| H      | NO       | Optional pronunciation note                                          |

#### Content Rules

1. Script length: **minimum 30 rows, maximum 60 rows** — FluencyDrill requires high volume
2. Average EnglishText word count: **5–15 words per turn** — no long paragraphs
3. Sentence variety: alternate question turns and statement turns freely
4. Natural contractions required: `I'm`, `don't`, `can't`, `it's`, `we're`, `won't`
5. No grammar explanation — scripts simulate natural conversation only
6. Topic must stay consistent throughout (no topic drift)
7. HintText is optional — FluencyDrill prioritises speed; translations slow reading
8. GrammarTag should be left blank unless a specific pattern is dominant
9. Turns should feel like a real-time conversation at natural speaking pace
10. No speaker should have more than 2 consecutive turns

#### Sample Data — FluencyDrill (Daily Life / Social Events)

| SequenceId | SpeakerLabel | EnglishText                               | HintText                                   | GrammarTag | ContextTag    | FocusWord | PronunciationNote |
|------------|--------------|-------------------------------------------|--------------------------------------------|------------|---------------|-----------|-------------------|
| 1          | Speaker A    | Did you go to the party last weekend?     | మీరు గత వారాంతంలో పార్టీకి వెళ్ళారా?     |            | Social Events |           |                   |
| 2          | Speaker B    | Yeah, I did! It was really fun.           | అవును, నేను వెళ్ళాను! అది నిజంగా సరదాగా ఉంది. |        | Social Events |           |                   |
| 3          | Speaker A    | Who else was there?                       | అక్కడ ఇంకా ఎవరు ఉన్నారు?                  |            | Social Events |           |                   |
| 4          | Speaker B    | Most of our friends from college.         | కళాశాల నుండి మన చాలా మంది స్నేహితులు.    |            | Social Events |           |                   |
| 5          | Speaker A    | Did you stay late?                        | మీరు ఆలస్యంగా ఉన్నారా?                    |            | Social Events |           |                   |
| 6          | Speaker B    | Until about midnight, yeah.               | అర్ధరాత్రి వరకు, అవును.                   |            | Social Events |           |                   |
| 7          | Speaker A    | That sounds like a great night.           | అది గొప్ప రాత్రిలా అనిపిస్తోంది.          |            | Social Events |           |                   |
| 8          | Speaker B    | It really was. You should have come!      | అది నిజంగా అలా ఉంది. మీరు వచ్చి ఉండాల్సింది! |       | Social Events |           |                   |
| 9          | Speaker A    | I know, I know. I had work to finish.     | నాకు తెలుసు, తెలుసు. నాకు పని పూర్తి చేయాలి. |       | Social Events |           |                   |
| 10         | Speaker B    | Next time, just come. Work can wait.      | తదుపరిసారి, కేవలం రండి. పని వేచి ఉండగలదు. |          | Social Events |           |                   |
| 11         | Speaker A    | You're right. Is there another event soon?| మీరు నిజమే చెప్పారు. త్వరలో మరొక ఈవెంట్ ఉందా? |       | Social Events |           |                   |
| 12         | Speaker B    | There's a game night next Friday!         | వచ్చే శుక్రవారం గేమ్ నైట్ ఉంది!          |            | Social Events |           |                   |
| 13         | Speaker A    | Oh nice, what kind of games?              | ఓహ్ బాగుంది, ఎలాంటి గేమ్స్?              |            | Social Events |           |                   |
| 14         | Speaker B    | Board games, trivia — things like that.   | బోర్డ్ గేమ్స్, ట్రివియా — అలాంటివి.      |            | Social Events |           |                   |
| 15         | Speaker A    | I'm definitely coming to that one!        | నేను ఖచ్చితంగా దానికి వస్తాను!            |            | Social Events |           |                   |

---

### 6.6 RepracticeRound

#### Purpose
Targeted mistake correction and reinforcement. Scripts are generated based on grammar errors that users previously made during live sessions. Each RepracticeRound script focuses on exactly one grammar error pattern and provides corrected practice through dialogue. Closely related to the **Backend Mistake Repractice Module**.

#### Upload Metadata Defaults

| Field              | Typical Value(s)                                                |
|--------------------|-----------------------------------------------------------------|
| `category`         | `Repractice Round`                                              |
| `grammarFocusTag`  | The specific grammar error being corrected: `Present Perfect vs Past Simple`, `Articles (a/an/the)`, `Subject-Verb Agreement`, `Preposition Use`, `Tense Consistency` |
| `contextTag`       | Same context as the original session where the mistake occurred: `Office`, `Travel`, `Daily Life`, etc. |
| `complexityLevel`  | Match the original session's complexity level                   |
| `targetAgeGroup`   | Match the original session's target age group                   |
| `hintLanguage`     | `Telugu` / `Hindi` / `Tamil` / `Kannada` / `None`              |

#### Speaker Labels (Required Set — RepracticeRound)

| SpeakerLabel   | Role Description                                                  |
|----------------|-------------------------------------------------------------------|
| `Coach`        | Models the correct form; corrects errors; provides explanation   |
| `Learner`      | Attempts the corrected form; confirms understanding              |

> RepracticeRound uses `Coach` and `Learner` to signal its corrective nature. This differs from GrammarDrill's generic `Speaker A/B`.

#### Column Usage Rules

| Column | Required | RepracticeRound-Specific Rule                                        |
|--------|----------|----------------------------------------------------------------------|
| A      | YES      | Sequential integers 1, 2, 3, …                                       |
| B      | YES      | Strictly `Coach` or `Learner`                                        |
| C      | YES      | Contains correct form of the previously-errored grammar pattern      |
| D      | YES      | REQUIRED — native translation helps learners understand the correct form |
| E      | YES      | REQUIRED — the exact grammar error tag matching `grammarFocusTag`    |
| F      | YES      | Scene context — must match the original error's context              |
| G      | NO       | The specific word that was misused originally                        |
| H      | NO       | Pronunciation of the FocusWord if pronunciation was part of the error|

#### Content Rules

1. Script length: **minimum 14 rows, maximum 28 rows** — RepracticeRounds are concise and focused
2. The script must only address one grammar error pattern — no blending
3. The Coach must explicitly model the correct form in the first 2 turns
4. At least 50% of Learner turns must produce the corrected grammar form
5. One Coach turn per 4 turns should provide a brief reinforcement note (e.g., "Notice how we use 'have been' here because the action is still ongoing")
6. GrammarTag (column E) must be the same for all rows — the error being corrected
7. HintText (column D) is REQUIRED — translation supports error comprehension
8. FocusWord (column G) should be the specific word where the error occurred
9. The script must end with the Learner successfully producing the correct form independently
10. Do not introduce new grammar concepts — RepracticeRound is single-error focused

#### Sample Data — RepracticeRound (Present Perfect vs Past Simple / Office)

| SequenceId | SpeakerLabel | EnglishText                                                                              | HintText                                              | GrammarTag                          | ContextTag | FocusWord | PronunciationNote |
|------------|--------------|------------------------------------------------------------------------------------------|-------------------------------------------------------|-------------------------------------|------------|-----------|-------------------|
| 1          | Coach        | Let's practise the difference between Present Perfect and Past Simple in a work context. | పని సందర్భంలో Present Perfect మరియు Past Simple మధ్య తేడాను సాధన చేద్దాం. | Present Perfect vs Past Simple | Office | practise | /ˈpræktɪs/ |
| 2          | Coach        | I have sent the report. — This means I sent it recently and it is still relevant now.   | నేను రిపోర్ట్ పంపాను — ఇది ఇటీవలే పంపాను మరియు ఇది ఇప్పటికీ సంబంధితంగా ఉంది. | Present Perfect vs Past Simple | Office | sent | /sɛnt/ |
| 3          | Learner      | So I use Present Perfect when the action is connected to now?                            | కాబట్టి చర్య ఇప్పటికీ సంబంధితంగా ఉన్నప్పుడు Present Perfect ఉపయోగిస్తాను? | Present Perfect vs Past Simple | Office | connected | |
| 4          | Coach        | Exactly. Now try: Tell me you finished the presentation. Use the correct tense.          | సరిగ్గా. ఇప్పుడు ప్రయత్నించు: మీరు ప్రెజెంటేషన్ పూర్తి చేశారని చెప్పండి. సరైన కాలాన్ని ఉపయోగించండి. | Present Perfect vs Past Simple | Office | finished | |
| 5          | Learner      | I have finished the presentation.                                                        | నేను ప్రెజెంటేషన్ పూర్తి చేశాను.                    | Present Perfect vs Past Simple      | Office     | finished  |                   |
| 6          | Coach        | Perfect. Now try Past Simple: tell me you sent the email yesterday.                      | సరైనది. ఇప్పుడు Past Simple ప్రయత్నించు: నిన్న ఇమెయిల్ పంపారని చెప్పండి. | Present Perfect vs Past Simple | Office | sent | |
| 7          | Learner      | I sent the email yesterday.                                                              | నేను నిన్న ఇమెయిల్ పంపాను.                          | Present Perfect vs Past Simple      | Office     | sent      |                   |
| 8          | Coach        | Correct! 'Yesterday' tells us exactly when — so Past Simple is right. Never say 'I have sent it yesterday.' | సరైనది! 'నిన్న' మనకు ఖచ్చితంగా ఎప్పుడు జరిగిందో చెప్తుంది — కాబట్టి Past Simple సరైనది. | Present Perfect vs Past Simple | Office | yesterday | |
| 9          | Learner      | I understand. 'Yesterday' means I cannot use Present Perfect.                            | నాకు అర్థమైంది. 'నిన్న' అంటే నేను Present Perfect ఉపయోగించలేను. | Present Perfect vs Past Simple | Office | | |
| 10         | Coach        | Exactly right. Now try: you have been working on a project since Monday.                 | ఖచ్చితంగా సరైనది. ఇప్పుడు ప్రయత్నించు: సోమవారం నుండి మీరు ప్రాజెక్ట్‌పై పని చేస్తున్నారు. | Present Perfect vs Past Simple | Office | since | /sɪns/ |
| 11         | Learner      | I have been working on the project since Monday.                                         | నేను సోమవారం నుండి ప్రాజెక్ట్‌పై పని చేస్తున్నాను.  | Present Perfect vs Past Simple      | Office     | since     |                   |
| 12         | Coach        | Excellent. You are using it confidently now. Let's do one final round.                   | అద్భుతం. మీరు ఇప్పుడు నమ్మకంగా ఉపయోగిస్తున్నారు. ఒక చివరి రౌండ్ చేద్దాం. | Present Perfect vs Past Simple | Office | confidently | /ˈkɒnfɪdəntli/ |
| 13         | Learner      | I am ready. Should I use Present Perfect or Past Simple for something that happened last week? | నేను సిద్ధంగా ఉన్నాను. గత వారం జరిగిన దాని కోసం Present Perfect లేదా Past Simple ఉపయోగించాలా? | Present Perfect vs Past Simple | Office | | |
| 14         | Coach        | Last week is a finished time reference. Use Past Simple. Well done — you have mastered this distinction. | గత వారం ముగిసిన సమయ సూచన. Past Simple ఉపయోగించండి. చాలా బాగుంది — మీరు ఈ తేడాను నేర్చుకున్నారు. | Present Perfect vs Past Simple | Office | mastered | /ˈmæstərd/ |

---

## 7. Metadata Upload Standards

When uploading any Excel file via `POST /api/v1/scripts/upload`, the following metadata form fields must be completed. These are sent as form data alongside the file.

### 7.1 Metadata Fields by Category

| Field              | GrammarDrill         | Roleplay              | MockInterview          | VocabularySprint       | FluencyDrill           | RepracticeRound        |
|--------------------|----------------------|-----------------------|------------------------|------------------------|------------------------|------------------------|
| `scriptTitle`      | Required, unique     | Required, unique      | Required, unique       | Required, unique       | Required, unique       | Required, unique       |
| `category`         | `Grammar Drill`      | `Roleplay`            | `Mock Interview`       | `Vocabulary Sprint`    | `Fluency Drill`        | `Repractice Round`     |
| `grammarFocusTag`  | Specific structure   | Topic/register tag    | Formal register tag    | Vocabulary theme tag   | `General Fluency`      | Specific error tag     |
| `contextTag`       | Scene location       | Scenario location     | Interview type         | Vocabulary theme       | Topic category         | Original error context |
| `complexityLevel`  | 1–5                  | 1–5                   | 3–5 only               | 1–5                    | 1–3 only               | Match original session |
| `targetAgeGroup`   | All/Child/Teen/Adult | All/Teen/Adult        | Adult only             | All/Teen/Adult         | All/Teen/Adult         | Match original session |
| `hintLanguage`     | Any valid            | Any valid             | Any valid              | Any valid              | Any valid              | Match original session |

### 7.2 Valid Enum Values

**`targetAgeGroup`**: `All` | `Child` | `Teen` | `Adult`

**`hintLanguage`**: `Telugu` | `Hindi` | `Tamil` | `Kannada` | `None`

**`complexityLevel`**: Integer `1` through `5`
- 1 = Absolute Beginner
- 2 = Beginner
- 3 = Intermediate
- 4 = Upper Intermediate
- 5 = Advanced

---

## 8. Validation Rules Guide

### 8.1 Client-Side Validation (Frontend — Pre-API)

| Rule                  | Condition                             | Error Shown                           |
|-----------------------|---------------------------------------|---------------------------------------|
| File extension        | File is not `.xlsx`                   | "Only .xlsx files are accepted"       |
| File size             | File exceeds 5 MB                     | "File must be under 5 MB"             |
| Script title          | Empty on Continue click               | Continue button stays disabled        |
| Category              | Not selected                          | Continue button stays disabled        |
| Complexity level      | Not selected                          | Continue button stays disabled        |
| Target age group      | Not selected                          | Continue button stays disabled        |
| Hint language         | Not selected                          | Continue button stays disabled        |

### 8.2 Server-Side Row Validation (Backend — `ExcelParserService`)

| Field          | Rule                                                          | Error Format                                    |
|----------------|---------------------------------------------------------------|-------------------------------------------------|
| `SequenceId`   | Must be a positive integer                                    | `Row {n} — SequenceId: must be a positive integer` |
| `SequenceId`   | Must be unique within the file                                | `Row {n} — SequenceId: duplicate value {v}`    |
| `SpeakerLabel` | Must not be empty                                             | `Row {n} — SpeakerLabel: is required`          |
| `EnglishText`  | Must not be empty                                             | `Row {n} — EnglishText: is required`           |
| `EnglishText`  | Must not exceed 512 characters                                | `Row {n} — EnglishText: exceeds 512 characters`|
| `HintText`     | Optional, but if present must not exceed 512 characters       | `Row {n} — HintText: exceeds 512 characters`   |

### 8.3 Category-Level Content Validation (Authoring Standards — Not API-Enforced)

These rules are enforced by content review, not the API. They must be validated manually or by AI generation pipelines before upload.

| Category        | Min Rows | Max Rows | D Required | G Required | H Required | Speaker Labels         |
|-----------------|----------|----------|------------|------------|------------|------------------------|
| GrammarDrill    | 12       | 30       | No         | No         | No         | `Speaker A`, `Speaker B` |
| Roleplay        | 16       | 40       | No         | No         | No         | Role-based (see §6.2)  |
| MockInterview   | 20       | 50       | No         | Yes        | No         | `Interviewer`, `Candidate` |
| VocabularySprint| 20       | 40       | YES        | YES        | YES (Tutor)| `Tutor`, `Learner`     |
| FluencyDrill    | 30       | 60       | No         | No         | No         | `Speaker A`, `Speaker B` |
| RepracticeRound | 14       | 28       | YES        | No         | No         | `Coach`, `Learner`     |

### 8.4 Pre-Upload Checklist

Before submitting any Excel file:

- [ ] File is `.xlsx` format
- [ ] File is under 5 MB
- [ ] Row 1 contains exact header labels (case-sensitive)
- [ ] SequenceId starts at 1 and is sequential with no gaps
- [ ] No blank rows within the data range
- [ ] SpeakerLabel values match category-required labels exactly
- [ ] No EnglishText cell exceeds 512 characters
- [ ] No HintText cell exceeds 512 characters
- [ ] GrammarTag values are consistent (especially GrammarDrill and RepracticeRound)
- [ ] FocusWord appears verbatim in the same row's EnglishText
- [ ] Row count is within category min/max limits
- [ ] ScriptTitle is unique (check existing library before upload)
- [ ] File is named per the naming convention (§3.1)

---

## 9. Content Writing Standards

### 9.1 English Quality Rules (All Categories)

| Rule                      | Standard                                                                 |
|---------------------------|--------------------------------------------------------------------------|
| Grammar accuracy          | All EnglishText must be grammatically correct standard British/American English |
| Sentence completeness     | Every EnglishText must be a complete sentence (no fragments except FluencyDrill) |
| Register consistency      | Do not mix formal and informal register within a single script           |
| Cultural neutrality        | Avoid culture-specific humour, religion, politics, or regional idioms    |
| Realism                   | Scenarios must reflect real, everyday professional or social situations  |
| No meta-commentary        | Never include instructions inside EnglishText (e.g., "say this slowly") |
| Character voice           | Maintain consistent character voice throughout the script               |

### 9.2 HintText Writing Standards

| Rule                         | Standard                                                              |
|------------------------------|-----------------------------------------------------------------------|
| Full sentence translation     | Translate the complete EnglishText — never word-by-word              |
| Natural target language       | Write in natural target language (Telugu/Hindi/Tamil/Kannada) — not transliteration |
| Script match                 | HintText must convey identical meaning to EnglishText                |
| Cultural adaptation          | Adapt phrasing naturally — do not force literal translation          |
| Formality match              | Formal EnglishText → formal HintText; informal → informal            |

### 9.3 GrammarTag Standards

| Rule                          | Standard                                                             |
|-------------------------------|----------------------------------------------------------------------|
| Consistency within script     | GrammarDrill and RepracticeRound: same tag on all rows             |
| Specificity                   | Use specific tags: `Present Perfect` not `Tense`                   |
| Recognisable taxonomy         | Use standard EFL grammar labels (Cambridge / Oxford taxonomy)      |
| Multi-structure scripts        | Roleplay/MockInterview may vary GrammarTag per row                 |
| Blank = no notable grammar    | Leave blank rather than inventing a tag                            |

**Approved GrammarTag List:**
`Present Simple` | `Present Continuous` | `Present Perfect` | `Present Perfect Continuous` | `Past Simple` | `Past Continuous` | `Past Perfect` | `Future Simple` | `Future Continuous` | `Future Perfect` | `Modal Verbs` | `Passive Voice` | `Conditionals` | `Reported Speech` | `Gerunds` | `Infinitives` | `Articles` | `Prepositions` | `Subject-Verb Agreement` | `STAR Method` | `Formal Register` | `Question Forms` | `Contrast` | `Vocabulary` | `General Fluency`

---

## 10. AI Generation Protocol

This section defines how Claude or any AI system must generate upload-ready Excel files following this standard.

### 10.1 AI Prompt Template (Instruction to AI)

When instructed to generate an Excel file for GoWithFlow, the AI must:

```
1. Identify category from user request → apply §6 rules for that category
2. Select appropriate speaker labels from §6 for the category
3. Select grammarFocusTag, contextTag, complexityLevel from §7.1 category defaults
4. Generate rows:
   - Start SequenceId at 1, increment by 1, no gaps
   - Alternate between the two speaker labels per category standard
   - Meet minimum row count for the category (§8.3)
   - Apply column D requirement (mandatory if category requires it)
   - Apply column G requirement (mandatory if category requires it)
   - Apply column H requirement (mandatory if category requires it)
5. Apply content writing standards from §9
6. Output in JSON format for downstream Excel generation:
   {
     "metadata": {
       "scriptTitle": "...",
       "category": "...",  ← exact DB value
       "grammarFocusTag": "...",
       "contextTag": "...",
       "complexityLevel": 3,
       "targetAgeGroup": "Adult",
       "hintLanguage": "Telugu"
     },
     "rows": [
       {
         "sequenceId": 1,
         "speakerLabel": "...",
         "englishText": "...",
         "hintText": "...",
         "grammarTag": "...",
         "contextTag": "...",
         "focusWord": "...",
         "pronunciationNote": "..."
       }
     ]
   }
```

### 10.2 AI Self-Validation Before Output

Before finalising any AI-generated script output, validate:

- [ ] SequenceId: starts at 1, sequential, no duplicates
- [ ] SpeakerLabel: matches category-required labels only
- [ ] EnglishText: grammatically correct, under 512 chars, no fragments (except FluencyDrill)
- [ ] HintText: present on all rows if category requires D-column; under 512 chars
- [ ] GrammarTag: consistent within category constraint; from approved list
- [ ] FocusWord: appears verbatim in same row's EnglishText
- [ ] Row count: within category min/max
- [ ] Speaker alternation: no speaker has 3+ consecutive turns (except RepracticeRound Coach explanation blocks, max 2 consecutive)
- [ ] Register: consistent throughout
- [ ] Metadata: all required fields present and valid enum values used

---

## 11. Upload Compatibility Rules

### 11.1 File Format Requirements

| Property           | Required Value                                 |
|--------------------|------------------------------------------------|
| Format             | `.xlsx` (OOXML — Excel 2007 and later)         |
| NOT accepted       | `.xls`, `.csv`, `.ods`, `.xlsm`, `.xlsb`       |
| Max file size      | 5 MB                                           |
| Sheet read index   | Sheet at index 0 (first sheet)                 |
| Header row         | Row 1 — always present, always skipped         |
| Data start row     | Row 2                                          |
| Parser library     | ClosedXML (C# backend)                         |

### 11.2 ClosedXML Compatibility Rules

- All cells must contain raw string or numeric values — no Excel formulas
- No merged cells within the data range (columns A–H, row 2+)
- No password protection on the workbook or sheets
- No hidden rows within the data range — hidden rows are parsed as data rows
- No drop-down validation or conditional formatting that contains data — cosmetic formatting only
- Date values in SequenceId column will fail — always use plain integers

### 11.3 Encoding

- All text must be UTF-8 compatible
- Telugu, Hindi, Tamil, Kannada characters are stored in `NVARCHAR` columns — full Unicode support
- Emoji characters are not permitted in any column
- Line breaks within a cell are not permitted — use a new row instead

---

## 12. Quick Reference Card

### Speaker Label Summary

| Category         | Speaker 1        | Speaker 2          |
|------------------|------------------|--------------------|
| GrammarDrill     | `Speaker A`      | `Speaker B`        |
| Roleplay         | Role-based       | Role-based         |
| MockInterview    | `Interviewer`    | `Candidate`        |
| VocabularySprint | `Tutor`          | `Learner`          |
| FluencyDrill     | `Speaker A`      | `Speaker B`        |
| RepracticeRound  | `Coach`          | `Learner`          |

### Mandatory Columns by Category

| Category         | D (HintText) | G (FocusWord) | H (PronunciationNote)   |
|------------------|:------------:|:-------------:|:-----------------------:|
| GrammarDrill     | Optional     | Optional      | Optional                |
| Roleplay         | Optional     | Optional      | Optional                |
| MockInterview    | Optional     | Required      | Optional                |
| VocabularySprint | Required     | Required      | Required (Tutor rows)   |
| FluencyDrill     | Optional     | Optional      | Optional                |
| RepracticeRound  | Required     | Optional      | Optional                |

### Row Count Limits

| Category         | Min Rows | Max Rows |
|------------------|:--------:|:--------:|
| GrammarDrill     | 12       | 30       |
| Roleplay         | 16       | 40       |
| MockInterview    | 20       | 50       |
| VocabularySprint | 20       | 40       |
| FluencyDrill     | 30       | 60       |
| RepracticeRound  | 14       | 28       |

### Column Map (All Categories)

| Col | Field              | Always Required |
|-----|--------------------|:---------------:|
| A   | SequenceId         | YES             |
| B   | SpeakerLabel       | YES             |
| C   | EnglishText        | YES             |
| D   | HintText           | Category-based  |
| E   | GrammarTag         | No              |
| F   | ContextTag         | YES             |
| G   | FocusWord          | Category-based  |
| H   | PronunciationNote  | Category-based  |

### File Naming Pattern

```
[CategoryCode]_[script-title-slug]_v[Version]_[YYYY-MM-DD].xlsx
```

---

*Document end. Version 1.0 — 2026-05-22.*
*This document is the permanent reference. All category templates, AI generation, content team uploads, and admin tooling must comply with this standard.*
