# GoWithFlow — Category Workflow & Product Plan
### Senior Project Manager Review | Date: 2026-06-01
### Status: WORKING DOCUMENT — Product and Workflow Planning Only

---

## Table of Contents

**Part I — Workflow and Optimisation Review**
1. [Executive Summary](#1-executive-summary)
2. [Platform Analysis — Current State](#2-platform-analysis--current-state)
3. [Universal Workflow Problem — Where It Breaks](#3-universal-workflow-problem--where-it-breaks)
4. [Category-Wise Analysis and Recommended Workflows](#4-category-wise-analysis-and-recommended-workflows)
   - 4.1 GrammarDrill
   - 4.2 Roleplay
   - 4.3 MockInterview
   - 4.4 VocabularySprint
   - 4.5 FluencyDrill
   - 4.6 RepracticeRound
5. [Voice Analysis vs Listener Feedback — Duplication Review](#5-voice-analysis-vs-listener-feedback--duplication-review)
6. [Excel Template Strategy — Category-Specific Design](#6-excel-template-strategy--category-specific-design)
7. [Required / Optional / Remove / Simplify Matrix](#7-required--optional--remove--simplify-matrix)
8. [Priority Recommendations Summary](#8-priority-recommendations-summary)

**Part II — New Feature Roadmap**
9. [Platform Gap Analysis](#9-platform-gap-analysis)
10. [New Feature Recommendations](#10-new-feature-recommendations)
    - Must Have: Post-Session Review, Guided Learning Path, Weekly Learning Report, Vocabulary Retention Tracker
    - High Value: Interview Performance Dashboard, Pronunciation Timeline, AI Script Generator, Session Prep Mode, Partner Matching, Learning Goals, Admin Script Analytics
    - Future Roadmap: Spaced Repetition, Cohort Management, Challenge Mode, Certificates, Audio Archive
11. [Voice Analysis — Capability Enhancement Recommendations](#11-voice-analysis--capability-enhancement-recommendations)
12. [Script Upload and Excel Template — Process Improvements](#12-script-upload-and-excel-template--process-improvement-recommendations)
13. [Feature Prioritisation Summary](#13-feature-prioritisation-summary)
14. [Product Differentiation Summary](#14-product-differentiation-summary)

**Part III — Implementation Prompt Library (Phase-Wise)**
- [Phase 0 — Workflow Fixes](#phase-0--workflow-fixes-do-before-any-new-features) (4 steps)
- [Phase 1 — Must Have Features](#phase-1--must-have-features) (4 steps)
- [Phase 2 — High Value Features](#phase-2--high-value-features) (9 steps)
- [Phase 3 — Future Roadmap Features](#phase-3--future-roadmap-features) (5 steps)

---

## 1. Executive Summary

GoWithFlow is an English fluency practice platform with six content categories. The platform currently applies a **single, uniform session workflow** across all six categories — a turn-based two-speaker alternation model with shared scoring, listener feedback tags, and a common voice analysis pipeline.

This approach works well for some categories but creates **friction, mismatch, and confusion** in others. Specifically:

- **MockInterview** and **RepracticeRound** have asymmetric roles where one party (Interviewer, Coach) should not be evaluated the same way as the other.
- **VocabularySprint** has an instructional structure where the Tutor's turns are content delivery, not speaking practice.
- The **listener feedback system** (Good / Hesitated / Mistake / Unclear Pronunciation) largely duplicates what the voice analysis engine already detects automatically — creating confusion, extra taps for users, and inconsistent data.
- The **single Excel template** is applied uniformly, but several categories need different data fields, different column emphasis, and different content structures.

This document defines the recommended workflow per category, identifies what should be required, optional, removed, and simplified, and provides a category-specific Excel template strategy.

---

## 2. Platform Analysis — Current State

### 2.1 What Is Common Today (Forced Uniformity)

The platform currently enforces the following universal pattern across all six categories:

| Component | Current Behaviour |
|---|---|
| Session structure | Two speakers alternate turns using script utterances |
| Turn scoring | Every speaker turn goes through full voice analysis (FluencyScore, ConfidenceScore, GrammarErrors, HesitationWords, etc.) |
| Listener feedback | Non-speaking members can click tags: Good / Hesitated / Mistake / Unclear Pronunciation — for any active speaker turn |
| Re-read | Both speakers can re-read up to 2 times per turn |
| Session completion | All members receive a shared summary with scores |
| Excel template | All categories use the same 8-column structure with shared column definitions |

### 2.2 What Works Well

- The voice analysis engine is **fully automatic and already comprehensive**. It captures fluency, confidence, grammar errors, pronunciation issues, hesitation words, repeated words, speaking speed, and pause count — without any human input.
- The turn-based structure works naturally for **GrammarDrill, Roleplay, and FluencyDrill** — categories where both speakers have equal performance roles.
- The category-specific speaker labels (Interviewer/Candidate, Tutor/Learner, Coach/Learner) are already differentiated in the script, which is a sound foundation for category-aware logic.

### 2.3 Where the Platform Is Over-Engineered or Mismatched

- **Listener feedback exists alongside automatic analysis** — both capture hesitation, mistakes, and pronunciation. This creates a redundant user action for something the system already knows.
- **Scoring is applied uniformly** to all turns regardless of whether the speaker's role is evaluative (e.g., Interviewer asking a question) or performative (e.g., Candidate answering).
- **Excel templates carry optional columns that vary by category** but are presented identically — creating confusion for content authors about what is actually needed for each category.

---

## 3. Universal Workflow Problem — Where It Breaks

The core problem is that the platform uses a **performance-parity model** — both participants are treated as equally assessed speakers — when several categories have **role-asymmetry by design**.

### 3.1 Performance-Parity Categories (Uniform Workflow is Correct)

These categories have genuinely equal speaking roles. The current workflow is appropriate:

| Category | Why Parity Works |
|---|---|
| GrammarDrill | Speaker A and Speaker B have equal turns practicing the same grammar structure |
| Roleplay | Both characters play roles of equal participation in the scenario |
| FluencyDrill | High-speed, equal-turn conversation — parity is the entire point |

### 3.2 Role-Asymmetric Categories (Uniform Workflow is a Mismatch)

These categories have one role that is instructional, evaluative, or facilitative — not performative:

| Category | Role Imbalance | Why the Current Workflow Breaks |
|---|---|---|
| MockInterview | Interviewer asks a question (short, formal), Candidate answers (long, structured response) | Scoring the Interviewer's fluency is irrelevant. The Interviewer is not practicing English — they are facilitating the Candidate's practice. Listener feedback on the Interviewer makes no sense. |
| VocabularySprint | Tutor introduces a word in context (instructional), Learner responds using the word (practice) | The Tutor's turn is content delivery. Scoring the Tutor's fluency is misleading. Only the Learner is practicing vocabulary. |
| RepracticeRound | Coach models and corrects (instructional), Learner practices the corrected form (performance) | The Coach is demonstrating the correct form. The session exists to improve the Learner — not to evaluate the Coach. Scoring the Coach equally undermines the corrective intent. |

### 3.3 The Consequence of Forcing Uniformity

When a single workflow is forced across all categories:

1. Users in asymmetric roles (Interviewer, Tutor, Coach) receive fluency scores that have no meaning.
2. Listener feedback on Interviewer/Tutor/Coach turns is irrelevant and creates noise.
3. The session summary shows scores for both participants equally — which misrepresents the session's purpose and value.
4. Content authors must populate the same Excel columns regardless of what is actually meaningful for their category, leading to incorrect or placeholder data.

---

## 4. Category-Wise Analysis and Recommended Workflows

---

### 4.1 GrammarDrill

**Purpose:** Structured practice of a single grammar structure through realistic dialogue between two equal speakers.

**Current Workflow Assessment:** Correct. No changes needed.

**Recommended Workflow:**

```
User Journey:
  1. User joins session and is assigned Speaker A or Speaker B slot.
  2. Both speakers alternate turns reading utterances that demonstrate the grammar target.
  3. Each turn: speaker records voice → automatic analysis runs → feedback shown.
  4. Listeners can optionally observe (listener feedback is low-value here — see Section 5).
  5. Session ends → both speakers receive fluency summary with grammar error breakdown.
  6. Mistakes detected in GrammarDrill feed the Repractice pipeline.

Best Practice:
  - Scripts focus on one grammar structure across all turns.
  - Minimum 12 turns, maximum 30 turns.
  - Both speakers receive equal analysis.
  - GrammarTag is the same for all rows (enforced by content rule).
```

**What Works:** Full voice analysis for both speakers. Equal scoring. Mistake extraction feeds RepracticeRound.

**What to Keep:** All current functionality. This category is the most stable.

**Category-Specific Need:** None — this category aligns well with the universal workflow.

---

### 4.2 Roleplay

**Purpose:** Real-world scenario simulation with two characters. Builds contextual fluency, social language, and natural conversation flow.

**Current Workflow Assessment:** Mostly correct. Both roles have genuinely equal participation. Minor improvements in session setup.

**Recommended Workflow:**

```
User Journey:
  1. User joins session and is assigned a character slot (e.g., Passenger, Check-In Agent).
  2. Both characters alternate turns following the scenario script.
  3. Each turn: speaker records voice → automatic analysis runs → Duolingo-style word feedback shown.
  4. Session ends → both members receive fluency summary in context of their role.

Best Practice:
  - Scripts have a clear beginning, middle, and end (scenario arc).
  - Role names should match real-world job/social titles (not generic Speaker A/B).
  - Politeness language embedded in script naturally (not forced grammar targets).
  - Minimum 16 turns, maximum 40 turns.
```

**What Works:** Role-based speaker labels add realism. Equal scoring suits the equal participation model.

**What to Improve:** The session summary should surface **scenario completion** as an outcome metric — "Did the conversation reach a natural resolution?" — rather than just fluency scores. This is aspirational, not urgent.

**What is Not Required:** Listener feedback (see Section 5 — it adds no value here beyond what automatic analysis provides).

**Category-Specific Need:** None structural. Content quality (scenario realism, politeness language) is the main lever for this category.

---

### 4.3 MockInterview

**Purpose:** Professional English interview practice. The Candidate answers questions using formal register, STAR method, and professional vocabulary. The Interviewer facilitates.

**Current Workflow Assessment:** Significantly mismatched. The current uniform workflow treats the Interviewer as an equally scored performance participant. This is incorrect for this category.

**Recommended Workflow:**

```
User Journey:
  1. User joins session as either Interviewer or Candidate.
  2. Interviewer reads questions from script (formal, no contractions).
  3. Candidate answers using structured professional English (STAR method encouraged).
  4. ONLY the Candidate's turns receive full voice analysis and scoring.
  5. Interviewer turns: text is displayed, speaker reads it aloud, no scoring required.
     (Optional: light analysis for the Interviewer's own pronunciation reference, not displayed to the Candidate.)
  6. Re-read: Candidate only. The Interviewer reads a formal question once — re-read is not needed.
  7. Session ends → Candidate receives full interview performance report:
       - Fluency score per answer
       - Grammar errors in answers
       - Professional vocabulary usage
       - Speaking speed and confidence
  8. Interviewer sees a session summary (questions asked, Candidate's overall score) but not their own fluency analysis.
  9. Mistakes from Candidate's grammar errors feed the Repractice pipeline.

Best Practice:
  - Scripts should include at least one challenging follow-up probe.
  - Candidate answers must be multi-sentence (enforced by content rule — minimum 2 sentences).
  - Formal register strictly maintained in Interviewer turns.
  - ContextTag must reflect the interview type (HR / Tech / Behavioural) for accurate scoring context.
```

**What Must Change:**
- **Scoring asymmetry:** Candidate turns receive full analysis. Interviewer turns are navigational only (read aloud, advance turn).
- **Session summary:** Only Candidate metrics are displayed as the performance outcome.
- **Listener feedback on Interviewer turns:** Not applicable. Should be suppressed for Interviewer turns.

**What to Keep:** Script structure (Interviewer/Candidate labels), FocusWord for professional vocabulary, GrammarTag (STAR Method, Formal Register).

**What to Remove:** Interviewer fluency scoring displayed as a performance metric. Re-read button on Interviewer turns.

**Category-Specific Need:** Category-aware session logic to distinguish "facilitator turns" from "performance turns" — Interviewer is a facilitator in MockInterview.

---

### 4.4 VocabularySprint

**Purpose:** Rapid vocabulary building through Tutor-introduced words and Learner reinforcement. Each session builds 10–20 new words in context.

**Current Workflow Assessment:** Mismatched. The Tutor's turns are instructional delivery, not speaking practice. Scoring the Tutor equally misrepresents the session's value.

**Recommended Workflow:**

```
User Journey:
  1. User joins session as either Tutor or Learner.
  2. Tutor introduces each vocabulary word in a sentence (FocusWord + context sentence).
  3. Learner responds using the same word to confirm understanding and produce the word.
  4. ONLY the Learner's turns receive full voice analysis.
     → Checks if the Learner correctly pronounced the FocusWord.
     → Checks if the FocusWord was spoken (word-level match against FocusWord column).
  5. Tutor turns: text is read aloud for Learner exposure. No performance analysis needed.
  6. After every Tutor-Learner pair, the Learner's word score is captured.
  7. Session ends → Learner receives a vocabulary acquisition report:
       - Words introduced: list of FocusWords from session
       - Words spoken correctly: FocusWords matched in Learner's voice analysis
       - Pronunciation accuracy per word (PronunciationNote vs actual spoken form)
       - Fluency score as secondary metric

Best Practice:
  - Each Tutor turn introduces exactly one new FocusWord.
  - Each Learner turn uses the same FocusWord as the preceding Tutor turn.
  - HintText is mandatory — language translation confirms comprehension.
  - PronunciationNote is mandatory for Tutor turns (IPA for each word).
  - Scripts progress from simpler words to more complex.
```

**What Must Change:**
- **Scoring:** Only Learner turns are performance-scored.
- **Session summary:** Should show "Words practiced today" as the primary outcome, not just a raw fluency score.
- **Listener feedback:** Not applicable — this is a 1-to-1 instructional flow.

**What to Keep:** Tutor/Learner speaker labels, mandatory HintText, mandatory FocusWord and PronunciationNote.

**What to Remove:** Tutor fluency scoring as a session performance metric.

**Category-Specific Need:** Word-level acquisition tracking (did the Learner speak the FocusWord correctly?) as the primary outcome metric, not just a global fluency score.

---

### 4.5 FluencyDrill

**Purpose:** High-volume, fast-paced conversation to build speaking confidence and reduce hesitation. Volume of output matters more than grammatical precision.

**Current Workflow Assessment:** Correct. Both speakers have equal roles. The uniform workflow suits this category.

**Recommended Workflow:**

```
User Journey:
  1. User joins session as Speaker A or Speaker B.
  2. Speakers alternate turns rapidly through a high-volume script (30–60 turns).
  3. Turns are short (5–15 words). The focus is speed and naturalness, not grammar.
  4. Both speakers receive full voice analysis (fluency and confidence are the key metrics).
  5. Grammar error analysis is secondary — FluencyDrill prioritises output volume.
  6. Session ends → both speakers receive a fluency summary:
       - Overall fluency score
       - Speaking speed (WPM)
       - Hesitation count
       - Confidence score
       Note: Grammar error detail is de-emphasised in the FluencyDrill summary view.

Best Practice:
  - Scripts must feel like real-time conversation — natural contractions required.
  - No grammar explanation turns — purely conversational.
  - No speaker should hold more than 2 consecutive turns.
  - Minimum 30 turns — high volume is the defining feature of this category.
```

**What Works:** Equal scoring, high-turn-count script, speed focus. This category aligns well with the universal workflow.

**What to Simplify:** The session summary for FluencyDrill should highlight **speaking speed and hesitation count** as the primary metrics, with grammar errors shown but downplayed. Grammar drill has its own category for that.

**Category-Specific Need:** Summary view emphasis on fluency metrics (speed, hesitation, confidence) over grammar error detail.

---

### 4.6 RepracticeRound

**Purpose:** Targeted correction of a specific grammar mistake a user previously made. The Coach models the correct form; the Learner practices it.

**Current Workflow Assessment:** Significantly mismatched. This category exists entirely to correct the Learner's errors. The Coach's role is instructional correction, not speaking performance.

**Recommended Workflow:**

```
User Journey:
  1. RepracticeRound is typically triggered from the Mistake dashboard:
     "You made 3 errors with Present Perfect. Start a repractice session."
  2. The Learner is the primary (and most common: solo) participant.
     The Coach role can be filled by another user OR by a read-aloud script display
     (the Coach turn is shown on screen and read aloud automatically, not requiring a second live human).
  3. Learner listens to Coach's model sentence, then practices the corrected form.
  4. ONLY the Learner's turns receive full voice analysis.
     → Focus is on whether the target grammar structure was correctly produced.
     → Grammar error analysis specifically checks for the error pattern being corrected.
  5. The Coach's model sentences are displayed text + optional TTS playback — they are NOT scored.
  6. Session ends → Learner receives a correction report:
       - Was the corrected pattern produced correctly? (per turn)
       - Improvement percentage vs. the original mistake session
       - Recommendation: practice again, or move on

Best Practice:
  - Scripts address exactly one grammar error pattern — no blending.
  - The Coach explicitly models the correct form in the first 2 turns.
  - The Learner must produce the corrected form independently by the final turns.
  - HintText is mandatory — translation helps with error comprehension.
  - GrammarTag is the same for all rows — the single error pattern being corrected.
  - Scripts are concise: 14–28 turns only.
```

**What Must Change:**
- **Solo mode consideration:** RepracticeRound should be usable by a single Learner (Coach turns are text display, not a live second speaker). Requiring two live humans to practice a mistake correction creates unnecessary friction.
- **Scoring:** Only Learner turns are performance-scored.
- **Session summary:** Correction report, not a fluency competition. Show "% of turns where the correct pattern was produced."

**What to Keep:** Coach/Learner labels, mandatory HintText, mandatory GrammarTag, single-error-pattern rule.

**What to Remove:** Coach fluency scoring. Listener feedback on Coach turns.

**Critical User Experience Concern:** If RepracticeRound requires a second live user to play the Coach role, the user who needs to correct their mistake faces a barrier to entry. The session should not depend on finding a practice partner to fix a personal grammar error.

---

## 5. Voice Analysis vs Listener Feedback — Duplication Review

### 5.1 What the Automatic Voice Analysis Already Captures

The platform's voice analysis engine runs automatically on every speaker turn and captures:

| Metric | Automatically Detected |
|---|---|
| Fluency score | YES — word match accuracy, weighted score |
| Confidence score | YES — API confidence minus hesitation penalty |
| Grammar errors | YES — ExpectedPhrase vs. SpokenPhrase with ErrorType |
| Hesitation words | YES — um, uh, er, hmm detected in transcript |
| Repeated words | YES — consecutive duplicate word detection |
| Pronunciation issues | YES — word-level phonetic mismatch |
| Speaking speed (WPM) | YES — words per minute of recording |
| Pause count | YES — silence events via VAD |

### 5.2 What the Listener Feedback System Currently Offers

| FeedbackTag | What It Captures |
|---|---|
| `Good` | A listener's subjective positive impression |
| `Hesitated` | A listener's perception of hesitation |
| `Mistake` | A listener's perception of a grammar/language error |
| `Unclear Pronunciation` | A listener's perception of unclear pronunciation |

### 5.3 Duplication Analysis

| Listener Tag | Automatic Equivalent | Overlap |
|---|---|---|
| `Hesitated` | `HesitationWords[]` detected in transcript | **Direct duplicate** — system already captures hesitation |
| `Mistake` | `GrammarErrors[]` with ExpectedPhrase/SpokenPhrase/ErrorType | **Direct duplicate** — system already captures grammar errors at word level |
| `Unclear Pronunciation` | `PronunciationIssues[]` with Word/IssueNote | **Direct duplicate** — system already captures pronunciation issues |
| `Good` | `OverallScore >= 75` corresponds to "Great job" band | **Partial duplicate** — the score already measures this |

### 5.4 Where Listener Feedback Adds Genuine Value

The listener feedback system adds value **only in one specific scenario**: a multi-participant session (3+ members) where non-speaking observers can provide real-time social encouragement or flag issues the voice analysis might miss due to contextual understanding.

For example: a listener might click `Good` to encourage a peer in the moment — that is a social/motivational signal, not a duplicate of the technical analysis.

However, `Hesitated`, `Mistake`, and `Unclear Pronunciation` are already captured more accurately by the automatic system than any human listener click.

### 5.5 Recommendations on Listener Feedback

| Recommendation | Rationale |
|---|---|
| **Keep `Good` tag** | This is a social positive-reinforcement signal. The automatic system gives a score, but a peer clicking "Good" has motivational value beyond the number. |
| **Remove `Hesitated` tag** | The system already detects hesitation words. Human tag adds noise, not signal. |
| **Remove `Mistake` tag** | The system already captures grammar errors. A listener clicking "Mistake" without specifying which word is lower quality than what the analysis provides. |
| **Remove `Unclear Pronunciation` tag** | The system already detects pronunciation issues at word level. The listener tag is a blunt, less accurate version of the same thing. |
| **Simplify to: `Good` and `Needs Work`** | If listener feedback is kept at all, two tags suffice: a positive signal and a gentle concern flag. This preserves the social layer without duplicating technical analysis. |
| **Suppress listener feedback on facilitator turns** | Interviewer, Tutor, and Coach turns should not receive listener feedback tags. These turns are not performance turns. |

### 5.6 Summary Position

> The listener feedback system in its current 4-tag form is largely redundant. The automatic analysis already captures hesitation, mistakes, and pronunciation issues with higher accuracy and more detail. Reducing listener feedback to a single social signal (`Good`) or a simplified 2-tag system (`Good` / `Needs Work`) removes duplication and reduces user confusion without losing meaningful data.

---

## 6. Excel Template Strategy — Category-Specific Design

### 6.1 Current Situation

All categories share a single 8-column template (A=SequenceId through H=PronunciationNote). The columns are universally defined, but each category treats them differently:
- Some columns are mandatory in some categories and irrelevant in others.
- Content authors preparing GrammarDrill scripts are presented with columns meant for VocabularySprint (FocusWord required, PronunciationNote required) — creating confusion.
- The sample template distributed via download is the same file for all categories.

### 6.2 Recommendation: Category-Specific Sample Templates

Each category should have its own downloadable sample template. The underlying database structure and parser do not need to change — the 8-column contract remains. What changes is the **guidance layer**: which columns are pre-filled, highlighted, or required in each category's sample file.

This is not a structural change. It is a content authoring experience improvement.

### 6.3 Per-Category Template Design

---

#### 6.3.1 GrammarDrill Template

**Primary purpose:** Grammar structure practice through dialogue.

| Column | Label | Required | Guidance in Template |
|---|---|---|---|
| A | SequenceId | YES | Auto-numbered 1, 2, 3... |
| B | SpeakerLabel | YES | Only `Speaker A` or `Speaker B` |
| C | EnglishText | YES | Must use the grammar target structure ≥ 60% of turns |
| D | HintText | Optional | Full sentence translation in target language |
| E | GrammarTag | YES | **Must be identical on all rows** — the grammar structure being practiced |
| F | ContextTag | YES | **Must be identical on all rows** — the scene (e.g., Office, Airport) |
| G | FocusWord | Optional | The grammar-target-bearing word in the sentence |
| H | PronunciationNote | Optional | IPA for the FocusWord |

**Template note in file:** "Columns E and F must contain the same value on every row. Vary the dialogue, not the grammar tag."

**Row count guidance:** 12 minimum, 30 maximum. Sample template shows 12 rows.

---

#### 6.3.2 Roleplay Template

**Primary purpose:** Real-world scenario simulation with character roles.

| Column | Label | Required | Guidance in Template |
|---|---|---|---|
| A | SequenceId | YES | Auto-numbered |
| B | SpeakerLabel | YES | Use real role names: Passenger, Check-In Agent, Doctor, Patient, etc. |
| C | EnglishText | YES | Natural, conversational English. Contractions OK. |
| D | HintText | Optional | Full sentence translation |
| E | GrammarTag | Optional | Only tag if a specific structure is deliberately used in that turn |
| F | ContextTag | YES | Scenario location — must match upload metadata |
| G | FocusWord | Optional | Scenario-specific vocabulary word |
| H | PronunciationNote | Optional | IPA for FocusWord |

**Template note in file:** "Column B must use exactly two distinct role names from your scenario. Do not use Speaker A/B in Roleplay."

**Row count guidance:** 16 minimum, 40 maximum. Sample template shows 16 rows.

---

#### 6.3.3 MockInterview Template

**Primary purpose:** Professional interview practice — Candidate performance, Interviewer facilitation.

| Column | Label | Required | Guidance in Template |
|---|---|---|---|
| A | SequenceId | YES | Auto-numbered |
| B | SpeakerLabel | YES | Only `Interviewer` or `Candidate` |
| C | EnglishText | YES | Interviewer: formal questions only. Candidate: structured answers ≥ 2 sentences. |
| D | HintText | Optional | Translation for vocabulary reference |
| E | GrammarTag | YES | Tag Candidate answers: `STAR Method`, `Past Simple`, `Formal Register`, etc. |
| F | ContextTag | YES | Interview type: `HR Interview`, `Tech Interview`, `Behavioural Interview` |
| G | FocusWord | YES | Professional/industry-specific term in each turn |
| H | PronunciationNote | Optional | IPA for professional FocusWords |

**Template note in file:** "Interviewer turns are facilitator turns — they are read aloud but not performance-scored. Focus your Candidate turns on structured answers. Every Candidate answer should be ≥ 2 sentences."

**Row count guidance:** 20 minimum, 50 maximum. Sample template shows 12 rows (6 Q&A pairs).

**Column E — Interview-Specific Tag Note:**
GrammarTag for MockInterview serves a different purpose than in GrammarDrill. In MockInterview it tags the communication method or answer structure (STAR Method, Formal Register), not just the tense. Content authors should be guided to use these tags for categorising answer types, not grammar patterns only.

---

#### 6.3.4 VocabularySprint Template

**Primary purpose:** Word-by-word vocabulary introduction and reinforcement.

| Column | Label | Required | Guidance in Template |
|---|---|---|---|
| A | SequenceId | YES | Auto-numbered |
| B | SpeakerLabel | YES | Only `Tutor` or `Learner` |
| C | EnglishText | YES | Tutor: use the FocusWord naturally in a sentence. Learner: use the same FocusWord in a response. |
| D | HintText | **MANDATORY** | Native language translation — REQUIRED on all rows |
| E | GrammarTag | Optional | Only if sentence demonstrates a specific grammar use |
| F | ContextTag | YES | Vocabulary theme — must match upload metadata |
| G | FocusWord | **MANDATORY** | The vocabulary word being introduced — REQUIRED on all rows |
| H | PronunciationNote | **MANDATORY (Tutor rows)** | IPA — required on all Tutor turns; optional on Learner turns |

**Template note in file:** "This template has three mandatory columns: D (HintText), G (FocusWord), and H (PronunciationNote on Tutor rows). Each Tutor turn introduces one word. The Learner's following turn uses the same word. Do not introduce two words in one Tutor turn."

**Row count guidance:** 20 minimum, 40 maximum. Sample template shows 10 Tutor-Learner pairs (20 rows).

---

#### 6.3.5 FluencyDrill Template

**Primary purpose:** High-volume, fast-paced natural conversation. Speed and fluency over grammar precision.

| Column | Label | Required | Guidance in Template |
|---|---|---|---|
| A | SequenceId | YES | Auto-numbered |
| B | SpeakerLabel | YES | Only `Speaker A` or `Speaker B` |
| C | EnglishText | YES | Short sentences — target 5–15 words. Contractions required. |
| D | HintText | Optional | Not recommended for FluencyDrill — translations slow reading pace |
| E | GrammarTag | NOT USED | Leave blank — FluencyDrill does not target grammar structures |
| F | ContextTag | YES | Topic/setting — matches upload metadata |
| G | FocusWord | NOT USED | Leave blank — no vocabulary focus in fluency drill |
| H | PronunciationNote | NOT USED | Leave blank |

**Template note in file:** "Columns E, G, and H are intentionally left blank in FluencyDrill. This category focuses on fast, natural conversation — not grammar tagging or vocabulary analysis. Keep sentences short (5–15 words). Use contractions naturally."

**Row count guidance:** 30 minimum, 60 maximum. Sample template shows 30 rows to demonstrate the high-volume requirement.

---

#### 6.3.6 RepracticeRound Template

**Primary purpose:** Single-error correction through Coach modelling and Learner practice.

| Column | Label | Required | Guidance in Template |
|---|---|---|---|
| A | SequenceId | YES | Auto-numbered |
| B | SpeakerLabel | YES | Only `Coach` or `Learner` |
| C | EnglishText | YES | Coach: model the correct form + brief explanation. Learner: produce the corrected form. |
| D | HintText | **MANDATORY** | Native language translation — essential for error comprehension |
| E | GrammarTag | **MANDATORY** | **Must be identical on ALL rows** — the exact error being corrected (e.g., `Present Perfect vs Past Simple`) |
| F | ContextTag | YES | Must match the original error's session context |
| G | FocusWord | Optional | The specific word that was misused in the original session |
| H | PronunciationNote | Optional | Only if pronunciation was part of the original error |

**Template note in file:** "This template addresses exactly ONE grammar error pattern. Column E must contain the same value on every single row. Column D (HintText) is mandatory — translation supports error understanding. The Learner must produce the corrected form independently by the final turns."

**Row count guidance:** 14 minimum, 28 maximum. Concise by design — RepracticeRound is focused correction, not extended practice.

---

### 6.4 Template Distribution Recommendation

| Recommendation | Detail |
|---|---|
| Separate sample download per category | Admin script upload UI should offer a download button per category, not a single generic template |
| Pre-populated guidance rows | The first 2 rows of each sample file (after the header) can be pre-filled with example data from the relevant ExcelTemplateStandard to guide content authors |
| Color coding in templates | Required columns: yellow header. Mandatory for this category: orange header. Optional: default/white header. |
| Column-level notes in sample templates | Row 2 of the sample file can optionally contain guidance text as a second header (not data) before the first real data row — but this must be stripped before upload or the content author must be told to delete it. Alternatively, use a second sheet for guidance notes. |

---

## 7. Required / Optional / Remove / Simplify Matrix

### 7.1 Session Workflow Elements

| Element | GrammarDrill | Roleplay | MockInterview | VocabularySprint | FluencyDrill | RepracticeRound |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| Full voice analysis (both speakers) | REQUIRED | REQUIRED | Candidate only | Learner only | REQUIRED | Learner only |
| Fluency score display | REQUIRED | REQUIRED | Candidate only | Learner only | REQUIRED | Learner only |
| Grammar error report | REQUIRED | Optional | REQUIRED | Optional | Simplified | REQUIRED |
| Re-read button | REQUIRED | REQUIRED | Candidate only | Learner only | REQUIRED | Learner only |
| Listener feedback — `Good` tag | Optional | Optional | Candidate turns only | Optional | Optional | REMOVE |
| Listener feedback — `Hesitated` tag | REMOVE | REMOVE | REMOVE | REMOVE | REMOVE | REMOVE |
| Listener feedback — `Mistake` tag | REMOVE | REMOVE | REMOVE | REMOVE | REMOVE | REMOVE |
| Listener feedback — `Unclear Pronunciation` tag | REMOVE | REMOVE | REMOVE | REMOVE | REMOVE | REMOVE |
| Session summary — both members scored | REQUIRED | REQUIRED | Candidate only | Learner only | REQUIRED | Learner only |
| Mistake extraction → RepracticeRound | REQUIRED | Optional | REQUIRED | Optional | Optional | Not applicable |

### 7.2 Excel Template Elements

| Element | GrammarDrill | Roleplay | MockInterview | VocabularySprint | FluencyDrill | RepracticeRound |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| A — SequenceId | REQUIRED | REQUIRED | REQUIRED | REQUIRED | REQUIRED | REQUIRED |
| B — SpeakerLabel | REQUIRED | REQUIRED | REQUIRED | REQUIRED | REQUIRED | REQUIRED |
| C — EnglishText | REQUIRED | REQUIRED | REQUIRED | REQUIRED | REQUIRED | REQUIRED |
| D — HintText | Optional | Optional | Optional | REQUIRED | NOT USED | REQUIRED |
| E — GrammarTag | REQUIRED (same all rows) | Optional | REQUIRED (varies) | Optional | NOT USED | REQUIRED (same all rows) |
| F — ContextTag | REQUIRED | REQUIRED | REQUIRED | REQUIRED | REQUIRED | REQUIRED |
| G — FocusWord | Optional | Optional | REQUIRED | REQUIRED | NOT USED | Optional |
| H — PronunciationNote | Optional | Optional | Optional | REQUIRED (Tutor) | NOT USED | Optional |

### 7.3 Admin Upload Wizard Elements

| Element | Status | Reason |
|---|---|---|
| Step 1: File selection + validation | REQUIRED | Core upload flow |
| Step 2: Metadata entry | REQUIRED | Category, complexity, target age — all necessary |
| Step 3: Preview + upload confirmation | REQUIRED | Data quality gate |
| Category-specific sample template download | REQUIRED | Each category needs its own download |
| Generic single-template download | SIMPLIFY → remove generic, add per-category | Category-specific is strictly better |
| Validation preview (first 5 rows) | REQUIRED | Content quality check |
| Row-level error display | REQUIRED | Authors need to know exactly which rows failed |

---

## 8. Priority Recommendations Summary

### 8.1 High Priority — Product Stability and User Clarity

These recommendations address current confusion, mismatch, or misleading information:

| Priority | Recommendation | Category | Impact |
|---|---|---|---|
| 1 | Remove `Hesitated`, `Mistake`, `Unclear Pronunciation` listener feedback tags. Keep only `Good` (or simplify to `Good` / `Needs Work`). | All | Removes duplication. Reduces user confusion. Automatic analysis already covers this. |
| 2 | Suppress all listener feedback on Interviewer, Tutor, and Coach turns. | MockInterview, VocabularySprint, RepracticeRound | These are facilitator turns, not performance turns. Listener feedback on them is meaningless. |
| 3 | Show fluency scoring and session summary only for the performing role. Interviewer/Tutor/Coach should not receive performance scores as session outcomes. | MockInterview, VocabularySprint, RepracticeRound | Prevents misleading data. Users understand the session outcome correctly. |
| 4 | Provide category-specific downloadable sample Excel templates in the Admin upload wizard. | All | Reduces content authoring errors. Reduces incorrect/placeholder data in optional columns. |

### 8.2 Medium Priority — User Experience Improvement

These recommendations improve the experience without breaking existing functionality:

| Priority | Recommendation | Category | Impact |
|---|---|---|---|
| 5 | MockInterview: Re-read button should be shown only on Candidate turns. | MockInterview | Reduces UI noise. Interviewer re-reading a question is not the practice goal. |
| 6 | FluencyDrill session summary: de-emphasise grammar error detail. Show speed, hesitation, and confidence as primary metrics. | FluencyDrill | Aligns the summary with the category's purpose. |
| 7 | VocabularySprint session summary: show "Words practiced today" as primary outcome. | VocabularySprint | Vocabulary acquisition is the measurable outcome, not just a global fluency score. |
| 8 | RepracticeRound: explore solo mode (Coach turns as read-aloud text display, not requiring a second live user). | RepracticeRound | Removes the barrier of needing a partner to correct your own grammar mistakes. |

### 8.3 Low Priority — Content and Tooling Enhancements

These improvements add value but are not urgent:

| Priority | Recommendation | Category | Impact |
|---|---|---|---|
| 9 | MockInterview: Distinguish GrammarTag usage guidance — "STAR Method" and "Formal Register" are method/register tags, not grammar pattern tags. Clarify in template documentation. | MockInterview | Helps content authors use the column correctly. |
| 10 | FluencyDrill: Add guidance in template that columns E, G, H should be left blank. Include a visual note in the sample file. | FluencyDrill | Reduces clutter in uploaded scripts. |
| 11 | Add a "category-aware validation" hint in the upload wizard: after the user selects a category, show the minimum/maximum row counts and required columns for that specific category. | All | Prevents upload failures from content authors who don't read the documentation. |

### 8.4 What Should Not Be Built

These are additions that would add complexity without clear user benefit:

| Item | Reason to Avoid |
|---|---|
| Additional listener feedback tags (e.g., "Good grammar", "Good vocabulary") | The voice analysis already provides this at a higher quality level. More tags = more complexity, not more value. |
| Manual scoring override by listeners | The automatic analysis is objective. Manual overrides introduce subjectivity and potential conflict in sessions. |
| Complex branching script structures (e.g., if Candidate answers well, go to Q5; if not, go to Q4) | GoWithFlow is a practice platform, not an adaptive testing engine. Branching scripts increase content authoring complexity significantly and are not required for language fluency improvement. |
| Per-utterance listener comment/text input | Free-text comments during a live session interrupt the flow and are not practical on mobile. |
| AI-generated real-time corrections during the session | This would compete with the voice feedback component and create cognitive overload. Post-session analysis is the right moment for detailed feedback. |

---

## Appendix — Category Decision Reference

### A.1 Role Type Classification

| Category | Speaker 1 | Role Type | Speaker 2 | Role Type |
|---|---|---|---|---|
| GrammarDrill | Speaker A | Performance | Speaker B | Performance |
| Roleplay | Character 1 | Performance | Character 2 | Performance |
| MockInterview | Interviewer | **Facilitator** | Candidate | **Performance** |
| VocabularySprint | Tutor | **Facilitator** | Learner | **Performance** |
| FluencyDrill | Speaker A | Performance | Speaker B | Performance |
| RepracticeRound | Coach | **Facilitator** | Learner | **Performance** |

**Definition:**
- **Performance role:** User is practicing their English. Voice analysis, scoring, and improvement tracking apply.
- **Facilitator role:** User is guiding, questioning, or modelling for the other participant. Voice analysis is optional for personal reference only. Scores do not represent session outcomes.

### A.2 Session Outcome Metrics by Category

| Category | Primary Outcome Metric | Secondary |
|---|---|---|
| GrammarDrill | Grammar accuracy in target structure | Fluency score |
| Roleplay | Fluency score in scenario context | Vocabulary used |
| MockInterview | Candidate fluency + grammar + professional vocabulary | STAR method usage |
| VocabularySprint | Words practiced + FocusWord pronunciation accuracy | Learner fluency |
| FluencyDrill | Speaking speed (WPM) + Hesitation count | Confidence score |
| RepracticeRound | % correct production of target pattern | Learner fluency |

---

---

# Part II — New Feature Roadmap
### Senior Product Manager Review | Date: 2026-06-01
### Focus: Significant Value Enhancements — User Experience, Engagement, Learning Outcomes, Reporting, and Product Differentiation

---

## 9. Platform Gap Analysis

### 9.1 What the Platform Does Well Today

Before identifying gaps, it is important to acknowledge what is already strong:

- The automatic voice analysis pipeline is genuinely comprehensive. FluencyScore, ConfidenceScore, GrammarErrors, HesitationWords, PronunciationIssues, SpeakingSpeedWpm, PauseCount — this is above-average for an English practice platform.
- The Repractice pipeline (mistake detection → RepracticeRound generation) is a thoughtful closed-loop learning mechanism. Most platforms detect mistakes but do nothing with them.
- The streak and badge system provides basic engagement hooks.
- Six distinct content categories signal product maturity and range.

### 9.2 The Core Gap: Practice Without Progress Visibility

The single most significant gap in the current platform is this:

> **Users can practice extensively but cannot clearly see whether they are improving, what they should do next, or what their learning goal is.**

A user can complete 20 sessions and still not know:
- Whether their grammar is improving in specific areas.
- Which vocabulary words they have retained vs. forgotten.
- Whether their interview readiness has increased.
- What the next recommended session is for their level.

The voice analysis produces rich data per session. That data is largely consumed once (during the session) and then sits in the database without being surfaced as a meaningful learning narrative.

This is the central opportunity for the product's next phase.

### 9.3 Secondary Gaps

| Gap | Area | Description |
|---|---|---|
| No guided progression | All categories | Users select sessions ad hoc. There is no recommended path based on their level, history, or goals. |
| No vocabulary retention tracking | VocabularySprint | Words are introduced per session but never tested for retention across sessions. The category does not close its own loop. |
| No post-session review | All categories | Voice analysis results are shown during the session. After the session ends, users cannot revisit their transcript, errors, or word-level results. |
| No partner discovery | All categories | Sessions require users to find their own partners. RepracticeRound especially suffers — a user who needs to fix a grammar mistake must also find a partner to play Coach. |
| No admin content effectiveness data | Admin | Admins can upload scripts but cannot see which scripts produce the best learning outcomes, which are skipped most often, or which categories are under-used. |
| No user-facing learning report | Users | No weekly or monthly summary of what the user has learned, improved, and still needs to work on. |
| Script creation is Excel-only | Admin | Script creation requires an Excel file prepared outside the platform. No in-platform creation tool exists. |
| Session preparation gap | Users | Users cannot preview the script before joining a session, so they arrive cold. |
| Interview readiness gap | MockInterview | No composite measure of interview performance improvement across multiple sessions. |
| No learning goals | Users | Users cannot set a target (e.g., "improve fluency from 65 to 80 in 4 weeks") and track progress against it. |

---

## 10. New Feature Recommendations

### Priority Legend
- **MUST HAVE** — Fills a critical gap that limits the core value proposition. Users cannot clearly measure their improvement without this.
- **HIGH VALUE** — Meaningful engagement or outcome improvement. Strong ROI relative to development effort.
- **FUTURE ROADMAP** — Differentiating capabilities for product maturity and B2B expansion.

---

### 10.1 MUST HAVE Features

---

#### Feature 1 — Post-Session Review (Session Replay)

**Problem it solves:** Voice analysis results are shown momentarily during the session. After the session ends, the data is gone from the user's view. Users cannot reflect on, learn from, or share their session performance.

**What it is:**
A dedicated post-session review screen accessible from the user's session history that shows:
- Full session transcript (EnglishText per turn, what was spoken per turn side by side)
- Per-turn score: fluency, confidence, speaking speed
- Grammar errors highlighted inline in the spoken text (wrong word shown with expected word)
- Hesitation words flagged in the transcript
- Pronunciation issues flagged per word with the expected phonetic vs. what was detected
- Overall session score with score band (Excellent / Great / Good / Keep Going / Try Again)
- Comparison to the user's previous session on the same script (if available)

**Why it is Must Have:**
The voice analysis pipeline already generates all of this data — it is stored in `tblVoiceAnalysis`. The gap is entirely a display gap, not a data gap. Without post-session review, the richness of the analysis is lost after the session ends. Users who want to improve need to be able to revisit their errors.

**Category applicability:** All categories. Most valuable in MockInterview (Candidate reviews their answers), GrammarDrill (user reviews their grammar accuracy), and RepracticeRound (Learner sees whether they produced the correct form).

**User value:** High. This is the difference between a practice platform and a learning platform.

---

#### Feature 2 — Guided Learning Path

**Problem it solves:** Users have no structured progression. A beginner and an advanced user see the same script library with no guidance on what to do next.

**What it is:**
A recommended learning path for each user based on:
- Their chosen category preference (e.g., "I want to improve my interview English")
- Their current complexity level (derived from their average session scores)
- Their grammar weakness profile (from tblMistake GrammarTag breakdown)
- Their session history (which scripts have they completed, which categories have they not tried)

The path surfaces 2–3 recommended next sessions on the user's home dashboard:
- "Your next GrammarDrill: [Script Title] — Intermediate — Present Perfect (you missed 3 turns in your last session)"
- "Ready for a harder Roleplay? Try [Script Title] — Level 3"
- "You have 2 unresolved mistakes — start a RepracticeRound"

**Why it is Must Have:**
Without direction, users drop off. Recommended next steps are the single most effective engagement and retention mechanism for any practice app. The data to power recommendations already exists — streak data, session history, mistake profile, grammar progress.

**Category applicability:** All categories. MockInterview benefits most (a structured progression from HR Interview Beginner → HR Interview Advanced → Tech Interview is a natural career path).

**User value:** Very high. Converts ad hoc practice into purposeful learning.

---

#### Feature 3 — Weekly Learning Report (User-Facing)

**Problem it solves:** Users practice but have no narrative summary of their progress. There is no moment where the platform says "here is what you achieved this week and what to focus on next."

**What it is:**
A weekly digest — either in-app (a dismissible summary card on the dashboard every Monday) or as an email notification — that shows:

```
This Week's Practice Summary
─────────────────────────────────────────
Sessions completed:        4
Total practice time:       38 minutes
Grammar errors detected:   12
Grammar errors resolved:   8 (via RepracticeRound)

Your Top Improvement:      Fluency score +7 pts vs last week
Area still to work on:     Present Perfect (3 errors this week)

Recommended this week:
  → GrammarDrill: Present Perfect Advanced
  → MockInterview: Behavioural Interview Intermediate
─────────────────────────────────────────
```

**Why it is Must Have:**
Users who do not see tangible evidence of progress stop practising. A weekly report closes the feedback loop between effort and outcome. It also reactivates users who have been inactive — "you haven't practised in 5 days, here's where you left off."

**Category applicability:** All categories. Most impactful for users working toward a specific goal (interview preparation, grammar improvement).

**User value:** High. Directly drives retention and re-engagement.

---

#### Feature 4 — Vocabulary Retention Tracker (VocabularySprint)

**Problem it solves:** VocabularySprint introduces new words per session but never tests whether the user retained them. Without retention tracking, the category does not fulfill its stated purpose of vocabulary building.

**What it is:**
After a VocabularySprint session, the FocusWords introduced in that session are saved to a personal vocabulary list for that user. The platform then:

1. **Tracks which words were correctly spoken** (FocusWord matched in Learner voice analysis) vs. which were stumbled on.
2. **Resurfaces words for review** in future sessions — if a word was introduced 7 days ago and has not been seen since, it is flagged as "due for review."
3. **Shows a vocabulary bank** on the user's profile: all words introduced across their VocabularySprint sessions, with correct/incorrect production history.
4. **Session outcome summary** shows: "Today you learned 8 new words. You have 24 words in your vocabulary bank. 6 are due for review."

**Why it is Must Have for VocabularySprint:**
Without this, the category produces no measurable vocabulary growth. A user can complete 10 VocabularySprint sessions and not know which words they actually retained. The FocusWord column already exists in `tblUtterance`. The voice analysis already checks word-level match. The data infrastructure is present — what is missing is the retention layer.

**Category applicability:** VocabularySprint only.

**User value:** Very high. This transforms VocabularySprint from a session-based activity into a vocabulary growth system with tangible, visible outcomes.

---

### 10.2 HIGH VALUE Features

---

#### Feature 5 — Interview Performance Dashboard (MockInterview)

**Problem it solves:** MockInterview users practice for a real-world outcome — a job interview. They need more than a fluency score. They need to know: "Am I actually getting better at interviews?"

**What it is:**
A dedicated MockInterview performance tracker — separate from the general dashboard — that shows:

- **Interview Readiness Score:** A composite score (0–100) calculated across the last 5 MockInterview sessions. Components: formal register maintenance (%), STAR method usage (%), professional vocabulary density, average answer length (word count), fluency score.
- **Progress Timeline:** A line chart showing Interview Readiness Score across sessions over time.
- **Strength/Weakness Breakdown:**
  - Grammar: most frequent errors in interview answers
  - Vocabulary: professional terms used successfully vs. stumbled on
  - Structure: percentage of Candidate answers that were multi-sentence and structured
- **Session Comparison:** Side-by-side comparison of any two MockInterview sessions — "how did your answers improve?"
- **Recommended focus:** "Your formal register score dropped in the last session. Practice this: [link to HR Interview script with Formal Register tag]"

**Why it is High Value:**
Interview preparation is a high-motivation use case. Users who are actively preparing for jobs are the most engaged and most likely to recommend the platform. An Interview Readiness Score is a concrete, shareable metric that differentiates GoWithFlow from generic language practice apps.

**Category applicability:** MockInterview only.

**User value:** Very high for the interview-preparation segment. Strong word-of-mouth potential ("my interview readiness went from 58 to 81 in 3 weeks").

---

#### Feature 6 — Pronunciation Improvement Timeline

**Problem it solves:** The platform detects pronunciation issues per turn, but there is no long-term tracking of whether the user's pronunciation on specific problem words is actually improving.

**What it is:**
A pronunciation tracking view — accessible from the user's profile analytics — that shows:

- **Problem Words List:** All words that have appeared in PronunciationIssues across the user's sessions, ordered by frequency.
- **Per-Word History:** For each problem word, a timeline of whether the user's pronunciation of that word has improved, stayed the same, or worsened across sessions.
- **IPA Reference:** For each word on the problem list, show the expected IPA (from `tblUtterance.PronunciationNote` where available) alongside what was detected.
- **"Work on these words" recommendation:** Top 5 words with the most consistent pronunciation errors — linked to scripts that contain those FocusWords so the user can target practice.

**Why it is High Value:**
Pronunciation improvement is slow and highly specific to individual words. Generic fluency scores do not capture this. A user who is repeatedly mispronouncing "determined" or "collaborate" will not know unless the platform surfaces it explicitly. The data already exists in `tblVoiceAnalysis.PronunciationIssues` — this is a visualisation and tracking feature, not a new data collection feature.

**Category applicability:** All categories. Most valuable in MockInterview (professional vocabulary pronunciation), VocabularySprint (FocusWord pronunciation), and GrammarDrill (focus word IPA tracking).

**User value:** High. Pronunciation improvement is one of the most requested outcomes from English learners. Making it visible and trackable is a meaningful differentiation.

---

#### Feature 7 — AI Script Generator (Admin UI)

**Problem it solves:** The current script creation process requires:
1. Understanding the Excel template format
2. Writing the full script content in Excel
3. Uploading the file
4. Correcting validation errors
5. Re-uploading

This is a high-friction process that limits how quickly new content can be created. It also requires the admin to be proficient in English content writing for all six categories.

**What it is:**
An in-platform script generation interface for admins:

```
Generate a New Script
─────────────────────────────────────────
Category:           [MockInterview ▼]
Interview Type:     [HR Interview ▼]
Complexity Level:   [Intermediate ▼]
Grammar Focus:      [STAR Method, Past Simple ▼]
Number of Q&A pairs: [8]
Hint Language:      [Telugu ▼]

[Generate Script Preview]
─────────────────────────────────────────
```

The platform generates a preview of the script in the standard format. The admin can:
- Review the generated content in a structured table view
- Edit individual turns inline
- Approve and upload directly (no Excel file required)
- Or export to Excel for offline review

**Why it is High Value:**
The ExcelTemplateStandard.md already contains a full AI generation protocol (Section 10) with a complete prompt template and self-validation checklist. This feature exposes that protocol through a UI, reducing script creation from a multi-hour Excel task to a 2-minute review-and-approve workflow. Content volume is directly correlated with platform engagement — more scripts = more sessions = more retention.

**Category applicability:** All categories. Most immediate value for RepracticeRound (where scripts need to be created per specific grammar error pattern — a uniquely targeted content need).

**Admin value:** Very high. Removes the primary bottleneck to content growth.

---

#### Feature 8 — Session Preparation Mode (Script Preview)

**Problem it solves:** Users currently join a session without seeing the script. They encounter the utterances for the first time during the live session, under the pressure of real-time voice recording. This increases anxiety and reduces performance quality — especially for MockInterview and RepracticeRound.

**What it is:**
A "Prepare" option on any script in the library that lets a user:
- Read through the full script before joining a session (read-only, no recording)
- Listen to a text-to-speech playback of each utterance (optional, where TTS is available)
- See the HintText translations alongside the English text
- Understand the scenario, grammar focus, and vocabulary before the live session

For MockInterview specifically: Candidate users can preview the interview questions to mentally prepare structured answers — exactly as they would prepare for a real interview.

**Why it is High Value:**
Preparation reduces first-attempt errors and improves session quality scores. Users who prepare are more likely to complete sessions successfully and return for more. This is especially important for beginners (Complexity Level 1–2) who feel overwhelmed by encountering new content live.

**Category applicability:** All categories. Critical for MockInterview and RepracticeRound. Valuable for GrammarDrill when a new grammar structure is introduced.

**User value:** High. Lowers the psychological barrier to starting a session.

---

#### Feature 9 — Partner Matching

**Problem it solves:** Sessions require two live users. Users with no existing practice partner cannot start a session. RepracticeRound is the worst case — a user who needs to fix a grammar mistake must find a friend willing to play the Coach role.

**What it is:**
An opt-in matching system where users can:
- Mark themselves as "Available to practice" for a selected category and complexity level
- Browse available partners currently online or scheduled for a session in the next 30 minutes
- Send a session invite to a matched partner
- Be automatically matched to the next available partner at their complexity level (instant match for GrammarDrill and FluencyDrill which are most casual)

For RepracticeRound specifically: the platform should surface "practice with a peer" and "practice solo" as two distinct modes — solo mode being the Coach-as-text-display approach described in Part I.

**Why it is High Value:**
Partner availability is one of the most common reasons users do not complete sessions. Even if a user is motivated to practice, not having a partner at that moment blocks them entirely. Reducing this friction directly increases session completion rates.

**Category applicability:** All categories. Most important for RepracticeRound (asymmetric pair) and MockInterview (requires commitment from both participants to maintain formal register).

**User value:** High. Converts motivated-but-blocked users into active sessions.

---

#### Feature 10 — Learning Goals and Progress Tracking

**Problem it solves:** Users have no personal goal anchoring their practice. Without a goal, practice is random and motivation drifts.

**What it is:**
A goal-setting feature on the user profile:

```
Set Your Practice Goal
─────────────────────────────────────────
I want to:          [Prepare for a job interview ▼]
                    [Improve my grammar ▼]
                    [Build vocabulary ▼]
                    [Speak more fluently ▼]

Timeline:           [4 weeks ▼]

My current level:   Intermediate (auto-detected from session history)
My target:          Advanced

Recommended plan:   3 sessions/week
                    2 × GrammarDrill + 1 × MockInterview per week
─────────────────────────────────────────
```

Progress toward the goal is tracked on the dashboard:
- Sessions completed toward goal: 4 of 12
- Fluency score movement: 67 → 74 (target: 85)
- Grammar errors per session: declining (positive trend)
- Estimated completion: 2 weeks remaining if current pace is maintained

**Why it is High Value:**
Goals are the single strongest predictor of consistent practice behaviour in language learning. Users with explicit goals practice 2–3x more frequently than users without goals. The goal-setting feature also gives admins a lens into user intent — which goals are most common, which are being achieved, and which are abandoned.

**Category applicability:** All categories. The goal type determines which categories are recommended.

**User value:** Very high. Anchors all other features (learning path, weekly report, pronunciation tracker) to a meaningful outcome the user actually cares about.

---

#### Feature 11 — Admin Script Quality Analytics

**Problem it solves:** Admins upload scripts but receive no feedback on whether those scripts are effective. They cannot tell which scripts produce good learning outcomes, which are frequently abandoned, or which categories are under-served.

**What it is:**
A script performance analytics view in the admin dashboard:

| Script Title | Category | Total Sessions | Avg Completion Rate | Avg Fluency Score | Avg Mistake Count | Avg Session Duration |
|---|---|---|---|---|---|---|
| Present Perfect — Office | GrammarDrill | 47 | 92% | 74.3 | 3.2 | 8 mins |
| Airport Check-In | Roleplay | 31 | 88% | 68.1 | 1.8 | 11 mins |
| Software Engineer HR | MockInterview | 19 | 71% | 61.4 | 6.1 | 22 mins |

Additional signals:
- **Re-read rate per script:** High re-read rates signal script content is too difficult.
- **Skip rate per turn:** Which specific utterances cause users to skip? These are candidate simplification targets for the content team.
- **Repractice conversion rate:** What percentage of sessions on this script triggered a RepracticeRound? (High = script is generating productive error correction opportunities)
- **Script freshness:** When was this script last used? Scripts not used in 60+ days are flagged for review or deactivation.

**Why it is High Value:**
Content quality directly affects learning outcomes. Without effectiveness data, admins publish scripts blindly. With this data, the content team can iteratively improve scripts, prioritise new content for underserved categories, and retire scripts that underperform.

**Admin value:** High. Turns script management from an upload-and-forget process into a data-driven content strategy.

---

### 10.3 FUTURE ROADMAP Features

---

#### Feature 12 — Spaced Repetition for Grammar Mistakes

**What it is:**
An extension of the existing RepracticeRound system using spaced repetition scheduling. Instead of the user manually triggering a RepracticeRound, the platform automatically schedules a review of each resolved mistake at increasing intervals: 1 day → 3 days → 7 days → 14 days → 30 days. If the user makes the same mistake again in a review session, the interval resets. If they succeed, the interval extends.

**Why it matters:**
The current RepracticeRound closes the immediate correction loop. Spaced repetition closes the long-term retention loop. A grammar error that was "resolved" once but never reinforced will re-appear. Spaced repetition is the most evidence-backed mechanism for converting short-term correction into long-term retention.

**Dependencies:** Requires the existing Repractice module to track resolution confidence (not just binary resolved/unresolved). A confidence score per mistake pattern is needed.

---

#### Feature 13 — Cohort Management (B2B / Training Use)

**What it is:**
A management layer for enterprise and training institution use:
- Admins can create cohorts (e.g., "Batch June 2026 — HR English Training")
- Users are assigned to cohorts
- Cohort-level analytics: average fluency progress, most common grammar mistakes across cohort, session completion rates, most improved users
- Batch reports: exportable PDF/Excel reports for HR managers and training coordinators

**Why it matters:**
English language training is a significant B2B market. Companies that train employees for client-facing roles (call centres, IT services, hospitality) need batch-level reporting, not individual user reporting. This feature is the primary enabler of a B2B revenue stream.

**Dependencies:** User management must support group assignment. Reporting must be filterable by cohort.

---

#### Feature 14 — Speaking Challenge Mode (Timed Sessions)

**What it is:**
A competitive practice mode — optional, not replacing the standard session:
- Weekly challenge: a single script is designated as the week's challenge script
- All users who attempt the challenge are scored on the same script in the same week
- A leaderboard shows the top fluency scores for that week's challenge script
- Users can attempt the challenge multiple times (best score counts)
- A "challenge badge" is awarded to users in the top 20% of scorers

**Why it matters:**
Gamification through challenges and leaderboards is a proven engagement driver for practice apps (Duolingo, Babbel, language exchange platforms all use variants of this). The challenge format also ensures all users experience the same high-quality content each week, which is beneficial for content strategy.

**Dependencies:** Requires leaderboard infrastructure and weekly script designation by admins.

---

#### Feature 15 — Completion Milestones and Certificates

**What it is:**
Structured completion milestones per category:
- GrammarDrill: "Grammar Foundation" certificate — complete 10 GrammarDrill sessions across 5 distinct grammar structures
- MockInterview: "Interview Ready" certificate — achieve an Interview Readiness Score ≥ 80 across 5 MockInterview sessions
- VocabularySprint: "Vocabulary Builder" certificate — accumulate 100 correctly produced FocusWords across sessions
- FluencyDrill: "Fluency Milestone" — maintain a speaking speed of 80–120 WPM across 8 FluencyDrill sessions
- RepracticeRound: "Grammar Corrector" — resolve 10 distinct grammar mistake types

Certificates are displayed on the user's profile and can be downloaded as a shareable image.

**Why it matters:**
Completion milestones give users a sense of achievement and a reason to continue beyond a single session. Shareable certificates add social proof and organic marketing. For B2B use cases, certificates are a tangible deliverable for HR training programs.

---

#### Feature 16 — Live Session Audio Archive (Optional, Privacy-Gated)

**What it is:**
With explicit user consent during session creation, recorded audio clips from the user's own turns (not other participants) can be archived for personal review. The user can:
- Play back their own spoken turns from a completed session
- Compare how they sound now vs. a session from 4 weeks ago on the same script
- Identify pronunciation patterns they cannot detect from transcribed text alone

This feature is strictly opt-in and user-controlled. No audio from other participants is ever archived without their separate consent.

**Why it matters:**
Hearing yourself speak is one of the most effective pronunciation improvement tools. Language learners who listen to playbacks of their own speech improve pronunciation measurably faster than those who only see transcribed text. This feature adds a dimension that no transcription-based analysis can replace.

**Dependencies:** Audio storage (significant infrastructure consideration), privacy and data governance framework, explicit consent flow at session start.

---

## 11. Voice Analysis — Capability Enhancement Recommendations

The current voice analysis engine is strong. The following enhancements would increase its value without replacing what exists.

### 11.1 Intonation and Stress Pattern Detection

**Current gap:** The engine detects hesitation, speed, and word accuracy but does not detect intonation patterns. Questions spoken with flat intonation, sentences with misplaced stress, and monotone delivery are not currently flagged.

**Recommended addition:** Detect whether rising intonation was used on question turns (e.g., "Have you finished the report?" should have rising pitch on "report?") and flag monotone delivery where pitch variance is very low across a multi-sentence answer.

**Most valuable for:** MockInterview (confident, varied intonation is a core professional communication skill), FluencyDrill (natural speaking rhythm includes natural stress patterns).

### 11.2 Filler Phrase Detection (Beyond Single Words)

**Current gap:** The engine detects single-word hesitations (um, uh, er, hmm). It does not detect filler phrases: "you know", "I mean", "basically", "kind of", "sort of", "like" (used as filler), "to be honest", "actually" (overused).

**Recommended addition:** Extend the hesitation detection from single words to multi-word filler phrases. Filler phrases are as damaging to professional communication as single-word hesitations — particularly in MockInterview contexts.

**Most valuable for:** MockInterview, Roleplay, FluencyDrill.

### 11.3 Answer Completeness Score (MockInterview)

**Current gap:** The engine scores whether words were spoken correctly but cannot judge whether the Candidate's answer was complete and structured.

**Recommended addition:** For Candidate turns in MockInterview, calculate an answer completeness signal:
- Word count: was the answer above the minimum meaningful response length (≥ 30 words for interview answers)?
- STAR structure signal: did the answer contain temporal markers (Situation: "when", "at my previous...", Task: "I was responsible for", Action: "I decided to", "I did", Result: "which resulted in", "we achieved")?
- A simple completeness score (0–100) based on these signals.

This does not replace the full STAR method evaluation by a human coach but provides an immediate automated signal.

### 11.4 Cross-Session Trend Analysis per Grammar Error Type

**Current gap:** Grammar errors are recorded per session. There is no automatic trend calculation showing whether a user's specific grammar error rate is increasing or decreasing over time.

**Recommended addition:** For each grammar error type in a user's history, calculate a 4-week rolling average error rate and compare it to the previous 4-week period:
- Present Perfect errors: 4.2 per session (↓ 1.1 from last month) — Improving
- Articles errors: 2.8 per session (↑ 0.6 from last month) — Regressing
- Subject-Verb Agreement: 0.3 per session (stable) — Stable

This turns static mistake records into an improvement narrative.

---

## 12. Script Upload and Excel Template — Process Improvement Recommendations

### 12.1 In-Platform Script Editor (No Excel Required)

The Excel upload process is the highest-friction part of admin content management. An alternative in-platform editor would allow admins to:
- Select category → enter metadata → add rows one at a time in a form interface
- See a live preview of the script as rows are added
- Validate each row inline (immediate error feedback, not a post-upload error report)
- Save as a draft (scripts in "draft" status are not available for sessions until published)
- Export to Excel at any point (for offline backup or sharing)

The Excel upload path remains available for bulk imports and AI-generated content. The in-platform editor serves the authoring use case.

### 12.2 Script Versioning and Rollback

**Current state:** Script versioning exists (`tblScriptVersion`) but rollback to a previous version is not surfaced to admins. If a script update introduces errors, the only option is to re-upload the corrected version.

**Recommended addition:** Show the version history for each script in the admin UI and allow rollback to any previous version with a single action.

### 12.3 Script Duplication

Allow admins to duplicate an existing script as a starting point for a new one. Particularly useful for:
- Creating a Complexity Level 4 version of an existing Level 3 script
- Creating a RepracticeRound script based on a GrammarDrill script's grammar focus
- Adapting a script for a different contextTag (e.g., converting an Office-context GrammarDrill to a Hospital-context version)

### 12.4 Bulk Script Import with Error Resume

**Current state:** If a bulk upload fails on row 15, the entire upload is rejected and the admin must correct the file and re-upload from scratch.

**Recommended addition:** A partial import mode that:
- Imports all valid rows
- Reports the failed rows with specific error reasons
- Allows the admin to fix and re-submit only the failed rows
- Marks the script as "partial" until all rows are successfully imported

---

## 13. Feature Prioritisation Summary

### 13.1 Must Have (Build First)

| # | Feature | Primary Benefit | Affected Users |
|---|---|---|---|
| 1 | Post-Session Review (Session Replay) | Converts voice analysis data into lasting learning value | All users |
| 2 | Guided Learning Path | Reduces drop-off by giving users clear next steps | All users |
| 3 | Weekly Learning Report | Drives re-engagement and shows tangible progress | All users |
| 4 | Vocabulary Retention Tracker | Closes the VocabularySprint learning loop | VocabularySprint users |

### 13.2 High Value (Build Next)

| # | Feature | Primary Benefit | Affected Users |
|---|---|---|---|
| 5 | Interview Performance Dashboard | Differentiates MockInterview as a career tool | MockInterview users |
| 6 | Pronunciation Improvement Timeline | Makes pronunciation progress visible and actionable | All users |
| 7 | AI Script Generator (Admin UI) | Dramatically reduces content creation friction | Admins |
| 8 | Session Preparation Mode | Reduces session anxiety and improves first-attempt quality | All users, especially beginners |
| 9 | Partner Matching | Removes the partner availability blocker | All users |
| 10 | Learning Goals and Progress Tracking | Anchors motivation to a concrete personal outcome | All users |
| 11 | Admin Script Quality Analytics | Enables data-driven content strategy | Admins |

### 13.3 Future Roadmap (Plan After Core is Stable)

| # | Feature | Strategic Value |
|---|---|---|
| 12 | Spaced Repetition for Grammar Mistakes | Long-term retention of grammar corrections |
| 13 | Cohort Management | B2B revenue stream enabler |
| 14 | Speaking Challenge Mode | Engagement and gamification layer |
| 15 | Completion Milestones and Certificates | Achievement system + social proof + B2B deliverable |
| 16 | Live Session Audio Archive | Pronunciation improvement acceleration (opt-in) |

### 13.4 Voice Analysis Enhancements (Phased)

| Enhancement | Priority | Category Impact |
|---|---|---|
| Filler phrase detection (beyond single words) | High | MockInterview, Roleplay, FluencyDrill |
| Cross-session grammar error trend analysis | High | All categories |
| Answer completeness score (MockInterview Candidate) | Medium | MockInterview |
| Intonation and stress pattern detection | Future | MockInterview, FluencyDrill |

---

## 14. Product Differentiation Summary

### What Makes GoWithFlow Different With These Features

| Generic Language App | GoWithFlow With This Roadmap |
|---|---|
| Practice sessions with a score | Practice sessions + post-session review + error replay |
| Random content selection | Guided learning path based on your goal and history |
| A weekly streak | A weekly learning report with specific improvement metrics |
| Vocabulary flashcards | Vocabulary sprint sessions + retention tracking + spaced review |
| Generic fluency score | Interview Readiness Score with STAR method and vocabulary analysis |
| Grammar corrections shown once | Grammar error trend analysis across weeks and months |
| Upload scripts via Excel | AI-generate scripts in 2 minutes inside the platform |
| Find your own partner | Partner matching by category and level |
| Practice without a purpose | Goal setting with a structured plan and completion milestones |

The combination of **automatic voice analysis** (already built and strong) + **post-session review** + **guided progression** + **retention tracking** creates a learning loop that most English practice platforms do not have. This is not a feature list — it is a learning system.

---

*Part II end — New Feature Roadmap.*
*This is a product-level feature planning document. No code changes, implementation details, or technical solutions are included.*
*Author: Senior Product Manager Review — 2026-06-01*

---

---

# Part III — Implementation Prompt Library (Phase-Wise)
### How to Use
> Copy the prompt for the current step and paste it into a new Claude Code session.
> Complete one step fully (code + ProjectOverview.md update) before moving to the next.
> Steps within the same phase are ordered by dependency — do not skip.

---

## Phase 0 — Workflow Fixes (Do Before Any New Features)

> These are corrections to the existing workflow — not new features. Complete all 4 steps before starting Phase 1.

---

### Phase 0 — Step 1 of 4 — Scoring Asymmetry Fix

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part I Sections 4.3 (MockInterview),
4.4 (VocabularySprint), and 4.6 (RepracticeRound) for the full context.

Task: Implement role-type awareness in session scoring and session summary.

Rules:
- MockInterview: only the Candidate receives voice analysis scoring and is shown
  in the session summary as a performance participant. The Interviewer turn is a
  facilitator turn — the script line is displayed and read aloud, but no fluency
  score, confidence score, or grammar error report is generated or shown for the
  Interviewer.
- VocabularySprint: only the Learner receives voice analysis scoring. The Tutor
  turn is an instructional turn — no performance score.
- RepracticeRound: only the Learner receives voice analysis scoring. The Coach
  turn is a model/correction turn — no performance score.
- GrammarDrill, Roleplay, FluencyDrill: no change. Both speakers are performance
  participants and receive equal scoring.

Read ModuleIndex.md first. Then read the Live Session Module (Speaker Turn Contract,
Save Voice Analysis flow, Complete Session flow) and the Session Module (session
summary) in ProjectOverview.md.
Update ProjectOverview.md after completing this task.
```

---

### Phase 0 — Step 2 of 4 — Listener Feedback Cleanup

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part I Section 5.5 for the full context.

Task: Simplify the listener feedback system as follows:

Changes required:
1. Remove the Hesitated, Mistake, and Unclear Pronunciation feedback tags entirely
   from both the frontend UI and the valid FeedbackTag values in the backend.
2. Keep only the Good tag. Optionally add a Needs Work tag as a second option —
   maximum two tags total.
3. Suppress all listener feedback UI on facilitator turns:
   - Interviewer turns in MockInterview sessions
   - Tutor turns in VocabularySprint sessions
   - Coach turns in RepracticeRound sessions
   Listeners should see no feedback buttons when a facilitator turn is active.

Read ModuleIndex.md first. Then read the Listener Feedback flow in the Live Session
Module in ProjectOverview.md.
Update ProjectOverview.md after completing this task.
```

---

### Phase 0 — Step 3 of 4 — Re-Read Button Restriction

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part I Sections 4.3, 4.4, and 4.6
for the full context.

Task: Restrict the Re-Read button to performance turns only.

Rules:
- MockInterview: Re-Read button visible on Candidate turns only.
  Hide the Re-Read button entirely on Interviewer turns.
- VocabularySprint: Re-Read button visible on Learner turns only.
  Hide on Tutor turns.
- RepracticeRound: Re-Read button visible on Learner turns only.
  Hide on Coach turns.
- GrammarDrill, Roleplay, FluencyDrill: no change — Re-Read remains
  available for both speakers as it is today.

Read ModuleIndex.md first. Then read the Speaker Turn — Stable Contract section
in ProjectOverview.md.
Update ProjectOverview.md after completing this task.
```

---

### Phase 0 — Step 4 of 4 — Category-Specific Excel Sample Templates

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part I Section 6.3 (all six category
template designs) and ExcelTemplateStandard.md Section 6 (category-specific rules)
for the full context.

Task: Create six separate downloadable sample Excel templates — one per category —
and update the Admin script upload wizard to offer a per-category download.

Requirements per template:
- Same 8-column structure (A–H) — no backend or parser changes.
- Row 1: standard header labels as defined in ExcelTemplateStandard.md Section 5.1.
- Row 2: one pre-filled sample data row using the correct speaker labels and a
  realistic example utterance for that category.
- Column header cells: visually distinguish Required columns (e.g. yellow fill)
  from Mandatory-for-this-category columns (orange fill) from Optional columns
  (no fill) using the per-category rules in PM_CategoryWorkflow_ProductPlan.md
  Section 6.3.
- Sheet 2 named "Guide": one row per column (A–H) explaining whether it is
  Required / Mandatory / Optional / Not Used for this specific category, with
  a one-line description of what to put in it.

Admin upload wizard change:
- Replace the single generic "Download Sample Template" button with six
  category-specific buttons, shown only after the admin selects a category
  in the upload form metadata step.

Read ModuleIndex.md first. Then read the Admin Script Upload Wizard Contract
and the Script Module in ProjectOverview.md.
Update ProjectOverview.md after completing this task.
```

---

## Phase 1 — Must Have Features

> Build in order. Step 1.3 must be done before Step 1.4.

---

### Phase 1 — Step 1 of 4 — Post-Session Review

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Feature 1 (Section 10.1)
for the full product spec.

Task: Build a Post-Session Review screen accessible from each completed session
in the user's session history.

Requirements:
- Each completed session in session history has a "Review" button.
- The review screen shows:
    - Full session transcript: EnglishText (expected) vs TranscribedText (spoken)
      side by side, per turn, in sequence order.
    - Per-turn scores: FluencyScore, ConfidenceScore, SpeakingSpeedWpm.
    - Grammar errors highlighted inline — show ExpectedPhrase and SpokenPhrase
      from tblVoiceAnalysis.GrammarErrors JSON column.
    - Hesitation words flagged in the transcript from HesitationWords CSV column.
    - Pronunciation issues flagged per word from PronunciationIssues JSON column.
    - Overall session score with score band label matching the VoiceFeedbackComponent
      bands already defined (Excellent 90–100 / Great 75–89 / Good 60–74 /
      Keep Going 40–59 / Try Again 0–39).
- For asymmetric categories (MockInterview, VocabularySprint, RepracticeRound):
    - Performance role turns (Candidate, Learner): show full analysis.
    - Facilitator role turns (Interviewer, Tutor, Coach): show transcript text only,
      no score display.

All data is already stored in tblVoiceAnalysis. This is a display-only feature —
no new data collection or new API endpoints required beyond a read endpoint if
one does not already exist.

Read ModuleIndex.md first. Then read the Live Session Module (Save Voice Analysis
flow, tblVoiceAnalysis schema) and the User Module (session detail) in
ProjectOverview.md.
Update ProjectOverview.md after completing this task.
```

---

### Phase 1 — Step 2 of 4 — Vocabulary Retention Tracker

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Feature 4 (Section 10.1)
for the full product spec.

Task: Build a Vocabulary Retention Tracker for VocabularySprint sessions.

Requirements:
1. After every completed VocabularySprint session, save each unique FocusWord
   from the session's utterances (tblUtterance.FocusWord, Learner turns only)
   to a new per-user vocabulary record. Store:
   - UserId, FocusWord, SourceSessionId, DateIntroduced
   - WasProducedCorrectly (bool): true if the FocusWord appears as a matched
     word in the Learner's tblVoiceAnalysis for that turn.

2. Vocabulary Bank view on the user profile:
   - List all FocusWords the user has encountered across VocabularySprint sessions.
   - Per word: first introduced date, times practiced, correct production rate (%).
   - Flag words not seen in any session for 7+ days as "Due for Review".

3. VocabularySprint session completion summary adds one line:
   "Today you practiced X words. Your vocabulary bank has Y words total.
    Z words are due for review."

Read ModuleIndex.md first. Then read the Script Module (tblUtterance schema),
Live Session Module (tblVoiceAnalysis), and User Module in ProjectOverview.md.
Update ProjectOverview.md after completing this task. Document the new vocabulary
tracking table schema in ProjectOverview.md before writing any code.
```

---

### Phase 1 — Step 3 of 4 — Weekly Learning Report

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Feature 3 (Section 10.1)
for the full product spec.

Task: Build a Weekly Learning Report displayed on the user dashboard.

Requirements:
- Shown as a dismissible card at the top of the user dashboard.
- Appears on the first login of each week (Monday or first session after Sunday).
- Content:
    - Sessions completed this week (count)
    - Total practice time this week (sum of ActualDurationSec / 60 across sessions)
    - Grammar errors detected this week (count from tblMistake this week)
    - Grammar errors resolved this week (resolved mistakes via RepracticeRound)
    - Top improvement: metric with the largest positive delta vs the previous week
      (compare FluencyScore avg, error rate, WPM avg week-over-week)
    - Area to work on: GrammarTag with the highest error count this week
    - 2 recommended sessions: one targeting the weakest grammar tag, one
      suggesting the next complexity step in the user's most-used category
- If the user had zero sessions this week: show a re-engagement message with
  their last session date and one recommended next session.

All data is read from existing tables: tblSession, tblVoiceAnalysis, tblMistake,
tblRepracticeSession, tblUserStreak. No new data collection needed.

Read ModuleIndex.md first. Then read the User Module (dashboard, analytics SPs),
Mistake/Repractice Module, and Session Module in ProjectOverview.md.
Update ProjectOverview.md after completing this task.
```

---

### Phase 1 — Step 4 of 4 — Guided Learning Path

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Feature 2 (Section 10.1)
for the full product spec.

Dependency: Complete Phase 1 Step 3 (Weekly Learning Report) before this step.

Task: Build a Guided Learning Path recommendation panel on the user dashboard.

Requirements:
- A panel showing 2–3 recommended next sessions, displayed above the script library.
- Recommendation logic (apply in priority order):
    1. Unresolved mistakes exist → recommend a RepracticeRound targeting the top
       GrammarTag error.
    2. Last session in any category scored below FluencyScore 65 → recommend
       repeating that category at the same complexity level.
    3. Last 3 sessions in a category all scored above FluencyScore 80 →
       recommend the next complexity level in that category.
    4. User has not started a category in 14+ days → suggest it as variety.
- Each recommendation card shows:
    - Script title and category
    - Complexity level
    - Short reason text (e.g. "You struggled with Present Perfect last session")
    - A direct "Start Session" link to the script library filtered to that script.

Read ModuleIndex.md first. Then read the User Module (dashboard, analytics),
Script Module, and Mistake/Repractice Module in ProjectOverview.md.
Update ProjectOverview.md after completing this task.
```

---

## Phase 2 — High Value Features

> Steps 2.1 through 2.9. Steps 2.5 and 2.6 depend on Phase 1 being complete.
> All other steps in this phase are independent of each other and can be built
> in any order within the phase.

---

### Phase 2 — Step 1 of 9 — Interview Performance Dashboard

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Feature 5 (Section 10.2)
for the full product spec.

Task: Build a dedicated Interview Performance Dashboard for MockInterview users.

Requirements:
- Accessible from the user profile/analytics as "Interview Performance".
- Visible only if the user has at least 1 completed MockInterview session.
- Interview Readiness Score (0–100): composite metric calculated across the
  user's last 5 MockInterview sessions. Inputs:
    - Average Candidate FluencyScore (weight: 40%)
    - Average Candidate ConfidenceScore (weight: 25%)
    - Grammar error rate per session — lower is better (weight: 20%)
    - Average Candidate answer word count (SpeakingSpeedWpm × duration proxy)
      — longer structured answers score higher (weight: 15%)
- Progress timeline: line chart of Interview Readiness Score per session over time.
- Strength/Weakness Breakdown:
    - Most frequent grammar error types in Candidate turns (top 3 GrammarTags)
    - Professional FocusWords spoken correctly vs stumbled on
    - Average answer length trend (improving / stable / declining)
- Recommended next session: link to a MockInterview script that targets the
  weakest grammar tag or lowest-scoring interview type.

All data from tblVoiceAnalysis and tblMistake filtered to MockInterview sessions
and Candidate turns only.

Read ModuleIndex.md first. Then read the Live Session Module, User Module
(analytics), and Script Module in ProjectOverview.md.
Update ProjectOverview.md after completing this task.
```

---

### Phase 2 — Step 2 of 9 — Pronunciation Improvement Timeline

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Feature 6 (Section 10.2)
for the full product spec.

Task: Build a Pronunciation Improvement Timeline view on the user profile analytics.

Requirements:
- Problem Words List: all words that appear in PronunciationIssues across the
  user's session history, ordered by frequency of occurrence.
- Per-word history: for each problem word, a session-by-session timeline showing
  whether the user produced it correctly, partially, or incorrectly. Visualise
  as a simple spark-line or coloured dot row.
- IPA Reference: for each word, show the expected IPA from
  tblUtterance.PronunciationNote where the word appears as a FocusWord.
- Top 5 persistent problem words: words with errors in 3 or more of the last
  10 sessions — shown prominently at the top with a "Practice this" link to any
  script whose tblUtterance.FocusWord matches the problem word.

Data source: tblVoiceAnalysis.PronunciationIssues (JSON column) joined across
sessions for the user. IPA from tblUtterance.PronunciationNote.

Read ModuleIndex.md first. Then read the Live Session Module (tblVoiceAnalysis
schema, PronunciationIssues field) and Script Module (tblUtterance) in
ProjectOverview.md.
Update ProjectOverview.md after completing this task.
```

---

### Phase 2 — Step 3 of 9 — Session Preparation Mode

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Feature 8 (Section 10.2)
for the full product spec.

Task: Build a Session Preparation (Script Preview) mode for users.

Requirements:
- A "Prepare" button on each script card in the script library.
- Opens a read-only preview screen showing:
    - Script metadata: category, complexity level, grammar focus tag, context tag.
    - Full utterance list in sequence order: SequenceId, SpeakerLabel, EnglishText.
    - HintText shown alongside EnglishText if the user has a hint language set
      on their profile and the column is populated.
    - GrammarTag and FocusWord shown per row where present.
- No recording, no scoring, no session creation from this screen.
  A "Back to Library" button returns the user to the script library.
- For MockInterview: the preview groups turns visually — Interviewer questions
  indented/labelled differently from Candidate answers — so the Candidate can
  read through the questions to mentally prepare answers before joining.

Data: tblScript + tblUtterance via the existing script detail API endpoint.
No new API needed if GET /api/v1/scripts/{scriptId} already returns utterances.

Read ModuleIndex.md first. Then read the Script Module in ProjectOverview.md.
Update ProjectOverview.md after completing this task.
```

---

### Phase 2 — Step 4 of 9 — Learning Goals and Progress Tracking

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Feature 10 (Section 10.2)
for the full product spec.

Dependency: Complete Phase 1 Step 4 (Guided Learning Path) before this step.

Task: Build a Learning Goal setting and progress tracking feature.

Requirements:
1. Goal Setup (user profile or onboarding flow):
   - User selects one goal type:
       Prepare for a job interview / Improve my grammar /
       Build vocabulary / Speak more fluently
   - User sets a timeline: 2 weeks / 4 weeks / 8 weeks
   - Platform auto-detects the user's current level from their average FluencyScore
     over the last 5 sessions (< 55 = Beginner, 55–74 = Intermediate, 75+ = Advanced)
   - Platform displays a recommended plan:
       sessions per week + which categories to focus on based on goal type

2. Progress Panel on dashboard:
   - Sessions completed toward goal vs. target total
   - Primary metric movement: FluencyScore or grammar error rate or vocabulary bank
     size (depends on goal type) — shown as start value → current value → target value
   - Trend indicator: Improving / Stable / Declining
   - Estimated weeks remaining at current pace

3. Goal links to the Guided Learning Path panel (Phase 1 Step 4) — the
   recommendations are filtered to scripts relevant to the active goal type.

Read ModuleIndex.md first. Then read the User Module (profile, dashboard) and
Mistake/Repractice Module in ProjectOverview.md.
Update ProjectOverview.md after completing this task. Document any new goal
tracking table or column in ProjectOverview.md before writing code.
```

---

### Phase 2 — Step 5 of 9 — Cross-Session Grammar Error Trend Analysis

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Section 11.4 for the full
product spec.

Dependency: Phase 1 must be complete before this step.

Task: Add cross-session grammar error trend analysis to the user's grammar
progress view.

Requirements:
- For each GrammarTag in the user's mistake history, calculate:
    - 4-week rolling average error count per session
    - Comparison to the previous 4-week average
    - Trend label: Improving (rate down > 10%) / Stable / Regressing (rate up > 10%)
- Display as a trend indicator column (↓ Improving / → Stable / ↑ Regressing)
  alongside each GrammarTag row in the existing grammar progress section of the
  user dashboard.
- Example display: "Present Perfect — 4.2 errors/session — ↓ 1.1 vs last month — Improving"
- GrammarTags labelled Regressing are automatically surfaced as priority items
  in the Guided Learning Path (Phase 1 Step 4) and the Weekly Report (Phase 1 Step 3).

Data: tblMistake grouped by GrammarTag and DateCreated for the user.
Existing SP uspGetGrammarProgressByUserId may need extension to return
date-bucketed counts.

Read ModuleIndex.md first. Then read the Mistake/Repractice Module and User Module
(grammar progress section) in ProjectOverview.md.
Update ProjectOverview.md after completing this task.
```

---

### Phase 2 — Step 6 of 9 — Filler Phrase Detection

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Section 11.2 for the full
product spec.

Dependency: Phase 1 Step 1 (Post-Session Review) must be complete before this step.

Task: Extend the voice analysis hesitation detection to include multi-word
filler phrases, beyond the current single-word detection.

Current state: TranscriptNormalizer detects single-word hesitations:
um, uh, er, hmm — stored in tblVoiceAnalysis.HesitationWords as CSV.

Required addition:
- Extend detection to also flag these filler phrases found in TranscribedText:
    "you know", "I mean", "basically", "kind of", "sort of", "you see",
    "to be honest", "at the end of the day" — and "like" and "actually"
    when they appear more than twice in a single turn (filler usage, not
    legitimate grammar usage).
- Store detected filler phrases in the same HesitationWords CSV field alongside
  single-word hesitations.
- Each filler phrase occurrence applies the same ConfidenceScore penalty as a
  single hesitation word (max 25 total penalty unchanged).
- Display filler phrases in the Post-Session Review screen (Phase 1 Step 1)
  flagged inline in the transcript alongside single-word hesitations.

Read ModuleIndex.md first. Then read the Frontend Voice Recognition Engine —
Stable Contract (TranscriptNormalizer section) and the Save Voice Analysis flow
in ProjectOverview.md.
Update ProjectOverview.md after completing this task.
```

---

### Phase 2 — Step 7 of 9 — Admin Script Quality Analytics

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Feature 11 (Section 10.2)
for the full product spec.

Task: Build a Script Quality Analytics view in the admin dashboard.

Requirements:
- A "Script Analytics" tab or section in the admin panel.
- Table showing per-script performance metrics:
    - Script Title, Category
    - Total sessions started on this script
    - Completion rate: sessions where Status = COMPLETED / total sessions started
    - Average FluencyScore across all sessions (from tblVoiceAnalysis)
    - Average MistakeCount per session (from tblMistake)
    - Average session duration in minutes
    - Re-read rate: average ReReadCount per turn (from tblTurnState)
    - Repractice conversion rate: % of sessions that have at least one
      linked tblRepracticeSession generated from mistakes detected
    - Last used date: most recent session DateCreated using this script
- Scripts not used in 60+ days: highlighted row + "Inactive" badge.
- Table is sortable by any column and filterable by category.
- Clicking a script row navigates to the existing script detail view.

Data joins: tblScript → tblSession (via ScriptId) → tblVoiceAnalysis,
tblMistake, tblTurnState, tblRepracticeSession.

Read ModuleIndex.md first. Then read the Admin Module, Script Module, and
Live Session Module in ProjectOverview.md.
Update ProjectOverview.md after completing this task.
```

---

### Phase 2 — Step 8 of 9 — Script Versioning Rollback and Duplication

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Sections 12.2 and 12.3
for the full product spec.

Task: Add Script Version Rollback and Script Duplication to the admin script
management UI.

Requirements — Rollback:
- In the script detail view, show a Version History tab listing all rows in
  tblScriptVersion for that script (VersionNumber, UploadedDate, VersionNotes,
  uploaded by FullName).
- A "Rollback to this version" button on any previous version row.
- On rollback: restore tblUtterance rows to the state of the selected version
  (re-parse from the stored version data or re-apply the utterance snapshot).
  Increment the current VersionNumber to indicate the rollback was a new event.

Requirements — Duplication:
- A "Duplicate Script" button on the script detail view.
- Creates a new tblScript row with:
    - ScriptTitle: "[Original Title] — Copy"
    - All metadata fields copied from the original
    - Status: IsActive = false (draft state — not available for sessions until admin activates)
    - Version: 1
- Copies all tblUtterance rows from the original script to the new ScriptId.
- Navigates the admin to the new script's detail/edit view immediately after creation.

Read ModuleIndex.md first. Then read the Script Module (tblScript, tblScriptVersion,
tblUtterance schemas and SPs) and the Admin Script Upload Wizard Contract in
ProjectOverview.md.
Update ProjectOverview.md after completing this task.
```

---

### Phase 2 — Step 9 of 9 — Claude Prompt Helper (Script Upload UI)

**Status:** [x] COMPLETE — 2026-06-01

**Revised Approach (No Backend AI Integration):**

The original plan called for direct Claude API integration in the backend. This was
revised to a simpler, more practical workflow: the admin downloads the category-specific
Excel template AND receives a pre-built, copy-ready Claude prompt alongside it.

The admin copies the prompt, pastes it into claude.ai, receives JSON output from Claude,
then uploads that output via the existing Excel upload wizard. No API keys, no new
backend services, no added complexity.

**What was built:**

In the Admin Script Upload Wizard (Step 1 screen):
- Each category download button now highlights when selected.
- After clicking any category template button (which downloads the .xlsx), a prompt
  panel appears below the download buttons showing the full Claude prompt pre-built
  for that category.
- The prompt includes: category-specific speaker label rules, mandatory column rules,
  row count constraints, self-validation checklist, and the exact JSON output contract
  from ExcelTemplateStandard.md Section 10.1.
- A "Copy Prompt" button copies the full prompt to the clipboard in one click.
- The copied state shows a checkmark confirmation for 3 seconds.

**Admin workflow:**
1. Select a category → download template + prompt panel appears
2. Click "Copy Prompt"
3. Paste into claude.ai → Claude returns JSON matching the output contract
4. Upload the JSON-generated content as an Excel file via the upload wizard

**No changes to:**
- Backend API
- ProjectOverview.md Script Module (no new endpoints)
- tblScript, tblUtterance, or the upload pipeline (unchanged)

**File changed:** `Frontend/src/app/modules/scripts/script-upload/script-upload.component.ts`

---

## Phase 3 — Future Roadmap Features

> Plan these after Phase 2 is stable. Each step is independent.

---

### Phase 3 — Step 1 of 5 — Spaced Repetition for Grammar Mistakes

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Feature 12 (Section 10.3)
for the full product spec.

Task: Extend the RepracticeRound system with spaced repetition scheduling.

Requirements:
- When a RepracticeRound is completed and a mistake is marked resolved, instead
  of simply setting IsResolved = true, schedule a review:
    - First review: 1 day after resolution
    - Second review: 3 days
    - Third review: 7 days
    - Fourth review: 14 days
    - Fifth review: 30 days (considered long-term retained after this)
- If the user makes the same grammar mistake again before their scheduled review:
  reset the interval back to 1 day.
- If the user produces the correct form in a RepracticeRound review session:
  advance to the next interval.
- Dashboard shows "Reviews due today" count and surfaces them in the Guided
  Learning Path panel (Phase 1 Step 4) with higher priority than new content.

Read ModuleIndex.md first. Then read the Mistake/Repractice Module (tblMistake,
tblRepracticeSession schemas and SPs) and the User Module (dashboard) in
ProjectOverview.md.
Update ProjectOverview.md after completing this task. Document the new review
schedule fields or table in ProjectOverview.md before writing code.
```

---

### Phase 3 — Step 2 of 5 — Cohort Management (B2B)

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Feature 13 (Section 10.3)
for the full product spec.

Task: Build a Cohort Management layer for enterprise and training institution use.

Requirements:
1. Admin can create a cohort with a name and optional description
   (e.g., "Batch June 2026 — HR English Training").
2. Admin can assign users to a cohort from the user management screen.
   One user can belong to one cohort at a time.
3. Cohort Analytics view in the admin panel:
   - Average FluencyScore across all cohort members over time
   - Most common grammar mistakes across the cohort (top 5 GrammarTags)
   - Session completion rate for the cohort
   - Most improved member (largest FluencyScore delta in last 30 days)
   - Least active members (no session in last 7 days) — flagged for follow-up
4. Cohort Report export: admin can download a PDF or Excel report summarising
   all cohort members' progress for a selected date range — suitable for
   HR managers and training coordinators.

Read ModuleIndex.md first. Then read the Admin Module and User Module in
ProjectOverview.md.
Update ProjectOverview.md after completing this task. Document the new cohort
table schema in ProjectOverview.md before writing code.
```

---

### Phase 3 — Step 3 of 5 — Speaking Challenge Mode

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Feature 14 (Section 10.3)
for the full product spec.

Task: Build a weekly Speaking Challenge mode.

Requirements:
1. Admin designates one script per week as the "Weekly Challenge" script
   in the admin script management UI (a toggle on the script detail view).
   Only one script can be the active weekly challenge at a time.
2. Challenge banner appears on the user dashboard showing:
   - The challenge script title and category
   - Days remaining this week
   - The user's current best score on the challenge (if they have attempted it)
   - A leaderboard showing the top 10 fluency scores for the week
3. Users can attempt the challenge script multiple times; their best FluencyScore
   counts toward the leaderboard.
4. Leaderboard resets every Monday when a new challenge script is designated.
5. Users who finish in the top 20% of scorers by Sunday midnight receive a
   "Challenge Badge" added to their profile.
6. Previous weekly challenges are archived and viewable but not scoreable.

Read ModuleIndex.md first. Then read the Admin Module, Script Module, Session Module,
and the Badge/Streak system in the User Module in ProjectOverview.md.
Update ProjectOverview.md after completing this task. Document any new challenge
or leaderboard table in ProjectOverview.md before writing code.
```

---

### Phase 3 — Step 4 of 5 — Completion Milestones and Certificates

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Feature 15 (Section 10.3)
for the full product spec.

Task: Build a Completion Milestone and Certificate system.

Milestone criteria (one certificate per category):
- GrammarDrill "Grammar Foundation": complete 10 GrammarDrill sessions across
  at least 5 distinct GrammarFocusTags.
- MockInterview "Interview Ready": achieve an Interview Readiness Score ≥ 80
  (Phase 2 Step 1) across any 5 MockInterview sessions.
- VocabularySprint "Vocabulary Builder": accumulate 100 FocusWords in the
  vocabulary bank with a correct production rate ≥ 70% (requires Phase 1 Step 2).
- FluencyDrill "Fluency Milestone": complete 8 FluencyDrill sessions with an
  average SpeakingSpeedWpm between 80 and 120.
- RepracticeRound "Grammar Corrector": fully resolve 10 distinct GrammarTag
  mistake types (resolve all instances of each tag).
- Roleplay "Scenario Master": complete 10 Roleplay sessions across at least
  5 distinct ContextTags with an average FluencyScore ≥ 70.

Requirements:
- Evaluate milestone criteria after every session completion.
- When criteria are met: award the certificate, show a congratulations modal,
  add the certificate to the user's profile certificates section.
- Certificates are displayed on the user profile with earned date.
- Each certificate can be downloaded as a shareable image (PNG) with the user's
  name, certificate title, and GoWithFlow branding.

Read ModuleIndex.md first. Then read the User Module (badge system, tblUserBadge,
uspCheckAndAwardBadge) in ProjectOverview.md — the certificate system extends
the existing badge mechanism.
Update ProjectOverview.md after completing this task.
```

---

### Phase 3 — Step 5 of 5 — Live Session Audio Archive (Opt-In)

**Status:** [x] COMPLETE — 2026-06-01

```
Review PM_CategoryWorkflow_ProductPlan.md Part II Feature 16 (Section 10.3)
for the full product spec.

Task: Build an opt-in personal audio archive for the user's own session turns.

Requirements:
1. Consent gate: during session creation (lobby screen), the session host sees
   an opt-in toggle: "Record my own voice turns for personal review". Default: OFF.
   The toggle affects only the user who sets it — other participants' audio is
   never recorded without their own separate opt-in.
2. When opted in: the client captures the user's own microphone audio per turn
   (using the existing WebRTC / MediaRecorder infrastructure) and uploads each
   turn's audio clip to secure user-scoped storage after the turn completes.
3. In the Post-Session Review screen (Phase 1 Step 1): if audio was archived for
   this session, a play button appears next to each of the user's own turns.
4. In session history: sessions with archived audio show an audio indicator icon.
5. Audio retention: user can delete any archived clip from their session history
   at any time. Default retention: 90 days, then auto-deleted.
6. Privacy constraint: archived audio files are scoped strictly to the user who
   opted in. No other user, admin, or platform function can access another user's
   archived audio.

Read ModuleIndex.md first. Then read the Live Session Module (WebRTC Voice Broadcast
Contract, Speaker Turn Contract) and the User Module in ProjectOverview.md.
Update ProjectOverview.md after completing this task. Document the audio storage
approach and retention policy in ProjectOverview.md before writing any code.
```

---

*Part III end — Implementation Prompt Library.*
*Each prompt is self-contained. Copy one prompt per session. Complete Phase 0 before Phase 1. Complete Phase 1 before Phase 2 Steps 5 and 6. All other steps within a phase are independent.*
