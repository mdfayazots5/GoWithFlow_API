# Go With Flow Deployment Notes

## Pre-Deployment

- ☐ Set `ASPNETCORE_ENVIRONMENT=Production`.
- ☐ Set the production SQL connection string through environment-variable backed configuration.
- ☐ Set `JwtSettings:SecretKey` from a secure environment variable and never commit it to source control.
- ☐ Verify the JWT secret is at least 32 characters.
- ☐ Verify `CorsSettings:AllowedOrigins` matches the production frontend URLs.
- ☐ Verify `appsettings.Production.json` is deployed with production-specific overrides only.
- ☐ Confirm avatar storage path `wwwroot/avatars/` exists and upload permissions are correct.

## Database

- ☐ Run EF migrations in the validated order:
  - `InitialCreate_Phase1`
  - `AddAdminModule_Phase2`
  - `AddScriptModule_Phase3`
  - `AddSessionModule_Phase4`
  - `AddLiveSessionModule_Phase5`
  - `AddMistakeModule_Phase6`
  - `AddUserModule_Phase7`
- ☐ All `tbl*` tables are created in the production database.
- ☐ All `usp*` stored procedures are deployed.
- ☐ Phase 9 composite indexes are created:
  - `IDX_tblMistake_UserId_MistakeType_IsResolved`
  - `IDX_tblVoiceAnalysis_SessionId_UserId`
  - `IDX_tblSession_Status_IsDeleted`
  - `IDX_tblSessionMember_SessionId_IsActive`
  - `IDX_tblRepracticeSession_UserId_Status`
- ☐ All remaining filtered and supporting indexes are created.
- ☐ Backup plan is configured; daily full backups are recommended.

## Security

- ☐ HTTPS enforcement remains enabled through `UseHttpsRedirection`.
- ☐ Rate limiting is active on auth endpoints.
- ☐ Auth, admin, user, and hub authorization policies are active in production.
- ☐ Swagger is disabled in production except when explicitly enabled for controlled diagnostics.
- ☐ Sensitive data is not written to Serilog sinks.

## Post-Deployment Verification

- ☐ `/api/health` returns `Healthy`.
- ☐ `/api/health/db` confirms SQL Server connectivity.
- ☐ `/api/health/detailed` returns structured per-check status output.
- ☐ Auth endpoints are working: `send-otp`, `verify-otp`, `refresh-token`, `logout`.
- ☐ Admin report export and user report Excel generation are working.
- ☐ Script upload and sample template download are working.
- ☐ SignalR hubs connect successfully for both `/hubs/session` and `/hubs/live-session`.
- ☐ A complete session updates streaks, badges, mistakes, and repractice data as expected.
