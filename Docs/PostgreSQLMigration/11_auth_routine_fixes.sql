-- --------------------------------------------
-- Auth routine compatibility fixes
-- --------------------------------------------
-- `06_stored_procedures.sql` defines `usprovkerefreshtoken`
-- (missing the second "e"), while application code maps
-- `dbo.uspRevokeRefreshToken` to `public.usprevokerefreshtoken`.
-- This corrective file creates the correctly named function
-- without removing the typoed legacy routine.

CREATE OR REPLACE FUNCTION usprevokerefreshtoken(
    p_token     VARCHAR(512),
    p_updatedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE tblrefreshtoken
    SET isrevoked   = TRUE,
        revokedat   = NOW(),
        updatedby   = p_updatedby,
        lastupdated = NOW(),
        ipaddress   = p_ipaddress
    WHERE token     = p_token
      AND isrevoked = FALSE
      AND isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;
