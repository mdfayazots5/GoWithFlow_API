-- Phase 17: AI Voice Participant — schema & seed
-- PostgreSQL (Supabase) — column/table names are all lowercase per EF/raw-SQL convention.
-- See Backend/Docs/Dev/AIVoiceParticipantArchitecture.md (Phase 0).
-- No behavior change — columns + reserved system user only.
--
-- The AI participant is modeled as a NON-HUMAN session member holding a slot. To satisfy the
-- existing tblsessionmember.userid FK without making it nullable (which would ripple through
-- many SPs), AI member rows point at ONE reserved system user seeded below. Because userid is
-- a BIGSERIAL (env-specific), application code resolves this user by its sentinel mobilenumber
-- ('AI_PARTICIPANT'), never by a hard-coded id.

-- 1) Flag AI-held slots on the session member.
ALTER TABLE public.tblsessionmember
  ADD COLUMN IF NOT EXISTS isai BOOLEAN NOT NULL DEFAULT FALSE;

-- 2) Per-session AI configuration (all nullable; populated only when AI is enabled at creation).
ALTER TABLE public.tblsession
  ADD COLUMN IF NOT EXISTS aienabled         BOOLEAN       NULL,
  ADD COLUMN IF NOT EXISTS aivoicegender     VARCHAR(8)    NULL,   -- 'Male' | 'Female'
  ADD COLUMN IF NOT EXISTS aispeechrate      DECIMAL(3,2)  NULL,   -- TTS rate multiplier, e.g. 0.75 / 1.00 / 1.25
  ADD COLUMN IF NOT EXISTS aiquestiondelaysec INT          NULL;   -- pause before AI reads next line

-- 3) Reserved system user that backs every AI member's FK. Idempotent on the sentinel mobile number.
INSERT INTO public.tbluser
    (fullname, mobilenumber, email, passwordhash, agegroup, preferredhintlanguage, avatarurl,
     groupcode, role, dailystreakcount, totalsessionsplayed, lastlogindate, isactive, registrationdate,
     sortorder, ipaddress, createdby, datecreated, isdeleted)
SELECT
    'AI Voice Participant', 'AI_PARTICIPANT', NULL, NULL, 'All', 'None', NULL,
    NULL, 'SYSTEM', 0, 0, NULL, TRUE, (now() AT TIME ZONE 'utc'),
    0, '127.0.0.1', 'System', (now() AT TIME ZONE 'utc'), FALSE
WHERE NOT EXISTS (
    SELECT 1 FROM public.tbluser WHERE mobilenumber = 'AI_PARTICIPANT'
);
