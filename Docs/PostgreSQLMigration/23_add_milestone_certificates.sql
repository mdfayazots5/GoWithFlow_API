-- Migration 23: Add uspCheckAndAwardMilestoneBadge (Phase 3 Step 4)
-- Apply directly to Supabase (PostgreSQL).

-- uspcheckandawardmilestonebadge = "uspCheckAndAwardMilestoneBadge".toLower()
CREATE OR REPLACE FUNCTION public.uspcheckandawardmilestonebadge(
    p_userid    BIGINT,
    p_createdby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
DECLARE
    v_count INT;
BEGIN
    -- GRAMMAR_FOUNDATION: 10+ GrammarDrill sessions across 5+ distinct GrammarFocusTags
    IF NOT EXISTS (SELECT 1 FROM tblUserbadge WHERE userid = p_userid AND badgecode = 'GRAMMAR_FOUNDATION') THEN
        SELECT COUNT(DISTINCT s.grammarfocustag)::INT INTO v_count
        FROM   tblsession ss
        INNER  JOIN tblscript s ON s.scriptid = ss.scriptid
        INNER  JOIN tblsessionmember sm ON sm.sessionid = ss.sessionid AND sm.userid = p_userid AND sm.isdeleted = FALSE
        WHERE  LOWER(s.category) LIKE '%grammar%'
          AND  ss.status = 'COMPLETED' AND ss.isdeleted = FALSE
        HAVING COUNT(DISTINCT ss.sessionid) >= 10;

        IF COALESCE(v_count, 0) >= 5 THEN
            PERFORM uspinsertuserbadge(p_userid, 'GRAMMAR_FOUNDATION', 'Grammar Foundation', p_createdby, p_ipaddress);
        END IF;
    END IF;

    -- INTERVIEW_READY: avg Candidate FluencyScore >= 75 across 5+ MockInterview sessions
    IF NOT EXISTS (SELECT 1 FROM tblUserbadge WHERE userid = p_userid AND badgecode = 'INTERVIEW_READY') THEN
        SELECT COUNT(DISTINCT ss.sessionid)::INT INTO v_count
        FROM   tblsession ss
        INNER  JOIN tblscript s ON s.scriptid = ss.scriptid
        INNER  JOIN tblsessionmember sm ON sm.sessionid = ss.sessionid AND sm.userid = p_userid AND sm.isdeleted = FALSE
        INNER  JOIN tblvoiceanalysis va ON va.sessionid = ss.sessionid AND va.userid = p_userid AND va.isdeleted = FALSE
        WHERE  LOWER(s.category) LIKE '%interview%'
          AND  ss.status = 'COMPLETED' AND ss.isdeleted = FALSE
        HAVING AVG(va.fluencyscore) >= 75;

        IF COALESCE(v_count, 0) >= 5 THEN
            PERFORM uspinsertuserbadge(p_userid, 'INTERVIEW_READY', 'Interview Ready', p_createdby, p_ipaddress);
        END IF;
    END IF;

    -- FLUENCY_MILESTONE: 8 FluencyDrill sessions with avg WPM 80–120
    IF NOT EXISTS (SELECT 1 FROM tblUserbadge WHERE userid = p_userid AND badgecode = 'FLUENCY_MILESTONE') THEN
        SELECT COUNT(DISTINCT ss.sessionid)::INT INTO v_count
        FROM   tblsession ss
        INNER  JOIN tblscript s ON s.scriptid = ss.scriptid
        INNER  JOIN tblsessionmember sm ON sm.sessionid = ss.sessionid AND sm.userid = p_userid AND sm.isdeleted = FALSE
        INNER  JOIN tblvoiceanalysis va ON va.sessionid = ss.sessionid AND va.userid = p_userid AND va.isdeleted = FALSE
        WHERE  LOWER(s.category) LIKE '%fluency%'
          AND  ss.status = 'COMPLETED' AND ss.isdeleted = FALSE
        HAVING AVG(va.speakingspeedwpm) BETWEEN 80 AND 120;

        IF COALESCE(v_count, 0) >= 8 THEN
            PERFORM uspinsertuserbadge(p_userid, 'FLUENCY_MILESTONE', 'Fluency Milestone', p_createdby, p_ipaddress);
        END IF;
    END IF;

    -- GRAMMAR_CORRECTOR: 10 distinct GrammarTag mistake types resolved
    IF NOT EXISTS (SELECT 1 FROM tblUserbadge WHERE userid = p_userid AND badgecode = 'GRAMMAR_CORRECTOR') THEN
        SELECT COUNT(DISTINCT grammartag)::INT INTO v_count
        FROM   tblmistake
        WHERE  userid = p_userid AND isresolved = TRUE AND grammartag IS NOT NULL AND isdeleted = FALSE;

        IF COALESCE(v_count, 0) >= 10 THEN
            PERFORM uspinsertuserbadge(p_userid, 'GRAMMAR_CORRECTOR', 'Grammar Corrector', p_createdby, p_ipaddress);
        END IF;
    END IF;

    -- SCENARIO_MASTER: 10 Roleplay sessions across 5+ distinct ContextTags with avg FluencyScore >= 70
    IF NOT EXISTS (SELECT 1 FROM tblUserbadge WHERE userid = p_userid AND badgecode = 'SCENARIO_MASTER') THEN
        SELECT COUNT(DISTINCT s.contexttag)::INT INTO v_count
        FROM   tblsession ss
        INNER  JOIN tblscript s ON s.scriptid = ss.scriptid
        INNER  JOIN tblsessionmember sm ON sm.sessionid = ss.sessionid AND sm.userid = p_userid AND sm.isdeleted = FALSE
        INNER  JOIN tblvoiceanalysis va ON va.sessionid = ss.sessionid AND va.userid = p_userid AND va.isdeleted = FALSE
        WHERE  (LOWER(s.category) LIKE '%roleplay%' OR LOWER(s.category) LIKE '%role play%')
          AND  ss.status = 'COMPLETED' AND ss.isdeleted = FALSE
        HAVING COUNT(DISTINCT ss.sessionid) >= 10 AND AVG(va.fluencyscore) >= 70;

        IF COALESCE(v_count, 0) >= 5 THEN
            PERFORM uspinsertuserbadge(p_userid, 'SCENARIO_MASTER', 'Scenario Master', p_createdby, p_ipaddress);
        END IF;
    END IF;

    -- VOCABULARY_BUILDER: 100 vocabulary words in bank
    IF NOT EXISTS (SELECT 1 FROM tblUserbadge WHERE userid = p_userid AND badgecode = 'VOCABULARY_BUILDER') THEN
        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'tblusservocabulary') THEN
            SELECT COUNT(*) INTO v_count
            FROM   public.tbluservocabulary
            WHERE  userid = p_userid AND isdeleted = FALSE AND correctcount > 0;

            IF v_count >= 100 THEN
                PERFORM uspinsertuserbadge(p_userid, 'VOCABULARY_BUILDER', 'Vocabulary Builder', p_createdby, p_ipaddress);
            END IF;
        END IF;
    END IF;

END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
