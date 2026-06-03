-- Migration 28: Abandon ACTIVE session when fewer than 2 members remain after a leave
-- Apply directly to Supabase (PostgreSQL).
--
-- Root cause: uspupdatesessionmemberleft only abandoned the session when the host left
-- OR when active_member_count = 0. In a 2-person ACTIVE session, if the non-host user
-- leaves, active_member_count drops to 1 (the host alone). The host-check fails (they
-- didn't leave) and the 0-count check fails (1 remains), so the session stayed ACTIVE.
-- The remaining user was stuck alone in a live session that could never continue.
--
-- Fix: after deactivating the leaving member, if the session is ACTIVE and fewer than 2
-- active members remain, mark it ABANDONED immediately.
--
-- Includes the migration 25 guard: never downgrade COMPLETED → ABANDONED.

CREATE OR REPLACE FUNCTION public.uspupdatesessionmemberleft(
    p_sessionid BIGINT,
    p_userid    BIGINT,
    p_updatedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
DECLARE
    v_hostuserid        BIGINT     := 0;
    v_activemembercount INT        := 0;
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

    -- Never downgrade a COMPLETED session to ABANDONED on member disconnect (migration 25).
    IF v_currentstatus = 'COMPLETED' THEN
        RETURN;
    END IF;

    SELECT COUNT(1) INTO v_activemembercount
    FROM tblsessionmember AS sem
    WHERE sem.sessionid = p_sessionid AND sem.isdeleted = FALSE AND sem.isactive = TRUE;

    -- Abandon if: host left, or no members remain, or session is ACTIVE with < 2 members.
    IF v_hostuserid = p_userid
       OR v_activemembercount = 0
       OR (v_currentstatus = 'ACTIVE' AND v_activemembercount < 2)
    THEN
        PERFORM uspupdatesessionstatus(p_sessionid, 'ABANDONED', p_updatedby, p_ipaddress);
    END IF;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
