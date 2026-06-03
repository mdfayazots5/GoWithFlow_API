-- Migration 25: Guard uspupdatesessionmemberleft against downgrading COMPLETED → ABANDONED
-- Apply directly to Supabase (PostgreSQL).
--
-- Root cause: when a member disconnects after session completion, the SP was setting
-- status = ABANDONED because it only checked if the host left or no active members remain,
-- without checking whether the session was already COMPLETED.
-- Fix: read current session status first; return immediately if already COMPLETED.

CREATE OR REPLACE FUNCTION public.uspupdatesessionmemberleft(
    p_sessionid BIGINT,
    p_userid    BIGINT,
    p_updatedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
DECLARE
    v_hostuserid        BIGINT := 0;
    v_activemembercount INT    := 0;
    v_currentstatus     VARCHAR(16) := '';
BEGIN
    UPDATE tblsessionmember
    SET leftat      = NOW(),
        isactive    = FALSE,
        isready     = FALSE,
        updatedby   = p_updatedby,
        lastupdated = NOW(),
        ipaddress   = p_ipaddress
    WHERE sessionid = p_sessionid
      AND userid    = p_userid
      AND isdeleted = FALSE
      AND isactive  = TRUE;

    SELECT ses.hostuserid, ses.status INTO v_hostuserid, v_currentstatus
    FROM tblsession AS ses
    WHERE ses.sessionid = p_sessionid AND ses.isdeleted = FALSE;

    -- Never downgrade a COMPLETED session to ABANDONED on member disconnect
    IF v_currentstatus = 'COMPLETED' THEN
        RETURN;
    END IF;

    SELECT COUNT(1) INTO v_activemembercount
    FROM tblsessionmember AS sem
    WHERE sem.sessionid = p_sessionid AND sem.isdeleted = FALSE AND sem.isactive = TRUE;

    IF v_hostuserid = p_userid OR v_activemembercount = 0 THEN
        PERFORM uspupdatesessionstatus(p_sessionid, 'ABANDONED', p_updatedby, p_ipaddress);
    END IF;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
