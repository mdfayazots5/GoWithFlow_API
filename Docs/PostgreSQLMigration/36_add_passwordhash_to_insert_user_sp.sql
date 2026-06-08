-- Migration 36: Add p_passwordhash to uspinsertuser
-- BUG: admin-created users could never log in. AdminService hashes dto.Password into
-- user.PasswordHash, but the value was dropped twice on the way to the DB:
--   1. UserRepository.InsertUserAsync never added an @PasswordHash parameter.
--   2. uspinsertuser had no p_passwordhash parameter and hard-coded NULL into
--      tbluser.passwordhash. The auth flow then rejects any user with a NULL hash.
-- Fix: repository now passes @PasswordHash; this migration adds the matching
-- p_passwordhash parameter and writes it into the column.
--
-- The function is called with NAMED arguments (p_x => @p_x), so the new parameter's
-- position is irrelevant to callers. Because adding a parameter changes the function
-- signature, the OLD 10-arg overload MUST be dropped first — otherwise both overloads
-- coexist and a named-arg call can resolve to the stale one. Drop any 11-arg overload
-- too, in case this migration is partially re-applied.

DROP FUNCTION IF EXISTS uspinsertuser(
    VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR
);
DROP FUNCTION IF EXISTS uspinsertuser(
    VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR
);

CREATE OR REPLACE FUNCTION uspinsertuser(
    p_fullname              VARCHAR(128),
    p_mobilenumber          VARCHAR(16),
    p_email                 VARCHAR(128) DEFAULT NULL,
    p_passwordhash          VARCHAR(512) DEFAULT NULL,
    p_agegroup              VARCHAR(32)  DEFAULT NULL,
    p_preferredhintlanguage VARCHAR(32)  DEFAULT NULL,
    p_avatarurl             VARCHAR(256) DEFAULT NULL,
    p_groupcode             VARCHAR(32)  DEFAULT NULL,
    p_role                  VARCHAR(16)  DEFAULT 'USER',
    p_createdby             VARCHAR(128) DEFAULT NULL,
    p_ipaddress             VARCHAR(64)  DEFAULT NULL
) RETURNS TABLE (userid BIGINT) AS $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM tbluser WHERE mobilenumber = p_mobilenumber AND isdeleted = FALSE
    ) THEN
        RAISE EXCEPTION 'Mobile number is already registered.' USING ERRCODE = 'P0001';
    END IF;

    RETURN QUERY
    INSERT INTO tbluser (
        fullname, mobilenumber, email, passwordhash, agegroup,
        preferredhintlanguage, avatarurl, groupcode, role,
        createdby, ipaddress
    )
    VALUES (
        p_fullname, p_mobilenumber, p_email, p_passwordhash, p_agegroup,
        p_preferredhintlanguage, p_avatarurl, p_groupcode, p_role,
        p_createdby, p_ipaddress
    )
    RETURNING tbluser.userid;
END;
$$ LANGUAGE plpgsql;
