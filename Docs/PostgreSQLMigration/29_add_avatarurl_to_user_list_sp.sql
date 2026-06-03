-- Migration 29: Add avatarurl to uspgetalluserbysearch result set
-- Reason: AdminUserListResponseDto now includes AvatarUrl so the admin user
-- list screen can display actual user avatars instead of initials only.

CREATE OR REPLACE FUNCTION uspgetalluserbysearch(
    p_searchterm VARCHAR(128) DEFAULT NULL,
    p_agegroup   VARCHAR(32)  DEFAULT NULL,
    p_isactive   BOOLEAN      DEFAULT NULL,
    p_pagenumber INT          DEFAULT 1,
    p_pagesize   INT          DEFAULT 10
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1   REFCURSOR;
    ref2   REFCURSOR;
    v_offset INT := (p_pagenumber - 1) * p_pagesize;
BEGIN
    OPEN ref1 FOR
    SELECT
        usr.userid, usr.fullname, usr.mobilenumber, usr.agegroup,
        usr.totalsessionsplayed, usr.dailystreakcount,
        usr.lastlogindate, usr.isactive,
        usr.avatarurl
    FROM tbluser AS usr
    WHERE usr.isdeleted = FALSE
      AND (p_searchterm IS NULL
           OR usr.fullname     ILIKE '%' || p_searchterm || '%'
           OR usr.mobilenumber ILIKE '%' || p_searchterm || '%')
      AND (p_agegroup IS NULL OR usr.agegroup = p_agegroup)
      AND (p_isactive IS NULL OR usr.isactive = p_isactive)
    ORDER BY usr.datecreated DESC, usr.userid DESC
    LIMIT p_pagesize OFFSET v_offset;
    RETURN NEXT ref1;

    OPEN ref2 FOR
    SELECT COUNT(1) AS totalcount
    FROM tbluser AS usr
    WHERE usr.isdeleted = FALSE
      AND (p_searchterm IS NULL
           OR usr.fullname     ILIKE '%' || p_searchterm || '%'
           OR usr.mobilenumber ILIKE '%' || p_searchterm || '%')
      AND (p_agegroup IS NULL OR usr.agegroup = p_agegroup)
      AND (p_isactive IS NULL OR usr.isactive = p_isactive);
    RETURN NEXT ref2;
END;
$$ LANGUAGE plpgsql;
