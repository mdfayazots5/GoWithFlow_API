-- --------------------------------------------
-- Dual-provider routine aliases
-- --------------------------------------------
-- These aliases keep PostgreSQL routine names compatible with
-- the SQL Server routine names referenced by the application.
-- SQL Server stays unchanged; PostgreSQL gets compatibility
-- wrappers where the generated function names drifted.

CREATE OR REPLACE FUNCTION uspgetalluserbysearch(
    p_searchterm VARCHAR(128) DEFAULT NULL,
    p_agegroup   VARCHAR(32)  DEFAULT NULL,
    p_isactive   BOOLEAN      DEFAULT NULL,
    p_pagenumber INT          DEFAULT 1,
    p_pagesize   INT          DEFAULT 10
) RETURNS SETOF REFCURSOR AS $$
BEGIN
    RETURN QUERY
    SELECT * FROM uspgetallusersbysearch(
        p_searchterm,
        p_agegroup,
        p_isactive,
        p_pagenumber,
        p_pagesize
    );
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION uspgetmistakebyuseridwithfilter(
    p_userid      BIGINT,
    p_mistaketype VARCHAR(32) DEFAULT NULL,
    p_isresolved  BOOLEAN     DEFAULT NULL,
    p_pagenumber  INT         DEFAULT 1,
    p_pagesize    INT         DEFAULT 20
) RETURNS SETOF REFCURSOR AS $$
BEGIN
    RETURN QUERY
    SELECT * FROM uspgetmistakebyuserwithfilter(
        p_userid,
        p_mistaketype,
        p_isresolved,
        p_pagenumber,
        p_pagesize
    );
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION uspgetrepracticesessionbyrepracticesessionid(
    p_repracticesessionid BIGINT
) RETURNS SETOF REFCURSOR AS $$
BEGIN
    RETURN QUERY
    SELECT * FROM uspgetrepracticesessionbyid(p_repracticesessionid);
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION uspgettopgrammarmistaketype(
    p_topn INT DEFAULT 5
) RETURNS TABLE (
    grammartag VARCHAR(64),
    usercount  BIGINT,
    percentage NUMERIC(5,2)
) AS $$
BEGIN
    RETURN QUERY
    SELECT *
    FROM uspgettopgrammarmistaketypes(p_topn);
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION uspgetuserfullreportbyuserid(
    p_userid BIGINT
) RETURNS SETOF REFCURSOR AS $$
BEGIN
    RETURN QUERY
    SELECT * FROM uspgetuserfulreportbyuserid(p_userid);
END;
$$ LANGUAGE plpgsql;
