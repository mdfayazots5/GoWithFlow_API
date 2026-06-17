# ProjectOverview

## Repository Structure

- Root folders:
  - `Backend`
  - `Docs`
  - `Frontend`
- Frontend validation assets:
  - `Frontend/playwright.config.ts`
  - `Frontend/e2e/smoke.spec.ts`
  - `npm run test:e2e` runs the smoke check against a dedicated preview server on port `4175`
- Frontend shared header branding:
  - `Frontend/src/app/shared/components/header/header.component.ts`
  - visible header site title: `GoWithFlow - Grow Together`
  - header logo links to `/user/dashboard`
  - shared header no longer renders route-based center titles such as `Join Session`; the layout shows only the left logo/back control and the right avatar/streak area
  - logo text truncates safely on narrow screens via `header.component.scss`
- Current validated backend solution:
  - `Backend/GoWithFlow.sln`
  - Projects:
    - `GoWithFlow.Domain`
    - `GoWithFlow.Application`
    - `GoWithFlow.Infrastructure`
    - `GoWithFlow.API`
- Current backend implementation scope:
  - Phase 1 authentication and user foundation
  - Phase 2 admin module source implementation
  - Phase 3 script module source implementation
  - Phase 4 session module source implementation
  - Phase 5 live session module source implementation
  - Phase 6 mistake tracking and repractice source implementation
  - Phase 7 user profile, progress, and streak source implementation
  - Phase 8 infrastructure, security, and hardening source implementation
  - Phase 9 production configuration, SQL optimisation, and master reference implementation

---

## Backend Authentication Foundation

### Architecture

- Pattern: Clean Architecture with four layers
  - Domain: entities and enums
  - Application: DTOs, validators, interfaces, mappings, services
  - Infrastructure: EF Core DbContext, entity configurations, repositories, external services
  - API: controllers, middleware, startup configuration
- API base route: `/api`
- Current controller implemented:
  - `api/auth`

### Runtime Configuration

- Database provider:
  - runtime selector key: `DatabaseProvider`
  - supported values: `SqlServer`, `PostgreSQL`
  - production default: `SqlServer`
- Configured connection string keys:
  - `ConnectionStrings:SqlServer`
  - `ConnectionStrings:PostgreSQL`
- Startup validation:
  - `Program.cs` validates `DatabaseProvider` before service registration
  - startup hosted service `DatabaseStartupValidationHostedService` runs `context.Database.CanConnectAsync()`
  - startup log format: `Database provider: {Provider} - Connection: OK`
  - invalid provider or missing connection string throws an `InvalidOperationException` during startup
- PostgreSQL timestamp compatibility:
  - `Program.cs` sets `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` before creating the builder
- JWT settings:
  - Issuer: `GoWithFlow`
  - Audience: `GoWithFlowApp`
  - Access token expiry: 15 minutes
  - Refresh token expiry: 7 days
- OTP settings:
  - Expiry: 5 minutes
  - Max attempts: 3
- API versioning:
  - Route prefix: `/api` — `ApiRoutes.VersionPrefix = "api"` (no `/v1/` segment)
  - All controller routes use `api/{resource}/...` pattern (e.g. `api/auth`, `api/users`, `api/sessions`)
  - No URL-segment version number is used — routes do not include `/v1/` or any version fragment
- Logging:
  - Serilog console sink
  - Serilog file sink at `logs/gwf-.txt`
  - development minimum level `Debug`
  - production minimum level `Warning`
- Serilog enrichers:
  - machine name
  - thread id
  - request id
- CORS policies:
  - development uses credential-friendly wildcard origin matching with any header and method
  - production reads specific origins from the `AllowedOrigins` array in configuration
- JWT bearer SignalR support:
  - reads `access_token` from query string for `/hubs/session` and `/hubs/live-session`
- Rate limiting:
  - global fixed window `100 requests / minute / IP`
  - auth endpoints fixed window `10 requests / minute / IP`
- Memory caching:
  - script library list cache: 5 minutes
  - admin dashboard cache: 2 minutes
  - sample template cache: 30 minutes
- Health checks:
  - `/api/health`
  - `/api/health/db`
  - `/api/health/detailed`
- Production configuration:
  - `appsettings.Production.json` defines production SQL connection placeholder sourced from environment-backed deployment configuration
  - `JwtSettings:SecretKey` production placeholder requires an environment-provided secret with minimum 32 characters
  - `CorsSettings:AllowedOrigins` stores production frontend origins
  - `FileStorage` stores avatar upload path and max file size
- Frontend local integration configuration:
  - `Frontend/src/app/environments/environment.ts` uses `isDemo: false`
  - local API base URL: `https://localhost:44378/api`
  - local SignalR base URL: `https://localhost:44378`
  - login session cache persists `gwf_token`, `gwf_refreshToken`, `gwf_userId`, `gwf_role`, and `gwf_user` so guarded user routes can hydrate the user shell immediately after login
- Frontend runtime compatibility:
  - Angular `19.2.22` frontend runtime requires `zone.js ~0.15.1`
  - `Frontend/src/main.ts` imports `@angular/compiler` so runtime JIT-required Angular partial libraries can bootstrap instead of failing with `JIT compiler unavailable`
- Phase 9 composite index script:
  - source logic now lives under EF Core migrations in `Backend/GoWithFlow.Infrastructure/Data/Migrations`
  - adds filtered indexes for mistake, voice-analysis, session, session-member, and repractice query paths
- Dual-provider EF tooling:
  - design-time factory: `Backend/GoWithFlow.Infrastructure/Data/GoWithFlowDbContextFactory.cs`
  - provider-specific migration output directories: `Backend/GoWithFlow.Infrastructure/Migrations/SqlServer` and `Backend/GoWithFlow.Infrastructure/Migrations/PostgreSQL`
- SQL script source directory:
  - `Backend/GoWithFlow.Infrastructure/Data/Configurations`
  - `Backend/GoWithFlow.Infrastructure/Data/Migrations`
  - generated PostgreSQL output: `Docs/PostgreSQLMigration`
  - dual-provider validation script: `scripts/validate_dual_provider_contract.py`

## Backend Authentication Foundation — Dual-Provider Development Contract

### Entry Points
- `Backend/GoWithFlow.Infrastructure/Data/DbCommandHelper.cs`
- `Backend/Docs/PostgreSQLMigration/*.sql`
- `scripts/validate_dual_provider_contract.py`

### UI Trigger
- Backend/API development, repository changes, SQL routine changes, and PostgreSQL migration preparation

### Request Contract
Endpoint: `[VERIFY]` repository development contract, not an HTTP endpoint
Inputs:
  - provider-aware repository changes
  - SQL Server routine additions or edits
  - PostgreSQL migration or compatibility patch files

### Response Contract
Success:
  - SQL Server runtime remains callable through `dbo.usp*`
  - PostgreSQL runtime remains callable through `public.<lowercased-sql-server-name>`
  - PostgreSQL row-returning routines consumed by repositories expose provider-safe tabular contracts through `13_provider_safe_tabular_routines.sql`
  - PostgreSQL auth/user identity readers expose corrected `passwordhash VARCHAR(512)` contracts through `14_auth_user_result_contract_fixes.sql`
  - PostgreSQL output-parameter nonquery routines remain callable through `DbCommandHelper.ExecuteNonQueryAsync` without repository branching
  - no runtime `NextResultAsync()` dependency remains in API, application, or infrastructure code
  - `python3 scripts/validate_dual_provider_contract.py` exits `0`
  - `dotnet build Backend/GoWithFlow.API/GoWithFlow.API.csproj` succeeds
Failure:
  - missing PostgreSQL function alias, provider-unsafe return contract, or reintroduced multi-result reader pattern breaks runtime validation before provider switch or deployment

### Validation
- Every repository command using `CommandType.StoredProcedure` must execute through `DbCommandHelper.ExecuteReaderAsync`, `ExecuteScalarAsync`, or `ExecuteNonQueryAsync`
- Every SQL Server routine name referenced in infrastructure code must resolve to a latest active PostgreSQL function present in `Backend/Docs/PostgreSQLMigration/*.sql`
- The latest active PostgreSQL definition for every code-called routine must not return `REFCURSOR` or `SETOF REFCURSOR`
- Provider-safe rowset routines consumed by repositories must return `TABLE(...)`
- The latest active PostgreSQL definitions for `uspgetuserbymobilenumber`, `uspgetuserbyuserid`, and `uspgetuserdetailbyuserid` must expose `passwordhash VARCHAR(512)` to match `tblUser.PasswordHash`
- `NextResultAsync()` is not allowed in runtime code under `Backend/GoWithFlow.API`, `Backend/GoWithFlow.Application`, or `Backend/GoWithFlow.Infrastructure`
- PostgreSQL compatibility deltas must be added as new ordered SQL files instead of silently mutating historical migration files after drift is detected

### Database / Stored Procedures
Tables read: `[VERIFY]` validator inspects source and PostgreSQL migration files only
Tables written: `[VERIFY]` validator inspects source and PostgreSQL migration files only
Stored procedures:
  - SQL Server runtime keeps canonical names such as `dbo.uspGetUserByMobileNumber`
  - PostgreSQL runtime must expose matching function entry points for the lowercased SQL Server base names, even when the original generated file used pluralization or typo variants
  - PostgreSQL rowset routines previously emitted as cursor contracts in `06_stored_procedures.sql` and `12_dual_provider_routine_aliases.sql` are superseded by later `RETURNS TABLE(...)` definitions in `13_provider_safe_tabular_routines.sql`
  - PostgreSQL auth/user width corrections are appended in `14_auth_user_result_contract_fixes.sql`
  - PostgreSQL output-parameter routines such as `uspvalidatejoincode` and `uspinsertsession` are executed as `SELECT public.function(...)` and have output values hydrated back into `DbParameter` instances by `DbCommandHelper`
Key queries:
  - `scripts/validate_dual_provider_contract.py` compares code-called `dbo.usp*` names against the latest PostgreSQL `CREATE OR REPLACE FUNCTION` declarations and rejects active `REFCURSOR` contracts

### Business Rules
- Local SQL Server objects can be updated in the local database when needed; PostgreSQL compatibility changes must be prepared as new SQL files under `Backend/Docs/PostgreSQLMigration`
- The API and UI must remain provider-agnostic; provider-specific behavior belongs only in `DbCommandHelper`, migration files, or provider-aware EF configuration
- PostgreSQL routine-name compatibility wrappers are acceptable when SQL Server names and generated PostgreSQL names drift, but active runtime contracts must remain standard tabular functions rather than cursor wrappers
- Any SQL Server routine that originally returned multiple result sets must be redesigned for PostgreSQL as a tabular first query plus separate provider-safe follow-up queries; cursor emulation is not part of the supported runtime contract

### State Transitions
- New backend routine introduced in code → matching PostgreSQL function or alias must exist before PostgreSQL is considered runnable
- PostgreSQL drift detected by validator → create next ordered SQL patch file, update `ProjectOverview.md`, and keep the latest active function definition provider-safe
- Repository flow needs secondary datasets beyond the primary routine rowset → move those reads to separate provider-safe queries instead of `NextResultAsync()`

### Realtime Events
- Not applicable

### Failure Cases
- Repository command bypasses `DbCommandHelper` execution helpers → PostgreSQL provider can regress at runtime
- PostgreSQL migration file omits a code-called routine alias → runtime function lookup failure
- Historical PostgreSQL generated routine uses typo/pluralization drift → later compatibility file required before provider validation passes
- Latest active PostgreSQL routine still returns `REFCURSOR`/`SETOF REFCURSOR` → provider-safe validator failure
- Runtime code reintroduces `NextResultAsync()` → provider-safe validator failure

### Recovery / Fallback Logic
- Keep SQL Server as canonical backend contract when local DB logic is already validated
- Add PostgreSQL-only compatibility or tabular replacement functions in a new numbered migration file when generated function names or rowset contracts drift from the code-called SQL Server names
- Run `python3 scripts/validate_dual_provider_contract.py` and `dotnet build Backend/GoWithFlow.API/GoWithFlow.API.csproj` after every provider-sensitive change

### Notes on Known Drift Prevented
- PostgreSQL auth/runtime drift is no longer handled as one-off debugging; the validator now blocks missing routine aliases before provider rollout
- Future PostgreSQL fixes must be appended as new numbered files instead of editing already-applied local SQL Server artifacts
- Checked-in PostgreSQL migration SQL files plus `scripts/validate_dual_provider_contract.py` are the validated runtime contract; `scripts/generate_postgresql_migration.py` must be revalidated before it is used to regenerate routine files
- Cursor-based PostgreSQL routines originally generated from SQL Server multi-result procedures are no longer treated as a supported runtime contract; the stable contract is now latest-definition `RETURNS TABLE(...)` plus repository split queries
- Auth/user PostgreSQL result-width drift is also part of the validated runtime contract; active user-reader functions must match `tbluser.passwordhash VARCHAR(512)`

## Backend Authentication Foundation — PostgreSQL Provider-Safe Routine Contract

### Entry Points
- Any API/UI flow that reaches infrastructure routines through `CommandType.StoredProcedure`
- `Backend/GoWithFlow.Infrastructure/Data/DbCommandHelper.cs`
- `Backend/Docs/PostgreSQLMigration/13_provider_safe_tabular_routines.sql`
- `Backend/Docs/PostgreSQLMigration/14_auth_user_result_contract_fixes.sql`

### UI Trigger
- Indirect trigger from authentication, users, admin, repractice, reports, dashboard, session-history, script-library, and live-session flows

### Request Contract
Endpoint: `[VERIFY]` internal repository/runtime contract, not an HTTP endpoint
Headers: Not applicable
Body:
  - `DatabaseProvider` (`string`, required): `PostgreSQL` selects function-style execution through `DbCommandHelper`
  - routine name (`string`, required): SQL Server canonical `dbo.usp*`; PostgreSQL runtime resolves to `public.<lowercased-name>`
  - input parameters (`DbParameter[]`, required): normalized to PostgreSQL `p_*` names
  - output parameters (`DbParameter[]`, optional): allowed only for nonquery-style functions that return a single row with matching normalized output column names

### Response Contract
Success:
  - reader/scalar rowset contract: `SELECT * FROM public.function(...)` returns a single tabular dataset consumable through `DbDataReader`
  - nonquery no-output contract: `SELECT public.function(...)`
  - nonquery output-parameter contract: `DbCommandHelper.ExecuteNonQueryAsync` reads the single returned row and copies normalized columns back into output `DbParameter` values
  - repository flows that previously depended on `NextResultAsync()` now load secondary datasets through separate EF/repository queries
Error responses:
  - runtime validator failure: latest active PostgreSQL definition missing or provider-unsafe
  - runtime execution failure: output column names do not match normalized parameter names

### Validation
- `scripts/validate_dual_provider_contract.py` must pass
- `dotnet build Backend/GoWithFlow.API/GoWithFlow.API.csproj` must pass
- no `NextResultAsync()` usage may remain in runtime code
- latest active definitions for these code-called PostgreSQL rowset routines must return `TABLE(...)`:
  - `uspgetalluserbysearch`, `uspexportuserreportdata`, `uspgetmistakebyuseridwithfilter`, `uspgetrepracticesessionbyrepracticesessionid`, `uspgetrepracticesessionlistbyuserid`, `uspgetscriptbysearch`, `uspgetscriptdetailbyscriptid`, `uspgetsessionbyjoincode`, `uspgetsessionbysessionid`, `uspgetsessioncompletionsummary`, `uspgetsessiondetailbysessionid`, `uspgetsessionlistbyuserid`, `uspgetstreakdatabyuserid`, `uspgetuserdashboardsummarybyuserid`, `uspgetuserdetailbyuserid`, `uspgetuserfullreportbyuserid`, `uspgetuserreportsummarylist`
- latest active definitions for all code-called PostgreSQL routines must not return `REFCURSOR` or `SETOF REFCURSOR`
- latest active auth/user read definitions for `uspgetuserbymobilenumber`, `uspgetuserbyuserid`, and `uspgetuserdetailbyuserid` must declare `passwordhash VARCHAR(512)` so `RETURN QUERY` matches `tbluser.passwordhash`

### Database / Stored Procedures
Tables read:
  - Primary rowset routines still read application tables such as `tblUser`, `tblSession`, `tblSessionMember`, `tblScript`, `tblMistake`, `tblRepracticeSession`, `tblVoiceAnalysis`, `tblListenerFeedback`, and `tblUserStreak`
Tables written:
  - nonquery auth/session routines may still write through PostgreSQL functions; provider-safe change in this contract is execution shape, not table ownership
Stored procedures:
  - SQL Server canonical names remain unchanged in repository code
  - PostgreSQL active tabular overrides are defined in `13_provider_safe_tabular_routines.sql`
  - PostgreSQL auth/user contract overrides are defined in `14_auth_user_result_contract_fixes.sql`
  - legacy cursor-era definitions in `06_stored_procedures.sql` and `12_dual_provider_routine_aliases.sql` remain historical migration artifacts only; once a later-numbered file recreates the same function name, the latest definition becomes the runtime contract
Key queries:
  - session/dashboard/report secondary datasets now come from separate EF/repository queries instead of a second `DbDataReader` result set

### Business Rules
- Repository logic must stay provider-neutral; provider branching remains centralized in `DbCommandHelper`
- PostgreSQL support is based on standard tabular consumption only
- Future SQL Server multi-result procedures must be redesigned for PostgreSQL as one tabular routine plus separate follow-up queries or structured payloads before they are considered dual-provider safe
- PostgreSQL migration changes are append-only and must use a new ordered file

### State Transitions
- Historical cursor-based routine detected in active runtime path → add a new PostgreSQL patch file with a later tabular definition
- Repository introduces a second result-set dependency → refactor to separate provider-safe queries before release
- Output-parameter routine added or changed → `DbCommandHelper` output hydration contract must remain aligned with normalized `p_*` column names

### Realtime Events
- Not applicable

### Failure Cases
- Code-called routine name missing in latest PostgreSQL migration set → runtime/validator failure
- Latest active PostgreSQL routine returns `REFCURSOR` or `SETOF REFCURSOR` → validator failure
- Runtime code uses `NextResultAsync()` → validator failure
- Output-parameter PostgreSQL function returns columns that do not match normalized output parameter names → runtime failure

### Recovery / Fallback Logic
- Keep SQL Server canonical procedure names in repositories
- Add the next numbered PostgreSQL patch file with `RETURNS TABLE(...)` replacements when a cursor-era routine is still active
- Load secondary datasets through separate provider-safe queries rather than cursor emulation
- Re-run provider validation and build before claiming PostgreSQL support

### Notes on Known Drift Prevented
- Login/runtime drift was initially caused by invoking PostgreSQL functions with SQL Server procedure semantics; `DbCommandHelper` now rewrites those calls
- Name-alias fixes in `12_dual_provider_routine_aliases.sql` solved lookup drift but did not solve cursor-based rowset drift
- `13_provider_safe_tabular_routines.sql` is the stable provider-safe contract for the affected rowset routines, while repository code now avoids `NextResultAsync()`
- `14_auth_user_result_contract_fixes.sql` corrects the auth/user return-width drift where older PostgreSQL functions still declared `passwordhash VARCHAR(256)` even though `tbluser.passwordhash` is `VARCHAR(512)`

### Database Schema

#### tblUser

- Primary key: `UserId BIGINT IDENTITY(1,1)`
- Business columns:
  - `FullName NVARCHAR(128) NOT NULL`
  - `MobileNumber NVARCHAR(16) NOT NULL`
  - `Email NVARCHAR(128) NULL`
  - `PasswordHash NVARCHAR(512) NULL`
  - `AgeGroup NVARCHAR(32) NOT NULL`
  - `PreferredHintLanguage NVARCHAR(32) NOT NULL`
  - `AvatarUrl NVARCHAR(256) NULL`
  - `GroupCode NVARCHAR(32) NULL`
  - `Role NVARCHAR(16) NOT NULL DEFAULT('USER')`
  - `DailyStreakCount INT NOT NULL DEFAULT(0)`
  - `TotalSessionsPlayed INT NOT NULL DEFAULT(0)`
  - `LastLoginDate DATETIME2 NULL`
  - `IsActive BIT NOT NULL DEFAULT(1)`
  - `RegistrationDate DATETIME2 NOT NULL DEFAULT(GETDATE())`
- Audit columns: `Tag`, `Comments`, `SortOrder`, `IPAddress`, `CreatedBy`, `DateCreated`, `UpdatedBy`, `LastUpdated`, `DeletedBy`, `DateDeleted`, `IsDeleted`
- Constraints: `PK_tblUser_UserId`, `UK_tblUser_MobileNumber`
- **Reserved system user (Phase 17 — AI Voice Participant):** one seeded row with `MobileNumber = 'AI_PARTICIPANT'`, `Role = 'SYSTEM'`, `FullName = 'AI Voice Participant'`. Backs the FK of every AI-held `tblSessionMember` (`IsAi = 1`) so the existing `UserId` FK never needs to be nullable. **Resolve it by the sentinel `MobileNumber`, never by a hard-coded id** — `UserId` is `IDENTITY`/`BIGSERIAL` and differs per environment. Seeded idempotently by both Phase 17 migrations.

#### tblRefreshToken

- Primary key: `RefreshTokenId BIGINT IDENTITY(1,1)`
- Business columns:
  - `UserId BIGINT NOT NULL`
  - `Token NVARCHAR(512) NOT NULL`
  - `ExpiresAt DATETIME2 NOT NULL`
  - `IsRevoked BIT NOT NULL DEFAULT(0)`
  - `RevokedAt DATETIME2 NULL`
  - `DeviceInfo NVARCHAR(256) NULL`
- Audit columns: standard
- Constraints and indexes: `PK_tblRefreshToken_RefreshTokenId`, `FK_tblRefreshToken_UserId_tblUser_UserId`, `IDX_tblRefreshToken_UserId`

#### Live SQL Server tables in `GoWithFlowDB`

`tblAdminNote`, `tblDashboardMetric`, `tblListenerFeedback`, `tblMistake`, `tblRefreshToken`, `tblRepracticeSession`, `tblRepracticeUtterance`, `tblScript`, `tblScriptVersion`, `tblSession`, `tblSessionMember`, `tblTurnState`, `tblUser`, `tblUserBadge`, `tblUserStreak`, `tblUtterance`, `tblVoiceAnalysis`

#### Removed live table

- `tblOtpVerification` was dropped by EF migration `20260517000000_RemoveOtpVerification`
- live SQL Server no longer contains the table even though residual OTP stored procedures still exist in the catalog

#### User-defined types

- `dbo.UtteranceTVP`

### Stored Procedures

- User: `uspInsertUser`, `uspGetUserByMobileNumber`, `uspGetUserByUserId`, `uspUpdateUserLastLogin`, `uspSoftDeleteUser`
- OTP catalog drift: `uspInsertOtpVerification`, `uspVerifyOtp` still exist in SQL Server, but their backing table `tblOtpVerification` is no longer present; treat the OTP persistence contract as `[VERIFY]`
- Refresh token: `uspInsertRefreshToken`, `uspGetRefreshTokenByToken`, `uspRevokeRefreshToken`
- Admin/reporting: `uspInsertAdminNote`, `uspGetAdminAnalyticsOverview`, `uspGetAdminDashboardSummary`, `uspGetAdminNoteByTargetUserId`, `uspGetAllUserBySearch`, `uspGetRecentActivityList`, `uspGetUserDetailByUserId`, `uspGetUserReportSummaryList`, `uspGetUserFullReportByUserId`, `uspExportUserReportData`
- Script/library: `uspInsertScript`, `uspInsertScriptVersion`, `uspBulkInsertUtterance`, `uspInsertUtterance`, `uspCheckScriptTitleExists`, `uspSoftDeleteScriptByScriptId`, `uspUpdateScriptActiveStatusByScriptId`, `uspUpdateScriptUtteranceCount`, `uspGetScriptBySearch`, `uspGetScriptDetailByScriptId`, `uspGetScriptVersionHistoryByScriptId`
- Session/lobby: `uspInsertSession`, `uspInsertSessionMember`, `uspGetAvailableSlotsBySessionId`, `uspGetSessionByJoinCode`, `uspGetSessionBySessionId`, `uspGetSessionDetailBySessionId`, `uspGetSessionListByUserId`, `uspUpdateSessionMemberLeft`, `uspUpdateSessionMemberReadyStatus`, `uspUpdateSessionStatus`, `uspValidateJoinCode`
- Live session: `uspInsertTurnState`, `uspInsertListenerFeedback`, `uspInsertVoiceAnalysis`, `uspGetCurrentTurnBySessionId`, `uspGetListenerFeedbackBySessionId`, `uspGetSessionCompletionSummary`, `uspGetTopPerformerListBySessionId`, `uspGetVoiceAnalysisBySessionId`, `uspGetVoiceAnalysisByUserId`, `uspIncrementReReadCount`, `uspUpdateTurnStatusByTurnStateId`
- User analytics/profile: `uspCalculateImprovementPercentByUserId`, `uspCheckAndAwardBadge`, `uspGetAnalyticsSummaryByUserId`, `uspGetImprovementDataByUserId`, `uspGetStreakDataByUserId`, `uspGetUserBadgeByUserId`, `uspGetUserDashboardSummaryByUserId`, `uspGetUserProfileByUserId`, `uspGetWeeklyFluencyScoreByUserId`, `uspInsertUserBadge`, `uspUpsertUserStreak`, `uspUpdateUserActiveStatusByUserId`, `uspUpdateUserProfile`
- Mistake/repractice: `uspInsertMistake`, `uspInsertRepracticeSession`, `uspInsertRepracticeUtterance`, `uspGetAllMistakeTypeCountByUserId`, `uspGetGrammarProgressByUserId`, `uspGetMistakeByUserIdWithFilter`, `uspGetMistakeSummaryByUserId`, `uspGetRepracticeSessionByRepracticeSessionId`, `uspGetRepracticeSessionListByUserId`, `uspGetTopGrammarMistakeType`, `uspGetUnresolvedMistakeByUserId`, `uspUpdateRepracticeSessionStatus`, `uspUpdateRepracticeUtteranceAttempt`

### Domain Model

- Live persistence entities in this foundation: `User`, `RefreshToken`
- OTP persistence entity status: `[VERIFY]` SQL table removed from live schema; do not treat `OtpVerification` as a validated database contract
- Shared base entity: `BaseAuditEntity`
- Enums: `AgeGroupType`, `PreferredHintLanguageType`, `UserRoleType`

### API Surface

#### POST /api/auth/login

- Request DTO: `LoginRequestDto` — fields: `MobileNumber`, `Password`
- Authenticates the user via `uspGetUserByMobileNumber`
- returns access token, refresh token, expiry, and cached profile shell fields

#### POST /api/auth/register

- Request DTO: `RegisterRequestDto` — fields: `FullName`, `MobileNumber`, `Email`, `AgeGroup`, `PreferredHintLanguage`, `AvatarUrl`
- Inserts user via `uspInsertUser`

#### POST /api/auth/refresh-token

- Request DTO: `RefreshTokenRequestDto` — fields: `RefreshToken`
- Revokes old token, issues new access and refresh tokens

#### POST /api/auth/logout

- Authorization: required. Request: `RefreshToken`. Revokes refresh token.

#### GET /api/health, /api/health/db, /api/health/detailed

### Request and Response Contracts

- Request DTOs: `LoginRequestDto`, `RegisterRequestDto`, `RefreshTokenRequestDto`
- Response DTOs: `AuthResponseDto`, `UserProfileResponseDto`
- Standard response wrapper: `ApiResponse<T>` — fields: `Success`, `Message`, `Data`, `Errors`

### Validation Rules

- `LoginRequestDto`: `MobileNumber` required exactly 10 digits; `Password` required minimum 6 characters
- `RegisterRequestDto`: `FullName` required len 2-60; `MobileNumber` required exactly 10 digits; `Email` optional email format; `AgeGroup` enum only; `PreferredHintLanguage` enum only; `AvatarUrl` optional max 256

### Authentication Business Logic

- JWT claims: `UserId`, `FullName`, `Role`, `MobileNumber`; 15-minute expiry
- Frontend post-login: `ADMIN` → `/admin/dashboard`; `USER` → `/user/dashboard`; dashboard greeting falls back to `Member` if cache not ready
- Refresh token: secure random, 7-day expiry, persisted in `tblRefreshToken`
- Role defaults to `USER` on registration
- `uspUpdateUserLastLogin` updates `LastLoginDate`, `UpdatedBy`, `LastUpdated` only — streak is managed separately by `uspUpsertUserStreak`
- OTP persistence remains `[VERIFY]` only; no live `AuthController` endpoint currently exposes OTP send/verify flows

### Infrastructure Wiring

- DbContext: `GoWithFlowDbContext`
- Entity configurations present in repo for this foundation: `UserConfiguration`, `RefreshTokenConfiguration`
- provider-aware model conventions:
  - `ColumnTypeHelper` centralizes SQL Server vs PostgreSQL store types for money/decimal, large text, JSON, dates, timestamps, and filtered indexes
  - `ModelBuilderExtensions.ApplyProviderConventions` remaps EF model metadata after `ApplyConfigurationsFromAssembly`
- provider-aware command wiring:
  - `DbCommandHelper` builds provider-specific `DbParameter` instances and maps SQL Server procedure names such as `dbo.uspGetUserByUserId` to PostgreSQL function names such as `public.uspgetuserbyuserid`
  - PostgreSQL migration output exposes auth routines as `CREATE OR REPLACE FUNCTION`, so `DbCommandHelper.ExecuteReaderAsync`, `ExecuteScalarAsync`, and `ExecuteNonQueryAsync` rewrite `CommandType.StoredProcedure` calls into explicit PostgreSQL `SELECT` statements at execution time
- startup connectivity guard:
  - `DatabaseStartupValidationHostedService` fails app boot if the selected provider cannot connect
- Repositories and services touching OTP persistence should be treated as `[VERIFY]` until the removed table contract is reconciled
- External services: `JwtService`, `OtpService`, `ExcelExportService`
- Middleware: `ExceptionMiddleware`
- Authorization policies: `AdminOnly`, `UserOrAdmin`, `ActiveUser`
- Real-time identity: `JwtUserIdProvider`, `HubConnectionTracker`

## Backend Authentication Foundation — Login Flow

### Entry Points
- Frontend login screen route: `/auth/login`
- Optional query parameter: `mobile` pre-fills the `mobileNumber` field when present
- API endpoint: `POST /api/auth/login`

### UI Trigger
- Login form submit button in `Frontend/src/app/modules/auth/login/login.component.html`
- On component init, `ActivatedRoute.queryParams` seeds `mobileNumber` from `?mobile=`

### Request Contract
Endpoint: `POST /api/auth/login`
Headers: `Content-Type: application/json`
Body:
  - `mobileNumber` (string, required): exactly 10 digits
  - `password` (string, required): minimum 6 characters

### Response Contract
Success (`200 OK`):
  - `success` (boolean): `true`
  - `message` (string): `Login successful.`
  - `data.accessToken` (string): JWT access token
  - `data.refreshToken` (string): server-issued refresh token
  - `data.expiresIn` (int): access-token lifetime in seconds
  - `data.userId` (long): authenticated user id
  - `data.fullName` (string): authenticated user display name
  - `data.role` (string): `ADMIN` or `USER`
  - `data.avatarUrl` (string|null): cached avatar URL
Error responses:
  - `401`: invalid mobile/password, inactive account, or refresh/authentication failure body wrapped in `ApiResponse<AuthResponseDto>`
  - `400`: request validation failure body wrapped in `ApiResponse<AuthResponseDto>`
  - `500`: provider/routine execution failure when PostgreSQL functions are invoked with procedure semantics
  - `500`: PostgreSQL routine contract failure when an active auth/user function declares a result width that does not match `tblUser` column types, e.g. `42804` on `RETURN QUERY`

### Validation
- `mobileNumber` must match `^\d{10}$`
- `password` must be present and at least 6 characters
- Authentication fails if the resolved user has no `PasswordHash`

### Database / Stored Procedures
Tables read: `tblUser`
Tables written: `tblUser`, `tblRefreshToken`
Stored procedures: `uspGetUserByMobileNumber` — load active user by mobile number; `uspUpdateUserLastLogin` — stamp `LastLoginDate`, `UpdatedBy`, `LastUpdated`, `IPAddress`; `uspInsertRefreshToken` — persist newly issued refresh token row
Key queries: PostgreSQL deployment stores these routines as `public.uspgetuserbymobilenumber`, `public.uspupdateuserlastlogin`, and `public.uspinsertrefreshtoken` functions; the latest active `uspgetuserbymobilenumber` definition must expose `passwordhash VARCHAR(512)` to match `tbluser.passwordhash`

### Business Rules
- Lookup user by `MobileNumber`
- Reject login when the user record is missing, `PasswordHash` is blank, or PBKDF2 password verification fails
- Reject login when `IsActive = false`
- On success, update `LastLoginDate`
- Generate a JWT access token and a new refresh token
- Persist the refresh token with `DeviceInfo = Web`
- Return token payload and profile shell fields for frontend session caching
- The login view is a single-screen auth surface: `login.component.scss` uses `min-height: 100dvh` with `box-sizing: border-box` and clamp-based spacing so the branded shell fits the active device viewport without adding first-load page scroll under normal desktop and phone heights

### State Transitions
- `tblUser.LastLoginDate` → previous timestamp or `NULL` → `NOW()` after successful login
- `tblRefreshToken.IsRevoked` → new row inserted with `false`

### Realtime Events
- Not applicable for login flow

### Failure Cases
- User not found → `401` → `ApiResponse` with `Authentication failed.` / `Invalid mobile number or password.`
- Password hash missing → `401` → `ApiResponse` with `Authentication failed.` / `Invalid mobile number or password.`
- Password verification mismatch → `401` → `ApiResponse` with `Authentication failed.` / `Invalid mobile number or password.`
- Inactive account → `401` → `ApiResponse` with `Authentication failed.` / `Account is inactive.`
- PostgreSQL routine invoked as a procedure instead of a function → `500` → `Npgsql.PostgresException` (`42883`)
- PostgreSQL auth/user function returns a `passwordhash` width that does not match `tbluser.passwordhash` → `500` → `Npgsql.PostgresException` (`42804`)

### Recovery / Fallback Logic
- SQL Server path can continue using `CommandType.StoredProcedure`
- PostgreSQL path must execute migrated routines as functions via `SELECT * FROM public.routine(...)` for result sets and `SELECT public.routine(...)` for void routines
- Apply `14_auth_user_result_contract_fixes.sql` after `13_provider_safe_tabular_routines.sql` so auth and user-read functions use the same `passwordhash VARCHAR(512)` contract as `tbluser`
- Frontend should treat `401` responses as credential/account failures and avoid retry loops
- On shorter viewports, the login shell reduces outer padding, brand scale, card padding, and form gaps through CSS `clamp(...)` sizing and a `max-height: 760px` rule instead of relying on body scroll to reveal the form

### Notes on Known Drift Prevented
- `ProjectOverview` previously omitted the `/api/auth/login` flow contract even though the controller and validator are live
- `ProjectOverview` previously documented PostgreSQL routine names with underscores; the generated migration actually emits lowercase function names without underscores
- Live PostgreSQL auth failures occurred because application code used stored-procedure invocation semantics against function-based migration output; provider-aware execution now rewrites those calls before execution
- Live PostgreSQL auth failures also occurred when an active function returned `passwordhash VARCHAR(256)` against `tbluser.passwordhash VARCHAR(512)`; the fix is appended in `14_auth_user_result_contract_fixes.sql`
- The frontend login shell previously used `min-height: 100vh` plus vertical padding, which produced unnecessary page scroll on compact viewports; the stable contract is now a device-fit `100dvh` shell with responsive spacing
- **Session persistence on Android (2026-06-04):** App always showed login on cold start even when the user was logged in. Root cause: `app.routes.ts` empty path `''` had `redirectTo: 'auth/login'` unconditionally — no token check. Fix: replaced with `autoLoginGuard` that reads `localStorage.getItem('gwf_token')` — if present, redirects to `/admin/dashboard` or `/user/dashboard` based on role; if absent, redirects to `/auth/login`. Tokens are correctly stored in `localStorage` by `modules/auth/auth.service.setSession()` on login — the issue was routing-only, not storage.

## Backend Authentication Foundation — Database Provider Selection and Startup Validation

### Entry Points
- `Backend/GoWithFlow.API/Program.cs`
- `Backend/GoWithFlow.API/appsettings.json`
- `Backend/GoWithFlow.API/appsettings.Development.json`
- `Backend/GoWithFlow.Infrastructure/Data/GoWithFlowDbContext.cs`

### UI Trigger
- API host startup only; no frontend trigger

### Request Contract
- Endpoint: `[VERIFY]` configuration-driven startup flow, not an HTTP endpoint
- Required configuration:
  - `DatabaseProvider` (`string`, required): must be `SqlServer` or `PostgreSQL`
  - `ConnectionStrings:SqlServer` (`string`, required when `DatabaseProvider=SqlServer`)
  - `ConnectionStrings:PostgreSQL` (`string`, required when `DatabaseProvider=PostgreSQL`)

### Response Contract
- Success:
  - registers `GoWithFlowDbContext` with `UseSqlServer(...)` or `UseNpgsql(...)`
  - logs `Database provider: {Provider} - Connection: OK`
  - exposes the selected provider to `GoWithFlowDbContext.DatabaseProvider`
- Failure:
  - throws `InvalidOperationException` during startup for invalid provider values, missing connection strings, or failed connectivity

### Validation
- `DatabaseProviderNames.Normalize(...)` rejects any provider other than `SqlServer` or `PostgreSQL`
- startup hosted service calls `context.Database.CanConnectAsync()` before the app begins serving requests
- PostgreSQL startup enables `Npgsql.EnableLegacyTimestampBehavior` before the web application builder is created

### Database / Stored Procedures
- DbContext runtime provider:
  - SQL Server: `Microsoft.EntityFrameworkCore.SqlServer`
  - PostgreSQL: `Npgsql.EntityFrameworkCore.PostgreSQL`
- Design-time migration factory:
  - `GoWithFlowDbContextFactory` supports `--provider SqlServer` and `--provider PostgreSQL`
- Routine naming contract:
  - SQL Server repositories call `dbo.usp*`
  - PostgreSQL repositories map those names to `public.usp*` using the lowercased SQL Server base name, e.g. `dbo.uspGetUserByMobileNumber` → `public.uspgetuserbymobilenumber`
- Raw SQL removed from active repository paths:
  - `RefreshTokenRepository.GetByTokenAsync` now uses EF LINQ
  - `SessionRepository.CheckJoinCodeStatusAsync` now uses EF LINQ

### Business Rules
- SQL Server remains the default provider and production path
- PostgreSQL is selected only by configuration; no controller or application-layer code changes are required
- EF model metadata is provider-aware after configuration assembly load:
  - `DateTime` columns map to `datetime2` or `timestamp without time zone`
  - `DateOnly` columns map to `date`
  - decimal score columns retain decimal precision per provider
  - JSON string columns ending in `Json` map to `nvarchar(max)` or `jsonb`
  - filtered indexes on `IsDeleted` / `IsActive` are rewritten per provider
- Repository helpers create provider-specific `DbParameter` instances instead of hardcoded `SqlParameter` objects
- script bulk utterance insert uses a SQL Server TVP for SQL Server and JSONB payload parameter for PostgreSQL

### State Transitions
- startup state:
  - configuration loaded → provider normalized → DbContext registered → connectivity verified → app starts serving requests

### Failure Cases
- unsupported `DatabaseProvider` value → startup exception
- missing `ConnectionStrings:{Provider}` entry → startup exception
- selected provider cannot connect → startup exception naming the failed provider
- PostgreSQL database missing the mapped lowercase `public.usp*` functions or equivalent logic → repository execution failure at runtime `[VERIFY]`

### Recovery / Fallback Logic
- switch `DatabaseProvider` back to `SqlServer` to retain current production behavior
- keep both provider connection strings populated in shared config so environment overrides change only the provider selector
- generate future migrations into provider-specific output folders with the `--provider` design-time argument

### Notes on Known Drift Prevented
- runtime config previously documented only `ConnectionStrings:DefaultConnection`; docs now reflect separate SQL Server and PostgreSQL connection keys
- startup previously described SQL Server-only `UseSqlServer` wiring; docs now capture the configuration-driven provider switch and live connectivity validation
- repository helpers previously assumed SQL Server `SqlParameter` and `dbo.` routine naming; docs now record the provider-aware parameter factory, lowercase PostgreSQL routine-name mapping, and function-style execution path
- `RefreshTokenRepository.GetByTokenAsync` and `SessionRepository.CheckJoinCodeStatusAsync` no longer depend on SQL Server-only raw SQL

## Backend Authentication Foundation — Supabase PostgreSQL Migration Contract

### Entry Points
- Local SQL Server source database: `GoWithFlowDB` on `(localdb)\MSSQLLocalDB`
- Generated PostgreSQL migration files: `Docs/PostgreSQLMigration/01_extensions.sql` through `Docs/PostgreSQLMigration/14_auth_user_result_contract_fixes.sql`

### UI Trigger
- Operator-run migration flow; no frontend trigger

### Request Contract
- Endpoint: `[VERIFY]` offline database migration, not an HTTP endpoint
- Inputs:
  - source catalog: live SQL Server tables, keys, indexes, TVP metadata, stored procedure inventory
  - target runtime: Supabase PostgreSQL with RLS enabled

### Response Contract
- Success artifact set:
  - `01_extensions.sql`
  - `02_schema.sql`
  - `03_constraints_indexes.sql`
  - `04_views.sql`
  - `05_functions.sql`
  - `06_stored_procedures.sql`
  - `07_triggers.sql`
  - `08_seed_data.sql`
  - `09_sequences_reset.sql`
  - `10_rls_policies.sql`
  - `11_auth_routine_fixes.sql`
  - `12_dual_provider_routine_aliases.sql`
  - `13_provider_safe_tabular_routines.sql`
  - `14_auth_user_result_contract_fixes.sql`
- Error state:
  - application code calling migrated PostgreSQL functions with stored-procedure semantics triggers runtime `42883` errors because PostgreSQL expects function `SELECT` invocation, not `CALL`

### Validation
- Live SQL Server object inventory validated:
  - tables: `17` application tables plus `__EFMigrationsHistory` omitted from PostgreSQL target
  - views: `0`
  - scalar/table-valued functions: `0`
  - stored procedures: `79`
  - triggers: `0`
  - non-PK / non-unique-constraint indexes: `33`
- PostgreSQL identifier normalization:
  - generated schema objects are lowercase
  - generated routine names lower-case the SQL Server base name without underscore insertion, e.g. `dbo.uspGetUserByMobileNumber` → `public.uspgetuserbymobilenumber`

### Database / Stored Procedures
- Tables read from live SQL Server:
  - `tblAdminNote`, `tblDashboardMetric`, `tblListenerFeedback`, `tblMistake`, `tblRefreshToken`, `tblRepracticeSession`, `tblRepracticeUtterance`, `tblScript`, `tblScriptVersion`, `tblSession`, `tblSessionMember`, `tblTurnState`, `tblUser`, `tblUserBadge`, `tblUserStreak`, `tblUtterance`, `tblVoiceAnalysis`
- Tables intentionally excluded from PostgreSQL target:
  - `__EFMigrationsHistory`
- Stored procedure handling:
  - live SQL Server inventory captured in `06_stored_procedures.sql`
  - SQL Server procedures are rewritten as `CREATE OR REPLACE FUNCTION` definitions under `public`
  - parameter names are normalized to PostgreSQL-style `p_...`
  - application callers must invoke migrated routines as PostgreSQL functions
  - auth compatibility correction: `11_auth_routine_fixes.sql` adds the correctly named `usprevokerefreshtoken(...)` function because `06_stored_procedures.sql` emitted `usprovkerefreshtoken(...)`
  - routine alias correction: `12_dual_provider_routine_aliases.sql` adds PostgreSQL wrappers for code-called names that drifted in pluralization, abbreviation, or typos inside `06_stored_procedures.sql`
  - provider-safe rowset correction: `13_provider_safe_tabular_routines.sql` recreates code-called cursor-era entry points as `RETURNS TABLE(...)` functions so the existing `DbDataReader` execution path remains provider-neutral
  - auth/user width correction: `14_auth_user_result_contract_fixes.sql` recreates `uspgetuserbymobilenumber`, `uspgetuserbyuserid`, and `uspgetuserdetailbyuserid` with `passwordhash VARCHAR(512)` so `RETURN QUERY` matches `tbluser.passwordhash`
- User-defined SQL Server table type:
  - `dbo.UtteranceTVP` detected; PostgreSQL routine definitions mark it for JSONB or composite-type redesign

### Business Rules
- Schema migration is ready for PostgreSQL for tables, keys, filtered indexes, sequence resets, and Supabase RLS bridge setup
- Auth and application routines in `06_stored_procedures.sql` are executable PL/pgSQL functions, but rowset routines originally emitted as cursor contracts are not treated as provider-safe until a later migration file replaces them with tabular definitions
- Supabase role mapping uses `public.user_auth_map(auth_user_id uuid, user_id bigint)` so `auth.uid()` can resolve legacy bigint `tblUser.UserId`

### State Transitions
- SQL Server source state:
  - current live schema has no `tblOtpVerification`
  - SQL catalog still retains `uspInsertOtpVerification` and `uspVerifyOtp`
- PostgreSQL target state:
  - schema, FK, index, sequence, and RLS artifacts can be applied in ordered files
  - migrated routines are callable only as PostgreSQL functions, not SQL Server-style stored procedures

### Failure Cases
- `tblOtpVerification` contract drift → OTP procedures cannot be trusted as live database logic
- SQL Server-specific constructs in procedures (`OPENJSON`, `TOP`, `NOLOCK`, TVPs, local transaction control, `SCOPE_IDENTITY`, date conversion helpers) → direct production conversion not validated
- Supabase auth bridge not backfilled → RLS ownership checks will not resolve `auth.uid()` to legacy users

### Recovery / Fallback Logic
- Use generated schema/index/sequence/RLS files as the structural migration baseline
- Invoke migrated routines with PostgreSQL function syntax from application code or compatibility helpers before exposing live API traffic
- Apply `11_auth_routine_fixes.sql` after `10_rls_policies.sql` so refresh-token revoke/logout paths have the correctly named PostgreSQL auth function
- Apply `12_dual_provider_routine_aliases.sql` after `11_auth_routine_fixes.sql` so PostgreSQL exposes the exact routine names used by the API repositories
- Apply `13_provider_safe_tabular_routines.sql` after `12_dual_provider_routine_aliases.sql` so cursor-era code-called rowset routines are replaced by provider-safe tabular contracts
- Apply `14_auth_user_result_contract_fixes.sql` after `13_provider_safe_tabular_routines.sql` so auth and user-read routines align with the live `tblUser.PasswordHash` width
- Backfill `public.user_auth_map` immediately after importing users so RLS policies can resolve ownership

### Notes on Known Drift Prevented
- `ProjectOverview` previously documented `Docs/Database/...` SQL directories; live repo now uses EF Core configurations and migrations plus generated `Docs/PostgreSQLMigration`
- `ProjectOverview` previously documented `tblOtpVerification` as live; EF migration `20260517000000_RemoveOtpVerification` removed the table
- SQL Server catalog still contains OTP stored procedures after the table drop; this is documented as catalog drift instead of a valid live contract
- `Migration State` previously stopped at `InitialCreate_Phase1`; live repo now includes migrations through `20260523000001_RelaxImprovementSP_IncludeAbandoned`
- `ProjectOverview` previously documented PostgreSQL stored-procedure stubs and underscore-delimited routine names; live migration file contains executable lowercase `CREATE OR REPLACE FUNCTION` entries such as `public.uspgetuserbymobilenumber`
- `06_stored_procedures.sql` currently misspells `uspRevokeRefreshToken` as `usprovkerefreshtoken`; `11_auth_routine_fixes.sql` adds the correctly named function expected by app code
- `06_stored_procedures.sql` also drifted on `uspGetAllUserBySearch`, `uspGetMistakeByUserIdWithFilter`, `uspGetRepracticeSessionByRepracticeSessionId`, `uspGetTopGrammarMistakeType`, and `uspGetUserFullReportByUserId`; `12_dual_provider_routine_aliases.sql` restores the exact provider-facing function names expected by infrastructure code
- `scripts/generate_postgresql_migration.py` is not the validated provider contract for routine naming/output; use the checked-in SQL files and validator as the runtime source of truth until the generator is aligned

### Migration State

- `20260516031205_InitialCreate_Phase1`
- `20260516034202_AddAdminModule_Phase2`
- `20260516041338_AddScriptModule_Phase3`
- `20260516043747_AddSessionModule_Phase4`
- `20260516065000_AddLiveSessionModule_Phase5`
- `20260516145818_AddMistakeModule_Phase6`
- `20260516071000_AddUserModule_Phase7`
- `20260517000000_RemoveOtpVerification`
- `20260518000001_AddSessionPreviewSPs`
- `20260519000001_FixSlotIndexCastInSessionSPs`
- `20260520000001_FixSessionJoinAndLobbyStateSPs`
- `20260521000001_AddStatusToLobbyStateSP`
- `20260522000001_FixMemberLeftSP_ResetIsReady`
- `20260523000001_RelaxImprovementSP_IncludeAbandoned`

---

## Backend User Module

### Module Scope

- Self-service user APIs under `/api/users`
- Dedicated dashboard API under `/api/dashboard`
- Profile read/update and avatar upload
- Session detail drill-down for the authenticated user
- Improvement dashboard composed from session analytics, weekly trends, grammar progress, repractice history, streaks, and badges
- Session completion hook that updates practice streaks and awards badges

### Database Schema

#### tblUserStreak

- Primary key: `UserStreakId BIGINT IDENTITY(1,1)`
- Business columns: `UserId BIGINT NOT NULL`, `StreakDate DATE NOT NULL`, `SessionCount INT NOT NULL DEFAULT(0)`, `PracticeMinutes INT NOT NULL DEFAULT(0)`
- Constraints: `PK_tblUserStreak_UserStreakId`, `FK_tblUserStreak_UserId_tblUser_UserId`, `UK_tblUserStreak_UserId_StreakDate`, `IDX_tblUserStreak_UserId`

#### tblUserBadge

- Primary key: `UserBadgeId BIGINT IDENTITY(1,1)`
- Business columns: `UserId BIGINT NOT NULL`, `BadgeCode NVARCHAR(64) NOT NULL`, `BadgeName NVARCHAR(128) NOT NULL`, `EarnedDate DATETIME2 NOT NULL DEFAULT(GETDATE())`
- Constraints: `PK_tblUserBadge_UserBadgeId`, `FK_tblUserBadge_UserId_tblUser_UserId`, `UK_tblUserBadge_UserId_BadgeCode`, `IDX_tblUserBadge_UserId`

### Stored Procedures

- User profile: `uspGetUserProfileByUserId`, `uspUpdateUserProfile`
- Streaks and badges: `uspUpsertUserStreak`, `uspGetStreakDataByUserId`, `uspInsertUserBadge`, `uspGetUserBadgeByUserId`, `uspCheckAndAwardBadge`
- Session analytics: `uspGetUserDashboardSummaryByUserId`, `uspGetAllMistakeTypeCountByUserId`, `uspGetAnalyticsSummaryByUserId`, `uspGetSessionDetailBySessionId`, `uspGetImprovementDataByUserId`, `uspGetWeeklyFluencyScoreByUserId`

### Domain Model

- Entities: `UserStreak`, `UserBadge`
- Entity configurations: `UserStreakConfiguration`, `UserBadgeConfiguration`
- DbContext: `DbSet<UserStreak> UserStreaks`, `DbSet<UserBadge> UserBadges`

### Request and Response Contracts

- Request DTOs: `UpdateProfileRequestDto`
- Response DTOs: `UserDashboardResponseDto`, `ActiveSessionBannerDto`, `UserProfileResponseDto`, `SessionDetailResponseDto`, `ImprovementDataResponseDto`, `StreakDataResponseDto`, `UserBadgeDto`

### API Surface

#### GET /api/dashboard

- Returns: `UserDashboardResponseDto` — `userName`, `currentStreak`, `todayDate`, `pendingRepracticeCount`, `recentSessions` (`SessionListItemResponseDto[]`), `pendingMistakes` (`MistakeResponseDto[]`)
- `recentSessions` fields in use: `sessionId`, `sessionName`, `sessionMode`, `sessionDate`, `duration`, `fluencyScore` (decimal — must use `number:'1.0-1'` pipe), `status`, `scriptTitle`
- `pendingMistakes` fields in use: `mistakeId`, `mistakeType`, `grammarTag`, `contextTag`, `spokenText`, `utteranceText`, `mistakeDetail`, `sessionName`, `scriptTitle`, `firstOccurrence`
- Frontend `getDashboard()` in `UserService` passes `r.data` through without mapping — all fields are accessed directly from the API response shape using camelCase JSON names
- **Notes on Drift (2026-06-03):** Template used `session.createdDate` (non-existent — should be `session.sessionDate`) and `session.myScore` (non-existent — should be `session.fluencyScore`). Pending mistakes used `mistake.text` (non-existent — should be `mistake.utteranceText`) and `mistake.type` (non-existent — should be `mistake.mistakeType`). Fields `sessionMode`, `duration`, `status`, `scriptTitle`, `mistakeDetail`, `grammarTag`, `contextTag`, `sessionName`, `firstOccurrence` were all present in the API response but never rendered. Score displayed raw decimal (`87.0000000000000000`) without the `number:'1.0-1'` pipe. Fixed: all bindings corrected to match API field names; all requested fields now rendered; `number:'1.0-1'` pipe applied; `track $index` replaced with `track mistake.mistakeId`.

#### GET /api/dashboard/weekly-report

- Returns: `WeeklyReportResponseDto` — this-week practice stats, fluency delta, weakest grammar tag, 2 recommended scripts. No new DB tables. See Phase 1 Step 3 contract.

#### GET /api/dashboard/learning-path

- Returns: `GuidedLearningPathResponseDto` — up to 3 personalized session recommendations. See Phase 1 Step 4 contract.
- `activeSession` prefers latest unexpired session where `tblSession.Status` is `LOBBY` or `ACTIVE`
- Dashboard fallback ignores `tblSessionMember.IsActive` — banner visible until `tblSession.RoomExpiresAt` passes
- `ACTIVE` sessions preferred over `LOBBY`; null if neither

#### GET /api/users/profile

- Returns: profile fields plus computed totals (sessions, last-30-day average fluency, resolved mistakes)
- Frontend: `ProfileComponent` reads `userState.avatarUrl()` (computed from `profile()?.avatar`) and binds it to `<app-user-avatar [name]="..." [avatarUrl]="..." size="xl">` in the hero section — falls back to initials automatically when photo is absent

#### PUT /api/users/profile

- Request DTO: `UpdateProfileRequestDto`
- Updates: full name, email, age group, preferred hint language, avatar URL

#### POST /api/users/profile/avatar

- Multipart form file; validates type (jpg/jpeg/png/webp) and size (max `MaxFileSizeMB` from `FileStorageSettings`)
- **Phase 10 (R2):** uploads to `gwf-avatars` bucket; key pattern `avatars/{userId}/{timestampMs}.{ext}`; saves R2 object key (not URL) to `tblUser.AvatarUrl`; returns presigned URL (1440-minute expiry)
- `GET /api/users/profile` — if `AvatarUrl` is an R2 key (does not start with `/` or `http`), a fresh presigned URL is generated on every fetch; old URL-style values returned as-is (backwards compatible)

#### GET /api/users/sessions/{sessionId}/detail

- Returns: session header, caller performance summary, caller mistake list, listener feedback received, all member scores
- `AllMemberScores` items include `AvatarUrl` (presigned R2 URL, resolved in `UserService.ResolveSessionDetailAvatarsAsync` — same R2 key → presigned URL pattern as profile endpoint)
- Frontend: `allMemberScores` in `SessionDetail` model has `avatar?: string`; `user.service.ts` maps `s.avatarUrl → avatar`; template binds `[avatarUrl]="member.avatar"` on `app-user-avatar`
- **Notes on Drift (2026-06-03):** `GetSessionMemberScoresAsync` in `UserRepository` selected only `UserId` and `FullName` — `AvatarUrl` was never projected, so `MemberScoreDto.AvatarUrl` was always `null`. `UserService.GetSessionDetailAsync` returned the DTO without resolving R2 keys. Frontend `allMemberScores` mapper omitted `avatarUrl` entirely; `SessionDetail.allMemberScores` type had no `avatar` field. Result: `app-user-avatar` on `/session/detail/{id}` always showed initials fallback. Fix: (1) `AvatarUrl` added to LINQ select in `GetSessionMemberScoresAsync` and mapped to `MemberScoreDto.AvatarUrl`; (2) `ResolveSessionDetailAvatarsAsync` added to `UserService` and called from `GetSessionDetailAsync`; (3) `avatar` field added to `SessionDetail.allMemberScores` type; (4) `avatarUrl → avatar` mapping added in `user.service.ts`.

#### GET /api/users/progress — User Progress (Improvement Data)

**Entry Points:** `GET /api/users/progress` (Angular route: `/user/progress`)
**Controller:** `UserController.GetImprovementDataAsync`
**Service:** `UserService.GetImprovementDataAsync`
**Repository:** `UserRepository` (6 sequential SP calls)

**Response Contract (HTTP 200):**
```json
{
  "RecentSessions":    [ { "SessionDate", "SessionName", "FluencyScore", "ConfidenceScore", "MistakeCount" } ],
  "WeeklyScores":      [ { "WeekLabel", "AvgFluencyScore" } ],
  "GrammarProgress":   [ { "GrammarTag", "TotalMistakes", "ResolvedMistakes", "ImprovementPercent", "ProgressBarValue" } ],
  "RepracticeHistory": [ { "RepracticeSessionId", "SourceSessionId", "Status", "TotalMistakes", "CompletedRounds", "ImprovementPercent", "GeneratedDate" } ],
  "BadgesEarned":      [ { "BadgeCode", "BadgeName", "EarnedDate", "IsEarned" } ],
  "StatsHeader":       { "SessionsCompleted", "AvgScoreThisWeek", "MistakesResolved", "CurrentStreak" }
}
```

**Frontend UI — `ImprovementTrackerComponent` (`/user/progress`, heading "Progress Journey"):**
Single hub page (max-w-lg, mobile-first). Section order, top → bottom:
1. **Stats grid** (2×2): Sessions, Avg Score, Resolved, Streak — from `StatsHeader`.
2. **Score Trend** ("Last 10 Sessions") — `RecentSessions`; shows real `SessionName`, date, fluency/confidence %.
3. **Grammar Focus** — `GrammarProgress` merged with grammar-trend endpoint (Improving/Stable/Regressing badge).
4. **Badges Earned** — `BadgesEarned`.
5. **Quick Links** (3 cards, all routes verified live):
   - "Interview Performance" → `/user/interview-performance` (`InterviewPerformanceComponent` — readiness score & trends)
   - "Vocabulary Bank" → `/user/vocabulary` (`VocabularyBankComponent` — words practiced)
   - "Pronunciation Timeline" → `/user/pronunciation-timeline` (`PronunciationTimelineComponent` — problem words & session history)
6. **Repractice History** — `RepracticeHistory` list; improvement % + status per card.

**UI Copy Rephrase (2026-06-08) — user-friendliness pass on Repractice History cards:**
- Was: card title `Session #{{ sourceSessionId }}` (raw internal session id, meaningless to users) + `{completedRounds}/{totalMistakes} rounds` + raw `status` (e.g. `IN_PROGRESS`).
- Now: title `Mistake Repractice`; subtitle `{date} · {completedRounds} of {totalMistakes} mistakes practiced`; status humanized via `statusLabel()` (underscores → spaces).
- **Recommended future enhancement (NOT done — needs backend):** `RepracticeHistory` DTO has no source-session name, only `SourceSessionId`. To show the originating script/session name on these cards, add `SourceSessionName` to `uspgetrepracticesessionlistbyuserid` + DTO + `RepracticeHistoryItem` model.

**Stored Procedures (PostgreSQL):**
| Call | SP | Returns |
|---|---|---|
| 1 | `uspgetuserprofilebyuserid` | Profile + stats totals |
| 2 | `uspgetimprovementdatabyuserid` | Top-10 sessions (COMPLETED + ABANDONED) |
| 3 | `uspgetweeklyfluencyscorebyuserid` | Last 4-week fluency trend |
| 4 | `uspgetgrammarprogressbyuserid` | Grammar tag breakdown |
| 5 | `uspgetrepracticesessionlistbyuserid` | Page 1 of repractice history (10 rows) |
| 6 | `uspgetuserbadgebyuserid` | Earned badges |
| 7 | `uspgetstreakdatabyuserid` | Current + longest streak |

**PostgreSQL SP Management:**
- All SPs are managed via `Docs/PostgreSQLMigration/*.sql` scripts applied directly to Supabase (NOT via EF migrations).
- EF migrations are SQL Server-only for SP changes.

**Known Drift Fixed (23 May 2026) — VERIFIED ON SUPABASE:**
- **Error:** `Npgsql.PostgresException 42804: structure of query does not match function result type` on `uspgetimprovementdatabyuserid(bigint)`
- **Confirmed root cause:** `SUM(BIGINT)` in PostgreSQL returns `NUMERIC` — not `BIGINT`. The RETURNS TABLE declared `mistakecount BIGINT` but the query returned `NUMERIC`. Confirmed: `SELECT pg_typeof(SUM(1::BIGINT)) → numeric`.
- **Fix:** Changed `mistakecount BIGINT` → `mistakecount INTEGER` in RETURNS TABLE. Added `::INTEGER` cast on the SUM result. Added `::TIMESTAMP` on sessiondate. ABANDONED sessions included in filter.
- **C# compatibility:** `UserRepository.GetInt32(reader, "MistakeCount")` requires `int4` (INTEGER) from PostgreSQL — confirmed compatible.
- **Files changed:** `Docs/PostgreSQLMigration/16_fix_improvementsp_type_mismatch.sql` (applied to Supabase directly), `Migrations/20260523000002_FixImprovementSP_PostgreSQLTypeSafety.cs`
- **Migration 20260523000001** (SQL Server `ALTER PROCEDURE`) was also made PostgreSQL-aware (guard added) so it no longer fails silently on the PostgreSQL migration pipeline.

**Failure Cases:**
- `404` if `uspgetuserprofilebyuserid` returns no row for the userId
- `500` if any SP call throws (e.g., type mismatch, missing function, DB unavailable)

#### GET /api/users/streak

- Returns: current streak, longest streak, last 30 streak rows

#### GET /api/users/badges

- Returns: all earned badges for the authenticated user

### User Business Logic

- `DailyStreakCount` represents practice streak from completed session dates, not login count
- `CompleteSessionAsync` upserts one streak record per active member when session moves to `COMPLETED`
- `uspUpsertUserStreak` increments `tblUser.TotalSessionsPlayed`
- Badge rules: `7_DAY_STREAK` when `DailyStreakCount >= 7`; `10_SESSIONS` when `TotalSessionsPlayed >= 10`; `50_MISTAKES_FIXED` when resolved mistakes reach 50

### Application and Infrastructure Wiring

- Dashboard: `UserDashboardController`, `IUserDashboardService`, `UserDashboardService`
- User: `UserController`, `IUserService`, `UserService`, `IUserRepository`, `UserRepository`
- `FileStorageSettings.MaxFileSizeMB` is still used for avatar upload size validation (2 MB default); disk path fields are unused after Phase 10
- `IWebHostEnvironment` is NO LONGER injected into `UserService` — removed in Phase 10
- Session completion in `LiveSessionService` calls streak upsert and badge evaluation
- **Important:** PostgreSQL SPs are NOT managed via EF migrations. Apply `Docs/PostgreSQLMigration/*.sql` scripts directly to Supabase when SP changes are needed.

### Migration State

- `AddUserModule_Phase7`
- `RelaxImprovementSP_IncludeAbandoned` (SQL Server only — now has PostgreSQL guard)
- `FixImprovementSP_PostgreSQLTypeSafety` (PostgreSQL only — fixes 500 on /api/users/progress)

---

## Backend Admin Module

### Module Scope

- API base route: `api/admin`
- Authorization: `[Authorize(Roles = "ADMIN")]`
- Controller: `AdminController`

### Database Schema

#### tblAdminNote

- Primary key: `AdminNoteId BIGINT IDENTITY(1,1)`
- Business columns: `AdminUserId BIGINT NOT NULL`, `TargetUserId BIGINT NOT NULL`, `NoteText NVARCHAR(512) NOT NULL`, `NoteDate DATETIME2 NOT NULL DEFAULT(GETDATE())`
- Constraints: `PK_tblAdminNote_AdminNoteId`, `FK_tblAdminNote_AdminUserId_tblUser_UserId`, `FK_tblAdminNote_TargetUserId_tblUser_UserId`, `IDX_tblAdminNote_TargetUserId`

#### tblDashboardMetric

- Primary key: `DashboardMetricId BIGINT IDENTITY(1,1)`
- Business columns: `MetricDate DATE NOT NULL`, `TotalUsers INT NOT NULL DEFAULT(0)`, `ActiveSessionsToday INT NOT NULL DEFAULT(0)`, `TotalScriptsUploaded INT NOT NULL DEFAULT(0)`, `TotalMistakesRecorded INT NOT NULL DEFAULT(0)`
- Constraints: `PK_tblDashboardMetric_DashboardMetricId`, `UK_tblDashboardMetric_MetricDate`

### Stored Procedures

- Dashboard: `uspGetAdminDashboardSummary`, `uspGetRecentActivityList`, `uspGetTopGrammarMistakeType`, `uspGetAdminAnalyticsOverview`
- User administration: `uspGetAllUserBySearch`, `uspGetUserDetailByUserId`, `uspUpdateUserActiveStatusByUserId`, `uspInsertAdminNote`, `uspGetAdminNoteByTargetUserId`
- Reporting: `uspGetUserReportSummaryList`, `uspGetUserFullReportByUserId`, `uspExportUserReportData`, `uspGetTopPerformerListBySessionId`

### Domain Model

- Entities: `AdminNote`, `DashboardMetric`
- DbSets: `AdminNotes`, `DashboardMetrics`
- EF configurations: `AdminNoteConfiguration`, `DashboardMetricConfiguration`

### Request and Response Contracts

- Request DTOs: `AdminUserSearchRequestDto`, `AdminNoteRequestDto`, `UpdateUserStatusRequestDto`, `AdminReportFilterRequestDto`
- Response DTOs: `AdminDashboardResponseDto`, `RecentActivityDto`, `GrammarMistakeSummaryDto`, `AdminUserListResponseDto`, `AdminUserDetailResponseDto`, `AdminReportSummaryDto`, `AdminUserFullReportDto`, `AdminNoteResponseDto`, `SessionSummaryDto`
- Pagination wrapper: `PagedResult<T>`

### API Surface

> Note: Routes use `api/admin/...` prefix (no `/v1/` version segment — `ApiRoutes.VersionPrefix = "api"`).

- `GET /api/admin/dashboard` — global totals, recent activities, top grammar mistakes
  - Response fields: `topGrammarMistakes[].grammarTag`, `topGrammarMistakes[].userCount`, `topGrammarMistakes[].percentage`
  - `recentActivities[].avatarUrl` — R2 key resolved to presigned URL (1440-min) in `AdminService.GetDashboardSummaryAsync` before caching; null if user has no avatar
  - Frontend mapping: `getDashboard()` in `AdminService` maps SP fields to `{ userName, sessionName, sessionDate, fluencyScore, mistakeCount, status, avatarUrl }`. Template uses `<app-user-avatar [name]="row.userName" [avatarUrl]="row.avatarUrl" size="sm">` — handles presigned URL display and initials fallback uniformly.
  - Notes on Drift: Bug fixed 2026-06-02 — service was passing `topGrammarMistakes` items raw to `weakAreas`; template reads `area.tag` / `area.count` but API returns `grammarTag` / `userCount`. Fix: added `.map()` in `getDashboard()` to rename fields.
  - Notes on Drift (2026-06-03): `uspgetrecentactivitylist` did not return `avatarurl` despite `tbluser` being joined. `RecentActivityDto` had no `AvatarUrl` field. Dashboard showed initials-only fallback for all rows. Migration 31 added `avatarurl` but declared `sessiondate TIMESTAMPTZ` — table column is `TIMESTAMP` (no tz), causing 42804. Migration 32 corrected with verified live DB types. Rule: always query `information_schema.columns` for actual column types before writing a `RETURNS TABLE` SP — never assume timezone variant. Full fix: SP updated (migration 32); `RecentActivityDto.AvatarUrl` added; repository mapper updated; `GetDashboardSummaryAsync` resolves R2 keys to presigned URLs before caching; Angular service maps `avatarUrl`; template shows `<img>` with initials fallback. Admin top bar also fixed: `AdminLayoutComponent` now reads `fullName` + `avatarUrl` from `AuthService.currentUser` and uses `app-user-avatar` in both topbar and profile menu (was hardcoded initials-only).
- `POST /api/admin/users` — create new user; `PUT /api/admin/users/{userId}` — update user
  - Notes on Drift: Bug fixed 2026-06-02 — stale async race condition in `AdminUsersComponent.openEditModal()`: `getUserDetail` async callback could fire after user closed edit modal and opened "Add User" modal, patching the reset form with old user data. Fix: guard in callback checks `editingUserId() !== user.id` and returns early if the modal context has changed.
- `GET /api/admin/users` — paginated admin user list; backed by `uspgetalluserbysearch` (**RETURNS TABLE** contract — must NOT use RETURNS SETOF REFCURSOR; see drift note below)
  - Notes on Drift (2026-06-03): Migration 29 reverted `uspgetalluserbysearch` to `RETURNS SETOF REFCURSOR` to add `avatarurl`. This broke the endpoint with `System.IndexOutOfRangeException: Field not found in row: UserId` because the C# reader loop (`AdminRepository.GetUsersAsync`) uses a simple `reader.ReadAsync()` loop that expects flat columns — it was written for the `RETURNS TABLE` contract established in migration 13. A REFCURSOR function called via `SELECT * FROM fn()` returns cursor name strings as rows, not data columns. Fixed in migration 30: restored `RETURNS TABLE` with all 9 columns including `avatarurl`. Rule: this SP must always use `RETURNS TABLE` — never `RETURNS SETOF REFCURSOR`.
- `GET /api/admin/users/{userId}` — full user profile with averages and recent sessions
- `PATCH /api/admin/users/status` — updates `tblUser.IsActive`
- `POST /api/admin/users/notes` — inserts admin note using admin JWT claim
- `GET /api/admin/users/{userId}/notes` — active admin notes for target user
- `GET /api/admin/reports` — paginated user report summaries; `ImprovementPercent = (resolved / total) * 100`; `avatarUrl` included — R2 key resolved to presigned URL in `GetReportSummaryAsync` before returning; frontend `getReports()` maps `avatarUrl`; list uses `app-user-avatar`
  - Notes on Drift (2026-06-03): `uspgetuserreportsummarylist` did not return `avatarurl`. Migration 33 added it to the `userreport` CTE (select + group by) and final SELECT. Live function signature verified via `pg_get_functiondef` before writing — used unqualified `CHARACTER VARYING` / `NUMERIC` / `TIMESTAMP` to match live declaration exactly.
- `GET /api/admin/reports/users/{userId}` — user header, session history, mistake breakdown, weekly scores
- `GET /api/admin/reports/export` — generates Excel in-memory via `ClosedXML`; **Phase 10 (R2):** uploads to `gwf-exports` bucket; key `exports/{userId}/{yyyyMMdd_HHmmss}.xlsx`; returns `ApiResponse<string>` where `Data` = presigned URL (30-minute expiry); controller no longer streams bytes
  - **Frontend download contract:** `admin.service.ts.exportReports()` requests JSON (default responseType) and maps `res.data` → presigned URL string; `AdminReportsComponent.exportReport()` triggers download via a transient `<a href=presignedUrl download>` anchor click. The presigned URL must be hit by a **direct browser navigation, NOT HttpClient** — the app's auth interceptor would attach an `Authorization` header and break the AWS4 (`X-Amz-Signature`) signature.
  - Notes on Drift (2026-06-08): **"Invalid format" Excel on admin reports export.** After the Phase 10 R2 migration the backend stopped streaming bytes and returned `ApiResponse<string>` (presigned URL), but the frontend was never updated — `exportReports()` still used `responseType: 'blob'`, so the JSON envelope text was saved as a `.xlsx` and Excel rejected it. Classic request/response drift (stale frontend after backend contract change). Fix: service returns the URL string; component downloads from the presigned URL via anchor navigation (no `responseType: 'blob'`, no HttpClient fetch of R2).
- `POST /api/admin/users` — create new user; **multipart/form-data** (`[FromForm]`); fields: `fullName`, `mobileNumber`, `email?`, `ageGroup`, `preferredHintLanguage`, `password?`, `avatar?` (IFormFile); if `avatar` is provided, uploads to `gwf-avatars` bucket via `AdminService.UploadAvatarInternalAsync`, saves R2 key to `tblUser.AvatarUrl`; returns `AdminCreateUserResponseDto` including `avatarUrl` (presigned, 1440-min) or null
  - **Password persistence contract:** `AdminService.CreateUserAsync` hashes `dto.Password` via PBKDF2 (`HashPassword`: 16-byte salt, 100k iterations, SHA-256, 32-byte hash) into `user.PasswordHash`, then `UserRepository.InsertUserAsync` → PG function `uspinsertuser` → `tblUser.PasswordHash`. The C# parameter name `@PasswordHash` is bound by name to `p_passwordhash` (PG named-arg call `p_passwordhash => @p_passwordhash`, see `DbCommandHelper.PreparePostgreSqlFunctionInvocation`). `password` is optional; when blank, `PasswordHash` is stored NULL and login is impossible until set via the update endpoint.
  - Notes on Drift (2026-06-08): **Admin-created users could never log in — password silently dropped at TWO layers.** The UI (`admin.service.ts` appends `password` to FormData) and `AdminService` (hashes correctly) were fine, but (1) `UserRepository.InsertUserAsync` never added an `@PasswordHash` parameter, and (2) `uspinsertuser` had **no `p_passwordhash` parameter** and hard-coded `NULL` into the `passwordhash` column. The auth flow then rejected the user (`PasswordHash` missing → 401). Fix: repository now passes `@PasswordHash`; `uspinsertuser` gained `p_passwordhash VARCHAR(512)` and writes it (canonical `06_stored_procedures.sql` updated for fresh builds; **migration 36** drops the old 10-arg overload and recreates for deployed DBs — required because adding a parameter creates a *new* overload that named-arg calls could mis-resolve). The `PUT` update path already wrote `passwordhash` correctly and was not affected. Rule: when a PG insert function is called via this codebase's named-arg binding, every entity field that must persist needs **both** a `CreateParameter("@X", ...)` in the repository **and** a matching `p_x` parameter wired into the function's INSERT — a missing column param fails silently (no error, NULL stored).
- `PUT /api/admin/users/{userId}` — update user profile; **multipart/form-data** (`[FromForm]`); same fields as create (all optional except `fullName`, `mobileNumber`, `ageGroup`, `preferredHintLanguage`); if `avatar` is provided, uploads and returns new presigned URL in `data` field; `data` is null if no avatar uploaded; **`POST /api/admin/users/{userId}/avatar` removed** (2026-06-03 — merged into PUT)
  - Notes on Drift (2026-06-03): `UpdateUserByAdminAsync` used `CreateParameter("@fn", ...)` which normalizes names to `p_fn` for PostgreSQL (designed for stored procs). The raw UPDATE SQL used `@fn`, causing `42601: syntax error at or near "=@"`. Fix: replaced with `cmd.CreateParameter()` directly (bypasses normalizer). Rule: for raw SQL commands in UserRepository, always use `cmd.CreateParameter()` — never the `CreateParameter()` helper method.
- `GET /api/admin/sessions/history` — paginated admin session history with filters
- `GET /api/admin/sessions/{sessionId}` — ADMIN; single session summary by id. Returns `ApiResponse<AdminSessionHistoryItemDto>` (same shape as one history row: sessionName, joinCode, hostName, memberCount, status, sessionDate, durationMin, avgFluency, mistakeCount). 200 on found, 404 with "Session not found" otherwise, 400 if `sessionId <= 0`. Backed by `IAdminService.GetSessionByIdAsync` → `IAdminRepository.GetSessionByIdAsync` (EF Core; same member/fluency/mistake enrichment as the history projection). Added 2026-06-06 so the admin session-detail page survives direct navigation / refresh (previously it only worked via router state — see Admin Session Detail special case).
- `GET /api/admin/sessions/{sessionId}/recordings` — ADMIN; returns all audio archive clips for a session across all users; each clip includes `UserName` (speaker name), `TurnIndex`, presigned `AudioUrl` (120-min expiry); backed by `IAudioArchiveService.GetAdminSessionRecordingsAsync` → `IAudioArchiveRepository.GetAllBySessionAsync` (raw SQL join to `tblUser`)

### Admin Session History — Stable Flow Contract

#### Entry Points
`/admin/sessions` — Angular standalone route `AdminSessionsComponent`

#### UI Trigger
`ngOnInit()` auto-loads on mount. Filter bar has Search / Status / FromDate / ToDate inputs with Apply + Clear buttons. `mat-paginator` triggers page change.

#### Request Contract
Endpoint: `GET /api/admin/sessions/history`
Auth: `AdminOnly` + `ActiveUser` JWT policies required
Query params:
- `searchTerm` (string, optional): matches session name or host full name (LIKE, case-insensitive)
- `status` (string, optional): `COMPLETED` | `ABANDONED` | `IN_PROGRESS` | empty = all
- `fromDate` (DateTime, optional): inclusive lower bound on session date (`EndedDate ?? StartedDate ?? DateCreated`)
- `toDate` (DateTime, optional): exclusive upper bound (end of that day)
- `pageNumber` (int, default 1)
- `pageSize` (int, default 20)

#### Response Contract
Success (HTTP 200): `ApiResponse<PagedResult<AdminSessionHistoryItemDto>>`
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "sessionId": 1,
        "sessionName": "string",
        "hostName": "string",
        "memberCount": 2,
        "status": "COMPLETED",
        "sessionDate": "2026-01-01T10:00:00",
        "durationMin": 15,
        "avgFluency": 82.5,
        "mistakeCount": 3
      }
    ],
    "totalCount": 100,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 5
  },
  "message": "Session history retrieved successfully."
}
```
Error: HTTP 400 `ApiResponse<PagedResult<...>>` with `success: false`

#### Validation
- `NormalizeSessionHistoryFilter` trims `SearchTerm`, uppercases `Status`, defaults `PageNumber`/`PageSize` to 1/20 if ≤ 0

#### Database / Stored Procedures
Tables read: `tblSession` (via `Sessions` DbSet), `tblSessionMember` (via `Set<SessionMember>()`), `tblVoiceAnalysis` (via `VoiceAnalyses`), `tblMistake` (via `Mistakes`)
Tables written: none
Stored procedures: none (pure EF Core LINQ)
Key queries:
- Main: filter + paginate `tblSession` ordered by `EndedDate ?? StartedDate ?? DateCreated DESC`
- Member count: `GROUP BY SessionId` on `tblSessionMember WHERE IsDeleted = false`
- Avg fluency: `AVG(FluencyScore)` per session from `tblVoiceAnalysis WHERE IsDeleted = false`
- Mistake count: `COUNT(*)` per session from `tblMistake WHERE IsDeleted = false`

#### Business Rules
- `DurationMin` = `CEIL(ActualDurationSec / 60)` if `ActualDurationSec > 0`, else falls back to `SessionDuration` (planned minutes)
- `AvgFluency` = 0 if no voice analysis rows exist for the session
- `MistakeCount` = 0 if no mistake rows exist
- Soft-deleted sessions excluded via `IsDeleted = false`
- Host name pulled from `Session.Host.FullName` (EF navigation property); defaults to empty string if null

#### State Transitions
Read-only. No state changes.

#### Realtime Events
None.

#### Failure Cases
- Missing or invalid JWT → 401 (framework, not middleware)
- Non-admin user → 403 (framework, not middleware)
- EF query exception → 500 via `ExceptionMiddleware`
- `KeyNotFoundException` thrown in repo → 404 via `ExceptionMiddleware`

#### Recovery / Fallback Logic
Empty sessions list returns early with `PagedResult { Items=[], TotalCount=0 }` without executing the member/fluency/mistake sub-queries.

#### Notes on Known Drift Prevented
- Route prefix confirmed: all routes use `/api/...` prefix with no version segment (`ApiRoutes.VersionPrefix = "api"`)
- Endpoint was added to `AdminController`, `IAdminService`, `AdminService`, `IAdminRepository`, `AdminRepository`, and all DTOs but ProjectOverview was not updated — corrected 2026-05-24
- 404 root cause on first run: backend process was running a stale binary compiled before the endpoint was added; fix = restart API from Visual Studio

---

### Admin Users List — Stable Flow Contract

#### Entry Points
`/admin/users` — Angular standalone route `AdminUsersComponent`

#### Request Contract
Endpoint: `GET /api/admin/users`
Query params:
- `searchTerm` (string, optional): filter by name or mobile
- `ageGroup` (string, optional): `"Child (6-12)"`, `"Teen (13-17)"`, `"Adult (18+)"`, or empty for all
- `isActive` (bool?, optional): **omit or send empty string to return ALL users**; send `true` to return active-only; send `false` to return inactive-only
- `pageNumber` (int, default 1): 1-based
- `pageSize` (int, default 10)

#### Response Contract
Success (200):
```json
{
  "success": true,
  "data": {
    "items": [ { "userId", "fullName", "mobileNumber", "ageGroup", "totalSessionsPlayed", "dailyStreakCount", "lastLoginDate", "isActive" } ],
    "totalCount": 0,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 0
  }
}
```

#### Frontend Service Mapping (`admin.service.ts → getUsers`)
- `isActive` must be `'true'` only when `activeOnly === true`; otherwise send `''` (empty string) so SP receives NULL and returns all users
- Response is mapped: `res.data` → `{ ...res.data, total: res.data.totalCount }` so component can read either `total` or `totalCount`
- `getUserDetail` must use `.pipe(map(res => res.data))` — API wraps all responses in `{ success, message, data }`

#### Notes on Known Drift Prevented
- **Bug fixed 2026-05-19**: Service was sending `isActive=false` when "Active Only" toggle was off. This caused SP to filter inactive-only users → always returned 0 results. Fix: only send `isActive=true` when `activeOnly===true`, otherwise omit (empty string → NULL → no filter).
- **Bug fixed 2026-05-19**: Component read `res.total` but API returns `totalCount`. Paginator always showed 0. Fix: service now maps `totalCount → total` in the spread.
- **Bug fixed 2026-05-19**: `getUserDetail` was missing `.pipe(map(res => res.data))` — the raw API wrapper was being passed to the component instead of the detail object.

### Dashboard Logic

- `TotalUsers`: active users where `IsDeleted = 0` and `IsActive = 1`
- `ActiveSessionsToday`: sessions where `Status = 'ACTIVE'`, `IsDeleted = 0`, `CAST(DateCreated AS DATE) = CAST(GETDATE() AS DATE)`

### Application and Infrastructure Wiring

- `IAdminService → AdminService`, `IAdminRepository → AdminRepository`, `IExcelExportService → ExcelExportService`

### Migration State

- `InitialCreate_Phase1`, `AddAdminModule_Phase2`

---

## Backend Script Module

### Module Scope

- API base route: `api/scripts`
- Purpose: validate admin-uploaded Excel content, upload script metadata and utterances, expose script library and version history, generate sample `.xlsx` template

### Database Schema

#### tblScript

- Primary key: `ScriptId BIGINT IDENTITY(1,1)`
- Business columns: `ScriptTitle NVARCHAR(128) NOT NULL`, `Category NVARCHAR(64) NOT NULL`, `GrammarFocusTag NVARCHAR(64) NOT NULL`, `ContextTag NVARCHAR(64) NOT NULL`, `ComplexityLevel TINYINT NOT NULL`, `TargetAgeGroup NVARCHAR(32) NOT NULL`, `HintLanguage NVARCHAR(32) NOT NULL`, `IsActive BIT NOT NULL DEFAULT(1)`, `UploadedDate DATETIME2 NOT NULL DEFAULT(GETDATE())`, `UploadedByUserId BIGINT NOT NULL`, `Version INT NOT NULL DEFAULT(1)`, `UtteranceCount INT NOT NULL DEFAULT(0)`, `ExcelStorageKey NVARCHAR(256) NULL` *(Phase 10 — R2 object key for original uploaded Excel)*
- Constraints: `PK_tblScript_ScriptId`, `FK_tblScript_UploadedByUserId_tblUser_UserId`, `IDX_tblScript_GrammarFocusTag`, `IDX_tblScript_Category`, `IDX_tblScript_IsActive`

#### tblUtterance

- Primary key: `UtteranceId BIGINT IDENTITY(1,1)`
- Business columns: `ScriptId BIGINT NOT NULL`, `SequenceId INT NOT NULL`, `SpeakerLabel NVARCHAR(64) NOT NULL`, `EnglishText NVARCHAR(512) NOT NULL`, `HintText NVARCHAR(512) NULL`, `GrammarTag NVARCHAR(64) NULL`, `ContextTag NVARCHAR(64) NULL`, `FocusWord NVARCHAR(64) NULL`, `PronunciationNote NVARCHAR(256) NULL`
- Constraints: `PK_tblUtterance_UtteranceId`, `FK_tblUtterance_ScriptId_tblScript_ScriptId`, `IDX_tblUtterance_ScriptId`, `UK_tblUtterance_ScriptId_SequenceId`

#### tblScriptVersion

- Primary key: `ScriptVersionId BIGINT IDENTITY(1,1)`
- Business columns: `ScriptId BIGINT NOT NULL`, `VersionNumber INT NOT NULL`, `VersionNotes NVARCHAR(256) NULL`, `UploadedByUserId BIGINT NOT NULL`, `UploadedDate DATETIME2 NOT NULL DEFAULT(GETDATE())`
- Constraints: `PK_tblScriptVersion_ScriptVersionId`, `FK_tblScriptVersion_ScriptId_tblScript_ScriptId`, `IDX_tblScriptVersion_ScriptId`

#### SQL Type

- `dbo.UtteranceTVP`

### Stored Procedures

- `uspInsertScript`, `uspInsertUtterance`, `uspBulkInsertUtterance`, `uspUpdateScriptUtteranceCount`, `uspUpdateScriptActiveStatusByScriptId`, `uspSoftDeleteScriptByScriptId`
- `uspGetScriptBySearch`, `uspGetScriptDetailByScriptId`, `uspGetScriptVersionHistoryByScriptId`, `uspCheckScriptTitleExists`, `uspInsertScriptVersion`

### Excel Parsing

- Library: `ClosedXML`
- Expected columns: `A=SequenceId`, `B=SpeakerLabel`, `C=EnglishText`, `D=HintText`, `E=GrammarTag`, `F=ContextTag`, `G=FocusWord`, `H=PronunciationNote`
- Row validation: `SequenceId` positive integer and unique within file; `SpeakerLabel` required; `EnglishText` required max 512; `HintText` optional max 512
- Validation response: `ValidRows` returns first 5 preview rows; `ErrorRows` returns row-level issues; `TotalRows`, `ValidCount`, `ErrorCount`, `IsValid`

### Excel Template Standard (Category-Wise)

- Permanent reference document: `Docs/ExcelTemplateStandard.md` (Version 1.0, 2026-05-22)
- Six registered categories (canonical names): `Grammar Drill`, `Roleplay`, `Mock Interview`, `Vocabulary Sprint`, `Fluency Drill`, `Repractice Round`
- Legacy category aliases accepted in upload validation (backward compatible with existing DB data): `Interview` = `Mock Interview`, `Vocabulary` = `Vocabulary Sprint`, `Repetition` = `Repractice Round`
- Each category defines: fixed speaker labels, mandatory columns (D/G/H), row count limits, content rules, metadata defaults, and sample data
- Speaker labels by category: GrammarDrill → `Speaker A/B`; Roleplay → role-based; MockInterview → `Interviewer/Candidate`; VocabularySprint → `Tutor/Learner`; FluencyDrill → `Speaker A/B`; RepracticeRound → `Coach/Learner`
- Column D (HintText) mandatory for: `Vocabulary Sprint`, `Repractice Round`
- Column G (FocusWord) mandatory for: `Mock Interview`, `Vocabulary Sprint`
- Column H (PronunciationNote) mandatory on Tutor rows for: `Vocabulary Sprint`
- File naming convention: `[CategoryCode]_[slug]_v[Version]_[YYYY-MM-DD].xlsx`
- AI generation output format: JSON with `metadata` block and `rows` array — spec in `ExcelTemplateStandard.md §10`
- Row limits: GrammarDrill 12–30; Roleplay 16–40; MockInterview 20–50; VocabularySprint 20–40; FluencyDrill 30–60; RepracticeRound 14–28

### API Surface

- `POST /api/scripts/validate` — ADMIN; validates Excel; returns parse result
- `POST /api/scripts/upload` — ADMIN; validates file, parses, inserts `tblScript`, bulk inserts `tblUtterance`, updates `UtteranceCount`, inserts `tblScriptVersion`; **Phase 10:** uploads original Excel to `gwf-scripts` bucket (`scripts/{scriptId}/v{version}.xlsx`), saves key to `tblScript.ExcelStorageKey`; returns `ScriptId`, `ScriptTitle`, `Version`, `UtteranceCount`, `ExcelDownloadUrl` (presigned, 60 min)
- `GET /api/scripts` — authenticated; paginated script list
- `GET /api/scripts/{scriptId}` — authenticated; script metadata and ordered utterances
- `PATCH /api/scripts/status` — ADMIN; updates `tblScript.IsActive`
- `GET /api/scripts/{scriptId}/versions` — ADMIN; version history
- `GET /api/scripts/{scriptId}/download` — ADMIN; generates Excel in-memory from current utterances via `ClosedXML`; returns `File()` bytes (not R2 — this is a live regeneration endpoint)
- `GET /api/scripts/{scriptId}/excel-download` — ADMIN; **Phase 10:** returns presigned URL for original uploaded Excel from R2 (`tblScript.ExcelStorageKey`); returns `ApiResponse<string>`
- `GET /api/scripts/sample-template?category={category}` — ADMIN; **Phase 10:** uploads generated template to `gwf-scripts/scripts/sample/template_v1.xlsx` if not already present; returns presigned URL (60-min) as `ApiResponse<string>`. Previously returned `File()` bytes.
- `GET /api/scripts/prompt-data?category={category}` — ADMIN; returns `ScriptPromptDataResponseDto` with DB-sourced `grammarTagsInUse`, `contextTagsInUse`, `approvedGrammarTags` (static list), `speakerLabels`, `minRows`, `maxRows`, `mandatoryColumns`, `activeScriptCount`. Used by the upload wizard to build the category-specific Claude prompt shown to the admin.
- `GET /api/scripts/analytics?category={category}` — ADMIN; per-script quality metrics (Phase 2 Step 7).
- `POST /api/scripts/{scriptId}/rollback?version={n}` — ADMIN; rolls back to a prior version (Phase 2 Step 8).
- `POST /api/scripts/{scriptId}/duplicate` — ADMIN; creates inactive copy with all utterances (Phase 2 Step 8).

### Admin Script Upload Wizard — Stable Flow Contract

#### Entry Points

- Route: `/admin/scripts/upload`

#### UI Trigger

- Step 1 validation starts when the admin selects or drops an `.xlsx` file and clicks `Validate Excel`
- Step 2 progression starts when the admin clicks `Continue` after metadata entry

#### Request Contract

Endpoint: `POST /api/scripts/validate`
Headers: authenticated admin session; multipart form upload
Body:
- `file` (`File`, required): `.xlsx` only, max size `5 MB`

Endpoint: `POST /api/scripts/upload`
Headers: authenticated admin session; multipart form upload
Body:
- `file` (`File`, required): same validated `.xlsx` selected in step 1
- `scriptTitle` (`string`, required): required by the step 2 form; `Continue` stays disabled while empty
- `category` (`string`, required): defaults to `Grammar Drill`
- `grammarFocusTag` (`string`, optional in UI, sent on upload): defaults to `Have Been`
- `contextTag` (`string`, optional in UI, sent on upload): defaults to `Office`
- `complexityLevel` (`number`, required): defaults to `3`
- `targetAgeGroup` (`string`, required): one of `All | Child | Teen | Adult`; defaults to `Adult`
- `hintLanguage` (`string`, required): one of `Telugu | Hindi | Tamil | Kannada | None`; defaults to `Telugu`

#### Response Contract

Success (`POST /validate`):
- `isValid` (`boolean`)
- `totalRows` (`number`)
- `validCount` (`number`)
- `errorCount` (`number`)
- `validRows` mapped to UI `rows` (`array`): preview payload rendered in step 2
- `errorRows` mapped to UI `errors` (`array`): each row rendered as `Row {rowNumber} — {columnName}: {errorMessage}`

Success (`POST /upload`):
- `ScriptId` (`number`)
- `ScriptTitle` (`string`)
- `Version` (`number`)
- `UtteranceCount` (`number`)
- `ExcelDownloadUrl` (`string`, nullable) — presigned R2 URL for the original uploaded Excel (60-minute expiry) *(Phase 10)*

Error responses:
- Validation or upload failure body shape: `[VERIFY]`

#### Validation

- Frontend rejects non-`.xlsx` files before API calls
- Frontend rejects files larger than `5 MB` before API calls
- Step 2 `Continue` button is disabled when `metadataForm.invalid` is true
- In current UI, `metadataForm.invalid` is driven by required fields, especially `scriptTitle`, plus `category`, `complexityLevel`, `targetAgeGroup`, and `hintLanguage`
- Step 2 `Continue` button is also disabled when `validationResult.errors.length > 0`
- Backend row validation for `POST /validate`: `SequenceId` positive and unique within file, `SpeakerLabel` required, `EnglishText` required max `512`, `HintText` optional max `512`

#### Database / Stored Procedures

Tables read:
- none during `POST /validate`

Tables written:
- `tblScript`
- `tblUtterance`
- `tblScriptVersion`

Stored procedures:
- `uspInsertScript` — create script metadata row
- `uspBulkInsertUtterance` — insert utterance rows from parsed Excel
- `uspUpdateScriptUtteranceCount` — persist final utterance count
- `uspInsertScriptVersion` — record uploaded version

Key queries:
- version resolution uses existing rows for the same `ScriptTitle` before insert

#### Business Rules

1. Admin must validate the Excel file before reaching step 2; successful validation advances `step` from `1` to `2`
2. Step 2 allows progression to confirmation only when required metadata is complete and the validation result has no row errors
3. Upload reuses the same selected file plus metadata form values; no separate file reselection is required
4. Successful upload advances the wizard to success state `step = 4`

#### State Transitions

- Upload wizard step `1 -> 2` after successful `POST /validate`
- Upload wizard step `2 -> 3` only when `Continue` is enabled and clicked
- Upload wizard step `3 -> 4` after successful `POST /upload`

#### Realtime Events

- None

#### Failure Cases

- Invalid file extension on client (`.xlsx` required) -> no API call -> toast error
- File larger than `5 MB` on client -> no API call -> toast error
- Validation API returns row errors -> `Continue` remains disabled -> user must correct the file and revalidate
- Upload API failure -> save state clears and user remains on confirmation step

#### Recovery / Fallback Logic

- `resetUpload()` returns the wizard to step `1`, clears selected file, validation result, preview expansion state, and upload response, and restores metadata defaults except `scriptTitle`
- User can return from step `3` to step `2` with `Back to edit`

#### Notes on Known Drift Prevented

- The frontend enablement rule for the step 2 `Continue` button was previously undocumented; the stable contract now records that the button stays disabled until required metadata is valid and validation produced zero row errors
- The UI sends `validRows` as `rows` and flattens `errorRows` into formatted strings; this mapping is now explicit to prevent request/response drift during future UI fixes

### Versioning Rule

- Next version number computed from highest existing version for the same `ScriptTitle`
- New `tblScript` row inserted per upload; corresponding `tblScriptVersion` row added

### Application and Infrastructure Wiring

- `IScriptService → ScriptService`, `IScriptRepository → ScriptRepository`, `IExcelParserService → ExcelParserService`

### Migration State

- `InitialCreate_Phase1`, `AddAdminModule_Phase2`, `AddScriptModule_Phase3`

**Phase 2 additions:**
- `GET /api/scripts/prompt-data` — see Phase 2 Step 9
- `GET /api/scripts/analytics` — see Phase 2 Step 7
- `POST /api/scripts/{id}/rollback` — see Phase 2 Step 8
- `POST /api/scripts/{id}/duplicate` — see Phase 2 Step 8

---

## Frontend Listen Script Module — Script Audio Player

### Module Scope

Standalone, **non-session** audio experience: listen to any script read aloud (Spotify-lyrics style)
without joining a live session and **without opening the microphone**. Frontend-only — **no new
backend, API, DB, or SignalR**. Reuses `GET /api/scripts/{scriptId}` for content. **v1 design
decisions (confirmed by user 2026-06-17):** on-device line-level TTS · both entry points (bottom-nav
tab + per-script action) · web + APK.

### Entry Points

- **Bottom-nav "Listen" tab** → `/scripts/listen` (picker) — `ListenPickerComponent`. The default
  user footer now has 5 tabs: Home · **Listen** · Review · Progress · History (`BottomNavComponent.defaultItems`).
- **Per-script "Listen" action** in the Script Library row → `/scripts/listen/:scriptId`
  (`ScriptLibraryComponent`, Headphones icon, alongside Preview/Prepare/Start).
- **Player route** `/scripts/listen/:scriptId` — `ListenScriptComponent`.

### UI Trigger

Picker row tap or library Listen button navigates to the player; player `Play` button (or tapping a
line) starts on-device narration.

### Request Contract

- Endpoint: `GET /api/scripts/{scriptId}` (existing — `ScriptService.getScriptDetail`). Returns
  `{ scriptId, scriptTitle, category, utterances: [{ utteranceId, sequenceId, speakerLabel, englishText, hintText, … }] }`.
- Picker list: existing `GET /api/scripts` (`getScripts`, `isActive=true`, page 0 / limit 50, search debounced 400 ms).
- **No new endpoints.**

### Core Engine — `ScriptPlaybackService` (`core/services/voice/script-playback.service.ts`)

On-device, **line-level** playback. Signals: `lines`, `currentIndex`, `isPlaying`, `rate`, `repeat`
(`off`|`one`|`all`), `roles`, `roleVoices`. Drives `TtsService.speak(text, { rate, gender, pitch })`
one utterance at a time.

- **Engine constraint (root of the design):** `@capacitor-community/text-to-speech` resolves only when
  a whole line finishes and exposes **no word/sentence timing** and **no mid-line seek**. Therefore:
  highlight = active LINE; "rewind/fast-forward/seek" = prev/next/jump LINE; "pause" stops the line and
  resume re-speaks it from the start. Word-level karaoke and a continuous scrubber are NOT possible on-device.
- **Sequential loop with cancellation token (`playToken`):** every `pause()/seekTo()/setRate()/setRoleGender()`
  bumps `playToken` and calls `TtsService.stop()`; after each `await speak()` the loop re-checks the token
  and exits if superseded. Prevents overlapping narration.
- **Role → voice presets:** distinct `speakerLabel`s (in first-appearance order) are assigned a
  `{ gender, pitch }` preset from a 4-entry palette (Female 1.0 / Male 0.92 / Female 1.18 / Male 1.12).
  User can override gender per role via the Voices sheet (`setRoleGender`). True distinct neural voices
  are NOT available on-device — differentiation is gender + pitch only.
- **Continue From Last Position:** `currentIndex` persisted to `localStorage` key `gwf_listen_pos_{scriptId}`
  on every line advance / transport action; `load()` restores it.
- **Speed:** `PLAYBACK_SPEEDS = [0.5, 1, 1.25, 1.5, 2]`; `cycleSpeed()`; changing rate restarts the current line.
- **Repeat:** `off` → linear (stops at end); `one` → repeat current line; `all` → loop whole script.

### Player UI — `ListenScriptComponent`

Lyrics list (active line highlighted/enlarged, past lines dimmed, animated wave on the speaking line);
`effect()` auto-scrolls the active line into view (`scrollIntoView`, guarded by `isPlatformBrowser`);
tap any line → `seekTo`. Sticky bottom dock: line-progress bar, Speed, Prev, Play/Pause, Next, Repeat,
and a Voices button opening `ListenVoicesSheetComponent` (per-role Male/Female). `ngOnDestroy → playback.reset()`
so narration never leaks across navigations.

### Voice Constitution Compliance

OUTPUT-only TTS — never opens the mic, so **no recognizer mic contention** (rule #1 safe). Not part of
a live session; no `SessionCapabilitiesService` gating needed (no recognizer/broadcast involved).
`TtsService.speak()` gained an **optional `pitch`** param (defaults to `1.0`) — additive; Phase 17 AI
Voice Participant behavior unchanged.

### Failure Cases

- Script load fails / not found → loading clears, "Nothing to play" empty state with link back to picker.
- `TtsService.speak()` failure is swallowed inside the service (warns, never throws) — narration never wedges; loop continues/ends gracefully.
- No installed device voice match → plugin falls back to its default voice (best-effort gender match).

### Notes on Drift / Verification

- **Drift fixed 2026-06-17 — Male voice spoke Female (gender substring collision).** `TtsService.resolveVoiceIndex`
  selected a voice via `name.includes(gender.toLowerCase())`. Because `"female"` *contains* `"male"`,
  selecting **Male** matched the first female-named voice (`…#female_1…`) → male selection spoke in a female
  voice. Affected **both** Phase 17 AI Voice Participant (create-session Male/Female pick) and Listen Script
  role voices. Fixed: match `Female` by `includes('female')`, and `Male` by `includes('male') && !includes('female')`.
  Drift type: logic defect (substring match). Shared `TtsService` → one fix covers both consumers.
- **UNVERIFIED on-device as of 2026-06-17** — typecheck green only. Per §5a this is BOTH interactive UI
  AND voice output; requires rendered + APK verification (lyrics highlight/auto-scroll, actual narration,
  per-role voice audible difference, speed/repeat, continue-from-last) on IV2201 before sign-off.
- **Future enhancements (out of v1 scope, per spec):** background playback, offline downloads, bookmarks,
  favorites/playlists, cloud neural per-role voices, word-level karaoke + scrubber (would require
  server-side TTS with word timestamps), multi-language voice packs.

---

## Backend Session Module

### Module Scope

Session lifecycle: create, validate join code, join lobby, get lobby state, ready toggle, host start, leave, end, session history.
Real-time lobby updates via SignalR at `/hubs/session`.

### Database Schema

#### tblSession

- Primary key: `SessionId BIGINT IDENTITY(1,1)`
- Business columns:
  - `SessionName NVARCHAR(128) NOT NULL`
  - `JoinCode NVARCHAR(8) NOT NULL`
  - `SessionMode NVARCHAR(64) NOT NULL`
  - `MaxMembers TINYINT NOT NULL DEFAULT(4)`
  - `SessionDuration INT NOT NULL`
  - `HostUserId BIGINT NOT NULL`
  - `ScriptId BIGINT NOT NULL`
  - `Status NVARCHAR(16) NOT NULL DEFAULT('LOBBY')`
  - `RoomExpiryMinutes INT NOT NULL`
  - `RoomExpiresAt DATETIME2 NULL`
  - `StartedDate DATETIME2 NULL`
  - `EndedDate DATETIME2 NULL`
  - `ActualDurationSec INT NULL`
  - `AiEnabled BIT NULL` *(Phase 17 — AI Voice Participant enabled for this session)*
  - `AiVoiceGender NVARCHAR(8) NULL` *(Phase 17 — `Male` | `Female`)*
  - `AiSpeechRate DECIMAL(3,2) NULL` *(Phase 17 — TTS rate multiplier, e.g. 0.75 / 1.00 / 1.25)*
  - `AiQuestionDelaySec INT NULL` *(Phase 17 — pause (sec) after candidate finishes before AI reads next line)*
- Constraints: `PK_tblSession_SessionId`, `FK_tblSession_HostUserId_tblUser_UserId`, `FK_tblSession_ScriptId_tblScript_ScriptId`, `UK_tblSession_JoinCode` (filtered `IsDeleted = 0`), `IDX_tblSession_Status`, `IDX_tblSession_HostUserId`, `IDX_tblSession_JoinCode`
- Valid status values: `LOBBY`, `ACTIVE`, `PAUSED`, `COMPLETED`, `ABANDONED`

#### tblSessionMember

- Primary key: `SessionMemberId BIGINT IDENTITY(1,1)`
- Business columns:
  - `SessionId BIGINT NOT NULL`
  - `UserId BIGINT NOT NULL`
  - `SlotIndex TINYINT NOT NULL`
  - `SlotName NVARCHAR(64) NOT NULL`
  - `IsReady BIT NOT NULL DEFAULT(0)`
  - `IsHost BIT NOT NULL DEFAULT(0)`
  - `JoinedAt DATETIME2 NULL`
  - `LeftAt DATETIME2 NULL`
  - `IsActive BIT NOT NULL DEFAULT(1)`
  - `IsAi BIT NOT NULL DEFAULT(0)` *(Phase 17 — slot held by the AI Voice Participant; see reserved system user under tblUser)*
- Constraints: `PK_tblSessionMember_SessionMemberId`, `FK_tblSessionMember_SessionId_tblSession_SessionId`, `FK_tblSessionMember_UserId_tblUser_UserId`, `UK_tblSessionMember_SessionId_SlotIndex` (filtered active rows), `IDX_tblSessionMember_SessionId`, `IDX_tblSessionMember_UserId`

### Stored Procedure Contracts

| SP | Input Parameters | Output / Return |
|---|---|---|
| `uspInsertSession` | `@SessionName`, `@SessionMode`, `@MaxMembers`, `@SessionDuration`, `@HostUserId`, `@ScriptId`, `@RoomExpiryMinutes`, `@CreatedBy`, `@IPAddress` | OUTPUT `@SessionId BIGINT`, `@JoinCode NVARCHAR(8)` |
| `uspInsertSessionMember` | `@SessionId`, `@UserId`, `@SlotIndex`, `@SlotName`, `@IsHost`, `@CreatedBy`, `@IPAddress` | `SessionMemberId` (non-query) |
| `uspInsertAiSessionMember` *(Phase 17)* | `@SessionId`, `@UserId`, `@SlotIndex`, `@SlotName`, `@CreatedBy`, `@IPAddress` | non-query; inserts `IsAi=1`, `IsReady=1`, `IsHost=0`; slot-occupied guard only (no duplicate-user guard) |
| `uspSetSessionAiConfig` *(Phase 17)* | `@SessionId`, `@AiEnabled`, `@AiVoiceGender`, `@AiSpeechRate`, `@AiQuestionDelaySec`, `@UpdatedBy`, `@IPAddress` | non-query; UPDATE AI config on `tblSession` |
| `uspGetSessionByJoinCode` | `@JoinCode` | RS1: `SessionId`, `SessionName`, `SessionMode`, `ScriptTitle`, `ScriptGrammarTag`, `Duration`, `MaxMembers`, `CurrentMemberCount`, `Status`; RS2: `SlotIndex`, `SlotName`, `IsOccupied`, `UserFullName`, `IsReady` |
| `uspGetSessionBySessionId` | `@SessionId` | RS1: `SessionId`, `SessionName`, `JoinCode`, `SessionMode`, `ScriptTitle`, `MaxMembers`, `SessionDuration`, `Status` (may be absent — see drift note); RS2: `UserId`, `FullName`, `AvatarUrl`, `SlotIndex`, `SlotName`, `IsReady`, `IsHost` (active members only) |
| `uspValidateJoinCode` | `@JoinCode` | OUTPUT: `@IsValid BIT`, `@SessionId BIGINT`, `@SessionName NVARCHAR(128)`, `@Status NVARCHAR(16)`, `@CurrentMemberCount INT` |
| `uspGetAvailableSlotsBySessionId` | `@SessionId` | Rows: `SlotIndex`, `SlotName`, `IsOccupied`, `UserFullName`, `IsReady` |
| `uspUpdateSessionMemberReadyStatus` | `@SessionId`, `@UserId`, `@IsReady`, `@UpdatedBy`, `@IPAddress` | non-query |
| `uspUpdateSessionStatus` | `@SessionId`, `@Status`, `@UpdatedBy`, `@IPAddress` | non-query |
| `uspUpdateSessionMemberLeft` | `@SessionId`, `@UserId`, `@UpdatedBy`, `@IPAddress` | non-query; auto-abandons session if: host leaves, OR 0 active members remain, OR session is ACTIVE and < 2 active members remain (migration 28). Never downgrades COMPLETED (migration 25 guard). |
| `uspGetSessionListByUserId` | `@UserId`, `@StatusFilter`, `@PageNumber`, `@PageSize` | RS1: `SessionId`, `SessionName`, `SessionMode`, `SessionDate`, `Duration`, `FluencyScore`, `MistakeCount`, `Status`, `ScriptTitle`; RS2: `TotalCount` |

### Domain Model

- Entities: `Session`, `SessionMember`
- Enums: `SessionModeType`, `SessionStatusType`
- EF configurations: `SessionConfiguration`, `SessionMemberConfiguration`
- DbContext: `DbSet<Session> Sessions`, `DbSet<SessionMember> SessionMembers`

### SessionModeType Enum Mapping

| Enum Value | Numeric | Stored String |
|---|---|---|
| `GrammarDrill` | 1 | `Grammar Drill` |
| `Roleplay` | 2 | `Roleplay` |
| `MockInterview` | 3 | `Mock Interview` |
| `VocabularySprint` | 4 | `Vocabulary Sprint` |
| `FluencyDrill` | 5 | `Fluency Drill` |
| `RepracticeRound` | 6 | `Repractice Round` |

Frontend sends numeric value; backend maps to stored string.

---

### Flow: Start Session with Script (Script Library → Create Session)

**Purpose:** User clicks "Start Session with this Script" in the Script Library preview sheet, which closes the sheet and navigates to the Create Session form with the chosen script pre-selected.

**Entry Points:**
- `/scripts` page → Preview bottom sheet (ScriptPreviewComponent) → "Start Session with this Script" button

**UI Trigger:** `(click)="startSession()"` on the primary button in `ScriptPreviewComponent`

**Flow:**
1. `startSession()` calls `sheetRef.dismiss()` then `router.navigate(['/session/create'], { state: { script: this.data } })`
2. `CreateSessionComponent` reads `router.getCurrentNavigation()?.extras?.state?.['script']` in its constructor and stores it as `_preSelectedScript`
3. In `ngOnInit()` after form and search setup, if `_preSelectedScript` is set, calls `selectScript(_preSelectedScript)` which patches the form's `scriptId` field and sets `selectedScript` signal

**Files:**
- `src/app/modules/scripts/script-library/script-preview.component.ts` — `startSession()` method
- `src/app/modules/session/create/create-session.component.ts` — constructor + `ngOnInit` pre-selection

**Notes on Drift Prevented:**
- Button existed visually but had no `(click)` binding — was non-functional from initial implementation
- Fixed: added `startSession()` handler + router state handoff + `CreateSessionComponent` constructor reads state

---

### Flow: Create Session

**Purpose:** Host creates a new session and is automatically placed in slot 1.

**Entry Points:**
- Frontend create-session screen (`ngClass` used for slash-based utility classes)
- UI triggers `Create Session` CTA after script is selected and all fields are filled
- Submit is enabled only when: session fields valid + `ScriptId` is set
- Can also be entered via "Start Session with this Script" from Script Library (script pre-selected via router state)

**UI Route / Screen:** `/session/create` or equivalent host create screen

**UI Trigger:** `Create Session` button click

**Preconditions:** User is authenticated; a valid script is selected

**Request Contract:**
```
POST /api/sessions
Authorization: Bearer {accessToken}
Body (CreateSessionRequestDto):
  - SessionName (string, required, min 3 max 60)
  - SessionMode (int enum, IGNORED — see drift note): derived on the backend from the script Category, not the DTO
  - MaxMembers (byte, IGNORED — see drift note): derived on the backend = count of distinct script SpeakerLabels
  - SessionDuration (int, required): must be one of [15, 30, 45, 60, 90]
  - ScriptId (long, required, > 0)
  - RoomExpiryMinutes (int, required): must be one of [60, 120, 360, 1440]
  - AiEnabled (bool, optional, default false) — Phase 17 AI Voice Participant
  - AiVoiceGender (string, required when AiEnabled): `Male` | `Female`
  - AiSpeechRate (decimal, required when AiEnabled): one of [0.75, 1.00, 1.25]
  - AiQuestionDelaySec (int, required when AiEnabled): one of [0, 1, 2, 3, 5]
```

**Response Contract:**
```
HTTP 200 — ApiResponse<CreateSessionResponseDto>
  - SessionId (long)
  - SessionName (string)
  - JoinCode (string, 8 chars)
  - Status (string): always "LOBBY"
  - ScriptTitle (string)
```

**Validation Rules:**
- `SessionName`: required, 3–60 chars
- `SessionMode` / `MaxMembers`: NOT validated from the DTO — derived from the script (see Business Rules + drift note)
- `SessionDuration`: exact values only — `[15, 30, 45, 60, 90]`
- `ScriptId`: > 0
- `RoomExpiryMinutes`: exact values only — `[60, 120, 360, 1440]`
- Script must exist and have at least 2 distinct `SpeakerLabel` values
- Host user must exist and be active
- **AI (Phase 17, only when `AiEnabled`):** `AiVoiceGender` ∈ {Male, Female}; `AiSpeechRate` ∈ {0.75, 1.00, 1.25}; `AiQuestionDelaySec` ∈ {0, 1, 2, 3, 5} (`CreateSessionRequestValidator.When(AiEnabled)`)

**Database Tables:** `tblSession` (insert), `tblSessionMember` (insert)

**Stored Procedures:**
- `uspInsertSession` → returns OUTPUT `@SessionId`, `@JoinCode`
- `uspInsertSessionMember` (host member)
- **Phase 17 (only when `AiEnabled`):** `uspSetSessionAiConfig` (persists `AiEnabled`/`AiVoiceGender`/`AiSpeechRate`/`AiQuestionDelaySec` on `tblSession`) + `uspInsertAiSessionMember` per non-host slot. `uspInsertAiSessionMember` is additive (core SPs untouched) and has the slot-occupied guard but NO duplicate-user guard, so the one reserved AI user can hold multiple AI slots.
- All calls wrapped in a single DB transaction; rollback on failure

**Business Rules:**
- `MaxMembers` and `SessionMode` are DERIVED from the script, not taken from the DTO. `MaxMembers` = count of distinct `SpeakerLabel`s; `SessionMode` = mapped from script `Category` (`MapCategoryToSessionMode`, incl. legacy aliases). The script must have ≥ 2 distinct labels, else: `"The selected script must have at least 2 distinct speaker labels."`
- Host is automatically inserted as slot 1 member; `IsReady = true`, `IsHost = true`
- Slot names derived from script `SpeakerLabel` values ordered by first `SequenceId` appearance then alphabetically
- `SessionMode` is stored as human-readable string (e.g., `"Grammar Drill"`), not the enum integer
- **AI Voice Participant (Phase 17):** when `AiEnabled`, the reserved AI system user (resolved by `MobileNumber = 'AI_PARTICIPANT'`) is inserted into EVERY non-host slot (`SlotIndex` 2..N) with `IsAi = 1`, `IsReady = 1`, `IsHost = 0`. This lets the host (candidate) run the session solo — an AI member counts as active, satisfying the ≥2-member start/abandon rules. If the reserved user is missing → creation fails: `"The AI Voice Participant is not available..."`. AI members are never scored (no `tblVoiceAnalysis`).

**State Transitions:** `tblSession.Status` starts as `LOBBY`

**SignalR / Realtime Events:** None at creation; hub join happens after navigation to lobby

**Failure Cases:**
- Host user not found → 200 `Success:false` "Session creation failed."
- Script not found → 200 `Success:false` "Session creation failed."
- Not enough script slots → 200 `Success:false` "Session creation failed."
- SP fails to return `@SessionId` or `@JoinCode` → throws `InvalidOperationException`

**Recovery / Fallback Logic:** DB transaction rollback on any exception during session or member insert

**Frontend (Phase 17 — AI Voice Participant):**
- `create-session.component.ts` has an "AI Voice Participant" toggle card. When ON it reveals three selects: Voice (`Female`/`Male` → `aiVoiceGender`), Speed (Slow/Normal/Fast → `aiSpeechRate` 0.75/1.00/1.25), Delay (0/1/2/3/5s → `aiQuestionDelaySec`). The payload includes these fields only when the toggle is on.
- **AI session skips the invite screen:** because the AI fills every non-host slot, there are no guest slots to invite — on success the client navigates straight to `/session/lobby/{sessionId}` instead of `/session/invite`. Human (non-AI) sessions still go to the invite screen.

**Notes on Known Drift Prevented:**
- **MaxMembers/SessionMode source drift (corrected 2026-06-17):** docs previously said both came from the DTO (`MaxMembers` 2–5, `SessionMode` numeric enum required). The code actually DERIVES both from the selected script (`SessionService.CreateSessionAsync`), and `CreateSessionRequestValidator` no longer validates them (frontend stopped sending them to avoid false 400s). Contract above now reflects the real behavior. **Drift type:** stale docs.
- `SessionDuration` and `RoomExpiryMinutes` are exact-value whitelists, not ranges
- **AI member insert (Phase 17):** AI slots reuse ONE reserved system user, which violates the human `uspInsertSessionMember` duplicate-user guard. Solved with an additive `uspInsertAiSessionMember` (no duplicate-user guard) rather than weakening the core SP — protects human create/join from regression.

---

### Flow: Validate Join Code

**Purpose:** Returns session preview with slot list. Used internally by the Create Session flow to fetch guest slots after session creation; the join-by-code UI has been removed.

**Entry Points:** `create-session.component.ts` — called after a successful `POST /api/sessions` to retrieve unoccupied guest slots for the invite screen.

**UI Route / Screen:** Not user-facing; invoked programmatically during session creation.

**UI Trigger:** Session creation success response

**Preconditions:** User is authenticated; join code is a 6-character string

**Request Contract:**
```
GET /api/sessions/validate/{joinCode}
Authorization: Bearer {accessToken}
Path param: joinCode — normalized to uppercase, trimmed
```

**Response Contract:**
```
HTTP 200 — ApiResponse<SessionPreviewResponseDto>
  - SessionId (long)
  - SessionName (string)
  - SessionMode (string)
  - ScriptTitle (string)
  - ScriptGrammarTag (string)
  - Duration (int)
  - MaxMembers (byte)
  - CurrentMemberCount (int)
  - Status (string)
  - Slots (List<SlotInfoDto>):
      - SlotIndex (byte)
      - SlotName (string)
      - IsOccupied (bool)
      - UserFullName (string, nullable)
      - IsReady (bool)
```

**Validation Rules:** `JoinCode` must be non-empty after trim; normalized to uppercase

**Database Tables:** `tblSession`, `tblSessionMember` (read via SPs)

**Stored Procedures:**
- `uspGetSessionByJoinCode(@JoinCode)` — RS1: session header; RS2: slot rows
- Fallback: if SP returns null, `CheckJoinCodeStatusAsync` runs an inline query to determine why:
  ```sql
  SELECT ses.Status, CAST(CASE WHEN ses.RoomExpiresAt <= GETDATE() THEN 1 ELSE 0 END AS BIT) AS IsExpired,
         ses.MaxMembers, COUNT(sem.SessionMemberId) AS CurrentMemberCount
  FROM dbo.tblSession ses
  LEFT JOIN dbo.tblSessionMember sem ON sem.SessionId = ses.SessionId AND sem.IsDeleted = 0 AND sem.IsActive = 1
  WHERE ses.JoinCode = @JoinCode AND ses.IsDeleted = 0
  GROUP BY ses.Status, ses.RoomExpiresAt, ses.MaxMembers
  ```

**Failure Cases:**
- Join code not found → `"This join code does not exist. Please check and try again."`
- Session expired (`RoomExpiresAt <= GETDATE()`) → `"This session has expired. Ask the host to create a new session."`
- Status is `ENDED` or `CANCELLED` → `"This session has already {status}."`
- `CurrentMemberCount >= MaxMembers` → `"This session is full. No available slots."`
- Any other → `"This session is no longer available."`

**Frontend Mapping Note:**
- Frontend maps `Duration`, `CurrentMemberCount` from this response into `SessionPreview` shape
- Fallback aliases retained only for compatibility with older payload shapes
- After validate succeeds, `create-session.component.ts` filters `Slots` where `IsOccupied = false` and passes them as query params to `/session/invite`

**Recovery / Fallback Logic:**
- SQL Server path may consume slot rows from result set 2 of the already-open `uspGetSessionByJoinCode` reader before disposing it
- PostgreSQL path must not call `GetAvailableSlotsBySessionIdAsync` from inside `GetSessionPreviewByJoinCodeAsync` while the first reader is still open on the same connection
- If the preview reader has no slot result set, dispose it first, then hydrate `Slots` through `uspGetAvailableSlotsBySessionId(sessionId)` on a new command
- If both the preview reader and standalone slot function return no slot rows, return an empty `Slots` list and treat it as data or stored-procedure drift

**Notes on Known Drift Prevented:**
- `uspGetSessionByJoinCode` must return `Duration` (not `SessionDuration`) for the repository to map correctly
- SQL Server preview contract returns slot rows as result set 2; the live PostgreSQL routine currently returns only the header row and requires a post-disposal fallback call to `uspGetAvailableSlotsBySessionId`
- Repository preview flow previously opened a second command (`uspGetAvailableSlotsBySessionId`) before disposing the active `uspGetSessionByJoinCode` reader; PostgreSQL rejects that pattern with an in-progress command error

---

### Flow: Get Lobby State

**Purpose:** Returns current lobby snapshot including members, readiness, and `CanStart` flag.

**Request Contract:**
```
GET /api/sessions/lobby/{sessionId}
Authorization: Bearer {accessToken}
Path param: sessionId (long)
```

**Response Contract:** `ApiResponse<LobbyStateResponseDto>` — same shape as Join Session response

**Business Rules:**
- `CanStart = Members.Count >= 2 AND All(m => m.IsReady)`
- Members list includes only `IsActive = true` members from `uspGetSessionBySessionId` RS2
- Status drift fallback runs after the lobby-state reader is disposed (see Join Session drift note)

**Stored Procedures:** `uspGetSessionBySessionId`

**Failure Cases:** `sessionId <= 0` → validation fail; session not found → `"Session lobby was not found."`

**SignalR / Realtime Events:**
- Lobby status messaging on frontend: once >= 2 active members exist, host UI switches from "waiting for players" to "waiting for readiness" state
- Frontend checks `CanStart` from this response to enable Start button

**Frontend Status-Redirect Contract (CRITICAL):**
- After loading lobby state, frontend checks `session.status` before rendering the lobby UI
- `ACTIVE` → immediately navigate to `/live-session/room/{sessionId}` (session already running; user is rejoining)
- `COMPLETED` or `ABANDONED` → navigate to `/user/dashboard` with error toast
- `LOBBY` → render lobby normally
- Without this check: users who navigate to the lobby URL for an ACTIVE session see stale ready-flags and the Start button, which fails with "Only lobby sessions can be started."

**Notes on Known Drift Prevented:**
- `btn-rejoin` links directly to `/session/lobby/{sessionId}` regardless of session status; lobby component must redirect ACTIVE sessions to the live room instead of rendering lobby UI.

---

### Flow: Ready / Unready

**Purpose:** Member toggles their readiness state in the lobby.

**Request Contract:**
```
PATCH /api/sessions/ready
Authorization: Bearer {accessToken}
Body (UpdateReadyStatusRequestDto):
  - SessionId (long, required, > 0)
  - IsReady (bool, required)
```

**Response Contract:** `ApiResponse<bool>` — `Data: true` on success

**Business Rules:**
- User must exist and be an active member of the lobby
- Calls `uspUpdateSessionMemberReadyStatus(@SessionId, @UserId, @IsReady, @UpdatedBy, @IPAddress)`

**SignalR / Realtime Events:**
- Hub method `SetReady(sessionId, userId, isReady)` calls the same service method
- Broadcasts `MEMBER_READY` to group: `{ userId, isReady }`

**Failure Cases:** User not found; user not in lobby members list

---

### Flow: Lobby Realtime Membership — Stable Contract (2026-06-05)

**Purpose:** Keep every lobby client's roster + readiness in sync in real time.

**Group membership:** `SessionHub.OnConnectedAsync` adds a connection to `session_{id}` **only if the SignalR URL carries `?sessionId=`** (the frontend `WebsocketService.connect(sessionId, …)` appends it). Auto-reconnect reuses the same URL, so membership is restored on every reconnect.

**Event triggers (who broadcasts what):**
- **MEMBER_JOINED** — broadcast by the hub `JoinLobby(sessionId, userId)` method. **The frontend MUST invoke `JoinLobby` after connecting** (`LobbyComponent.announceJoin`). Adding the connection to the group via `OnConnectedAsync` does NOT announce the join — without the explicit `JoinLobby` invoke, no `MEMBER_JOINED` is ever emitted and existing members never refresh.
- **MEMBER_READY** — broadcast by hub `SetReady`; handler patches the member's `ready` flag. If the member is not yet in the local roster, the client falls back to a full `loadLobby()` (covers a missed/out-of-order `MEMBER_JOINED`).
- **MEMBER_LEFT** — broadcast by hub `LeaveLobby` / grace-window `LobbyReconnectTracker`.

**Frontend handlers (`LobbyComponent.subscribeToLobbyEvents`):** `MEMBER_JOINED` → `loadLobby()`; `MEMBER_READY` → patch (or `loadLobby()` if member unknown); `SESSION_STARTED` → navigate to room; `MEMBER_LEFT` → remove from roster.

**Fallback poll (resilience):** `startSessionActivePoll()` calls `loadLobby()` every 3 s. This converges the roster + readiness within 3 s even if SignalR is degraded or an event was missed, and also catches `SESSION_STARTED`. It is the self-healing net behind the realtime path.

**Notes on Known Drift Prevented:**
- **Drift (2026-06-05): Host stuck at "1/2", never sees the guest join or become ready.** Root cause: the frontend never invoked the `JoinLobby` hub method, so `MEMBER_JOINED` was **never broadcast**. The host (in the group via `OnConnectedAsync`) received the guest's `MEMBER_READY`, but its handler patched a member not in the roster → silent no-op → count stayed `1/2`. Drift type: **missing client→hub invocation** (the hub method existed and was correct; nothing called it). **Fix:** (a) `LobbyComponent.announceJoin()` invokes `JoinLobby` after connect → `MEMBER_JOINED` fires → existing members `loadLobby()`; (b) `MEMBER_READY` handler does a full `loadLobby()` when the member is unknown; (c) the 3 s poll now refreshes the full roster (was ACTIVE-only); (d) `WebsocketService` logs connect/reconnecting/reconnected/close per hub. Any one of (a)/(c) fixes the symptom; together they are resilient to SignalR drops.

---

### Flow: Start Session

**Purpose:** Host starts the session, transitions it to ACTIVE, and initializes the first turn.

**Request Contract:**
```
POST /api/sessions/{sessionId}/start
Authorization: Bearer {accessToken}
Path param: sessionId (long)
```

**Response Contract:** `ApiResponse<bool>` — `Data: true` on success

**Preconditions:**
- Caller must be the session host (`tblSession.HostUserId == callerId`)
- `tblSession.Status` must be `LOBBY`
- `CanStart` must be true: >= 2 active members AND all `IsReady`

**Business Rules:**
1. `UpdateSessionStatus` → `ACTIVE`
2. `GetCurrentTurnAsync(sessionId)` — creates turn 1 immediately
3. If turn 1 creation fails: `UpdateSessionStatus` → `LOBBY` (rollback); return failure
4. Turn 1 failure propagates the inner error message

**Atomic Rollback:** If the live session turn initialization fails after the status is set to `ACTIVE`, the service reverts status back to `LOBBY`. No orphaned `ACTIVE` session without a current turn.

**SignalR / Realtime Events:**
- Hub method `StartSession(sessionId)` is the authoritative start path
- On success: hub immediately calls `GetCurrentTurnAsync(sessionId)` and broadcasts `SESSION_STARTED` to `session_{sessionId}`: `{ sessionId, firstSpeakerId }`
- `firstSpeakerId` is `currentTurn.ActiveMemberId` from the newly created active turn, not a lobby-member sort derived in a second read path
- **Host navigation**: host navigates in `emit('StartSession').then(...)` — immediately after the hub method resolves, NOT waiting for the `SESSION_STARTED` event. This prevents the Start button from getting stuck if the event is missed (connection drop between hub resolve and event delivery).
- **Host fallback recovery**: if the hub invoke resolves late, rejects after the backend already changed state, or the event is missed, the lobby component polls `GET /api/sessions/lobby/{sessionId}` for a short window; `status = ACTIVE` forces navigation to `/live-session/room/{sessionId}` and clears the `Starting...` state
- **Router fallback**: live-session navigation first uses Angular `router.navigateByUrl('/live-session/room/{sessionId}')`; if Angular returns `false` or does not move the URL, the lobby component falls back to `window.location.assign('/live-session/room/{sessionId}')`
- **Guest navigation**: guests still navigate on `SESSION_STARTED` event (they have no `.then()` path)
- On failure: hub throws the first detailed service error from `ApiResponse.Errors` when present; it must not collapse the cause back to the generic message `Session start failed.`
- Angular route target for successful start is `/live-session/room/:sessionId`, mounted under `path: 'live-session'` and protected by `authGuard` plus `sessionGuard`

**Frontend lobby state status (CRITICAL fix):**
- `getLobbyState()` in `session.service.ts` previously hardcoded `status: 'LOBBY'` — the actual DB status was ignored
- Fixed to `status: d.status ?? 'LOBBY'` so that if a session is ACTIVE/COMPLETED/ABANDONED, `loadLobby()` properly redirects instead of showing lobby UI
- Without this fix: a session in ACTIVE status would show the Start Session button, and clicking it would trigger a hub error without resetting `isStarting`, leading to the button being stuck in "Starting..." state indefinitely

**Failure Cases:**
- Not host → `"Only the host can start this session."`
- Not LOBBY status → `"Only lobby sessions can be started."`
- Not all ready → `"All active lobby members must be ready before the session can start."`
- Turn init failure → `"The session started, but the first turn could not be initialized."` (session rolled back to LOBBY)
- Speaker-slot mismatch during turn init → `"No active member holds the slot '{speakerLabel}'. Active slots: [{slots}]. Check that the script speaker labels match the session slot names."`
- Missing session utterances during turn init → `"The script linked to this session has no utterances."`

**Notes on Known Drift Prevented:**
- Session `14` showed `tblSession.Status = ACTIVE` and turn `1` existed while the host lobby button still displayed `Starting...`; this proves the business flow completed and the drift was in frontend recovery after a successful start
- Lobby start UX must reconcile hub errors or delayed responses against fresh lobby status before leaving the button in a permanent loading state
- Session `15` returned lobby state `status = ACTIVE` but the host remained on `/session/lobby/15`; redirect logic now treats failed Angular router navigation as a client-side drift and hard-navigates to the live-session route

---

### Flow: Leave Session (Lobby + Live Room)

**Purpose:** Member leaves a session — works for both LOBBY and ACTIVE status. Host leaving abandons the session.

**Request Contract:**
```
POST /api/sessions/{sessionId}/leave
Authorization: Bearer {accessToken}
Path param: sessionId (long)
```

**Response Contract:** `ApiResponse<bool>` — `Data: true` on success

**Business Rules:**
- User must be a currently **active** member (`IsActive = 1`) of the session — enforced via `GetLobbyStateBySessionIdAsync` LINQ filter
- Calls `uspUpdateSessionMemberLeft(@SessionId, @UserId, @UpdatedBy, @IPAddress)`
- SP sets `IsActive = 0`, `IsReady = 0`, `LeftAt = GETDATE()` for the leaving member
- SP auto-abandons session (`Status = 'ABANDONED'`) when: host leaves OR no active members remain OR (status = ACTIVE AND active member count < 2). Never downgrades a COMPLETED session (migration 25 guard).
- Calling leave when already inactive → "Session member was not found" (correct, idempotent-safe)

**`GetLobbyStateBySessionIdAsync` LINQ filter (CRITICAL):**
```csharp
where sessionMember.SessionId == sessionId
  && sessionMember.IsActive == true   // ← required — filters out left members
  && sessionMember.IsDeleted == false
  && user.IsDeleted == false
```
Without this filter: left members still appear in lobby, blocking rejoin and giving wrong CanStart.

**ResolveCanStart logic:**
```csharp
// Only active members are in Members list (filtered by IsActive above)
return activeMembers.Count >= 2 && activeMembers.All(m => m.IsReady);
```

**SignalR / Realtime Events — Lobby Hub (`/hubs/session`):**
- `LeaveLobby(sessionId, userId)` calls `LeaveSessionAsync` → broadcasts `MEMBER_LEFT { userId, slotIndex }`
- `OnDisconnectedAsync`: only calls leave when `Status == "LOBBY"`; skips for `ACTIVE` (hub has separate live session disconnect handling)

**SignalR / Realtime Events — Live Session Hub (`/hubs/live-session`):**
- `OnDisconnectedAsync`: always calls `_liveSessionService.MarkMemberLeftAsync(sessionId, userId)` — best-effort DB update (IsActive=0, IsReady=0); also broadcasts `MEMBER_LEFT { userId, slotIndex }`
- Frontend `confirmLeave()`: calls `POST /api/sessions/{sessionId}/leave` then navigates. Navigation always happens (success or error). `OnDisconnectedAsync` acts as the safety net for browser-close/network-loss.

**Rejoin flow (after leave):**
- The join-by-code page has been removed; rejoin is handled exclusively through the invitation flow (`SessionInvitationService.RespondToInvitationAsync`)
- `_sessionRepository.JoinSessionAsync` (repository method) inserts a new `tblSessionMember` row; the previous inactive row is kept for history
- `GetAvailableSlotsBySessionIdAsync` filters `AND IsActive = 1` — slot appears as available after leave

**Failure Cases:**
- User not found → 400 with failure (Session or user was not found)
- Session not found → 400 with failure (same message)
- User has NO member record at all for this session → 400 with failure (Session member was not found in the lobby)
- User is already inactive (already left) → **200 success** (idempotent — see Drift 5 below)

**Idempotency Contract (CRITICAL):**
`LeaveSessionAsync` is idempotent for users who have a member record (active or inactive).
Flow:
1. Check `GetLobbyStateBySessionIdAsync` — only returns `IsActive = 1` members
2. If user NOT in active members → call `HasSessionMemberAsync(sessionId, userId)` (checks any row including `IsActive = 0`)
3. If ANY member record exists → return success (already left — skip SP call, no double-update needed)
4. If NO member record exists → return failure "Session member was not found in the lobby."
5. If user IS active → call `uspUpdateSessionMemberLeft` → deactivate row

**`ISessionRepository.HasSessionMemberAsync` contract:**
```csharp
// LINQ — checks tblSessionMember with IsDeleted = 0, any IsActive value
_dbContext.SessionMembers.AnyAsync(sm => sm.SessionId == sessionId && sm.UserId == userId && sm.IsDeleted == false)
```

**Stored Procedures:** `uspUpdateSessionMemberLeft`, `uspGetAvailableSlotsBySessionId`, `uspInsertSessionMember` (all filter by `IsActive`)

**Notes on Known Drift Prevented:**
- **Drift 1:** `GetLobbyStateBySessionIdAsync` LINQ was missing `&& sessionMember.IsActive == true` — left members appeared in lobby state, blocking rejoin and giving wrong CanStart. Fixed 2026-05-22.
- **Drift 2:** `session-room.component.ts` `confirmLeave()` only called `router.navigate()` — no DB update on leave. Fixed 2026-05-22: now calls `POST /api/sessions/{sessionId}/leave` before navigating.
- **Drift 3:** `LiveSessionHub.OnDisconnectedAsync` only broadcast `MEMBER_LEFT` but did not call `MarkMemberLeftAsync` — DB state not updated on browser close or network loss. Fixed 2026-05-22.
- **Drift 4:** `uspUpdateSessionMemberLeft` previously did not reset `IsReady = 0`; leaving a lobby while ready preserved the flag — on rejoin the member appeared ready without toggling. Fixed in migration `FixMemberLeftSP_ResetIsReady`.
- **Drift 5:** `LeaveSessionAsync` returned 400 "Session member was not found in the lobby" when `IsActive = 0`, which happens legitimately when `OnDisconnectedAsync` (SessionHub or LiveSessionHub) fires on a WebSocket drop/reconnect cycle BEFORE the user manually clicks Leave. Frontend received 400 but user was already left — idempotent path was missing. Fixed 2026-05-23: added `HasSessionMemberAsync` check; if member exists (any state) but not active → return 200 success. Only returns 400 when member has NO record at all in the session.

---

### Flow: End Session (REST)

**Purpose:** Marks session as COMPLETED without generating a summary. Distinct from live-session complete.

**Request Contract:**
```
POST /api/sessions/{sessionId}/end
Authorization: Bearer {accessToken}
Path param: sessionId (long)
```

**Response Contract:** `ApiResponse<bool>`

**Business Rules:**
- Session must not already be `COMPLETED` or `ABANDONED`
- `UpdateSessionStatus` → `COMPLETED` (UpdatedBy = "System")
- No streak, badge, or mistake extraction — use live session `complete` endpoint for that

---

### Flow: Session History

**Purpose:** Returns paginated session history for the authenticated user.

**Request Contract:**
```
GET /api/sessions/history?statusFilter=&pageNumber=1&pageSize=20
Authorization: Bearer {accessToken}
Query params:
  - statusFilter (string, optional): LOBBY | ACTIVE | PAUSED | COMPLETED | ABANDONED
  - pageNumber (int, default 1)
  - pageSize (int, default 20)
```

**Response Contract:**
```
ApiResponse<PagedResult<SessionListItemResponseDto>>
PagedResult:
  - Items (List<SessionListItemResponseDto>):
      - SessionId, SessionName, SessionMode, SessionDate, Duration, FluencyScore (decimal, nullable), MistakeCount, Status, ScriptTitle
  - TotalCount, PageNumber, PageSize
```

**Stored Procedures:** `uspGetSessionListByUserId(@UserId, @StatusFilter, @PageNumber, @PageSize)` — RS1: session rows; RS2: `TotalCount`

**Notes:** `FluencyScore` returns `null` until voice-analysis data exists for the session

---

### Application and Infrastructure Wiring

- `ISessionService → SessionService`, `ISessionRepository → SessionRepository`, controller: `SessionController`
- SignalR hub: `SessionHub` at `/hubs/session`
- JWT bearer reads `access_token` query string for `/hubs/session`
- Hub auto-joins `session_{sessionId}` group when `sessionId` provided in query string
- `IUserIdProvider → JwtUserIdProvider`, `IHubConnectionTracker → HubConnectionTracker`

### SignalR Session Hub — Full Contract

**Hub path:** `/hubs/session`
**Authorization:** `UserOrAdmin` + `ActiveUser` policies
**Group format:** `session_{sessionId}`

**Client → Server methods:**

| Method | Parameters | Result |
|---|---|---|
| `JoinLobby` | `sessionId: string, userId: string` | Broadcasts `MEMBER_JOINED` |
| `SetReady` | `sessionId: string, userId: string, isReady: bool` | Broadcasts `MEMBER_READY` |
| `StartSession` | `sessionId: string` | Broadcasts `SESSION_STARTED` |
| `LeaveLobby` | `sessionId: string, userId: string` | Broadcasts `MEMBER_LEFT` |

**Server → Client events:**

| Event | Payload |
|---|---|
| `MEMBER_JOINED` | `{ userId: long, name: string, slotIndex: byte }` |
| `MEMBER_READY` | `{ userId: long, isReady: bool }` |
| `SESSION_STARTED` | `{ sessionId: long, firstSpeakerId: long }` |
| `MEMBER_LEFT` | `{ userId: long, slotIndex: byte }` |

**Connection rules:**
- Hub auto-joins group on `OnConnectedAsync` if `sessionId` in query string
- `userId` param in hub methods must match authenticated JWT `UserId` — enforced server-side
- `OnDisconnectedAsync` calls `LeaveSessionAsync` and broadcasts `MEMBER_LEFT` only when `Status == "LOBBY"`; skips when `ACTIVE`

**Frontend SignalR configuration:**
- `wsBaseUrl` stores HTTPS origin only (e.g., `https://localhost:44378`)
- Websocket service appends `/hubs/{hubPath}` — `wsBaseUrl` must not include `/hubs` suffix or `wss://` scheme
- Hub method calls guarded by connection-state check; methods invoked only when connection state is `Connected`
- **Token attachment:** the JWT is supplied via `accessTokenFactory` (NOT a static `?access_token=` query param). SignalR invokes the factory on **every negotiate and every auto-reconnect**, so the freshest `gwf_token` is always used. `sessionId` remains the only manual query param (`?sessionId={id}`). SignalR sends the token as a `Bearer` header on the negotiate HTTP request and as `access_token` query param on the WebSocket upgrade — both accepted by the backend (JWT bearer reads `access_token` query for `/hubs/*`).
- **Proactive refresh:** `accessTokenFactory` decodes the JWT `exp` (30s skew); if the token is missing/expired and a `gwf_refreshToken` exists, it awaits `AuthService.refreshToken()` and uses the new token before negotiating. Concurrent negotiate/reconnect attempts share one in-flight refresh.

**Notes on Drift — negotiate 401 (fixed 2026-06-17):**
- **Drift type:** stale-token / missing-fallback. `WebsocketService.connect()` read `gwf_token` once and baked it into a static `?access_token=` query string. `withAutomaticReconnect()` reuses the same URL, so after the short-lived access token rotated/expired (HTTP `authInterceptor` refreshes it, but the socket kept the old snapshot) every (re)negotiate sent an expired token → `Failed to complete negotiation … Status code '401'` at `/session/lobby/{id}`. An empty token (connect before login) hit the same 401.
- **Fix:** switched to `accessTokenFactory` + proactive `exp`-based refresh (see above). Backend hub contract unchanged.

### Migration State

- `InitialCreate_Phase1`, `AddAdminModule_Phase2`, `AddScriptModule_Phase3`, `AddSessionModule_Phase4`
- `AddStatusToLobbyStateSP` — adds `ses.Status` to `uspGetSessionBySessionId` RS1; required for `SessionHub` disconnect guard and `ResolveLobbyStatusAsync` fallback
- `FixMemberLeftSP_ResetIsReady` — adds `IsReady = 0` to `uspUpdateSessionMemberLeft` UPDATE; prevents stale ready-flag on member re-join
- `AddSessionInvitation_Phase16` — adds `tblSessionInvitation`, `ScheduledAt` on `tblSession`, 6 stored procedures, `ISessionInvitationService`, `ISessionInvitationRepository`, `SessionNotifier` SignalR push service

---

## Backend Session Invitation Module

### Module Scope

Push-based role assignment and invitation workflow. Host assigns specific users to roles before session starts. Invitees receive real-time notifications and accept/decline via dashboard. Replaces the friction of share-by-code join flow as the primary path.

### Database Schema

#### tblSessionInvitation

- `InvitationId BIGINT IDENTITY(1,1)` PK
- `SessionId BIGINT NOT NULL` → FK tblSession
- `UserId BIGINT NOT NULL` → FK tblUser (the invitee)
- `SlotIndex TINYINT NOT NULL`
- `SlotName NVARCHAR(64) NOT NULL`
- `Status NVARCHAR(16) NOT NULL DEFAULT 'PENDING'` — valid: PENDING, ACCEPTED, DECLINED, EXPIRED, CANCELLED
- `SentAt DATETIME2 NOT NULL DEFAULT GETDATE()`
- `RespondedAt DATETIME2 NULL`
- `ExpiresAt DATETIME2 NULL` — 24h for immediate sessions, 1h past ScheduledAt for scheduled sessions
- Full audit columns

#### tblSession — added column

- `ScheduledAt DATETIME2 NULL` — optional future session time; NULL = start immediately

### Stored Procedure Contracts

| SP | Purpose |
|---|---|
| `uspInsertSessionInvitation` | Idempotent insert; updates existing PENDING row for same session+user, otherwise inserts new |
| `uspUpdateInvitationStatus` | Sets ACCEPTED or DECLINED + RespondedAt |
| `uspCancelSessionInvitation` | Host rescinds (sets CANCELLED) |
| `uspGetInvitationsBySessionId` | Host waiting-room view: all invitations for session with user name + avatar |
| `uspGetPendingInvitationsByUserId` | User dashboard inbox: PENDING, non-expired invitations for the user |
| `uspSearchUsersByName` | Returns top 20 users matching LIKE %@SearchTerm%, excludes calling user |

### Flow: Send Invitations

#### Entry Points
- `/session/invite?sessionId=X&sessionName=Y&slots=[...]` — navigated from create-session after session is created

#### UI Trigger
- "Send Invitations" button (enabled only when all guest slots have an assigned user)

#### Request Contract
```
POST /api/sessions/{sessionId}/invitations
Authorization: Bearer {accessToken}
Body (SendInvitationsRequestDto):
  - SessionId (long, required)
  - Assignments (List<InvitationSlotAssignmentDto>):
      - UserId (long, required): the invitee
      - SlotIndex (byte, required): slot to assign
```

#### Response Contract
```
HTTP 200 — ApiResponse<List<SessionInvitationDto>>
  - InvitationId, SessionId, UserId, SlotIndex, SlotName, Status, SentAt, RespondedAt, ExpiresAt, FullName, AvatarUrl
```

#### Business Rules
1. Only host can send invitations (`session.HostUserId == hostUserId`)
2. Session must be in LOBBY status
3. Host slot (slot 1) is excluded from invitation — host already placed by CreateSession
4. `uspInsertSessionInvitation` is idempotent: resends to same user on same session updates existing PENDING row
5. ExpiresAt = ScheduledAt + 1h if scheduled, else Now + 24h
6. After DB insert, `SessionNotifier.NotifyInvitationReceivedAsync` fires `INVITATION_RECEIVED` to each invitee's personal SignalR group (`user_{userId}`)
7. Non-fatal if user not connected — they see invitation on next dashboard load

#### State Transitions
- `tblSessionInvitation.Status`: `PENDING` (on insert)

#### Failure Cases
- Host not found / not host → 400
- Session not in LOBBY → 400

### Flow: Respond to Invitation (Accept / Decline)

#### Entry Points
- `/user/invitations` — user dashboard invitation inbox
- Push notification tapped → deep-link to invitation page

#### Request Contract
```
PATCH /api/sessions/{sessionId}/invitations/{invitationId}
Authorization: Bearer {accessToken}
Body (RespondToInvitationRequestDto):
  - Status (string, required): "ACCEPTED" or "DECLINED"
```

#### Response Contract
```
HTTP 200 — ApiResponse<bool>
Success: true
```

#### Business Rules
1. Invitation must belong to the calling user (UserId must match JWT UserId)
2. Invitation must be in PENDING status — already-responded invitations return 400
3. If ACCEPTED: calls `JoinSessionAsync` to create a `tblSessionMember` row — user auto-joins the lobby
4. If DECLINED: no lobby record created
5. After status update, `SessionNotifier.NotifyInvitationRespondedAsync` fires `INVITATION_RESPONDED` to `session_{sessionId}` group (host sees update in lobby)

#### State Transitions
- `tblSessionInvitation.Status`: `PENDING → ACCEPTED` or `PENDING → DECLINED`
- `tblSessionMember` row inserted (IsReady=false) only on ACCEPTED

### Flow: Cancel Invitation (Host)

#### Request Contract
```
DELETE /api/sessions/{sessionId}/invitations/{invitationId}/cancel
Authorization: Bearer {accessToken}
```

#### Business Rules
1. Only session host can cancel
2. Sets Status → CANCELLED
3. Fires `INVITATION_CANCELLED` to invitee's personal group `user_{userId}`

### Flow: User Search (for role assignment)

#### Request Contract
```
GET /api/users/search?q={searchTerm}
Authorization: Bearer {accessToken}
```

#### Response Contract
```
HTTP 200 — ApiResponse<List<UserSearchResultDto>>
  - UserId, FullName, AvatarUrl
```

#### Business Rules
- Search term must be ≥ 2 characters
- Calls `uspSearchUsersByName` — LIKE '%@SearchTerm%' on FullName, max 20 results
- Excludes calling user from results

### Flow: My Invitations (User Inbox)

#### Request Contract
```
GET /api/users/invitations
Authorization: Bearer {accessToken}
```

#### Response Contract
```
HTTP 200 — ApiResponse<List<UserInvitationDto>>
  - InvitationId, SessionId, SlotIndex, SlotName, Status, SentAt, ExpiresAt
  - SessionName, SessionMode, SessionDuration, ScheduledAt
  - HostName, HostAvatarUrl
```

#### Business Rules
- Returns only PENDING invitations where ExpiresAt IS NULL OR ExpiresAt > NOW()
- Sorted by SentAt DESC

### SignalR Events Added (Session Hub)

| Event | Group | Direction | Payload |
|---|---|---|---|
| `INVITATION_RECEIVED` | `user_{userId}` | Server → Invitee | `{ invitationId, sessionId, sessionName, sessionMode, slotName, hostName, scheduledAt }` |
| `INVITATION_RESPONDED` | `session_{sessionId}` | Server → Host/Lobby | `{ invitationId, userId, fullName, slotName, status }` |
| `INVITATION_CANCELLED` | `user_{userId}` | Server → Invitee | `{ invitationId, sessionId, sessionName }` |

**User-level group:** `OnConnectedAsync` now always adds the authenticated user to group `user_{userId}` for invitation push delivery regardless of session context.

### Application Wiring

- `ISessionInvitationService → SessionInvitationService`
- `ISessionInvitationRepository → SessionInvitationRepository`
- `ISessionNotifier → SessionNotifier` (API layer, uses IHubContext<SessionHub>)
- Registered in `Program.cs` as scoped services
- `SessionController` extended with 4 invitation endpoints
- `UserController` extended with user search + my-invitations endpoints

### Frontend Components

| Component | Path | Purpose |
|---|---|---|
| `InviteSessionComponent` | `/session/invite` | Host role-assignment + send invitations after session creation |
| `MyInvitationsComponent` | `/user/invitations` | User inbox: view and accept/decline pending invitations |
| `UserDashboardComponent` | `/user/dashboard` | Shows pending invitation count badge; links to /user/invitations |
| `LobbyComponent` | `/session/lobby/:id` | Subscribes to `INVITATION_RESPONDED` to refresh members when invitee accepts |

### Session Lifecycle — Updated End-to-End

```
Host creates session  →  navigated to /session/invite
Host assigns roles    →  searches users by name, assigns each guest slot
Host sends invites    →  POST /api/sessions/{id}/invitations
                         INVITATION_RECEIVED pushed to each invitee (SignalR)
Invitee on dashboard  →  sees invitation badge + card
Invitee accepts       →  PATCH → Status=ACCEPTED, auto-joined to lobby, INVITATION_RESPONDED pushed
Invitee declines      →  PATCH → Status=DECLINED, INVITATION_RESPONDED pushed
Host in lobby         →  sees INVITATION_RESPONDED, lobby refreshes with new member
All ready             →  CanStart=true, host clicks Start
SESSION_STARTED       →  all navigate to live room
```

### Notes on Known Drift Prevented

- Join code (pull model) remains as fallback for ad-hoc sessions — invitation flow is primary path only
- Host's own slot (slot 1) is never included in invitation assignments; host is placed by CreateSession
- `uspInsertSessionInvitation` is idempotent to prevent duplicate rows on resend
- `JoinSessionAsync` inside `RespondToInvitationAsync` uses `HasSessionMemberAsync` guard to prevent duplicate member rows on repeated Accept calls

---

## Backend Live Session Module

### Module Scope

Active session execution: turn orchestration, re-read, voice analysis capture, listener feedback, session completion, summary.
Real-time events via SignalR at `/hubs/live-session`.

### Database Schema

#### tblTurnState

- Primary key: `TurnStateId BIGINT IDENTITY(1,1)`
- Business columns:
  - `SessionId BIGINT NOT NULL`
  - `TurnIndex INT NOT NULL`
  - `TotalTurns INT NOT NULL`
  - `ActiveMemberId BIGINT NOT NULL`
  - `ActiveSlotIndex TINYINT NOT NULL`
  - `UtteranceId BIGINT NOT NULL`
  - `ReReadAllowed BIT NOT NULL DEFAULT(1)`
  - `ReReadCount INT NOT NULL DEFAULT(0)`
  - `MaxReReads INT NOT NULL DEFAULT(2)`
  - `TurnStatus NVARCHAR(16) NOT NULL DEFAULT('ACTIVE')`
  - `TurnStartedAt DATETIME2 NULL`
  - `TurnCompletedAt DATETIME2 NULL`
- Constraints: `PK_tblTurnState_TurnStateId`, `FK_tblTurnState_SessionId`, `FK_tblTurnState_ActiveMemberId`, `FK_tblTurnState_UtteranceId`, `IDX_tblTurnState_SessionId`, `IDX_tblTurnState_SessionId_TurnIndex`
- Valid `TurnStatus` values: `ACTIVE`, `COMPLETED`

#### tblVoiceAnalysis

- Primary key: `VoiceAnalysisId BIGINT IDENTITY(1,1)`
- Business columns:
  - `SessionId BIGINT NOT NULL`
  - `UserId BIGINT NOT NULL`
  - `TurnIndex INT NOT NULL`
  - `UtteranceId BIGINT NOT NULL`
  - `TranscribedText NVARCHAR(512) NULL`
  - `ExpectedText NVARCHAR(512) NOT NULL`
  - `FluencyScore DECIMAL(5,2) NOT NULL DEFAULT(0)`
  - `ConfidenceScore DECIMAL(5,2) NOT NULL DEFAULT(0)`
  - `SpeakingSpeedWpm INT NOT NULL DEFAULT(0)`
  - `PauseCount INT NOT NULL DEFAULT(0)`
  - `HesitationWords NVARCHAR(256) NULL` (stored as CSV)
  - `RepeatedWords NVARCHAR(256) NULL` (stored as CSV)
  - `GrammarErrorsJson NVARCHAR(512) NULL` (stored as JSON)
  - `PronunciationJson NVARCHAR(512) NULL` (stored as JSON)
  - `OverallScore DECIMAL(5,2) NOT NULL DEFAULT(0)`
  - `RecordedAt DATETIME2 NOT NULL DEFAULT(GETDATE())`
  - `AudioStorageKey NVARCHAR(256) NULL` *(Phase 10 — R2 key for audio blob; set only when frontend sends `AudioBase64`; key pattern `sessions/{sessionId}/turns/{turnIndex}/{userId}.ogg`)*
- Constraints: `PK_tblVoiceAnalysis_VoiceAnalysisId`, FKs to `tblSession`, `tblUser`, `tblUtterance`, `IDX_tblVoiceAnalysis_SessionId`, `IDX_tblVoiceAnalysis_UserId`

#### tblListenerFeedback

- Primary key: `ListenerFeedbackId BIGINT IDENTITY(1,1)`
- Business columns:
  - `SessionId BIGINT NOT NULL`
  - `TurnIndex INT NOT NULL`
  - `FromUserId BIGINT NOT NULL`
  - `TargetUserId BIGINT NOT NULL`
  - `FeedbackTag NVARCHAR(32) NOT NULL`
  - `FeedbackAt DATETIME2 NOT NULL DEFAULT(GETDATE())`
- Constraints: `PK_tblListenerFeedback_ListenerFeedbackId`, FKs to `tblSession`, `tblUser` (from and target), `IDX_tblListenerFeedback_SessionId_TurnIndex`

### Stored Procedures

| SP | Purpose |
|---|---|
| `uspInsertTurnState` | Inserts new turn row |
| `uspGetCurrentTurnBySessionId` | Returns the current ACTIVE turn for the session |
| `uspUpdateTurnStatusByTurnStateId` | Marks a turn COMPLETED |
| `uspIncrementReReadCount` | Increments ReReadCount on current turn |
| `uspInsertVoiceAnalysis` | Inserts one voice analysis record |
| `uspGetVoiceAnalysisBySessionId` | Returns all voice analysis for session |
| `uspGetVoiceAnalysisByUserId` | Returns voice analysis for user across sessions |
| `uspInsertListenerFeedback` | Inserts one listener feedback record |
| `uspGetListenerFeedbackBySessionId` | Returns all feedback for session |
| `uspGetSessionCompletionSummary` | Returns per-member score summary + session totals |

### Domain Model

- Entities: `TurnState`, `VoiceAnalysis`, `ListenerFeedback`
- Enums: `TurnStatusType`, `ListenerFeedbackTagType`
- EF configurations: `TurnStateConfiguration`, `VoiceAnalysisConfiguration`, `ListenerFeedbackConfiguration`
- DbContext: `DbSet<TurnState> TurnStates`, `DbSet<VoiceAnalysis> VoiceAnalyses`, `DbSet<ListenerFeedback> ListenerFeedbacks`

---

### Flow: Get Current Turn

**Purpose:** Returns the active turn for the session. Lazily creates turn 1 if session is ACTIVE and no turn exists.

**Request Contract:**
```
GET /api/turns/{sessionId}/current
Authorization: Bearer {accessToken}
Path param: sessionId (long)
```

**Response Contract:**
```
ApiResponse<TurnStateResponseDto>
  - SessionId (long)
  - TurnIndex (int)
  - TotalTurns (int)
  - ActiveMemberId (long)
  - ActiveMemberName (string)
  - ActiveMemberAvatarUrl (string?): presigned R2 URL for the active speaker's avatar; null if no photo uploaded; resolved in service layer via ResolveAvatarUrlAsync
  - ActiveSlotIndex (byte)
  - Utterance (UtteranceResponseDto): full utterance record for this turn
  - ReReadAllowed (bool)
  - ReReadCount (int)
  - MaxReReads (int): always 2 for new turns
  - IsFacilitatorTurn (bool): true when the active slot is a facilitator role (Interviewer/Tutor/Coach) — read-aloud, no scoring
  - IsAi (bool): *(Phase 17)* true when the active slot is held by the AI Voice Participant. Client narrates via on-device TTS (no recognizer, no scoring) and then calls `AdvanceAiTurn`. Computed by a slot-match EXISTS against `tblSessionMember.IsAi` (matched on `ActiveSlotIndex`, NOT UserId — the one reserved AI user can hold multiple slots).
  - AiVoiceGender (string?), AiSpeechRate (decimal?), AiQuestionDelaySec (int?): *(Phase 17)* session AI config, joined from `tblSession` onto the turn payload so the narrating client has rate/voice/delay without a second call. Null on non-AI sessions.
```

**Business Rules:**
- If active turn exists → return it
- If no active turn and session `Status == "ACTIVE"` → create turn 1 (same logic as shift)
- If no active turn and session not `ACTIVE` → fail with current status

**Stored Procedures:** `uspGetCurrentTurnBySessionId` (via EF-backed repository)

**Failure Cases:**
- `sessionId <= 0` → validation fail
- Session not ACTIVE and no current turn → `"Session is not active. Current status: {status}."`
- Turn inserted but not retrievable → `"Turn was inserted but could not be retrieved. Check uspGetCurrentTurnBySessionId stored procedure."`

---

### Flow: Shift Turn (Complete Turn)

**Purpose:** Active speaker marks current turn complete and the next turn is created.

**Entry Points:**
- REST: `POST /api/turns/{sessionId}/shift`
- SignalR hub: `CompleteTurn(sessionId, memberId, turnIndex, score)`

**Request Contract:**
```
POST /api/turns/{sessionId}/shift
Authorization: Bearer {accessToken}
Body (TurnShiftRequestDto):
  - SessionId (long, > 0)
  - MemberId (long, > 0) — must equal authenticated userId
  - TurnIndex (int, > 0) — must match current active turn
  - AnalysisScore (decimal)
```

**Response Contract:**
```
ApiResponse<TurnStateResponseDto> — TurnStateResponseDto for the next turn
  (same shape as Get Current Turn response)
```

**Business Rules (ORDER IS CRITICAL — validate-before-mutate):**
1. `MemberId` must equal authenticated `userId` — only the active speaker can shift
2. Current turn must have `ActiveMemberId == MemberId` AND `TurnIndex == dto.TurnIndex`
3. Session must be `ACTIVE`
4. **Resolve & validate the next turn FIRST, before any mutation** (`ResolveNextTurnAsync` — pure, no DB writes). This resolves the next speaker by matching `tblUtterance.SpeakerLabel` to `tblSessionMember.SlotName` (case-insensitive trim match) and checks bounds. The current turn is NOT touched yet.
   - If resolution returns an **error** (no active members, no utterances, slot mismatch) → return failure; current turn stays `ACTIVE`; **session is NOT bricked** and the speaker can retry after the config is fixed.
   - If `nextTurnIndex > orderedUtterances.Count` → **end of script** (NOT an error): mark the final turn `COMPLETED` and return the completion signal (`Message = TurnShiftSignals.SessionComplete = "No further turns remain"`) so the hub auto-completes the session.
5. Otherwise **atomic advance** via `CompleteAndAdvanceTurnAsync`: in ONE DB transaction, mark current turn `COMPLETED` (`uspUpdateTurnStatusByTurnStateId`) AND insert the next turn (`uspInsertTurnState`). If the insert fails, the completion is rolled back — the session can never be left without an `ACTIVE` turn.
6. After a successful advance, re-fetch the canonical current turn (`GetCurrentTurnAsync`) and resolve the avatar URL for the broadcast.
7. `MaxReReads` is always 2 for new turns

**Repository contract:**
- `CompleteAndAdvanceTurnAsync(completedTurnStateId, completedStatus, completedBy, completedByIp, nextTurn, ct)` — transactional (mirrors `SessionRepository.CreateSessionAsync` pattern). This is the ONLY path that should mark-complete-then-insert; never call `UpdateTurnStatusAsync(COMPLETED)` followed by a separate `InsertTurnStateAsync` for a shift.
- `ResolveNextTurnAsync` returns `(TurnState? NextTurn, bool IsEndOfScript, string? Error)`; `CreateNextTurnAsync` (start-session/turn-1 path) wraps it: resolve → insert → read back.

**Next Speaker Resolution:**
```
nextUtterance = orderedUtterances[nextTurnIndex - 1]
activeMember = activeMembers.FirstOrDefault(m =>
    m.SlotName.Trim().Equals(nextUtterance.SpeakerLabel.Trim(), OrdinalIgnoreCase))
```
If no member matches → error: `"No active member holds the slot '{speakerLabel}'. Active slots: [{slots}]. Check that the script speaker labels match the session slot names."`

**SignalR / Realtime Events:**
- Hub `CompleteTurn(sessionId, memberId, turnIndex, score)` calls `ShiftTurnAsync`
- Broadcasts `TURN_SHIFT` to `live_{sessionId}`: `{ newActiveMemberId, newActiveMemberName, activeMemberAvatarUrl, slotIndex, turnIndex, nextUtterance, isAi }` (`isAi` added Phase 17 — tells clients whether the NEXT turn is AI-narrated; emitted by both `CompleteTurn` and `AdvanceAiTurn` via the shared `BroadcastTurnShiftAsync`)

**Frontend Transition Contract:**
- `TURN_SHIFT` is a partial event, not a full `TurnStateResponseDto`
- The active speaker must submit turn completion through hub method `CompleteTurn`, not the REST `POST /api/turns/{sessionId}/shift` endpoint, so every connected client receives `TURN_SHIFT` without needing a manual page reload
- After receiving `TURN_SHIFT`, the live-session room must refresh `GET /api/turns/{sessionId}/current` to hydrate the canonical state for all clients
- The room may optimistically swap `activeMemberId`, `activeMemberName`, `activeMemberAvatarUrl`, `turnIndex`, and `utterance`; `activeSlotIndex` is optional in the temporary client-side transition because the canonical current-turn reload runs immediately afterward
- The shared frontend `TurnState` model should still include `activeSlotIndex` to match the backend contract, but the live room must not depend on that field in the optimistic `TURN_SHIFT` patch path
- `activeMemberName` MUST be included in the optimistic `TURN_SHIFT` patch — the `updateState()` guard blocks same-turn API confirmations from overwriting state (prevents double-trigger of `ngOnChanges`), so the name must come from the event itself, not from the subsequent `loadCurrentTurn()` response

**Failure Cases:**
- `MemberId != userId` → `"Only the active speaker can complete the current turn."`
- Session, current turn, or user not found → `"Session, current turn, or user was not found."`
- Session not ACTIVE → `"Session must be active to shift turns."`
- Turn mismatch → `"The provided turn does not match the active turn."`
- No further turns → `"No further turns remain in this session. Complete the session."`
- Speaker label not matched (next turn) → `"No active member holds the slot '{speakerLabel}'. Active slots: [{slots}]. Check that the script speaker labels match the session slot names."`
- Next utterance index exceeds count → `"Turn {n} exceeds the total utterance count ({count}). Session may already be complete."`
- No active members → `"No active session members found."`
- Script has no utterances → `"The script linked to this session has no utterances."`

**Error Message Contract (CRITICAL — where the reason lives):**
- `ShiftTurnAsync` returns `ApiResponse.FailureResult(errors, message)`. The **specific** cause is in `Errors[0]`; `Message` is a generic bucket label that is `"Turn shift failed."` for EVERY non-completion failure.
- `LiveSessionHub.CompleteTurn` surfaces failures via the `DescribeFailure(response)` helper, which joins `Errors` and falls back to `Message`. So the client `HubException` now carries the specific reason (e.g. `"No active member holds the slot 'Customer'..."`), **not** the generic `"Turn shift failed."`.
- The completion signal is the exception: it is carried in `Message` (`TurnShiftSignals.SessionComplete` = `"No further turns remain"`) and matched there by the hub before any error is thrown.

**Notes on Known Drift Prevented:**
- `SpeakerLabel` and `SlotName` matched case-insensitively with trim — prevents mismatches from whitespace or casing differences in script upload vs. session slot assignment
- Treating `TURN_SHIFT` as a full turn DTO leaves listeners on stale speaker text and blocks the next speaker from seeing the recorder; clients must re-fetch current turn after the event
- **Speaker name drift (2026-06-03):** `handleTurnShift()` spread `...currentState` into the optimistic update, carrying the previous turn's `activeMemberName`. The `updateState()` guard (which prevents same-`turnIndex` re-fires to avoid double-triggering `ngOnChanges`) blocked the subsequent `loadCurrentTurn()` response from correcting it. Result: when the same role (e.g. "Receptionist") appeared multiple times in the script, every turn transition showed the previous speaker's name until the next turn. **Fix:** `newActiveMemberName` added to the `TURN_SHIFT` hub broadcast and consumed in the optimistic patch in `handleTurnShift`. The name is now correct from the moment the event fires.
- **Bricked-session on turn shift (2026-06-05, CRITICAL):** `ShiftTurnAsync` marked the current turn `COMPLETED` **before** attempting to create the next turn, with no transaction. When next-turn creation failed — most commonly because the **current turn was the last turn** (`nextTurnIndex > utteranceCount`), but also on a speaker-label/slot mismatch — the current turn was already `COMPLETED` and no `ACTIVE` turn remained. The session was permanently stuck: every subsequent `CompleteTurn` returned `"Session, current turn, or user was not found."` (because `GetCurrentTurnEntityAsync` filters `TurnStatus == 'ACTIVE'`), while voice-analysis saves still "succeeded" against the completed turn (`GetTurnBySessionAndTurnIndexAsync` has no ACTIVE filter), producing the confusing "save OK + shift fail" pair. Reproduced on sessions 93 and 94 at turn 16. **Compounding latent bug:** the end-of-script path never actually fired — `CreateNextTurnAsync` returned an *error* string for `nextTurnIndex > count`, but `ShiftTurnAsync` only treated a `(null, null)` as completion, so the documented "auto-complete on last turn" path (see Session Completion) threw `"Turn shift failed."` and bricked the final turn instead of completing. **Drift type:** missing transaction / mutation-before-validation + stale docs (documented auto-complete behavior not implemented). **Fix:** split resolution from persistence (`ResolveNextTurnAsync`), validate the next turn before any mutation, route true end-of-script to the `SessionComplete` signal, and make complete+insert atomic via `CompleteAndAdvanceTurnAsync`. **Recovery for already-bricked sessions:** they have a `COMPLETED` last turn and no `ACTIVE` turn — finalize them with the **End Session** button (`EndSession` hub → `CompleteSessionAsync`, which does not require an active turn) to get the summary; or start a fresh session.
- **Generic error masking (2026-06-05):** `LiveSessionHub.CompleteTurn` threw `new HubException(response.Message)`, and `ShiftTurnAsync` stamps `Message = "Turn shift failed."` on every non-completion failure while putting the real cause in `Errors`. Result: the client (and the warning log) only ever saw `HubException: Turn shift failed.` with no way to tell whether it was a turn mismatch, wrong user, or a script speaker-label / session-slot mismatch on the next turn. **Drift type:** stale docs / contract drift — the Failure Cases above claimed specific messages surfaced, but the code surfaced the generic bucket. **Fix:** added `DescribeFailure<T>(ApiResponse<T>)` helper in the hub (joins `Errors`, falls back to `Message`); `CompleteTurn`, `SubmitListenerFeedback`, `RequestReRead`, and `EndSession` now throw the specific reason and log it. The most common underlying cause of a real (non-stale) `CompleteTurn` failure is the next-turn speaker-label/slot mismatch from `CreateNextTurnAsync` — that exact text now reaches the client.
- **Listener-screen avatar missing (2026-06-04):** `TurnState` had no `activeMemberAvatarUrl` field; `<app-user-avatar>` in listener-screen always fell back to initials. **Fix (full-stack):** `TurnStateResponseDto.ActiveMemberAvatarUrl` added; `LiveSessionRepository.GetCurrentTurnAsync` selects `activeMember.AvatarUrl` (already joined from `tblUser`); `LiveSessionService.ResolveAvatarUrlAsync` resolves R2 key to presigned URL and is called after both the existing-turn and created-turn return paths in `EnsureCurrentTurnAsync` / `CreateNextTurnAsync`; `LiveSessionHub.CompleteTurn` adds `activeMemberAvatarUrl` to the `TURN_SHIFT` broadcast; `TurnState` frontend model adds `activeMemberAvatarUrl?: string | null`; `TurnShiftEvent` type updated; optimistic patch in `handleTurnShift` sets `activeMemberAvatarUrl`; listener-screen template binds `[avatarUrl]="turnState.activeMemberAvatarUrl"` on `app-user-avatar`.

---

### Flow: Advance AI Turn (Phase 17)

**Purpose:** Advance a turn that is currently held by the AI Voice Participant. The AI reads its scripted line via on-device TTS on the candidate's device; when done, the candidate's client advances the turn. No voice analysis is written for AI turns.

**Entry Points:**
- REST: `POST /api/turns/{sessionId}/advance-ai`
- SignalR hub: `AdvanceAiTurn(sessionId, turnIndex)`

**Request Contract:**
```
POST /api/turns/{sessionId}/advance-ai
Authorization: Bearer {accessToken}
Body (AdvanceAiTurnRequestDto):
  - TurnIndex (int) — must match the current active (AI) turn; SessionId comes from the route
```

**Response Contract:** `ApiResponse<TurnStateResponseDto>` for the next turn (same shape as Get Current Turn; includes `isAi` so the client knows if the NEXT turn is also AI).

**Business Rules (validate-before-mutate, mirrors Shift Turn):**
1. Caller must be an **active human member** of the session (`GetActiveSessionMemberByUserIdAsync`; `callerMember.IsAi` must be false). The AI holds no hub connection, so it never calls this.
2. Session must be `ACTIVE`; `turnIndex` must equal the current active turn.
3. The current turn must be **AI-held** (`currentTurnDto.IsAi == true`); otherwise rejected — human turns must go through `CompleteTurn` (which scores). 
4. Advance reuses the **shared** `AdvanceFromCurrentTurnAsync` (same as `CompleteTurn`): resolve+validate next turn → atomic complete-and-advance, or route true end-of-script to the `TurnShiftSignals.SessionComplete` signal. **No `tblVoiceAnalysis` write.**

**Stored Procedures / DB:** none new — reuses `uspUpdateTurnStatusByTurnStateId` + `uspInsertTurnState` via `CompleteAndAdvanceTurnAsync`.

**Realtime Events:**
- Hub `AdvanceAiTurn` broadcasts `TURN_SHIFT` (same payload incl. `isAi`) via the shared `BroadcastTurnShiftAsync`.
- End of script → hub auto-completes the session and broadcasts `SESSION_ENDED` (identical to `CompleteTurn`).

**Failure Cases:**
- Not an AI turn → `"The current turn is not an AI turn."`
- Caller is the AI member → `"AI members cannot advance turns."`
- Turn mismatch → `"The provided turn does not match the active turn."`
- Session not active / not found / caller not a member → respective messages, bucket `"AI turn advance failed."`
- Specific cause is surfaced to the client via the hub's `DescribeFailure` (joins `Errors`), same as `CompleteTurn`.

**Frontend narration (Phase 17 — session-room):**
- On the canonical current-turn (`updateState`), if `turnState.isAi` the client narrates `utterance.englishText` via `TtsService` (`@capacitor-community/text-to-speech` v8 — native Android TTS on the APK, Web Speech on web) at `aiSpeechRate`, gender best-effort from `aiVoiceGender`, lang `en-US`. On completion it waits `aiQuestionDelaySec` then calls hub `AdvanceAiTurn(sessionId, turnIndex)`. Fires once per turn (`_narratedTurnKey` guard). Recognizer is NOT started on AI turns — the human is a listener then, so `SpeakerScreenComponent` (which owns the recognizer) is not rendered. TTS is output-only → no mic contention. Timers/TTS are stopped on `SESSION_ENDED` and `ngOnDestroy`.
- Files: `core/services/voice/tts.service.ts` (new), `session-room.component.ts` (narration trigger), `voice.model.ts` (`TurnState.isAi` + AI config).
- **Known limitation:** narration currently runs on every connected human client. In a solo + AI session (the target use case) there is exactly one human, so this is correct. In a multi-human + AI session, multiple devices may speak the AI line simultaneously (overlapping audio); turn integrity is still safe because the backend rejects the duplicate `AdvanceAiTurn`. A host-only narrator designation is a future refinement.

**Notes on Known Drift Prevented:**
- The brick-prevention resolve-before-mutate logic is **single-sourced** in `AdvanceFromCurrentTurnAsync`; `CompleteTurn` and `AdvanceAiTurn` both call it so the two paths cannot diverge and re-introduce the 2026-06-05 bricked-session bug.
- AI turns reuse one reserved system user across slots, so `IsAi` on the turn state is resolved by **slot match** (`ActiveSlotIndex`), never by UserId.

---

### Flow: Save Voice Analysis

**Purpose:** Active speaker submits their voice analysis result for a completed turn.

**Request Contract:**
```
POST /api/turns/{sessionId}/voice-analysis
Authorization: Bearer {accessToken}
Body (SaveVoiceAnalysisRequestDto):
  - SessionId (long, > 0)
  - TurnIndex (int, > 0)
  - UtteranceId (long, > 0)
  - TranscribedText (string, nullable)
  - ExpectedText (string, required)
  - FluencyScore (decimal)
  - ConfidenceScore (decimal)
  - SpeakingSpeedWpm (int)
  - PauseCount (int)
  - HesitationWords (List<string>) — stored as CSV in tblVoiceAnalysis.HesitationWords
  - RepeatedWords (List<string>) — stored as CSV in tblVoiceAnalysis.RepeatedWords
  - GrammarErrors (List<GrammarErrorDto>):
      - ExpectedPhrase (string)
      - SpokenPhrase (string)
      - ErrorType (string)
      - Position (int)
  - PronunciationIssues (List<PronunciationIssueDto>):
      - Word (string)
      - ExpectedPhonetic (string)
      - IssueNote (string)
  - OverallScore (decimal)
  - AudioBase64 (string, optional) — *(Phase 10)* Base64-encoded audio blob; when provided, audio is uploaded to `gwf-audio` R2 bucket and key saved to `tblVoiceAnalysis.AudioStorageKey`; invalid Base64 is silently skipped (does not fail the voice analysis save)
```

**Response Contract:**
```
ApiResponse<VoiceAnalysisResponseDto>
  - VoiceAnalysisId, SessionId, UserId, FullName, TurnIndex, UtteranceId
  - TranscribedText, ExpectedText
  - FluencyScore, ConfidenceScore, SpeakingSpeedWpm, PauseCount
  - HesitationWords (List<string>), RepeatedWords (List<string>)
  - GrammarErrors (List<GrammarErrorDto>)
  - PronunciationIssues (List<PronunciationIssueDto>)
  - OverallScore, RecordedAt (DateTime UTC)
```

**Validation Rules:**
- `SessionId`, `TurnIndex`, `UtteranceId`, `UserId` all > 0
- Caller must be active member of the session
- Turn's `ActiveMemberId` must equal `userId` AND turn's `UtteranceId` must equal `dto.UtteranceId`
- Only one voice analysis record allowed per session + user + turnIndex (duplicate check before insert)

**Storage Notes:**
- `HesitationWords` and `RepeatedWords` stored as comma-separated strings in NVARCHAR column
- `GrammarErrors` and `PronunciationIssues` stored as JSON strings in NVARCHAR columns
- Deserialized back to typed lists in response
- PostgreSQL insert path requires `GrammarErrorsJson` and `PronunciationJson` to be sent as `jsonb` parameters, not plain `varchar` parameters

**Failure Cases:**
- Session member or turn not found → `"Session member or turn was not found."`
- Caller not active speaker for the turn → `"Voice analysis can only be saved for the caller's active turn."`
- ~~Duplicate → rejected~~ — **REMOVED (2026-05-23 Drift 2 fix)**. Duplicate is now an UPDATE (UPSERT semantics). Re-recording the same active turn before `CompleteTurn` is valid (e.g. page refresh). Returns 200 with message `"Voice analysis updated successfully."`

**Frontend UtteranceData Model Contract:**
- `UtteranceData` interface MUST include `utteranceId: number` (the DB primary key from `tblUtterance.UtteranceId`)
- The `utteranceId` field in the voice analysis payload must come from `turnState.utterance.utteranceId` — NOT `turnState.utterance.sequenceId`
- `sequenceId` is the display position in the script (1, 2, 3...) and is NOT the DB key
- The backend `UtteranceResponseDto` returns both `utteranceId` and `sequenceId`; only `utteranceId` is used for API validation

**Notes on Known Drift Prevented:**
- Session 32 passed accidentally: its first utterance happened to have DB `UtteranceId = 1`, matching `sequenceId = 1`
- Session 33 (new script) failed: new utterances have different DB IDs, exposing the bug
- Root cause: `UtteranceData` interface was missing `utteranceId`; component used `sequenceId` as substitute
- Fix: Added `utteranceId: number` to `UtteranceData` and changed `speaker-screen.component.ts` line 103 to use `utteranceId`
- PostgreSQL `uspInsertVoiceAnalysis` expects `p_grammarerrorsjson jsonb` and `p_pronunciationjson jsonb`; sending them as generic string parameters causes function-resolution failure before insert

---

### Flow: Listener Feedback

**Purpose:** Non-speaker session members tag the active speaker's performance during a turn.

**Entry Points:**
- REST: `POST /api/turns/{sessionId}/listener-feedback`
- SignalR hub: `SubmitListenerFeedback(sessionId, tag, targetTurnIndex)`

**Request Contract:**
```
POST /api/turns/{sessionId}/listener-feedback
Authorization: Bearer {accessToken}
Body (ListenerFeedbackRequestDto):
  - SessionId (long, > 0)
  - TurnIndex (int, > 0)
  - TargetUserId (long, > 0)
  - FeedbackTag (string, required)
```

**Valid FeedbackTag values (normalized server-side):**
- `"Good"` → `"Good"`
- `"Hesitated"` → `"Hesitated"`
- `"Mistake"` → `"Mistake"`
- `"Unclear Pronunciation"` → `"Unclear Pronunciation"`
- `"UnclearPronunciation"` → `"Unclear Pronunciation"` (alias accepted)

**Business Rules:**
1. `TargetUserId` must match the `ActiveMemberId` of the requested turn
2. `TargetUserId` must not equal `userId` (no self-feedback)
3. Duplicate check: same session + turnIndex + fromUserId + targetUserId + normalizedTag is rejected
4. Hub path: `SubmitListenerFeedback(sessionId, tag, targetTurnIndex)` — hub resolves `TargetUserId` from the turn

**SignalR / Realtime Events:**
- Hub broadcasts `LISTENER_TAG` to `live_{sessionId}`: `{ tag, fromUserId }`

**Frontend Transition Contract:**
- Listener quick-feedback buttons must call hub method `SubmitListenerFeedback(sessionId, tag, targetTurnIndex)` so all clients receive `LISTENER_TAG` immediately
- REST `POST /api/turns/{sessionId}/listener-feedback` remains valid as a data endpoint, but using it directly in the live room bypasses the realtime broadcast and leaves other clients stale until reload

**Failure Cases:**
- Invalid feedback tag → `"FeedbackTag is invalid."`
- Source, target, or turn not found → `"Feedback source, target, or turn was not found."`
- Target not active speaker for turn → `"TargetUserId does not match the requested turn speaker."`
- Self-feedback → `"Users cannot submit listener feedback for themselves."`
- Duplicate → `"Duplicate listener feedback is not allowed for the same turn and tag."`

---

### Flow: Re-Read

**Purpose:** Active speaker requests to re-read the current turn's utterance. Capped at 2.

**Entry Points:**
- REST: `POST /api/turns/{sessionId}/re-read`
- SignalR hub: `RequestReRead(sessionId, requesterId)`

**Request Contract:**
```
POST /api/turns/{sessionId}/re-read
Authorization: Bearer {accessToken}
Path param: sessionId (long)
```

**Business Rules:**
- Current turn must exist
- `ReReadAllowed` must be `true`
- `ReReadCount < MaxReReads` (MaxReReads is always 2)
- Calls `uspIncrementReReadCount(@TurnStateId, @UpdatedBy, @IPAddress)`

**SignalR / Realtime Events:**
- Hub broadcasts `RE_READ_REQUESTED` to `live_{sessionId}`: `{ requesterId, reReadCount }` (reReadCount = current count after increment)

**Frontend Transition Contract:**
- Speaker re-read requests in the live room must use hub method `RequestReRead(sessionId, requesterId)` so listeners receive the banner immediately
- Calling the REST re-read endpoint directly updates the backend count but does not emit `RE_READ_REQUESTED` to already connected live-room clients

**Failure Cases:**
- Current turn not found or member not active → `"Current turn or session member was not found."`
- Re-reads exhausted → `"No re-reads remain for the current turn."`

---

### Flow: Complete Session

**Purpose:** Ends the live session, extracts mistakes, updates streaks and badges, returns per-member summary.

**Entry Points:**
- REST: `POST /api/sessions/{sessionId}/complete`
- SignalR hub: `EndSession(sessionId)`

**Request Contract:**
```
POST /api/sessions/{sessionId}/complete
Authorization: Bearer {accessToken}
Path param: sessionId (long)
```

**Response Contract:**
```
ApiResponse<SessionSummaryResponseDto>
  - MemberScores (List<MemberScoreDto>):
      - UserId (long)
      - FullName (string)
      - AvatarUrl (string|null): presigned R2 URL (resolved in service layer); null if no avatar set
      - FluencyScore (decimal)
      - ConfidenceScore (decimal)
      - MistakeCount (int): per-user count from tblMistake (canonical, populated before summary build)
      - ListenerRating (decimal)
      - IsFacilitator (bool)
      - IsAi (bool): *(Phase 17)* true for the AI Voice Participant. Tagged in `GetSessionCompletionSummaryAsync` from `tblSessionMember.IsAi`. Excluded from the scored leaderboard + top-score; shown as an "AI partner" on the report. **Note:** the AI user can hold multiple slots, so the member/slot lookup is built with `GroupBy(UserId)` (a plain `ToDictionary(UserId)` would throw on the duplicate key — fixed 2026-06-17).
  - TotalTurns (int)
  - ScriptTitle (string)
  - GrammarFocusTag (string)
  - TotalMistakesAllMembers (int): total across all members from tblMistake
```

**MistakeCount source clarification:** `MemberScoreDto.MistakeCount` is read from `tblMistake` (per user, per session) — NOT from `grammarerrorsjson` in `tblvoiceanalysis`. The SP `uspGetSessionCompletionSummary` counts grammar errors from the JSON array in voice analysis records; that value is overridden in `GetSessionCompletionSummaryAsync` with a grouped EF count from `tblMistake` to ensure consistency with `TotalMistakesAllMembers`.

**Business Rules:**
1. Idempotent: if session already `COMPLETED`, return existing summary immediately
2. Mark current active turn `COMPLETED` (if any exists)
3. `UpdateSessionStatus` → `COMPLETED`
4. For each distinct active member:
   a. `SaveMistakesFromSessionAsync(sessionId, memberId)` — extracts mistakes from voice analysis
   b. `UpsertStreakAsync(memberId, practiceMinutes)` — `practiceMinutes` = `ActualDurationSec / 60` (min 1) or `SessionDuration` if no actual duration
   c. `CheckAndAwardBadgesAsync(memberId)`
5. `GetSessionCompletionSummary(sessionId)` → `uspGetSessionCompletionSummary`
6. Mistake counts in summary reflect persisted mistake records (extracted before summary built)

**Stored Procedures:** `uspUpdateTurnStatusByTurnStateId`, `uspUpdateSessionStatus`, `uspGetSessionCompletionSummary`

**Cross-Module Integration:**
- `IMistakeService.SaveMistakesFromSessionAsync` — extracts from `tblVoiceAnalysis`
- `IUserService.UpsertStreakAsync` — updates `tblUserStreak` and `tblUser.TotalSessionsPlayed`
- `IUserService.CheckAndAwardBadgesAsync` — evaluates badge rules

**SignalR / Realtime Events:**
- Hub `EndSession(sessionId)` calls `CompleteSessionAsync`
- Broadcasts `SESSION_ENDED` to `live_{sessionId}`: `{ sessionId, summary: SessionSummaryResponseDto }`
- Hub `CompleteTurn(...)` also triggers `SESSION_ENDED` automatically when `ShiftTurnAsync` returns `"No further turns remain..."` — the auto-complete path.

**Failure Cases:**
- `sessionId <= 0` → validation fail
- Session not found → `"Session was not found."`
- Summary could not be generated → `"Session summary could not be generated."`

**Session Auto-Completion Guard (CRITICAL — 2026-06-04 fix):**
- `CompleteTurn` hub method only auto-completes the session when `ShiftTurnAsync` returns a message containing `"No further turns remain"`.
- All other `ShiftTurnAsync` failures (turn index mismatch, wrong user, session not active, speaker slot not found, duplicate submit) throw `HubException` back to the caller — they do NOT trigger session completion.
- **Drift prevented:** Prior to this fix, ANY `ShiftTurnAsync` failure (including duplicate `CompleteTurn` with a stale turn index) caused the hub to call `CompleteSessionAsync` and broadcast `SESSION_ENDED`, ending the session prematurely mid-conversation. The fix adds an explicit message check before calling `CompleteSessionAsync`.

**Notes on Known Drift Prevented:**
- `"The provided turn does not match the active turn"` — can occur on duplicate or stale `CompleteTurn` calls (network retry, auto-submit timer firing late). Now surfaces as `HubException`; does not end session.
- `"Only the active speaker can complete the current turn"` — rejected `CompleteTurn` from wrong user. Now surfaces as `HubException`; does not end session.
- `"No active member holds the slot '...'"` — script speaker label / session slot mismatch on the next turn. Now surfaces as `HubException`; session should not end just because of a slot config error.
- **All of these specific messages reach the client only because of the 2026-06-05 `DescribeFailure` fix** — previously the hub threw `response.Message` (`"Turn shift failed."`) and the specific reason in `Errors` was dropped. See the Turn Shift flow "Error Message Contract" and "Generic error masking" drift note.

---

### Flow: Live Session Entry and Transition

**Purpose:** Members transition from lobby to live session room after `SESSION_STARTED` fires.

**Sequence:**
1. Frontend receives `SESSION_STARTED` from `/hubs/session` — both host and guest navigate to live session screen
2. Frontend connects to `/hubs/live-session?sessionId={id}&access_token={token}`
3. Hub `OnConnectedAsync` calls `GetActiveSessionMemberByUserIdAsync` to resolve slot and fullName for the connection
4. Frontend calls `JoinLiveSession(sessionId, userId)` → broadcasts `MEMBER_JOINED`: `{ userId, name, slotIndex }`
5. Frontend calls `GET /api/turns/{sessionId}/current` to load the first turn
6. On each `TURN_SHIFT`, every connected client reloads `GET /api/turns/{sessionId}/current` before deciding whether to render speaker or listener UI

**Precondition for live hub connection (UPDATED — page-refresh reconnect supported):**
- `ResolveConnectionMetadataAsync` first checks `GetActiveSessionMemberByUserIdAsync` (`IsActive = 1`)
- If not found (can happen after page refresh races with `OnDisconnectedAsync → MarkMemberLeftAsync`):
  - Falls back to `GetSessionMemberByUserIdAsync` (any `IsActive` state)
  - If any member row found → calls `ReactivateMemberAsync` (sets `IsActive = 1`) → connection proceeds
  - If no member row at all → throws `HubException("Active session member was not found...")`
- This allows members to reconnect transparently after a page refresh without a full rejoin flow

**Page-refresh race (CRITICAL — Drift 1 for live session):**
```
Page refresh → WebSocket drops → OnDisconnectedAsync → MarkMemberLeftAsync → IsActive = 0
↓
New page load → new WebSocket connect → OnConnectedAsync → ResolveConnectionMetadataAsync
↓
GetActiveSessionMemberByUserIdAsync → null (IsActive = 0)
↓ (before fix) → HubException → connection fails → user stuck with broken live room
↓ (after fix)  → GetSessionMemberByUserIdAsync → found → ReactivateMemberAsync → IsActive = 1 → connected
```

**OnDisconnect behavior (UPDATED 2026-06-17 — grace window):** Hub no longer broadcasts `MEMBER_LEFT` / calls `MarkMemberLeftAsync` immediately. Instead `OnDisconnectedAsync` calls `ILiveSessionReconnectTracker.ScheduleLeave(sessionId, userId, slotIndex, name, groupName)` which defers the `MEMBER_LEFT` broadcast `{ userId, name, slotIndex }` + `MarkMemberLeftAsync` (IsActive = 0, IsReady = 0) by a **20s grace window**. `OnConnectedAsync` calls `CancelPendingLeave(sessionId, userId)` — a reconnect within the window cancels the pending leave so **no** `MEMBER_LEFT` is sent. A genuine leave (browser close / real exit) still emits `MEMBER_LEFT` after the grace period. Mirrors the lobby hub's `LobbyReconnectTracker`. See drift note "Live presence false-leave" below.

#### Live Presence False-Leave — Transient Disconnect Shows "<member> left" / Sticky Speaker-Left Banner (2026-06-17)

**Drift type:** Missing-grace-window / state-reconciliation drift on the LIVE hub (the lobby hub had the fix; the live hub did not). **Symptom (reported on APK):** a participant ("Ayan") never left the room, but the other phone showed Ayan as left — and when Ayan was the active speaker the "The current speaker has left" banner stayed stuck.

**Root cause (two compounding defects):**
1. **Backend — no grace window.** `LiveSessionHub.OnDisconnectedAsync` broadcast `MEMBER_LEFT` and called `MarkMemberLeftAsync` on *every* socket drop, immediately. On mobile, transient WebSocket drops are routine (Wi-Fi↔cellular handoff, app backgrounded, WebView/page reconnect, doze). Each drop fired a false `MEMBER_LEFT` to the group even though the member reconnected (via the `ResolveConnectionMetadataAsync` reactivation fallback) seconds later. The lobby hub already deferred this behind `LobbyReconnectTracker` (20s); the live hub never got the equivalent.
2. **Frontend — sticky banner never cleared on rejoin.** `session-room.component.ts` `MEMBER_LEFT` handler sets a persistent `speakerLeftAlert` signal when the *active speaker* leaves. It was only ever cleared on `TURN_SHIFT` (line ~417) or manual dismiss — the `MEMBER_JOINED` handler did **not** clear it. So after a false (or genuine) speaker drop→rejoin with no intervening turn shift, the "current speaker has left" banner stayed on screen indefinitely.

**Fix:**
- **Backend:** new `ILiveSessionReconnectTracker` / `LiveSessionReconnectTracker` (singleton, registered in `Program.cs`) modeled on `LobbyReconnectTracker`. `OnDisconnectedAsync` → `ScheduleLeave(...)` (20s deferred `MEMBER_LEFT` + `MarkMemberLeftAsync`); `OnConnectedAsync` → `CancelPendingLeave(...)`. A reconnect within 20s cancels the pending leave → no false event; a real exit still emits after the window.
- **Frontend:** `MEMBER_JOINED` handler now clears `speakerLeftAlert` when the rejoiner is the current `turnState().activeMemberId` — reconciliation safety-net for a rejoin that beats the grace window or any leave→rejoin without a `TURN_SHIFT`.

**Notes on Drift Prevented:** any hub that treats a raw socket disconnect as a domain "leave" MUST debounce it behind a reconnect grace window — mobile transports drop constantly. Do NOT revert the live hub to an immediate `MEMBER_LEFT`. On the client, any *sticky* presence indicator (banner/badge) must have a clear path that the corresponding rejoin event resets — a transient toast is fine to leave self-expiring, but persistent state needs explicit reconciliation.

---

### Application and Infrastructure Wiring

- `ILiveSessionService → LiveSessionService`, `ILiveSessionRepository → LiveSessionRepository`, controller: `LiveSessionController`
- SignalR hub: `LiveSessionHub` at `/hubs/live-session`
- JWT bearer reads `access_token` query string for `/hubs/live-session`
- Hub auto-joins `live_{sessionId}` group when `sessionId` provided in query string
- `LiveSessionService` depends on: `IUserRepository`, `ISessionRepository`, `ILiveSessionRepository`, `IUserService`, `IMistakeService`

### SignalR Live Session Hub — Full Contract

**Hub path:** `/hubs/live-session`
**Authorization:** `UserOrAdmin` + `ActiveUser` policies
**Group format:** `live_{sessionId}`

**Client → Server methods:**

| Method | Parameters | Result |
|---|---|---|
| `JoinLiveSession` | `sessionId: string, userId: string` | Broadcasts `MEMBER_JOINED` |
| `CompleteTurn` | `sessionId: string, memberId: string, turnIndex: int, score: decimal` | Broadcasts `TURN_SHIFT` |
| `SubmitListenerFeedback` | `sessionId: string, tag: string, targetTurnIndex: int` | Broadcasts `LISTENER_TAG` |
| `RequestReRead` | `sessionId: string, requesterId: string` | Broadcasts `RE_READ_REQUESTED` |
| `EndSession` | `sessionId: string` | Broadcasts `SESSION_ENDED` |
| `VoiceBroadcastStart` | `sessionId: string, speakerId: string` | Broadcasts `VOICE_BROADCAST_STARTED` |
| `VoiceBroadcastStop` | `sessionId: string, speakerId: string` | Broadcasts `VOICE_BROADCAST_STOPPED` |
| `RequestVoiceStream` | `sessionId: string, listenerUserId: string` | Broadcasts `VOICE_STREAM_REQUESTED` |
| `SendWebRTCOffer` | `sessionId: string, toUserId: string, offerJson: string` | Broadcasts `WEBRTC_OFFER` |
| `SendWebRTCAnswer` | `sessionId: string, toUserId: string, answerJson: string` | Broadcasts `WEBRTC_ANSWER` |
| `SendICECandidate` | `sessionId: string, toUserId: string, candidateJson: string` | Broadcasts `ICE_CANDIDATE` |

**Server → Client events:**

| Event | Payload |
|---|---|
| `MEMBER_JOINED` | `{ userId: long, name: string, slotIndex: byte }` |
| `MEMBER_LEFT` | `{ userId: long, slotIndex: byte }` |
| `TURN_SHIFT` | `{ newActiveMemberId: long, newActiveMemberName: string, activeMemberAvatarUrl: string|null, slotIndex: byte, turnIndex: int, nextUtterance: UtteranceResponseDto }` |
| `LISTENER_TAG` | `{ tag: string, fromUserId: long }` |
| `RE_READ_REQUESTED` | `{ requesterId: long, reReadCount: int }` |
| `SESSION_ENDED` | `{ sessionId: long, summary: SessionSummaryResponseDto }` |
| `VOICE_BROADCAST_STARTED` | `{ speakerId: string }` |
| `VOICE_BROADCAST_STOPPED` | `{ speakerId: string }` |
| `VOICE_STREAM_REQUESTED` | `{ listenerUserId: string }` |
| `WEBRTC_OFFER` | `{ fromUserId: string, toUserId: string, offerJson: string }` |
| `WEBRTC_ANSWER` | `{ fromUserId: string, toUserId: string, answerJson: string }` |
| `ICE_CANDIDATE` | `{ fromUserId: string, toUserId: string, candidateJson: string }` |

**Connection rules:**
- Live hub connection requires caller to be a member of the session (active OR inactive due to reconnect) — see page-refresh race note above
- `JoinLiveSession` and `OnConnectedAsync` both call `ResolveConnectionMetadataAsync` which handles the reactivation fallback
- `userId` param must match JWT `UserId` — enforced server-side; throws `HubException` if mismatch
- `HubConnectionMetadata` stores `SlotIndex` and `FullName` for disconnect broadcast (no DB lookup needed on disconnect)
- WebRTC methods (`VoiceBroadcastStart/Stop`, `RequestVoiceStream`, `SendWebRTCOffer/Answer`, `SendICECandidate`) are pure relay: no DB writes, broadcast to group, clients filter by `toUserId`

**`ILiveSessionRepository` additions (2026-05-23):**
- `GetSessionMemberByUserIdAsync(sessionId, userId)` — any `IsActive` state, used by reconnect fallback and voice analysis save
- `ReactivateMemberAsync(sessionId, userId)` — EF update setting `IsActive = true` for the matching row
**`ILiveSessionService` additions (2026-05-23):**
- `ReactivateMemberAsync(sessionId, userId)` — best-effort wrapper; swallows exceptions to not break hub reconnect

### Live Session Business Rules

- Next speaker resolved by matching `SessionMember.SlotName` to `Utterance.SpeakerLabel` (case-insensitive trim)
- Only active speaker can complete the current turn
- Only active speaker can save voice analysis for their own turn
- One voice-analysis record per session + user + turn — INSERT on first recording, UPDATE on re-recording (UPSERT). Re-recording the same active turn (before `CompleteTurn` fires) is valid and updates the existing row.
- Re-read count capped at `MaxReReads = 2`
- Session completion summary aggregates: fluency score, confidence score, listener rating, grammar mistake count per member

### User Session Preferences — Stable Contract

**Storage:** `localStorage` key `gwf_session_prefs` (JSON). Managed by `SessionPreferencesService`. No DB table. Device-local.

| Key | Type | Default | Behaviour |
|---|---|---|---|
| `defaultVoiceStarter` | bool | `true` | **ACTIVE (2026-06-04)** — Re-implemented in `SpeakerScreenComponent.ngOnChanges()`. On turn change, sets `_pendingAutoStart = true`; `ngAfterViewChecked` fires `voiceRecorder.startRecording()` after 700 ms once the ViewChild is ready. **Platform guard:** suppressed on mobile-web only (`isMobileDevice && !Capacitor.isNativePlatform()`). On Capacitor native Android the native speech plugin starts silently with no bell — auto-start is allowed. On mobile web browsers, the Web Speech API plays a system bell on every `start()` call, so mobile-web users must tap manually. |
| `autoSubmitOnStop` | bool | `false` | **ACTIVE (2026-06-04)** — Re-implemented in `SpeakerScreenComponent.onRecordingComplete()`. The feedback screen is **always** shown first so users see their score. When this pref is `true`, a 3-second countdown begins immediately after the score screen appears. A pill indicator `"Auto-submitting in Xs — tap below to cancel"` counts down. After 3 s, `onDoneSpeaking()` fires automatically. The timer is cancelled on: manual "Done Speaking" tap, "Try Again" tap (`onRetryRecording()`), Skip tap, `ngOnDestroy()`, and turn change. Users can always interrupt auto-submit by tapping any action button. |
| `listenVoiceBroadcast` | bool | `false` | On `VOICE_BROADCAST_STARTED`: listener emits `RequestVoiceStream`; speaker creates WebRTC offer in response |
| `showReReadSkipButtons` | bool | `false` | Shows Skip button below `VoiceRecorderComponent` during recording phase, and Skip + Try Again below `VoiceFeedbackComponent` during feedback phase |

**UI entry:** Gear icon in session room top bar → `showSettings` signal → 4-toggle panel slides in below top bar. Each toggle calls `sessionPrefs.update({ key: !current })`.
**Toggle UI contract:** Track remains `w-11 h-6 rounded-full`; thumb remains `left-0.5 top-0.5 h-5 w-5 rounded-full`; active state moves the thumb with inline `transform: translateX(1.25rem)`, inactive state uses `translateX(0)`. Do not rely on utility translation classes alone for thumb geometry in this component.

### Flow: Session Room Route Bootstrap — Stable Contract

### Entry Points
- Lobby and rejoin navigation target: `/live-session/room/:sessionId`
- Parent Angular route: `path: 'live-session'` in `Frontend/src/app/app.routes.ts`
- Child Angular route: `Frontend/src/app/modules/live-session/live-session.routes.ts`
- Frontend bootstrap file: `Frontend/src/main.ts`

### UI Trigger
- Host or guest receives `SESSION_STARTED` and navigates from lobby
- Returning participant re-enters an already active session from session detail or lobby recovery flow

### Request Contract
Endpoint: Angular navigation only, not an HTTP endpoint
Headers: `[VERIFY]` not applicable
Body:
  - `sessionId` (route param, required): numeric session identifier used by `sessionGuard`, `SessionRoomComponent.initSession()`, and live-session hub connection setup

### Response Contract
Success:
  - `authGuard` admits the parent `live-session` route
  - `sessionGuard` admits `room/:sessionId`
  - `LIVE_SESSION_ROUTES` resolves `SessionRoomComponent` through `component: SessionRoomComponent` inside the already lazy-loaded child route file
  - `Frontend/src/main.ts` imports `@angular/compiler` before `bootstrapApplication(AppComponent, appConfig)` as runtime compatibility fallback when any partially compiled dependency requests JIT
Error responses:
  - Route guard rejection redirects away from live-session room before component init
  - If the compiler fallback is removed while a dependency still requires JIT, browser runtime throws `The component 'SessionRoomComponent' needs to be compiled using the JIT compiler, but '@angular/compiler' is not available`

### Validation
- `sessionId` route param must be present
- Caller must satisfy `authGuard`
- Caller must satisfy `sessionGuard`
- `SessionRoomComponent` must stay within Angular's compiled graph; avoid a second dynamic component import from this child route unless the replacement path is verified under the current Vite/Angular compiler setup
- In the inline `SessionRoomComponent` template, Tailwind utility tokens containing `/` or `.` must not be expressed via `[class.some-token]` bindings; use `ngClass` or plain `class` strings instead so Angular template parsing remains stable under JIT and AOT

### Database / Stored Procedures
Tables read: none directly during route resolution
Tables written: none directly during route resolution
Stored procedures: none
Key queries: none

### Business Rules
- `app.routes.ts` lazy-loads the live-session route file; the room component does not need an additional nested lazy import in that child route
- `SessionRoomComponent` becomes responsible for REST and SignalR startup only after route guards pass
- The compiler import in `main.ts` is a compatibility fallback, not the preferred primary compilation mode; AOT-safe route/component wiring remains the target state

### State Transitions
- User shell route → `/live-session/room/:sessionId`
- Session room bootstrap pending → room component initialized → current turn load + hub connection start

### Realtime Events
Hub: `/hubs/live-session`
Event: bootstrap begins listening for `TURN_SHIFT`, `LISTENER_TAG`, `RE_READ_REQUESTED`, `SESSION_ENDED`, `VOICE_BROADCAST_STARTED`, `VOICE_STREAM_REQUESTED`, `WEBRTC_OFFER`, `WEBRTC_ANSWER`, `ICE_CANDIDATE`, `VOICE_BROADCAST_STOPPED`
Payload: handled after `SessionRoomComponent.initSession(sessionId)`
Subscribers: active session members inside the live room

### Session Room — Turn Shift and Completion Flow (STABLE CONTRACT — 2026-06-04)

**Turn shift sequence (normal path):**
1. Speaker records → `SpeakerScreenComponent.onDoneSpeaking()` → `completeTurnRealtime(sessionId, userId, turnIndex, score)` → hub `CompleteTurn`
2. Backend: `ShiftTurnAsync` succeeds → broadcasts `TURN_SHIFT` → hub method returns
3. Frontend receives `TURN_SHIFT` → `handleTurnShift()` → optimistic state update → `loadCurrentTurn()` (canonical confirm)
4. `completeTurnRealtime` observable resolves → `turnShifted.emit()` → `onTurnShifted()` → `loadCurrentTurn()` (redundant but safe — `updateState()` guard skips re-render if turn index unchanged)
5. `SpeakerScreenComponent` stays mounted throughout (no loading flash); `ngOnChanges` fires from the optimistic update in step 3

**Critical: `onTurnShifted()` does NOT set `isLoading=true`**
Setting `isLoading=true` would destroy and recreate `SpeakerScreenComponent`, cancelling its auto-start timer and adding a second 700ms delay before the next recording begins. The loading state is only used on initial bootstrap and explicit retry.

**Session-end sequence (last turn):**
1. Speaker completes last turn → `completeTurnRealtime(N, N)` → hub `CompleteTurn`
2. Backend: `ShiftTurnAsync` finds no next turn and returns a **failure response whose `Message` is the shared `TurnShiftSignals.SessionComplete` constant** (`"No further turns remain"`). The hub matches `response.Message` against that constant → calls `CompleteSessionAsync` → broadcasts `SESSION_ENDED` → `return`. The hub call resolves normally (no HubException).
3. Frontend `SESSION_ENDED` handler: sets `_sessionEnded = true` → saves session duration to sessionStorage → navigates to `/session/report/:id`
4. `completeTurnRealtime` resolves → `turnShifted.emit()` → `onTurnShifted()` → guarded by `_sessionEnded`, returns immediately
5. Any in-flight `loadCurrentTurn` calls are also guarded and no-op

**Completion-signal contract (CRITICAL — do not break):**
The "session is finished" signal travels in **`ApiResponse.Message`**, set to the shared constant `GoWithFlow.Application.Common.TurnShiftSignals.SessionComplete = "No further turns remain"`. Both `ShiftTurnAsync` (writer) and `LiveSessionHub.CompleteTurn` (reader) reference that one constant. Distinction the service MUST preserve: `nextTurn == null && nextTurnError == null` ⇒ genuine completion (Message = sentinel); `nextTurn == null && nextTurnError != null` ⇒ real failure (Message = `"Turn shift failed."`, NOT the sentinel) so a turn-creation error never masquerades as completion.

**`_sessionEnded` guard (critical):** Once `SESSION_ENDED` is received, `_sessionEnded = true` is set. `loadCurrentTurn()`, `onTurnShifted()`, and `handleTurnShift()` all return early. Prevents post-navigation operations against the now-COMPLETED session.

**CompleteTurn error handling (non-completion failures):**
When `ShiftTurnAsync` fails for reasons other than "no further turns" (turn index mismatch, wrong user, session not active, slot mismatch), the hub throws `HubException`. The `completeTurnRealtime` observable's `error()` callback fires → toast shown → `isSubmitting = false`. The session is NOT ended.

### Failure Cases
- Missing `sessionId` route param → room init does not run
- Guard rejection → navigation blocked before room render
- Compiler fallback removed while a dependency still requests JIT → browser runtime failure on room navigation
- Current turn API failure after successful route load → room renders retry state with `loadError`
- `CompleteTurn` hub rejects (stale turn index, wrong user) → toast `"Failed to advance turn. Please try again."` — session continues

### Recovery / Fallback Logic
- `main.ts` imports `@angular/compiler` so JIT-required dependencies do not hard-fail bootstrap
- Child live-session route resolves the room through a static component reference inside the lazy child route file
- `SessionRoomComponent.retryLoad()` re-runs `GET /api/turns/{sessionId}/current` after transient API failure
- Session preference toggle states use `ngClass` string switching for `bg-white/15`, `bg-white/5`, `text-white/40`, and `translate-x-0.5` style utilities instead of `[class.*]` bindings
- Session preference toggle thumbs use explicit `left-0.5` anchoring plus inline transform distance instead of class-only translation so the knob stays visually aligned in rendered HTML

### Notes on Known Drift Prevented
- **Drift 12 (2026-06-05): Final "Done Speaking" throws a SignalR error and never completes the session** — Drift type: **response-field / contract drift** (signal carried in the wrong field). On the last turn `ShiftTurnAsync` returned `FailureResult(errors: ["No further turns remain in this session…"], message: "Turn shift completed.")` — the sentinel lived in **`Errors`**, but `LiveSessionHub.CompleteTurn` matched **`Message`** against the literal `"No further turns remain"`. The match failed → the hub fell through to `throw new HubException(response.Message)`, sending the client a SignalR error (ironically `"Turn shift completed."`). The session stayed `ACTIVE`, so every subsequent **Done Speaking** click re-ran the identical path → the same recurring console error; the report was never reached. **Fix (2026-06-05):** introduced shared constant `TurnShiftSignals.SessionComplete = "No further turns remain"` (new file `Application/Common/TurnShiftSignals.cs`); `ShiftTurnAsync` now carries it in **`Message`** for the genuine-completion case (`nextTurnError == null`) and returns `"Turn shift failed."` when `nextTurnError != null`; the hub matches `response.Message` against the same constant. Both sides now reference one value → cannot drift apart. Frontend needed no change — its `SESSION_ENDED` handler + `_sessionEnded` guard already exit gracefully once the broadcast fires; the recurring error disappears because the first click now completes the session. Added explicit final-turn logging in the hub (`TurnIndex` + `MemberId`).
- Stale docs drift: `ProjectOverview.md` previously stated that `Frontend/src/main.ts` imported `@angular/compiler`, but source had drifted and the import was missing
- Route compilation drift: the live-session room is documented to resolve through `component: SessionRoomComponent` within the lazy child route contract to reduce runtime JIT-only failures on this page
- Template compilation drift: session-room settings toggles previously used escaped `[class.bg-white\/15]` and `[class.translate-x-0\.5]` bindings, which broke Angular template parsing and surfaced as unclosed `button` tags during JIT compilation
- UI drift: session-room preference toggles previously rendered with class-driven thumb translation but no fixed left anchor, producing an unstable knob position in the actual DOM
- **Drift 1 (2026-05-23): Page-refresh breaks hub reconnect + voice analysis + leave** — `OnDisconnectedAsync` sets `IsActive = 0` on every disconnect including page refreshes. On page reload: (a) hub `OnConnectedAsync` → `GetActiveSessionMemberByUserIdAsync` → null → HubException → connection fails; (b) `SaveVoiceAnalysisAsync` → same null check → 400 "Voice analysis save failed."; (c) `LeaveSessionAsync` → same null check → 400 "Session leave failed." Fixed 2026-05-23 via three-part fix: hub `ResolveConnectionMetadataAsync` now falls back to `GetSessionMemberByUserIdAsync` and calls `ReactivateMemberAsync` on reconnect; `SaveVoiceAnalysisAsync` now uses `GetSessionMemberByUserIdAsync` (turn ownership enforced by `ActiveMemberId == userId`); `LeaveSessionAsync` already made idempotent in Drift 5 of Session Module.
- **Drift 4 (2026-05-23): Single skipped word cascades entire alignment → all subsequent words score near-zero** — `alignAndScore` only detected *insertions* (extra spoken words) in its look-ahead, never *deletions* (skipped expected words). When a speaker skips one word (e.g. "I have **[been]** waiting..."), the algorithm did a substitution ("waiting" scored against "been"), then compared every following word against the wrong expected slot. Entire sentence offset by 1 position → fluency=15 for a near-perfect sentence. **Mathematical proof from screen:** spoken missed only "been", yet fluency=15 and "[project]" showed as missing at the end (exhausted spokenIdx one slot early). **Fix:** Added deletion look-ahead in `alignAndScore`: when `wordSimilarity(spokenWord, expected[expectedIdx+1]) >= 0.7 AND > current similarity`, mark `expected[expectedIdx]` as `isMissing`, advance `expectedIdx` only (keep `spokenIdx` on same word). Priority: insertion checked first, deletion second, substitution last. **Verified trace:** with fix, "I have waiting to see it how long have you been working on this project" vs expected gives 15/16 matched, [been] missing, fluency≈94, overall≈93 — correct.
- **Drift 3 (2026-05-23): 8000ms fallback timer fires before isFinal for long sentences → all scores wrong** — Timer started at mic press; interim results did not reset it. For a 12-word sentence where the user takes 2–3s to prepare then speaks for 4–5s, the 8000ms timer fired just before the browser emitted `isFinal=true`. `finalize()` ran with `allFinalTranscripts=[]` → `transcribedText=""`, `confidenceScore=70` (hardcoded fallback), `fluencyScore=0`, `overallScore=18` (= `0.25 × 70`), all words `[missing]`. **Fix:** In the `onresult` handler's `else` (interim) branch, when `allFinalTranscripts.length===0`, call `resetSilenceTimeout(8000)`. This makes the fallback deadline "8s from last speech activity" not "8s from mic press". Once a `isFinal` arrives the 2500ms timer takes over. **Secondary fix:** `VoiceFeedbackComponent.speedLabel` now returns `'— Not detected'` for `wpm===0` instead of `'🐢 Too slow'` — 0 WPM means capture failed, not that the speaker was too slow.
- **Drift 2 (2026-05-23): Page-refresh after recording-complete causes 400 on re-submit** — Drift 1 fixed hub reconnect and member lookup, but did not address: the voice analysis is saved immediately at `onRecordingComplete` (before `CompleteTurn`). If the user refreshes at that point, `ngOnChanges` resets phase to `'recording'` (in-memory state lost). The user re-records the same active turn and the backend rejects with 400 "already exists". **Two-part fix applied 2026-05-23:** (a) **Backend UPSERT**: `SaveVoiceAnalysisAsync` now calls `GetVoiceAnalysisByUserTurnAsync`; if a record exists for this session+user+turn, it calls `UpdateVoiceAnalysisAsync` (EF `ExecuteUpdateAsync` on all score fields) and returns 200 `"Voice analysis updated successfully."` — no more 400 on re-record. New repo methods: `GetVoiceAnalysisByUserTurnAsync(sessionId, userId, turnIndex)`, `UpdateVoiceAnalysisAsync(voiceAnalysisId, updates, updatedBy)`. (b) **Frontend sessionStorage persistence**: `SpeakerScreenComponent` stores the `VoiceSessionResult` in `sessionStorage` under key `gwf_va_{sessionId}_{turnIndex}_{userId}` on `onRecordingComplete`. On `ngOnChanges`, `tryRestoreFromStorage()` checks this key and if found, restores `sessionResult` and sets `analysisPhase = 'feedback'` — user lands back on their score screen after refresh, not a blank recorder. Entry is cleared on `CompleteTurn` success or `Skip` success.
- **Drift 5 (2026-06-04): Web Speech API fails on Edge mobile browser** — Two root causes: (a) `requestMicPermission()` called `getUserMedia`, stopped the stream, then immediately called `SpeechRecognition.start()`. Edge/Chrome mobile do not release the audio hardware immediately after `stop()`, so `SpeechRecognition` got an `audio-capture` error. After 3 retries this became a permanent failure. (b) Edge mobile does not support the `en-IN` locale — `language-not-supported` fired but was not in the retryable list, causing an immediate hard error with no fallback. **Fix (2026-06-04):** (a) On mobile browsers where `SpeechRecognition` is available, `requestMicPermission()` now skips `getUserMedia` entirely — `SpeechRecognition` handles its own permission prompt and surfaces `not-allowed` via `onerror` if denied. (b) Added `language-not-supported` handling: first occurrence sets `_useFallbackLang = true` and retries with `en-US`. (c) Added `not-allowed` error handler: surfaces a clear message instead of falling through to the generic error. (d) `_useFallbackLang` is reset at the start of each `startSession()` call. Zero changes to `speaker-screen.component.ts`.

### Flow: Speaker Turn — Stable Contract

> ⚠ UPGRADED — Voice Recognition Engine v1.0 (2026-05-23). Old `VoiceAnalysisService` recording pattern replaced. See "Frontend Voice Recognition Engine" section below.

**Entry:** `SpeakerScreenComponent` rendered when `isSpeaker() = true`.

**Phase State Machine:**
```
analysisPhase: 'recording' | 'feedback' | 'confirmed'
sessionResult: VoiceSessionResult | null
```
- `'recording'` → `VoiceRecorderComponent` shown. User taps mic to start/stop.
- `'feedback'` → `VoiceFeedbackComponent` shown with word-level scoring. User taps "Done Speaking".
- Reset only when `turnIndex` changes in `ngOnChanges(changes: SimpleChanges)`.

**Turn Start:**
1. `ngOnChanges(changes: SimpleChanges)` → checks `changes['turnState'].previousValue?.turnIndex !== currentTurnIndex`. Only if the turn index changed: `resetPhase()` → `analysisPhase = 'recording'`, `sessionResult = null`. Same-turn re-inputs (e.g. server API confirmation of an already-set optimistic state) are ignored — phase and auto-start are NOT reset.
2. `words.set(...)` always updates (text could theoretically differ between optimistic and server, safe to always sync)
3. User taps mic inside `VoiceRecorderComponent` → `VoiceRecognitionEngine.startSession()` begins

**Recording Phase — VoiceRecognitionEngine flow:**
1. `requestMicPermission()` — uses Permissions API if available; falls back to getUserMedia to avoid double-stream on iOS
2. VAD configured for device via `configure()` before `AudioActivityDetector.start()`
3. `SpeechRecognition` configured: `lang = 'en-IN'`, `continuous = !isIOS`, `interimResults = true`, `maxAlternatives = 3`
4. `onresult` — interim buffered for display only; final chunks collected in `allFinalTranscripts[]`; interim resets fallback timer; finals set `_hasSpoken = true` when ≥ 2 words
5. VAD silence detected → fires callback only if `_hasSpeechStarted` (amplitude gate); engine only stops if `_hasSpoken` (transcript gate) → `_intentionalStop = true` → `recognition.stop()` after 400 ms (600 ms on mobile)
6. `_intentionalStop` flag: set before any deliberate `recognition.stop()` call — suppresses `onend` restart path and prevents a second browser bell sound
7. `onerror: 'no-speech'` — if transcripts exist, extends timeout instead of retrying with a new instance (which would play a bell). Only retries with new instance when no speech at all.
8. Finalize triggers: VAD silence / post-final timeout / fallback timeout / unexpected `onend` with finals
9. Minimum-duration guard in `finalize()`: reschedules if elapsed < 2000 ms AND wordCount < 2 AND !_hasSpoken
10. `finalize()` calls `recognition.stop()` with `_intentionalStop = true` before scoring to prevent bell on later `onend`
11. iOS: `continuous = false`, restarts on `onresult` final — guarded by `!_intentionalStop`

**Device-aware timeouts:**

| Timeout | Desktop | Mobile |
|---|---|---|
| Fallback (no finals yet) | 8 000 ms | 12 000 ms |
| Post-first-final | 2 500 ms | 4 500 ms |
| Stop delay after VAD silence | 400 ms | 600 ms |
| Minimum recording duration guard | 800 ms | 2 000 ms |

**Device detection:** `isIOS = /iPad\|iPhone\|iPod/.test(ua) \|\| (maxTouchPoints > 1 && /Macintosh/.test(ua))` — covers iPadOS 13+ which sends a desktop UA. `isMobile = isIOS || /Android|.../.test(ua)`. Public getter `isMobileDevice` consumed by `SpeakerScreenComponent` to gate auto-start.

**onRecordingStarted():**
- Calls `VoiceBroadcastService.startBroadcast()` (fire-and-forget)

**onRecordingComplete(result: VoiceSessionResult):**
- Sets `sessionResult = result`, `analysisPhase = 'feedback'`
- Calls `VoiceBroadcastService.stopBroadcast()`
- Maps `VoiceSessionResult` → voice analysis payload
- POST `/api/turns/{sessionId}/voice-analysis` — non-blocking (`.subscribe()` no handler)

**onDoneSpeaking() — Turn Submission:**
- Guard: `if (isSubmitting()) return`
- Sets `isSubmitting = true`
- SignalR `CompleteTurn(sessionId, userId, turnIndex, overallScore)` → `TURN_SHIFT`
- On success: `resetPhase()` → `turnShifted.emit()`
- On error: toast "Failed to advance turn. Please try again." + `isSubmitting = false`

**onSkip():**
- Calls `voiceRecorder.stopEarly()` (ViewChild reference) if recorder active
- Calls `VoiceBroadcastService.stopBroadcast()`
- Guard: `if (isSubmitting()) return`
- SignalR `CompleteTurn` with score `0` → `TURN_SHIFT`
- On success: `resetPhase()` → `turnShifted.emit()`

**onReRead():**
- Calls `requestReReadRealtime(sessionId, userId)` → `RE_READ_REQUESTED` broadcast
- On success: `resetPhase()` → back to recording phase

**RE-SPEAK visibility:** `turnState.reReadAllowed = true` AND `turnState.reReadCount < turnState.maxReReads` (max = 2)


**Failure Cases:**
- Mic permission denied → engine throws; component shows error message via `errorOccurred` event
- Browser no SpeechRecognition (Firefox) → engine throws "Please use Chrome or Edge."
- `saveVoiceAnalysis` error → non-blocking, silently swallowed (turn can still be completed). Backend uses UPSERT so a re-record after page refresh is no longer an error path.
- `completeTurnRealtime` error → toast "Failed to advance turn. Please try again." + reset `isSubmitting`
- `getUserMedia` for VAD denied → `AudioActivityDetector` logs warning, falls back to timeout-only silence detection

**Notes on Drift Prevented:**
- Old system: string equality compare `spoken === expected` always scored 0 due to punctuation/casing. Fixed: `TranscriptNormalizer` + `PronunciationScorer` token-level fuzzy match.
- Old system: `recognition.lang` not set → browser defaulted to OS language (Telugu/Hindi on Indian phones). Fixed: `lang = 'en-IN'`.
- Old system: interim results treated as final. Fixed: only `isFinal = true` results collected.
- Old system: no silence detection → recording ran forever. Fixed: `AudioActivityDetector` VAD + 8s fallback timeout.
- Old system: no mic permission pre-check → silent failure on first use. Fixed: `requestMicPermission()` before engine starts.
- `defaultVoiceStarter` and `autoSubmitOnStop` session prefs re-activated 2026-06-04 — see "User Session Preferences — Stable Contract" for full behavior. Both were previously stripped during the Voice Recognition Upgrade but have been correctly re-implemented with the new engine.
- **Drift 11 (2026-06-03): Voice listening unreliable on mobile/tablet — premature stops, double bell, spurious auto-start** — Five converging root causes identified and fixed:
  1. **Auto-start bell on mobile web** — `defaultVoiceStarter: true` (default) caused `SpeakerScreenComponent` to auto-start recording on every device including mobile. Every `recognition.start()` call plays the browser's system speech-recognition bell. On mobile web, this fires before the user is ready. Fix: `ngOnChanges` in `SpeakerScreenComponent` guards `_pendingAutoStart` with `&& !isMobileWebOnly` where `isMobileWebOnly = isMobileDevice && !Capacitor.isNativePlatform()`. Mobile web users must tap the mic button explicitly. Capacitor native (Android APK) is exempt — the native speech plugin starts silently with no bell, so auto-start works on native even on mobile hardware. *(Updated 2026-06-04: original fix used `!voiceEngine.isMobileDevice` which also blocked native Android; refined to mobile-web-only guard.)*
  2. **Second bell during speech (`no-speech` retry)** — Mobile Chrome fires `onerror { error: 'no-speech' }` when its own internal silence timer expires (separate from our VAD). The retry path called `startRecognition()` which creates a NEW `SpeechRecognition` instance and calls `.start()` → second system bell, even mid-speech. Fix: if `allFinalTranscripts.length > 0`, `'no-speech'` error now extends the post-final timeout instead of retrying with a new instance. A new recognition instance (and its bell) is only created when the user genuinely has not spoken yet.
  3. **Premature finalization from 2500 ms post-final timer** — Mobile speech API fires partial final results aggressively (after the first few words). Once any final result arrives, the 2500 ms silence timeout began counting. A natural breath or inter-phrase pause > 2.5 s triggered `finalize()` while the user was still speaking. Fix: post-final timeout raised to 4500 ms on mobile (vs 2500 ms on desktop). Fallback (no-finals) timeout raised from 8000 to 12000 ms on mobile.
  4. **VAD fires after startup ambient noise** — `AudioActivityDetector` silence callback fired 1200 ms after the recording started if the user hadn't spoken yet (ambient room noise drove db just above threshold, then dipped below). This caused an immediate stop before any speech. Fix: `_hasSpeechStarted` latch in VAD — silence callback is suppressed until the audio level has exceeded `speechThresholdDB` at least once. Also: `_hasSpoken` flag in `VoiceRecognitionEngine` — VAD silence path additionally requires a final transcript of ≥ 2 words before it can stop recording. VAD silence duration raised from 1200 to 2500 ms on mobile.
  5. **AGC-induced amplitude instability** — `autoGainControl: true` in the `AudioActivityDetector` `getUserMedia` stream caused rapid gain shifts between words, producing brief near-silence readings mid-speech and triggering the silence timer. Fix: `autoGainControl: false` in the VAD stream. The speech recognition API manages its own audio path independently and is unaffected. Also removed `sampleRate: 16000` from getUserMedia constraints — this is not a valid constraint spec and silently fails or rejects on some mobile browsers.
  6. **Minimum-duration guard** — Added in `finalize()`: if `elapsed < 2000 ms` AND `wordCount < 2` AND `!_hasSpoken`, reschedule and keep listening. Prevents any sub-2-second trigger on noise from producing a result.
  7. **`_intentionalStop` flag** — Set to `true` before any deliberate `recognition.stop()` call (in `finalize()`, `stopSession()`, `handleSilenceDetected()`). The `onend` handler skips all restart logic when this flag is set, preventing another `recognition.start()` (and bell) from firing after a controlled stop.
  Files changed: `audio-activity-detector.ts`, `voice-recognition.engine.ts`, `speaker-screen.component.ts`.
- **Drift 10 (2026-06-03): Non-host leaving a 2-person ACTIVE session does not abandon the session** — `uspupdatesessionmemberleft` abandons only when the host leaves OR when `active_member_count = 0`. In a 2-person ACTIVE session where the non-host leaves, `active_member_count` drops to 1 (host alone). Neither condition triggers, so the session stays `ACTIVE` and the remaining user is stuck alone in a live session that can never continue. **Fix (Migration 28):** Added `OR (v_currentstatus = 'ACTIVE' AND v_activemembercount < 2)` to the abandon condition. When any member leaves and fewer than 2 active members remain in an ACTIVE session, the session is immediately set to `ABANDONED`. LOBBY sessions are unaffected (count < 2 is common while waiting for others to join). Migration file: `28_fix_active_session_single_member_abandon.sql`. Drift type: DB contract drift.
- **Drift 9 (2026-06-03): Completed session shows as ABANDONED in session history** — `uspupdatesessionmemberleft` is called on every member disconnect (hub `OnDisconnectedAsync` → `MarkMemberLeftAsync`; REST leave endpoint → `LeaveSessionAsync`). The SP marks the member inactive then checks: if the host left OR no active members remain → calls `uspupdatesessionstatus(ABANDONED)`. It did NOT check the current session status first. So after `CompleteSessionAsync` sets status = `COMPLETED`, when members disconnect from the live hub (normal navigation away), the SP overwrites COMPLETED → ABANDONED. **Fix (Migration 25):** Added `v_currentstatus VARCHAR(16)` to the SP DECLARE block; reads `ses.status` alongside `ses.hostuserid` in a single SELECT; returns immediately if `v_currentstatus = 'COMPLETED'`. Apply `25_fix_leave_abandoned_guard.sql` to Supabase. Session 46 (Hotel Checking) was affected by this bug — its DB status is ABANDONED but the actual data (voice analyses, mistakes, scores) is complete.
- **Drift 8 (2026-06-03): Session report shows per-user mistakes = 0 despite 21 total mistakes** — `MemberScoreDto.MistakeCount` was sourced from `uspGetSessionCompletionSummary` SP which counts `grammarerrorsjson` elements from `tblvoiceanalysis`. If voice analysis grammar errors are empty (user scored 0 grammar errors in voice engine), per-user count is 0. But `TotalMistakesAllMembers` reads from `tblMistake` (populated by `SaveMistakesFromSessionAsync` before summary is built), causing the mismatch. **Fix:** After SP call in `GetSessionCompletionSummaryAsync`, override each member's `MistakeCount` with a grouped EF count from `tblMistake` (`_dbContext.Mistakes.GroupBy(m => m.UserId)`). This aligns per-user and total counts to the same canonical source.
- **Drift 7 (2026-06-03): Session report leaderboard shows DiceBear placeholder avatar instead of user's actual avatar** — `MemberScoreDto` lacked `AvatarUrl`; `uspGetSessionCompletionSummary` only returned `FullName`. Frontend hardcoded DiceBear `api.dicebear.com/7.x/avataaars/svg?seed=<name>`. **Fix:** Added `AvatarUrl` to `MemberScoreDto`; in `GetSessionCompletionSummaryAsync`, after SP call, fetch `tblUser.AvatarUrl` for all member user IDs via EF query; in `LiveSessionService.ResolveAvatarUrlsAsync`, convert R2 keys to presigned URLs using `_storageService.GetPresignedUrlAsync` (same pattern as user profile). Frontend: `ScoreboardRow.avatarUrl` added; template uses `[src]="row.avatarUrl || dicebear"` with `(error)` fallback.
- **Drift 6 (2026-06-03): `CompleteTurn` on last turn throws `NpgsqlOperationInProgressException`** — `GetSessionCompletionSummaryAsync` in `LiveSessionRepository` used `await using var reader` at method scope. The `await using var` declaration keeps the reader open until the end of the enclosing block (entire method body). After the `while` reader loop completed, EF Core LINQ queries (`FirstOrDefaultAsync`, `CountAsync`, `ToListAsync`) executed on the same `DbConnection` while the raw reader was still technically undisposed. PostgreSQL does not support concurrent commands on a single connection (no MARS equivalent), so it threw `NpgsqlOperationInProgressException: A command is already in progress`. **Fix:** Wrapped the `command` + `reader` in an explicit `await using (var command = ...) { await using var reader = ...; while (...) {...} }` block — both reader and command are disposed at the closing `}`, before EF queries execute. File: `GoWithFlow.Infrastructure/Repositories/LiveSessionRepository.cs`.
- **Drift 5 (2026-06-01): Non-host speaker gets double bell + "no data detected" on auto-start** — `handleTurnShift()` in `SessionRoomComponent` did two back-to-back `turnState.set()` calls for the same turn: (1) an optimistic update immediately on `TURN_SHIFT` event, (2) the API-confirmed state when `loadCurrentTurn()` returned. Each `turnState.set()` triggered `ngOnChanges()` in `SpeakerScreenComponent`. The second firing reset `analysisPhase = 'recording'` mid-recording and re-armed `_pendingAutoStart`, causing a second `voiceRecorder.startRecording()` call ~700ms later. The second `startSession()` wiped `allFinalTranscripts = []`, started a competing `SpeechRecognition` instance (playing a second browser bell sound), and eventually resolved with empty transcript data. Host user was unaffected because their turn loaded via `initSession()` (single `loadCurrentTurn` path, no `TURN_SHIFT`). **Three-part fix applied 2026-06-01:** (a) **`updateState()` guard in `SessionRoomComponent`**: `turnState.set()` only fires when `turnIndex` or `sessionId` actually changes — same-turn API confirmation is a no-op on the signal. (b) **`ngOnChanges` turnIndex guard in `SpeakerScreenComponent`**: `SimpleChanges` added; `resetPhase()` and auto-start are only triggered when `ts.previousValue?.turnIndex !== currentTurnIndex`, not on every input reference change. (c) **`startSession()` active-session guard in `VoiceRecognitionEngine`**: if `state$ !== 'idle'`, `stopSession()` is called before starting a new one — prevents transcript wipe and competing recognition instances if the double-start ever occurs.

### Frontend Voice Recognition Engine — Stable Contract

**Introduced:** Voice Recognition Upgrade v1.0 (2026-05-23)
**Replaces:** `VoiceAnalysisService` recording + analysis pattern in `SpeakerScreenComponent`

#### File Map

| File | Path | Role |
|---|---|---|
| `VoiceRecognitionEngine` | `core/services/voice/voice-recognition.engine.ts` | Orchestrator. Entry point for all voice sessions. |
| `AudioActivityDetector` | `core/services/voice/audio-activity-detector.ts` | Web Audio API VAD. Waveform data + silence detection. |
| `PronunciationScorer` | `core/services/voice/pronunciation-scorer.ts` | Token-level fuzzy match. Levenshtein + Soundex. |
| `TranscriptNormalizer` | `core/services/voice/transcript-normalizer.ts` | Contraction expansion + punctuation strip + filler removal. |
| `VoiceRecorderComponent` | `modules/voice/voice-recorder/` | Standalone mic UI. Waveform canvas. Interim display. |
| `VoiceFeedbackComponent` | `modules/voice/voice-feedback/` | Standalone Duolingo-style result UI. Word chips + score bands. |

#### VoiceRecognitionEngine — Public API

```typescript
state$: BehaviorSubject<RecordingState>         // 'idle'|'requesting'|'listening'|'processing'|'done'|'error'
interimTranscript$: BehaviorSubject<string>     // live display only — never scored
waveformData$: BehaviorSubject<Uint8Array>      // 128-sample time-domain data for canvas
volumeLevel$: BehaviorSubject<number>           // 0–100 normalized

startSession(expectedText): Promise<VoiceSessionResult>
stopSession(): void
```

#### Web Startup Watchdog — "Stuck on Requesting microphone…" Fix (2026-06-08)

**Drift type:** Missing-fallback / state-machine drift on the WEB path (desktop & mobile-web). Symptom reproduced on desktop Chrome at `https://<LAN-IP>:4200/repractice/56` (self-signed cert): the recorder sat on **"Requesting microphone…"** and never advanced to "Listening".

**Root cause:** `state$` goes `requesting` in `startSession()` and only advances to `listening` *inside* `startRecognition()`. Between them sit three awaits — `requestMicPermission()`, `vad.start()`, and the optional `captureAudio` `getUserMedia`. The recorder **disables the mic button while state is `requesting`** (`voice-recorder.component.html`), so if any of those awaits *throws* (e.g. `vad.start()` was awaited with **no try/catch** — a `getUserMedia` rejection from a mic held by another app/tab left `state$` stuck on `requesting` even though the promise rejected) or *never settles* (ignored permission prompt, OS audio hang), the UI wedged permanently with no error and no way to retry. Unlike the native path, the web path had **no watchdog/hard-ceiling** covering the pre-recognition phase.

**Fix (`voice-recognition.engine.ts`):**
1. **`startWebRecognition(expectedText, gen)`** — the permission→VAD→recognizer startup now runs inside a `Promise.race` against a **12 s mic watchdog**. If `listening` is not reached in time (guard: still `requesting` AND same `gen`), the watchdog bumps `_sessionGen` (so a late `getUserMedia` aborts as `SUPERSEDED`), stops the VAD, sets `state$ = 'error'`, and rejects with an actionable message ("…allowed mic access… no other app is using it, then tap the mic to try again"). The orphaned startup's late rejection is swallowed (`startup.catch(()=>{})`) to avoid an unhandledrejection.
2. **State-leave guarantee** — `startSession()` wraps `startWebRecognition()` in try/catch: any non-`SUPERSEDED` throw stops the VAD and forces `state$ = 'error'` before re-throwing, so `requesting` is **never** the terminal state. The recorder re-enables the mic button on `error` (retry works).
3. Watchdog cleared the instant `state$` becomes `listening` (`startRecognition`) — recognition itself may legitimately run far longer than 12 s — and in `stopSession()`.

**Notes on Drift Prevented:** future agents must keep the web pre-recognition phase covered by the watchdog and the `state$ → error` guarantee. `requesting` must always resolve to `listening` or `error`; never leave a `getUserMedia`/`vad.start()` await able to strand `state$`. This is the web-path analogue of the native keep-alive/hard-ceiling. The fix does NOT grant mic permission — if the OS/browser denies or the prompt is ignored, the user now sees a clear error + retry instead of an eternal spinner.

#### Repractice ↔ Session Room Parity — Sticky captureAudio Drift (2026-06-08)

**Drift type:** Shared-singleton state leakage. Repractice recognition reached `listening`, the mic opened (`onaudiostart` fired), but **no `onresult`** arrived → "No speech detected" — while the Session Room speaker worked on the same browser.

**Root cause:** `VoiceRecognitionEngine` is a `providedIn:'root'` **singleton**; its `captureAudio` flag (set by `enableAudioCapture()`) is **sticky** — never auto-reset. `SpeakerScreenComponent` sets it **every turn** (`enableAudioCapture(audioArchiveSvc.shouldCapture())`). `RepracticeSpeakerComponent` never set it, so it **inherited** whatever the last Session Room left on the engine. If that was capture-ON, `startSession()` opened an **extra `getUserMedia` + MediaRecorder** stream alongside the VAD stream and the recognition stream — three concurrent desktop mic captures starve the Web Speech recognizer (mic opens but it receives no audio → empty transcript). The Room "worked" only because it configures the flag deterministically each turn.

**Fix (`repractice-speaker.component.ts` `ngOnChanges`):** mirror the Room's engine setup — call `voiceEngine.enableAudioCapture(false)` (repractice has no audio-archive need) before auto-start, plus `prewarm()` in manual mode and a 300 ms auto-start delay for full parity. Now repractice runs the same minimal 2-stream path (VAD + recognition) as a capture-disabled Room turn.

**Notes on Drift Prevented:** any component that drives the shared engine must **explicitly set `enableAudioCapture()`** for its own needs — never assume the default. Sticky singleton flags (`captureAudio`, and by the same token the language cache) leak across feature boundaries (Room → Repractice). When a voice feature "works in one screen but not another" with the *same* engine, suspect leftover singleton state before suspecting the engine path.

#### VoiceRecorderComponent — Zone-Safe State Updates (2026-06-08)

**Drift type:** Angular zone / change-detection drift. Symptom: recognition reached `listening` and captured audio (scoring worked), but the recorder UI stayed on **"Requesting microphone…"** and never showed the listening/waveform view.

**Root cause:** `VoiceRecognitionEngine` advances `state$` to `listening` from inside an async chain that runs **after** `navigator.mediaDevices.getUserMedia()` / `vad.start()`. zone.js does not reliably reschedule native-API promise continuations back into the Angular zone, so `state$.next('listening')` fired **outside** the zone → the recorder's `subscribe` set `this.state` but **no change-detection tick ran** → the template kept the stale label. The Session Room masked this with constant broadcast + SignalR activity (frequent incidental CD ticks); repractice is idle, so it exposed the latent bug.

**Fix (`voice-recorder.component.ts`):** inject `NgZone` and run the `state$` and `interimTranscript$` subscription updates through `zone.run(...)`, guaranteeing a CD tick regardless of the emitting zone. Applies to **every** consumer of the recorder (Room + Repractice).

**Notes on Drift Prevented:** UI bound to `VoiceRecognitionEngine` streams must not assume the engine emits in the Angular zone — it frequently does not (post-`getUserMedia` continuations). Keep recorder state/interim updates wrapped in `zone.run`. Do not "fix" this by adding incidental activity (e.g. a broadcast) to force ticks — that only masks it.

**Concurrency contract (added 2026-06-06):** the engine is a `providedIn:'root'` **singleton** with a single shared `state$`. Only one session may run at a time. `startSession()` carries a monotonic `_sessionGen` token (bumped on every `startSession()`/`stopSession()`); after each internal `await` it re-checks the token and, if superseded, throws the static sentinel `VoiceRecognitionEngine.SUPERSEDED` and aborts **without mutating `state$`** — the superseding session owns state. Callers (`VoiceRecorderComponent.startRecording()`) must (1) early-return if state is `requesting`/`listening`/`processing` to avoid launching a duplicate, and (2) swallow `SUPERSEDED` rather than surfacing it as an error. This prevents the "stuck on Requesting microphone…" wedge when auto-start (`defaultVoiceStarter`) races a manual mic tap — see the repractice flow drift note.

#### VoiceSessionResult Shape

```typescript
{
  transcribedText: string       // raw joined final transcripts (display)
  expectedText: string          // original input
  fluencyScore: number          // 0–100 — word match accuracy weighted
  confidenceScore: number       // 0–100 — API confidence minus hesitation penalty
  overallScore: number          // fluency * 0.75 + apiConfidence * 0.25
  speakingSpeedWpm: number      // words / minutes of recording
  hesitationWords: string[]     // 'um','uh','er','hmm' etc. found in raw transcript
  repeatedWords: string[]       // consecutive duplicate words (len > 2)
  wordResults: WordResult[]     // per-word alignment result (for VoiceFeedbackComponent)
  pauseCount: number            // silence events detected by VAD
  durationMs: number            // total recording duration
  retryCount: number            // how many SpeechRecognition retries occurred
}
```

#### WordResult Shape

```typescript
{
  word: string        // spoken word
  expected: string    // expected word
  matched: boolean    // similarity >= 0.6
  score: number       // 0–100
  isHesitation: bool
  isExtra: bool       // spoken but not in expected
  isMissing: bool     // in expected but not spoken
}
```

#### Scoring Algorithm

```
1. Normalize both sides: lowercase → expand contractions → strip punctuation → remove fillers
2. Apply Indian English pronunciation map to spoken tokens (V/W swap, TH→D/T, contractions)
3. Align spoken vs expected with greedy look-ahead (extra word detection)
4. Per word: similarity = max(levenshteinSim, soundexSim)
   - >= 0.8 → matched (full credit)
   - >= 0.6 → partial match (half credit)
   - <  0.6 → wrong (no credit)
5. fluencyScore = (matchRatio * 0.6 + avgWordScore/100 * 0.4) * 100
6. overallScore = fluencyScore * 0.75 + apiConfidence * 100 * 0.25
7. confidenceScore = (apiConfidence * 100) - (hesitationCount * 5, max 25)
```

#### AudioActivityDetector — Key Config

Thresholds are set at runtime via `configure()` — values differ between desktop and mobile.

| Setting | Desktop | Mobile / Tablet | Tuning Note |
|---|---|---|---|
| `silenceThresholdDB` | `-50` | `-55` | More negative = must be quieter to count as silent. Mobile needs stricter threshold to avoid false silence during inter-word pauses |
| `silenceDurationMs` | `1200` | `2500` | Mobile users pause longer between phrases; 1.2 s triggers too early |
| `speechThresholdDB` | `-35` | `-30` | Amplitude must EXCEED this to latch `_hasSpeechStarted`. Mobile mics produce lower output so threshold is slightly higher |
| `fftSize` | `512` | `512` | Increased from 256 for smoother RMS |
| Audio constraints | `echoCancellation: true`, `noiseSuppression: true`, `autoGainControl: false` | same | AGC disabled — rapid AGC gain shifts caused false near-silence readings between words, triggering premature stops. AGC on the RECOGNITION stream is browser-managed and unaffected. |
| `sampleRate` | browser default | browser default | Removed explicit 16 000 Hz from getUserMedia — not a valid constraint and silently fails/rejects on some mobile browsers |

**`_hasSpeechStarted` flag:** one-way latch within a session. Set to `true` the first time the measured dB rises above `speechThresholdDB`. The silence callback is suppressed until this flag is true — prevents ambient/startup noise from triggering a premature stop before the user has spoken.

#### Known Mobile Drift — Bell Every 5 Seconds + Nothing Detected Fix (2026-06-04)

**Drift type:** Browser API contract drift — Android Chrome `continuous` mode behavior

**Root cause (bell every 5 s):** With `continuous=true` on mobile Chrome, the browser fires an internal silence timeout every ~5 seconds and triggers `onend` even while the user is speaking. Each `onend` caused a restart → bell sound. Users heard a bell every 5 seconds.

**Root cause (nothing detected):** Each restart via `recognition.start()` on an ended instance was unreliable. After 3 exhausted retries, the 12-second fallback timer fired `finalize()` with empty `allFinalTranscripts` → "(nothing detected)".

**Fixes applied in `voice-recognition.engine.ts`:**
1. **VAD skipped on mobile** — `AudioActivityDetector.start()` calls `getUserMedia` with custom audio constraints. When this stream is held open, the Web Speech API's internal audio pipeline on Android Chrome receives no audio (zero `onresult` events). Fix: `if (!this._isMobile) { await this.vad.start(); }`. Mobile end-of-speech detection is handled entirely by the recognition's own `onend` cycle.
2. `recognition.continuous = !this.isIOS && !this._isMobile` — mobile now uses `continuous=false` like iOS. The browser handles end-of-speech internally; `isFinal` fires reliably; no more 5-second internal timeout restarts.
3. `onend` with finals + mobile: restart recognition (same instance) instead of finalizing — let the 4.5 s silence timer finalize when user truly stops.
4. `onend` no-finals: restart via `recognition.start()` (same instance) — avoids race condition with `onerror` that also calls `startRecognition`.
5. `_accumulatedInterim` fallback: accumulated from full `event.results` buffer on every `onresult`. `finalize()` uses it when `allFinalTranscripts` is empty — safety net for any browser still not firing `isFinal`.

**VoiceFeedbackComponent change:** `showDetailedBreakdown` set to `true` by default — full comparison panel always visible without requiring user click.

> ⚠ The fix above applies to the **mobile WEB** path (Android Chrome, `isCapacitorNative=false`). The installed **Capacitor APK** uses a different code path (`startNativeSession`) documented next. Do not confuse the two.

#### Mobile/Tablet WEB Browser — Secure-Context & Engine Support (2026-06-05)

**Symptom:** Speech recognition works in the installed APK but fails in mobile/tablet browsers (Chrome, Edge).

**Drift type:** Browser-platform contract drift — secure-context requirement + engine-capability mismatch. Masked by a generic `"Speech recognition error: <code>"` message.

**Root cause (confirmed from config + Web Platform contract):**
1. **Insecure context (primary, affects Chrome too).** The Vite dev server runs `host: '0.0.0.0'`, port 4200, **plain HTTP, no TLS** (`vite.config.ts`); the dev origin resolves to `http://<LAN-IP>:4200` (`environment.ts` default `http://10.147.254.186:4200`). The Web Speech API and `getUserMedia` require `window.isSecureContext === true`. `localhost`/HTTPS qualify; a **LAN IP over HTTP does not**. So on a device browser pointed at the LAN dev server, `webkitSpeechRecognition` exists (looks supported) but `.start()` can never capture audio → silent failure or `not-allowed`/`service-not-allowed`. **This is why the APK works and the browser does not:** the Capacitor WebView loads bundled assets from a secure (`https://localhost` / `capacitor://`) origin and uses the on-device recognizer plugin — neither LAN-secure-context nor cloud-dependent.
2. **Engine has no speech backend (secondary, Edge/Firefox).** Even on HTTPS, **Edge and Firefox on Android have no speech service**. Only Chrome and Samsung Internet ship Google's backend. Edge-mobile exposes the API but errors `service-not-allowed`/`network`; Firefox lacks `webkitSpeechRecognition` entirely.

**Fix (graceful gating + guidance — no server-side STT):**
- **Dev server now HTTPS.** `vite.config.ts` adds `@vitejs/plugin-basic-ssl` (self-signed) → `https://<LAN-IP>:4200` is a secure context. Tap through the one-time cert warning on the device. Opt out with `HTTPS=false npm run dev`. The APK is unaffected (it loads bundled assets, not the dev server).
- **Pre-flight capability guard** in `VoiceRecognitionEngine.startSession()` (web path only): computes `getCapabilities()` and throws an actionable error BEFORE attempting recognition:
  - `!isSecureContext` → "Speech recognition needs a secure (HTTPS) connection. This page is open over an insecure address (`<origin>`). Open it via https://, or install the GoWithFlow app."
  - `!hasSpeechApi` → "Speech recognition isn't supported in this browser. Please use Google Chrome, or install the GoWithFlow app."
- **Error-handler upgrades** (`startRecognition.onerror`): `service-not-allowed` → non-retryable, browser-specific message (names Edge/Firefox when detected); terminal `network` on mobile Edge/Firefox → same guidance instead of an opaque code.
- **Engine detection** (`detectEngine`): `chrome | edge | firefox | samsung | safari | other` (Samsung/Edge/Firefox tested before Chrome/Safari because their UAs also contain those tokens; `EdgA`/`EdgiOS`/`FxiOS`/`CriOS` handled). Mobile Edge/Firefox flagged `speechSupported=false` for up-front UI guidance, but still allowed to attempt.
- **Diagnostics:** new Web Speech lifecycle logs (`onstart`, `onaudiostart`, `onspeechstart`, `onspeechend`, `onaudioend`) — the absence of `onaudiostart` after `onstart` is the signature of a blocked path. `getCapabilities()` snapshot logged at session start. Speech Debug page (`/user/speech-debug`) now shows Browser Engine, Secure Context, Origin, Speech Supported, and Blocker rows.

**Public API added:** `VoiceRecognitionEngine.getCapabilities(): VoiceCapabilities` — `{ isCapacitorNative, isSecureContext, hasSpeechApi, browserEngine, origin, online, isMobile, isIOS, speechSupported, blockerReason }`.

**Notes on Drift Prevented:** future agents must (a) keep the dev server on HTTPS for any mobile-browser voice testing — a LAN IP over HTTP will ALWAYS fail the Web Speech path regardless of code; (b) not treat `webkitSpeechRecognition in window === true` as "supported" — it is necessary, not sufficient (secure context + a real speech backend are also required); (c) not attempt to "fix" Edge/Firefox-mobile in-browser — they have no Web Speech backend; the only universal-browser path is server-side STT (explicitly deferred). The APK remains the recommended path for guaranteed mobile voice.

#### Native (Capacitor APK) Speech Path — Stable Contract (2026-06-04)
> Covers: language resolution, mic-contention, number accuracy, and responsiveness tuning.

**Applies to:** Android/iOS APK where `Capacitor.isNativePlatform() === true`. This path uses `@capacitor-community/speech-recognition` v7 (`startNativeSession` in `voice-recognition.engine.ts`). The Web Speech API is NOT available here.

**Drift type:** DB/API contract drift equivalent — *device speech-engine contract drift*. The native plugin drives the device's **default** recognizer, which is frequently the **offline SODA engine** (`com.google.android.tts/GoogleTTSRecognitionService`), NOT the cloud recognizer used by desktop/mobile Chrome.

**Root cause (confirmed on device QGCAAETSYXRS95KV — iQOO IV2201, Android 13, 2026-06-04):**
1. **Offline-only recognizer + language-pack mismatch.** The offline SODA engine only serves languages whose offline model is **installed**. Device locale was `en-GB`; logcat (`LanguagePackMaintenance: Ideal set: [en-GB_v3071]`) showed **only en-GB installed**. The old code hard-requested `en-IN` then `en-US` — neither installed → recognizer errored → no transcript → "No speech detected".
2. **Swallowed error channel.** In `partialResults` mode the plugin's `start()` calls `call.resolve()` immediately; a later `onError()` rejects that **already-resolved** call (swallowed) and emits **no** `listeningState` event. The old en-US fallback was keyed on `listeningState:stopped`, which **never fired** on this error — the session hung until the 20 s hard timeout. The old code comment claiming "listeningState:stopped is the only JS-visible signal on onError" was factually wrong.
3. **Why it "reappeared".** The previously-working version used the Web Speech API (cloud STT, en-IN supported). The 2026-06-03 Capacitor wrap switched mobile to the native offline recognizer, reintroducing the failure. **This is also why desktop ≠ mobile** (desktop still uses cloud Web Speech).

**API constraints that shape the fix:**
- `getSupportedLanguages()` is **unavailable on Android 13+** — cannot query supported languages at runtime.
- `start({ language })` — `language` is **optional**; omitting it makes the plugin use `Locale.getDefault()` = device locale = the installed pack (guaranteed-available fallback).

**Fix — language fallback chain + keep-alive + cache (`startNativeSession`):**
1. **Candidate chain** (`buildLanguageCandidates`, de-duplicated): `[cachedLang, deviceEnglishVariant, en-IN, en-US, en-GB, undefined]`. `deviceEnglishVariant` = `navigator.language` if it is `en-*`, else `en-<REGION>` derived from the locale. `undefined` = omit language → device default (`Locale.getDefault()`, the installed pack).
2. **Same-language keep-alive (7 s), NOT a short watchdog.** A bad language is silent *and* errorless — but so is a working recognizer while the user is still reading. The earlier 4 s watchdog conflated the two and tore down the working en-GB recognizer (confirmed on device: it thrashed the whole chain every 4 s and the user never got a window to speak). Replaced by: if no signal in 7 s, **relisten on the SAME language** (Android `SpeechRecognizer` is single-utterance; a slow starter just produces no signal). Only after `MAX_SILENT_RESTARTS` (2) silent relistens does it advance to the next candidate.
3. **`langConfirmed` latch.** Set on the first `started`/`partial`; once set the language is never switched again. On confirmation the working language is cached in `localStorage['gwf_voice_lang']` so later turns succeed on attempt 0.
4. **Token-guarded attempts (`attemptToken`).** Each (re)start bumps a token; stale callbacks from a superseded attempt are ignored.
5. **Finalize triggers:** silence timer (**adaptive `SILENCE_MS`** after last new partial — see below) → finalize; `listeningState:stopped` with transcript → finalize; hard ceiling 30 s; external stop (`_intentionalStop`) → finalize with whatever was captured.
   - **Adaptive end-of-speech window (`SILENCE_MS`, 2026-06-17):** keyed to the expected utterance's word count — `≤6 words → 1200 ms`, `7–12 → 2000 ms`, `>12 → 2500 ms`. A flat 1.2 s truncated long Session Room sentences (a natural breath at a comma/period boundary tripped the timer). Short repractice drills keep the snappy Duolingo stop. See drift note below.
6. **Terminal failure** (chain exhausted with no working language) → actionable error: "…download an English voice model in Settings → Voice input…".

**Mic-contention fix (PROVEN root cause of "No speech detected" even with a valid language):** `VoiceBroadcastService.startBroadcast()` opened `getUserMedia` (`AUDIO_SOURCE_VOICE_COMMUNICATION`) the instant recording began, which **preempted** the native recognizer's `AUDIO_SOURCE_VOICE_RECOGNITION` capture (device log: `getInputForAttr() source 7` opening after `source 6`). Two processes cannot share one Android mic, and the native `SpeechRecognizer` cannot be fed an external stream. **Fix:** `startBroadcast()` early-returns on `Capacitor.isNativePlatform()` — the recognizer keeps exclusive mic access. **Product decision (confirmed):** on the APK, live WebRTC broadcast is OFF; scoring (on-device, free, offline) takes priority. Live broadcast remains available on web/desktop.

**Accuracy fix — number normalization (`TranscriptNormalizer.numbersToDigits`):** the recognizer emits digits ("305", "25") while scripts may use words; the scorer's Levenshtein/Soundex cannot bridge "305"↔"three oh five". Both spoken and expected sides are now canonicalized to digits before scoring. Handles digit-sequences ("three oh five"→"305", "double five"→"55") and cardinals ("twenty five"→"25", "three hundred five"→"305"). Display text is unaffected (uses the raw transcript).

**Responsiveness fixes:**
- `VoiceRecognitionEngine.prewarm()` — pre-loads the SODA model (brief start+stop) so the first tap is instant. Called from `SpeakerScreenComponent` in **manual** mode (in auto-start mode the auto-start itself is the warm-up). Guarded: native-only, once per app run, idle-only, never prompts, never stops a real session.
- Auto-start delay trimmed 700 ms → 300 ms.

**Diagnostics:** all native logs prefixed `[VRE]` → `adb logcat | grep VRE`. Key lines: `Native session start` (chain + cachedLang), `Native start()` (per attempt + lang), `Native language confirmed`, `Native relisten`, `Keep-alive`.

**Notes on Drift Prevented:** the native path is now resilient to whatever English pack the device has installed, independent of locale, and self-heals via caching. Future agents must: (a) NOT reintroduce a hard-coded `en-IN`/`en-US`-only assumption; (b) remember native `onError` is invisible to JS in `partialResults` mode — use timeouts/keep-alive, never rely on an error/`stopped` signal to detect a bad language; (c) NOT re-enable `startBroadcast()` mic capture on native (it starves the recognizer); (d) keep number normalization applied to BOTH sides of the comparison.

#### Native Long-Utterance Truncation — Adaptive Silence Window (2026-06-17)

**Drift type:** Premature-finalize / fixed-threshold drift on the native path. Symptom (confirmed on IV2201, Session Room speaker turn, logcat captured): expected "Good evening, sir! Welcome to the Grand Stay Hotel. Let me look that up for you." — the recognizer transcribed only "…welcome to the Grand", finalized there, and the remaining 9 words showed as **Missing** → Fluency 19, overall **36/100**. Voice capture worked; the score was wrong because the utterance was cut off mid-sentence.

**Root cause (from `[VRE]` native logs):** partials grew continuously to "…the Grand" at t=43.335 s, then the user took a natural breath at the comma/period boundary ("Grand **Stay Hotel.**"). The flat **`SILENCE_MS = 1200`** timer (reset on each new partial in the `partialResults` handler) fired ~1.2 s later at t=44.541 → finalize. No `listeningState:stopped` event preceded it — the SODA recognizer kept the single utterance open; the JS silence timer alone triggered the cut-off. The 1.2 s value was tuned for snappy short drills and was too aggressive for multi-clause sentences.

**Fix (`voice-recognition.engine.ts` `startNativeSession`):** `SILENCE_MS` is now **adaptive to `expectedText` word count** — `≤6 → 1200 ms`, `7–12 → 2000 ms`, `>12 → 2500 ms` — computed once before the recognition promise. Long sentences ride through inter-phrase pauses; short repractice/vocabulary drills keep the 1.2 s snap.

**Secondary path NOT changed (watch item):** if the SODA engine *does* fire `listeningState:'stopped'` (onEndOfSpeech) during a mid-utterance pause, `finalizeWithTranscript()` still finalizes immediately regardless of `SILENCE_MS`. The reproduced failure did not hit this path. If long-utterance truncation recurs *with* a `stopped` event in the log, the fix is to relisten-and-append on `stopped` when the transcript covers materially less than expected (deferred — adds ghost/double-capture risk; only implement against a confirmed repro).

**Separate, un-fixed artifact:** a leading "good morning" ghost was prepended before the real "good evening" — a pure SODA misrecognition (the engine's own running hypothesis), not a timer/alignment bug. Mitigation belongs in `PronunciationScorer` extra-word alignment, not here.

**Notes on Drift Prevented:** do NOT revert `SILENCE_MS` to a flat constant — short and long utterances have opposite end-of-speech needs. Any future "snappier stop" request must preserve the long-utterance tier or it will re-truncate Session Room sentences.

#### VoiceFeedbackComponent — UI Defaults

- `showDetailedBreakdown` defaults to `true` — full comparison panel (You said / Expected) is always visible on result, no click needed.

#### VoiceFeedbackComponent — Score Bands

| Score | Band | Emoji |
|---|---|---|
| 90–100 | Excellent! | 🌟 |
| 75–89 | Great job! | ✅ |
| 60–74 | Good try! | 👍 |
| 40–59 | Keep going! | 💪 |
| 0–39 | Try again | 🔄 |

#### Speaking Speed Interpretation

| WPM | Label |
|---|---|
| 0 (exactly) | — Not detected (capture failed — not a speed judgment) |
| 1–69 | 🐢 Too slow |
| 70–99 | ✅ Good pace |
| 100–139 | 👏 Natural speed |
| ≥ 140 | ⚡ Too fast |

#### Browser Compatibility

> **HARD PREREQUISITE for ALL web rows: a secure context (`window.isSecureContext === true`).** The Web Speech API and `getUserMedia` are gated on a secure context. `https://` origins and `localhost`/`127.0.0.1` qualify; a **LAN IP over plain HTTP (e.g. `http://10.x.x.x:4200`) does NOT** — the API surface still exists (`webkitSpeechRecognition in window` is `true`, so it *looks* supported) but `.start()` can never capture audio. See "Mobile/Tablet WEB Browser — Secure-Context & Engine Support" below.

| Browser | Platform | Support |
|---|---|---|
| Chrome 100+ | Android / Desktop | ✅ Full (cloud STT) — requires secure context — use `lang=en-IN`, `continuous=true` desktop / `false` mobile |
| Edge | **Desktop** | ✅ Full (has speech backend) |
| Edge | **Android/mobile** | ❌ None — exposes the API but has **no speech service** → `service-not-allowed`/`network`. Gated with a "use Chrome / install the app" message |
| Safari 15+ | iOS / iPadOS | ⚠ Partial — `continuous=false`, restart on `onend` |
| Samsung Internet | Android | ✅ Full (Chrome engine) |
| Firefox | Any | ❌ None — no `webkitSpeechRecognition` → caught by `!hasSpeechApi` guard |
| **Capacitor APK** | **Android / iOS** | ✅ Uses the **device default recognizer** (often offline SODA), NOT cloud. No secure-context/cloud dependency. Language depends on installed offline packs — handled by the native language fallback chain (see "Native (Capacitor APK) Speech Path" above). |

#### Integration Points

- `SpeakerScreenComponent` imports `VoiceRecorderComponent` + `VoiceFeedbackComponent` as standalone components
- `VoiceRecorderComponent` emits: `recordingComplete(VoiceSessionResult)`, `recordingStarted()`, `errorOccurred(string)`
- `VoiceFeedbackComponent` receives: `@Input() result: VoiceSessionResult | null`
- `VoiceRecognitionEngine` is `providedIn: 'root'` — single instance per app
- `AudioActivityDetector`, `PronunciationScorer`, `TranscriptNormalizer` are all `providedIn: 'root'`

---

### Flow: WebRTC Voice Broadcast — Stable Contract

**Services:** `VoiceBroadcastService` (singleton). Call `init(sessionId, userId)` from `SessionRoomComponent.initSession()`. Call `destroy()` from `ngOnDestroy()`.

**Internal state:**
- `localStream` — speaker getUserMedia stream
- `peerConnections: Map<peerId, RTCPeerConnection>`
- `pendingCandidates: Map<peerId, RTCIceCandidateInit[]>` — ICE candidate queue (race-condition guard)
- `remoteDescriptionSet: Set<peerId>` — tracks which peers have had `setRemoteDescription` called
- `isReceivingAudio: signal<boolean>` — drives "Live Audio" badge
- `isBroadcasting: signal<boolean>` — prevents duplicate startBroadcast calls

**STUN:** `stun:stun.l.google.com:19302`. No TURN server. Requires direct or NAT-traversable network.

**Full Sequence:**

```
Speaker startRecording()
  → startBroadcast() → getUserMedia({ audio }) → ws.emit('VoiceBroadcastStart')
  → Hub broadcasts VOICE_BROADCAST_STARTED { speakerId }

Listener (listenVoiceBroadcast = true)
  → handleBroadcastStarted(speakerId) [guard: speakerId !== myUserId]
  → ws.emit('RequestVoiceStream') → Hub broadcasts VOICE_STREAM_REQUESTED { listenerUserId }

SessionRoomComponent (speaker side)
  → on VOICE_STREAM_REQUESTED: if isSpeaker() → createOfferForListener(listenerUserId)
    → createPeerConnection(listenerUserId): resets pendingCandidates + remoteDescriptionSet for peer
    → addTrack(audioTrack) → createOffer → setLocalDescription
    → ws.emit('SendWebRTCOffer') → Hub broadcasts WEBRTC_OFFER { fromUserId, toUserId, offerJson }

SessionRoomComponent (listener side)
  → on WEBRTC_OFFER where toUserId === myUserId → handleOffer(fromUserId, offerJson)
    → createPeerConnection(fromUserId)
    → pc.ontrack: attach stream to HTMLAudioElement → play → isReceivingAudio = true
    → setRemoteDescription(offer) → remoteDescriptionSet.add(fromUserId)
    → drainPendingCandidates(fromUserId) [applies queued ICE candidates]
    → createAnswer → setLocalDescription
    → ws.emit('SendWebRTCAnswer') → Hub broadcasts WEBRTC_ANSWER { fromUserId, toUserId, answerJson }

SessionRoomComponent (speaker side)
  → on WEBRTC_ANSWER where toUserId === myUserId → handleAnswer(fromUserId, answerJson)
    → setRemoteDescription(answer) → remoteDescriptionSet.add(fromUserId)
    → drainPendingCandidates(fromUserId)

ICE exchange (both sides, async)
  → pc.onicecandidate → ws.emit('SendICECandidate') → Hub broadcasts ICE_CANDIDATE
  → handleIceCandidate(fromUserId, candidateJson):
      If remoteDescriptionSet has fromUserId → addIceCandidate immediately
      Else → push to pendingCandidates[fromUserId] (drained on setRemoteDescription)

Speaker stopRecording()
  → stopBroadcast(): isBroadcasting = false, stop tracks, closePeerConnections()
    → ws.emit('VoiceBroadcastStop') → Hub broadcasts VOICE_BROADCAST_STOPPED
  → Listeners: handleBroadcastStopped(): pause audio, closePeerConnections(), isReceivingAudio = false
```

**ICE Race Condition — Prevention:**
ICE candidates arriving before `setRemoteDescription` are queued in `pendingCandidates[peerId]`. Drained synchronously in `handleOffer()` and `handleAnswer()` after `setRemoteDescription` completes. Previously: caught silently in try-catch, losing candidates and causing connection failure on slow networks.

**Destroy sequence:** `handleBroadcastStopped()` (sync: closes connections, clears maps) → `stopBroadcast()` (stops tracks, best-effort WS emit with `.catch(() => {})` — WS may already be disconnecting)

**Known Limitations:**
- No TURN server: fails in symmetric NAT (some corporate networks / mobile carriers)
- No renegotiation on network change

### Flow: LISTENER_TAG — Corrected Contract

**Bug fixed (pre-existing):** Frontend read `tagData.feedbackTag`; backend always sent `{ tag, fromUserId }`. Field corrected to `tagData.tag` in `SessionRoomComponent`.

Hub payload: `{ tag: string, fromUserId: long }` → `listenerTagFlash.set(tagData.tag)` → shown 2 s in listener screen

### Notes on Drift Prevented

| Drift Type | Description | Fixed In |
|---|---|---|
| Request drift | `LISTENER_TAG` payload: `feedbackTag` → `tag` — backend always sent `tag`; frontend silently received `undefined` | `session-room.component.ts` |
| Logic drift | `onConfirm()` had no re-entry guard; Skip + autoSubmitOnStop double-fired submission | `speaker-screen.component.ts` |
| Protocol drift | WebRTC ICE candidates dropped before `setRemoteDescription` — candidate queue added | `voice-broadcast.service.ts` |
| Cleanup drift | `destroy()` called `closePeerConnections()` twice — resolved by listener-first ordering + `isBroadcasting` guard | `voice-broadcast.service.ts` |

---

## Backend Live Session Module — Phase 0 Workflow Fixes (2026-06-01)

### Facilitator Role Awareness — Stable Contract

**Purpose:** Distinguish facilitator turns (Interviewer, Tutor, Coach) from performance turns. Facilitator turns are read-aloud only — no voice analysis, no scoring, no listener feedback, no re-read button.

**Facilitator Role Mapping:**

| Category (canonical) | Facilitator Speaker Label | Performer Speaker Label |
|---|---|---|
| Mock Interview / Interview | Interviewer | Candidate |
| Vocabulary Sprint / Vocabulary | Tutor | Learner |
| Repractice Round / Repetition | Coach | Learner |
| Grammar Drill, Roleplay, Fluency Drill | — (both speakers perform) | Both |

**Implementation:**
- `GoWithFlow.Application.Common.FacilitatorRoles` — static helper, `IsFacilitator(category, speakerLabel)`. Handles both canonical and legacy category names.
- `TurnStateResponseDto.IsFacilitatorTurn` (bool) — computed in `LiveSessionRepository.GetCurrentTurnAsync` by joining `tblScript` via `utterance.ScriptId` and calling `FacilitatorRoles.IsFacilitator(script.Category, utterance.SpeakerLabel)`.
- `MemberScoreDto.IsFacilitator` (bool) — tagged in `GetSessionCompletionSummaryAsync` after loading session members' slot names. Enables the session report to exclude facilitators from the performance scoreboard.
- Frontend `TurnState.isFacilitatorTurn` (bool) — mapped from API response.
- `SpeakerScreenComponent`: when `isFacilitatorTurn = true`, renders a "Read Aloud" mode UI (text display + "Done Reading — Next Turn" button). No voice recorder, no voice analysis call, no score. Calls `onSkip()` which submits `CompleteTurn` with score 0.
- `SpeakerScreenComponent.showReReadButton`: also checks `!isFacilitatorTurn` — ensures re-read button never appears on facilitator turns.
- `SessionReportComponent.scoreboard`: filters `MemberScores` to `!isFacilitator && !isAi` for the performance leaderboard. Facilitator names shown in a separate "Facilitator" section; AI Voice Participant(s) shown in a separate "AI Partner" chip (Phase 17) — never ranked. `topScore` likewise excludes facilitators + AI.
- **Mobile leaderboard layout (2026-06-17):** the report leaderboard renders as **stacked per-member cards on mobile** (`md:hidden`) and the full **table only on desktop** (`hidden md:block`) — honors the no-horizontal-scroll standard (the 5-column table was clipping Rating/Mistakes on phones). Header/stat sizes tightened to the mobile caps (≤22px scores).
- `handleTurnShift()` in `SessionRoomComponent`: optimistic update sets `isFacilitatorTurn: false` (safe default — the canonical `loadCurrentTurn()` call immediately follows and sets the correct value).

---

### Listener Feedback — Simplified Contract (Phase 0 Step 2)

**Change:** Reduced from 4 tags to 2 tags. Automatic voice analysis already captures hesitation, grammar errors, and pronunciation issues at higher accuracy than human listener tags.

**Valid FeedbackTag values (current):**
- `"Good"` — social positive signal. Peer encouragement.
- `"Needs Work"` — gentle concern flag. Alias `"NeedsWork"` accepted server-side.

**Removed tags:** `"Hesitated"`, `"Mistake"`, `"Unclear Pronunciation"` — all direct duplicates of automatic voice analysis.

**Facilitator turn suppression:** Listener feedback buttons are hidden entirely when `turnState.isFacilitatorTurn = true`. Facilitator turns (Interviewer, Tutor, Coach) are not performance turns — listener feedback on them is meaningless.

**Files changed:**
- `ListenerFeedbackTagType.cs` — enum reduced to `Good = 1`, `NeedsWork = 2`
- `ListenerFeedbackRequestValidator.cs` — `ValidTags` now `["Good", "Needs Work"]`
- `LiveSessionService.FeedbackTagMap` — maps only `Good` and `Needs Work / NeedsWork`
- `ListenerScreenComponent` — `feedbackActions` reduced to 2 buttons; feedback section wrapped in `@if (!turnState.isFacilitatorTurn)`

---

### Re-Read Button Restriction (Phase 0 Step 3)

Handled structurally by the facilitator turn UI: facilitator turns render "Read Aloud" mode only — no recording phase, no feedback phase, so the re-read button is never reached. Additionally, `showReReadButton` getter in `SpeakerScreenComponent` explicitly checks `!this.turnState.isFacilitatorTurn`.

### Migration State

- `InitialCreate_Phase1`, `AddAdminModule_Phase2`, `AddScriptModule_Phase3`, `AddSessionModule_Phase4`, `AddLiveSessionModule_Phase5`, `AddUserModule_Phase7`, `AddVocabularyModule_Phase8`

---

---

## Phase 1 Feature Contracts (2026-06-01)

### Phase 1 Step 1 — Post-Session Review

**Purpose:** Users can revisit their completed session with a full per-turn transcript, inline grammar error annotations, hesitation flags, pronunciation issues, and per-turn scores.

**Entry Points:** `/session/review/:sessionId` (Angular route). "Review" icon button on session history list.

**API Endpoint:** `GET /api/turns/{sessionId}/review`
- Auth: UserOrAdmin + ActiveUser
- Returns: `SessionReviewResponseDto` with `SessionId`, `ScriptTitle`, `Category`, `GrammarFocusTag`, `TotalTurns`, `AverageOverallScore`, `Turns: List<SessionReviewTurnDto>`
- Each `SessionReviewTurnDto`: `TurnIndex`, `SpeakerLabel`, `IsFacilitatorTurn`, `EnglishText`, `TranscribedText`, `FluencyScore`, `ConfidenceScore`, `SpeakingSpeedWpm`, `OverallScore`, `HesitationWords`, `GrammarErrors`, `PronunciationIssues`, `WasAnalyzed`

**Data source:** `tblUtterance` (all turns), `tblVoiceAnalysis` (per-user voice analysis joined by UtteranceId), `tblScript` (session metadata via `tblSession.ScriptId`).

**Implementation:**
- `ILiveSessionRepository.GetSessionReviewAsync(sessionId, userId)` — EF LINQ query building `SessionReviewResponseDto`. Calls existing `GetVoiceAnalysisByUserIdAsync(userId, sessionId)` and joins by `UtteranceId`.
- `ILiveSessionService.GetSessionReviewAsync(sessionId, userId)` — validation wrapper
- `LiveSessionController.GetSessionReviewAsync` — `GET /api/turns/{sessionId}/review`
- Frontend: `SessionReviewComponent` (`/session/review/:sessionId`), `live-session.service.getSessionReview()`, `SessionReview` model, route added to `session.routes.ts`
- Session history: "Review" icon button (FileText icon) added alongside chevron

**Facilitator turns:** `WasAnalyzed = false`, score = 0. Only performance turns (non-facilitator) are scored. `AverageOverallScore` calculated from analyzed performance turns only.

---

### Phase 1 Step 2 — Vocabulary Retention Tracker

**Purpose:** Tracks FocusWords introduced in VocabularySprint sessions per user. Shows vocabulary bank, correct production rate, and words due for review (not seen in 7+ days).

**DB Table:** `tblUserVocabulary`
- Key columns: `UserId`, `FocusWord NVARCHAR(64)`, `SourceSessionId`, `DateIntroduced`, `WasProducedCorrectly BIT`, `TimesEncountered INT`
- Unique constraint: `(UserId, FocusWord, SourceSessionId)` — prevents duplicates per session
- Indexes: `IDX_tblUserVocabulary_UserId`, `IDX_tblUserVocabulary_UserId_FocusWord`
- Migration: `20260601000001_AddVocabularyModule_Phase8.cs` (raw SQL `migrationBuilder.Sql()` — no EF entity mapping)
- PostgreSQL equivalent: `Docs/PostgreSQLMigration/17_add_vocabulary_module.sql`

**API Endpoint:** `GET /api/vocabulary/bank`
- Returns: `VocabularyBankResponseDto` with `TotalWords`, `DueForReviewCount`, `Words: List<VocabularyBankItemDto>`
- Each word: `FocusWord`, `DateIntroduced`, `TimesEncountered`, `TimesCorrect`, `CorrectRate` (%), `IsDueForReview` (not seen in 7+ days)

**Hook:** `LiveSessionService.CompleteSessionAsync` — if `session.SessionMode` is `Vocabulary Sprint` or `Vocabulary`, calls `IVocabularyService.SaveSessionVocabularyAsync(sessionId, learnerId)` for each Learner member.

**Word correctness determination:** Learner voice analysis `OverallScore >= 60` → `WasProducedCorrectly = true`.

**Session summary enrichment:** `SessionSummaryResponseDto.VocabularySummary` (type: `SessionVocabularySummaryDto`) populated for VocabularySprint sessions only.

**Services/Repository:**
- `IVocabularyRepository`, `VocabularyRepository` — raw ADO.NET queries on `tblUserVocabulary` (MERGE upsert for SQL Server)
- `IVocabularyService`, `VocabularyService` — hooks session completion and provides bank data
- `VocabularyController` — `GET /api/vocabulary/bank`
- Frontend: `VocabularyBankComponent` at `/user/vocabulary`, `user.service.getVocabularyBank()`

---

### Phase 1 Step 3 — Weekly Learning Report

**Purpose:** Dismissible card on user dashboard showing this-week practice stats, improvement delta vs. last week, and 2 recommended scripts.

**API Endpoint:** `GET /api/dashboard/weekly-report`
- Returns: `WeeklyReportResponseDto` with `SessionsThisWeek`, `PracticeMinutesThisWeek`, `ErrorsDetectedThisWeek`, `ErrorsResolvedThisWeek`, `TopImprovementMetric`, `WeakestGrammarTag`, `RecommendedScript1Id/Title`, `RecommendedScript2Id/Title`, `IsReengagement`, `LastSessionDate`
- Week boundary: Monday 00:00 UTC to Sunday 23:59 UTC
- No new DB tables. All data from: `tblSession`, `tblSessionMember`, `tblVoiceAnalysis`, `tblMistake`, `tblScript`

**Implementation:**
- `UserRepository.GetWeeklyReportAsync(userId)` — EF LINQ queries (no SPs)
- `UserDashboardService.GetWeeklyReportAsync(userId)`
- `UserDashboardController.GetWeeklyReportAsync` — `GET /api/dashboard/weekly-report`
- Frontend: `UserDashboardComponent` loads on `ngOnInit`. Shown once per calendar week (tracked in `localStorage` key `gwf_weekly_report_{year}-W{week}`). Dismiss sets the key to `'dismissed'` — not shown again until next week.

**Re-engagement Branch (`IsReengagement = true`):**
- Shown when `IsReengagement = true` (no sessions this week)
- Displays last session date and a "Pick up where you left off" CTA
- CTA is only rendered when `RecommendedScript1Id` has a value
- CTA navigates to: `/scripts/prepare/{RecommendedScript1Id}` — opens Script Preparation view for the specific recommended script
- **Notes on Drift (2026-06-02):** CTA previously navigated to `/scripts` (generic library). Fixed to use `/scripts/prepare/:id` matching the consistent navigation pattern used by Guided Learning Path recommendations. `/scripts` provides no re-engagement context and requires the user to rediscover the correct script manually.

---

### Phase 1 Step 4 — Guided Learning Path

**Purpose:** Shows 2–3 personalized next-session recommendations on the dashboard based on mistake history, recent performance, and category variety.

**API Endpoint:** `GET /api/dashboard/learning-path`
- Returns: `GuidedLearningPathResponseDto` with `Recommendations: List<LearningPathRecommendationDto>`
- Each recommendation: `ScriptId`, `ScriptTitle`, `Category`, `ComplexityLevel`, `ReasonText`, `RecommendationType` (`repractice` | `low_score` | `variety`)

**Recommendation logic (priority order):**
0. `goal` *(added 2026-06-02)*: User has an active goal → recommend the newest active script whose `Category` matches the goal's primary category (Goal-to-category: interview→Mock Interview/MockInterview, grammar→Grammar Drill/GrammarDrill, vocabulary→Vocabulary Sprint/VocabularySprint, fluency→Fluency Drill/FluencyDrill). Reads active goal from `tblUserGoal` via raw ADO.NET before EF LINQ rules run.
1. `repractice`: Unresolved mistakes exist → RepracticeRound script matching top GrammarTag
2. `low_score`: Last sessions in a category average < 65 → repeat that category
3. `variety`: User hasn't tried a category in 14+ days → suggest it
4. Fallback: most recently uploaded active script

**RecommendationType values:** `goal` | `repractice` | `low_score` | `variety`

**Navigation from Goals Page:** "View Guided Learning Path" button links to `/user/dashboard` — the Guided Learning Path panel is on the dashboard. This is correct per the PM plan (Feature 2, Phase 1 Step 4). No dedicated `/user/guided-path` route exists or is needed.

**Implementation:**
- `UserRepository.GetGuidedLearningPathAsync(userId)` — EF LINQ queries + raw ADO.NET for Rule 0 goal read
- `UserDashboardService.GetGuidedLearningPathAsync(userId)`
- `UserDashboardController.GetGuidedLearningPathAsync` — `GET /api/dashboard/learning-path`
- Frontend: `UserDashboardComponent` loads learning path on `ngOnInit`. Panel displayed when recommendations exist. Each card links to `/scripts` with `scriptId` query param. `recIcon/recIconBg/recIconColor` handle `'goal'` type (uses TargetIcon + primary blue).

**Notes on Drift Prevented (2026-06-02):**
- Drift type: **PM requirement not implemented** — Phase 2 Step 4 Requirement #3 states "recommendations are filtered to scripts relevant to the active goal type." `GetGuidedLearningPathAsync` never read `tblUserGoal`. Users with an active "Job Interview" goal received the same generic recommendations as users with no goal at all. Fixed by adding Rule 0 that queries `tblUserGoal` and injects a goal-category-matched script as the top recommendation.

## Backend Mistake Repractice Module

### Module Scope

- Stores user mistakes extracted from completed live-session voice analysis
- Generates focused repractice sessions from unresolved mistakes
- Serves mistake list, summary, grammar progress, repractice detail, repractice history, attempt update, and completion APIs

### Database Schema

- `tblMistake`: `MistakeId`, `UserId`, `SessionId`, `UtteranceId`, `ScriptId`, `UtteranceText`, `SpokenText`, `MistakeType`, `MistakeDetail`, `GrammarTag`, `ContextTag`, `CorrectionText`, `PracticeCount`, `IsResolved`, `FirstOccurrence`, `LastAttempt`; indexes: `IDX_tblMistake_UserId`, `IDX_tblMistake_SessionId`, `IDX_tblMistake_UserId_IsResolved`, `IDX_tblMistake_GrammarTag`
- `tblRepracticeSession`: `RepracticeSessionId`, `UserId`, `SourceSessionId`, `TotalMistakes`, `CompletedRounds`, `ImprovementPercent`, `Status`, `GeneratedDate`; index: `IDX_tblRepracticeSession_UserId`
- `tblRepracticeUtterance`: `RepracticeUtteranceId`, `RepracticeSessionId`, `MistakeId`, `OriginalUtteranceId`, `EnglishText`, `HintText`, `MistakeType`, `MistakeDetail`, `CorrectionNote`, `AttemptCount`, `BestScore`, `LastScore`, `IsResolved`; index: `IDX_tblRepracticeUtterance_RepracticeSessionId`

### Stored Procedures

`uspInsertMistake`, `uspGetMistakeByUserIdWithFilter`, `uspGetMistakeSummaryByUserId`, `uspGetUnresolvedMistakeByUserId`, `uspInsertRepracticeSession`, `uspInsertRepracticeUtterance`, `uspGetRepracticeSessionByRepracticeSessionId`, `uspGetRepracticeSessionListByUserId`, `uspUpdateRepracticeUtteranceAttempt`, `uspCalculateImprovementPercentByUserId`, `uspUpdateRepracticeSessionStatus`, `uspGetGrammarProgressByUserId`

### Domain Model

- Entities: `Mistake`, `RepracticeSession`, `RepracticeUtterance`
- Enums: `MistakeTypeCode`, `RepracticeStatusType`
- EF configurations: `MistakeConfiguration`, `RepracticeSessionConfiguration`, `RepracticeUtteranceConfiguration`
- DbContext: `DbSet<Mistake> Mistakes`, `DbSet<RepracticeSession> RepracticeSessions`, `DbSet<RepracticeUtterance> RepracticeUtterances`

### Request and Response Contracts

- Request DTOs: `MistakeFilterRequestDto`, `GenerateRepracticeRequestDto`, `UpdateAttemptRequestDto`
- Response DTOs: `MistakeResponseDto`, `MistakeSummaryResponseDto`, `RepracticeSessionResponseDto`, `RepracticeUtteranceResponseDto`, `GrammarProgressResponseDto`, `CompleteRepracticeResponseDto`, `UpdateAttemptResponseDto`
- `GenerateRepracticeRequestDto`: `{ SourceSessionId: long, IncludeAllSessions: bool }`

### API Surface

- All endpoints require authentication. `UserId` always sourced from JWT — not from request body.

#### GET /api/mistakes

- Returns paginated mistakes; supports `MistakeType` and `IsResolved` filters

#### GET /api/mistakes/summary

- Returns: total, resolved, pending, improvement-percentage for authenticated user

#### GET /api/mistakes/grammar-progress

- Returns: grammar-tag level progress with resolved counts and progress-bar percentages

#### POST /api/repractice/generate

- Loads unresolved mistakes for the authenticated user; creates one repractice session with one repractice utterance per mistake
- Request body: `{ sourceSessionId: long, includeAllSessions: bool }`
  - `includeAllSessions = false` (per-row "practice"): restricts to `sourceSessionId`; FluentValidation requires `SourceSessionId > 0` (`.When(!IncludeAllSessions)`)
  - `includeAllSessions = true` ("Practice All Mistakes"): pulls EVERY unresolved mistake across ALL sessions; `sourceSessionId` may be 0
- Mistake selection: `RepracticeService` computes `sessionFilter = IncludeAllSessions ? 0 : SourceSessionId` and calls `IMistakeRepository.GetUnresolvedMistakesAsync(userId, sessionFilter)`. The stored procedure `uspGetUnresolvedMistakeByUserId` treats `@SourceSessionId = 0` as "all sessions" (`AND (p_sourcesessionid = 0 OR mst.sessionid = p_sourcesessionid)`).
- FK anchor: `tblRepracticeSession.SourceSessionId` is **NOT NULL with FK → tblSession(SessionId)**, so it must store a real session. In all-sessions mode the service derives the anchor from `mistakes[0].SessionId` (a guaranteed-valid session); it is provenance only and does not affect which mistakes were practiced.
- Frontend: `MyMistakesComponent` at `/user/my-mistakes`
  - "Practice All Mistakes" CTA → `practiceAll()` → `startPractice(0, true)` → all unresolved mistakes across all sessions. CTA is gated on `summary().pendingMistakes > 0` (NOT on the tab-filtered list).
  - Individual row "practice" button → `startPractice(+mistake.sessionId, false)` — that specific session only
  - `RepracticeService.generateRepracticeSession(sourceSessionId: number, includeAllSessions = false)`
- Notes on Drift:
  - **Drift fixed 2026-06-02 (request drift):** both button handlers were sending `0` as `sourceSessionId`; API rejected `SourceSessionId <= 0`. Interim fix read `sessionId` from the loaded mistake record.
  - **Drift fixed 2026-06-06 (logic drift — REGRESSION introduced by the 2026-06-02 fix):** sending `mistakes[0].sessionId` made "Practice All Mistakes" generate a round from only ONE session's mistakes while the CTA badge advertised the full cross-session pending count. Users practiced a subset and the round "stopped early / skipped pending items." Root cause: the generate path always filtered by a single session even though the SP already supported `0 = all`, and the `NOT NULL` FK on `SourceSessionId` blocked passing `0` directly. Fix: explicit `IncludeAllSessions` flag drives the mistake query with `0` while a derived real session satisfies the FK.

#### GET /api/repractice/{repracticeSessionId}

- Validates ownership against authenticated user; returns session metadata + ordered repractice utterances

#### GET /api/repractice/history

- Returns paginated repractice history

#### PATCH /api/repractice/attempt

- Updates: `AttemptCount`, `BestScore`, `LastScore`, linked mistake `PracticeCount`
- Resolves both repractice utterance and linked mistake after **2 consecutive scores > 80** (SP `uspUpdateRepracticeUtteranceAttempt`: `v_shouldresolve := (p_score > 80 AND previous_lastscore > 80)`). Because each utterance is shown once per pass, resolution requires either a "Try Again → Done" producing two recorded attempts, or a second round via "Practice Again".
- Response DTO: `UpdateAttemptResponseDto { repracticeUtteranceId, isResolved, attemptCount, bestScore, lastScore }` — the service re-reads the utterance post-update so the client reflects authoritative server-side resolution (added 2026-06-06). Previously returned `bool` only.
- Called by `CorrectionRoundComponent.onPracticeAdvanced()` when `skipped = false`. The component adds `repracticeUtteranceId` to a resolved-id `Set` when `isResolved = true` and updates the live "X RESOLVED" counter from `Set.size` (race-free).
- NOT called on skip — skipped utterances do not count as attempts

#### POST /api/repractice/{repracticeSessionId}/complete

- Validates ownership; recalculates improvement percent; marks session `COMPLETED`; re-runs badge evaluation

### Frontend Repractice Practice Flow — Stable Contract (2026-06-04, updated 2026-06-06)

**Entry point:** `MyMistakesComponent` → `startPractice(sessionId)` → `RepracticeService.generateRepracticeSession()` → navigate to `/repractice/:repracticeSessionId`

**Component hierarchy:**
```
CorrectionRoundComponent (orchestrator — /repractice/:id)
  └── RepracticeSpeakerComponent   (per-utterance UI)
        ├── VoiceRecorderComponent (reused — identical to live session)
        └── VoiceFeedbackComponent (reused — identical to live session)
```

**RepracticeSpeakerComponent** — `Frontend/src/app/modules/repractice/repractice-speaker/repractice-speaker.component.ts`

Inputs:
- `utterance: RepracticeUtterance` — what to practice
- `utteranceIndex: number` — progress display and recorder key
- `totalUtterances: number` — progress display

Output: `practiceAdvanced: EventEmitter<{ score: number; skipped: boolean }>`

**Voice engine:** `VoiceRecognitionEngine` — the production engine, identical to live session. Uses Capacitor native speech plugin on Android, Web Speech API on desktop. Full Levenshtein + Soundex + Indian English phonetic scoring. No legacy `VoiceAnalysisService`.

**Phase state machine:** `'recording' → 'feedback'` (same as `SpeakerScreenComponent`)

**Auto Start:** reads `SessionPreferences.defaultVoiceStarter`. Blocked on mobile web (Web Speech API bell issue), allowed on Capacitor native. 700ms delay after `ngAfterViewChecked`.

**Auto Submit:** reads `SessionPreferences.autoSubmitOnStop`. Always shows feedback screen first. 3-second countdown pill, then emits `practiceAdvanced`. Identical to speaker screen — same preference key, same delay, same countdown UI.

**Try Again:** unlimited (no `maxReReads` cap — repractice is solo, no waiting participant). Resets to recording phase, clears `sessionResult`.

**Skip:** emits `practiceAdvanced({ score: 0, skipped: true })`. Does NOT call `updateAttempt`. Utterance stays unresolved.

**Mistake context display:** shows `mistakeDetail` (struck through, red) and `correctionNote` (highlighted, green) from `RepracticeUtterance` when non-empty. Mistake type badge is colour-coded by type.

**CorrectionRoundComponent** handles:
- Session load via `getRepracticeSession(id)`. On load it seeds a `_resolvedIds: Set<number>` and the `resolvedCount` signal from any utterances already `isResolved` (supports resumed/second rounds).
- Progress bar (coloured segment per utterance: past=blue, current=orange, future=dim)
- `onPracticeAdvanced(event)` — when `!skipped`, calls `updateAttempt`; on the response, if `isResolved` and not already counted, adds the id to `_resolvedIds` and sets `resolvedCount = _resolvedIds.size`. Then `advanceToNext()` runs **synchronously** (progression does NOT block on the attempt API — mirrors the Room speaker flow which advances the turn immediately).
- `advanceToNext()` — increments index; shows `utterances[nextIndex]`; when `nextIndex >= length` calls `finishSession()`. Processes EVERY loaded utterance one-by-one until the end.
- `finishSession()` — calls `completeRepracticeSession()`, sets `resolvedCount` from the authoritative backend `resolvedCount`, shows completion dashboard
- `restartRound()` — reloads the session via `loadSession()` (re-seeds resolved state from the backend), resets index/improvement, shows utterance[0] again

**Resolved count logic (2026-06-06):** Resolution is authoritative server-side (2 consecutive scores > 80). The component reflects it from the `UpdateAttemptResponseDto.isResolved` flag into a `_resolvedIds` Set — there is NO local "consecutive pass" guessing. The completion dashboard uses the backend `resolvedCount` from `POST /complete`.

**Skip behaviour:** Advances to next utterance with no API call. Does not affect `_resolvedIds`.

**Logging (2026-06-06):** Full `[Repractice]` console lifecycle logging added across the flow — generate request/response (`RepracticeService`), session load, per-utterance advance, attempt record + resolution, skip, try-again, recording complete, voice error, auto-submit schedule, finish, restart. Backend `RepracticeService` logs generate (scope + mistake count) and each attempt (score, attemptCount, isResolved) via `ILogger<RepracticeService>`. This mirrors the `[Speaker]` logging already present in `SpeakerScreenComponent`.

**Notes on Drift Prevented:**
- `CorrectionRoundComponent` previously used `VoiceAnalysisService` — a legacy service with naive word-intersection scoring, no Levenshtein/Soundex, no Indian English map, no Capacitor native path, no VAD, no waveform. On Android APK it would never return useful results. Replaced with `VoiceRecognitionEngine` (production engine) via `RepracticeSpeakerComponent`.
- `VoiceAnalysisService` is no longer imported or used in the repractice module. It remains only as a service file (not deleted — may be referenced elsewhere) but is not wired up to any active component.
- Feedback UI was a custom pass/fail card. Replaced with `VoiceFeedbackComponent` — same word chips, score bands, and breakdown as the live session speaker screen.
- **Resolved-counter race (fixed 2026-06-06):** the old `_consecutivePass` counter was incremented inside the async `updateAttempt` callback but reset to `0` synchronously by `advanceToNext()` before that callback fired, so it never reached 2 and the live "RESOLVED" counter was permanently stuck at 0. Replaced with the server-authoritative `_resolvedIds` Set keyed on the attempt response.
- **Single-session "Practice All" (fixed 2026-06-06):** see `POST /api/repractice/generate` drift note — the CTA now practices all pending mistakes across all sessions.
- **Drift (fixed 2026-06-06): "Start voice" stuck on "Requesting microphone…" / phantom skip of utterance 0.** Symptom (production web, `/repractice/:id`, desktop Chrome): tapping the mic showed "Requesting microphone…" and never advanced to listening; console showed `[Repractice] skip … utteranceIndex: 0` followed by auto-start + recording on utterance 1. **Root cause — double `startSession()` on the singleton `VoiceRecognitionEngine`.** `defaultVoiceStarter` (default `true`) makes `RepracticeSpeakerComponent.ngAfterViewChecked` auto-call `voiceRecorder.startRecording()` 700 ms after each utterance renders; when the user *also* taps the mic, two `startSession()` runs overlap on the one shared `state$`. `startSession()` sets `state='requesting'` then **awaits** `requestMicPermission()`/`vad.start()` before reaching `'listening'`. The lone guard `if (state !== 'idle') stopSession()` only resets state for the later caller; the earlier call's promise had already passed that line and continued after its await with **no supersession check**, setting `'listening'` and starting a `SpeechRecognition` that was immediately orphaned (`this.recognition` overwritten). The orphan's `onend`/`onerror` acted on the foreign instance → double bell, "already started" throw, and — when the race landed during the permission await — `state$` wedged on `'requesting'`. The mic button is `[disabled]` while `'requesting'`, so the user could not recover and tapped SKIP (the index-0 skip log); skip→advance re-armed auto-start, which then ran cleanly on utterance 1 (the `score:18` log). Same defect class as live-speaker Drift 5, never guarded on the repractice/recorder path. **Fix (two layers, root cause):** (a) `VoiceRecognitionEngine` now carries a monotonic `_sessionGen` token — bumped on every `startSession()` and `stopSession()`; `startSession()` captures its generation and re-checks after each `await` (permission, `vad.start`, capture `getUserMedia`, pre-recognizer), throwing `VoiceRecognitionEngine.SUPERSEDED` to abort a stale continuation **without touching `state$`** (the superseding session owns it). (b) `VoiceRecorderComponent.startRecording()` early-returns if state is `requesting`/`listening`/`processing` (kills the duplicate at the source — auto-start vs. manual tap) and swallows the `SUPERSEDED` sentinel so no spurious `recordingComplete`/error is emitted. Drift type: frontend concurrency / shared-singleton state drift. Files: `core/services/voice/voice-recognition.engine.ts`, `modules/voice/voice-recorder/voice-recorder.component.ts`.
  - **Drift (fixed 2026-06-06, follow-up — the actual confirmed root cause): `Speech recognition error: aborted` rejects the whole session.** Live `[VDIAG]` tracing on desktop Chrome proved the concurrency guard above was working (single `startSession`, `gen:1` — no overlap), so the double-start theory was NOT the cause here. The real sequence: mic opens (`onstart`/`onaudiostart`), the user pauses to read the sentence, the recognizer fires `onend` with no finals yet, and the engine's no-finals restart called **`this.recognition.start()` on the same already-ended instance** → Chrome emits a `'aborted'` SpeechRecognition error → it fell through to the terminal `else` in `onerror` and **`reject(new Error('Speech recognition error: aborted'))`**, flipping state to `error`. The orphaned instance left alive by restarts could also fire `onend`/`onerror` into the live session's promise. **Fix (three parts in `startRecognition`):** (1) before constructing a new `SpeechRecognition`, detach the previous instance's handlers (`onresult/onerror/onend/onstart/onaudiostart = null`) and `abort()` it, so no orphan can fire into the current promise; (2) handle `event.error === 'aborted'` as **benign** — if `_intentionalStop` return, if finals exist `finalize()`, else stay listening (let `onend` relisten) — never reject; (3) the desktop no-finals `onend` restart now goes through `startRecognition()` (fresh instance) instead of re-`start()`-ing the dead instance. Drift type: browser SpeechRecognition lifecycle drift (restart-on-ended-instance). Same files as above.
  - **Drift (fixed 2026-06-06, Edge — phantom "score 18" on empty transcript).** Verified via a Playwright Edge probe against `/repractice/53`: on **desktop Microsoft Edge** the Web Speech surface opens (`onstart`/`onaudiostart`/`onspeechstart` all fire) but **`onresult` never fires — zero transcript**. The web `finalize()` then scored an EMPTY string and resolved `overallScore ≈ 18` (the scorer baseline for empty input: `fluency·0.75 + apiConfidence·100·0.25 ≈ 0 + 0.7·100·0.25 = 17.5`), then `recordingComplete` advanced the user with a bogus result — the recurring "nothing recognized but score 18" symptom. The native path already rejected empty input ("No speech detected") but the **web path did not**. **Fix:** `finalize()` now takes an optional `reject` (threaded through every call site) and, when `wordCount === 0`, stops/cleans up and **rejects** with "No speech detected. Please tap the mic and speak clearly." — appending an Edge-specific hint (`engine === 'edge'`) to use Chrome or the app. No more phantom score; the user gets an actionable retry. Note: desktop Edge is NOT hard-blocked up front (some installs do transcribe); the hint is surfaced only after an actual empty result. Recommended path for reliable web voice remains Chrome or the installed app. Drift type: browser speech-backend drift (Edge no-onresult). File: `core/services/voice/voice-recognition.engine.ts`.

### Application and Infrastructure Wiring

- `IMistakeService → MistakeService`, `IRepracticeService → RepracticeService`
- `IMistakeRepository → MistakeRepository`, `IRepracticeRepository → RepracticeRepository`
- Controllers: `MistakeController`, `RepracticeController`
- Cross-module: `LiveSessionService.CompleteSessionAsync()` calls `IMistakeService.SaveMistakesFromSessionAsync(sessionId, memberId)` for each active member

### Mistake Extraction Rules

Applied to `tblVoiceAnalysis` rows for the target user and session:
- `HESITATION`: hesitation-word count > 2
- `GRAMMAR`: one mistake per grammar-error item in `GrammarErrorsJson`
- `SPEED`: `SpeakingSpeedWpm < 60`
- `INCOMPLETE`: transcribed-word count < 50% of expected-word count
- `PRONUNCIATION`: one mistake per pronunciation issue in `PronunciationJson`
- Fallback `PRONUNCIATION`: if no other mistake category captured
- Duplicate prevention: checks user + session + utterance + mistakeType + mistakeDetail before insert

### Repractice Generation Order

`GRAMMAR`, `PRONUNCIATION`, `HESITATION`, `SPEED`, `SKIP`, `INCOMPLETE`

- Hint text sourced from mistake `GrammarTag`
- Correction note sourced from `CorrectionText`; falls back to `MistakeDetail`

### Migration State

- `InitialCreate_Phase1`, `AddAdminModule_Phase2`, `AddScriptModule_Phase3`, `AddSessionModule_Phase4`, `AddLiveSessionModule_Phase5`, `AddUserModule_Phase7`, `AddMistakeModule_Phase6`
- `AddMistakeModule_Phase6` creates only `tblMistake`, `tblRepracticeSession`, `tblRepracticeUtterance`
- `tblUserBadge` and `tblUserStreak` are in `AddUserModule_Phase7`

**Phase 2 PostgreSQL Migrations (applied to Supabase 2026-06-01):**
- `17_add_vocabulary_module.sql` → `tbluservocabulary` (lowercase identifiers, `INSERT ON CONFLICT` upsert)
- `18_add_learning_goal_module.sql` → `tblusergoal` (lowercase identifiers, boolean columns, `NOW()`)
- Note: PostgreSQL uses lowercase unquoted table/column names. All raw SQL in `VocabularyRepository` and Goal methods in `UserRepository` are provider-aware (SQL Server branch uses `dbo.` prefix + `GETDATE()` + `MERGE`; PostgreSQL branch uses `public.` prefix + `NOW()` + `INSERT ON CONFLICT`).

---

## Phase 2 Feature Contracts (2026-06-01)

### Phase 2 Step 1 — Interview Performance Dashboard

**Purpose:** Dedicated MockInterview performance analytics view for users, showing Interview Readiness Score, session timeline, grammar weaknesses, and professional vocabulary performance.

**Entry Points:** `/user/interview-performance` (Angular route). "Interview Performance" quick-link card on Improvement Tracker page.

**API Endpoint:** `GET /api/dashboard/interview-performance`
- Auth: UserOrAdmin + ActiveUser
- Returns: `InterviewPerformanceDashboardResponseDto`
  - `HasData`: bool — false if no completed MockInterview sessions exist
  - `InterviewReadinessScore` (0–100): composite from last 5 sessions (40% fluency, 25% confidence, 20% error rate, 15% answer speed)
  - `ReadinessTrend`: Improving / Stable / Declining
  - `TotalMockSessions`: int
  - `SessionTimeline`: last 10 sessions with per-session readiness score, fluency, confidence, mistake count
  - `TopGrammarErrors`: top 3 grammar error tags from Candidate turns
  - `FocusWordPerformance`: professional vocabulary correct/stumbled rates
  - `AnswerLengthTrend`: Improving / Stable / Declining (based on WPM)
  - `AvgAnswerSpeedWpm`: average Candidate answer speed
  - `RecommendedScriptId/Title/Reason`: next script targeting weakest grammar tag

**Data source:** `tblVoiceAnalysis` + `tblUtterance` (SpeakerLabel = "Candidate") + `tblMistake` + `tblSession` (Category = "Mock Interview" or "Interview").

**Implementation:**
- `IUserRepository.GetInterviewPerformanceDashboardAsync(userId)` — EF LINQ
- `IUserDashboardService.GetInterviewPerformanceDashboardAsync(userId)`
- `UserDashboardController.GetInterviewPerformanceDashboardAsync` — `GET /api/dashboard/interview-performance`
- Frontend: `InterviewPerformanceComponent` at `/user/interview-performance`
- Quick-link card added to `ImprovementTrackerComponent`

---

### Phase 2 Step 2 — Pronunciation Improvement Timeline

**Purpose:** Shows per-word pronunciation problem history across all sessions — problem word list, per-session dot timeline, IPA references, and "practice this" script links.

**Entry Points:** `/user/pronunciation-timeline`. Link card on Improvement Tracker.

**API Endpoint:** `GET /api/dashboard/pronunciation-timeline`
- Auth: UserOrAdmin + ActiveUser
- Returns: `PronunciationTimelineResponseDto`
  - `HasData`: bool
  - `ProblemWords`: top 20 words from `tblVoiceAnalysis.PronunciationJson`, ordered by frequency
  - `TopPersistentWords`: words with issues in 3+ of last 10 sessions (top 5) — each with `PracticeScriptId/Title` linking to a script where the word is a FocusWord

**Implementation:**
- `IUserRepository.GetPronunciationTimelineAsync(userId)` — parses `PronunciationJson` from last 300 turns (in memory), builds word → session map, joins `tblUtterance.PronunciationNote` for IPA
- `UserDashboardController.GetPronunciationTimelineAsync` — `GET /api/dashboard/pronunciation-timeline`
- Frontend: `PronunciationTimelineComponent` at `/user/pronunciation-timeline`

---

### Phase 2 Step 3 — Session Preparation Mode

**Purpose:** Full-page read-only script preview allowing users to read through all utterances before joining a session.

**Entry Points:** `/scripts/prepare/:scriptId` — "Prepare" icon button (BookMarked icon) added to each script card in the Script Library.

**Data:** Uses existing `GET /api/scripts/{scriptId}` endpoint — no new backend work.

**Implementation:**
- `ScriptService.getScriptDetail(scriptId)` added to Angular `ScriptService`
- `ScriptPrepareComponent` at `/scripts/prepare/:scriptId` — shows metadata, full utterance list with HintText and GrammarTag/FocusWord; facilitator turns (Interviewer/Tutor/Coach) displayed as shaded
- Route added to `SCRIPTS_ROUTES`
- "Prepare" `BookMarked` icon button added to script library card actions

---

### Phase 2 Step 4 — Learning Goals and Progress Tracking

**Purpose:** Users set a practice goal (interview/grammar/vocabulary/fluency), a timeline (2/4/8 weeks), and see progress tracking on their dashboard.

**DB Table:** `tblUserGoal`
- Key columns: `UserId`, `GoalType NVARCHAR(64)`, `TimelineWeeks INT`, `StartDate`, `TargetDate`, `DetectedLevel NVARCHAR(32)`, `StartingScore DECIMAL(5,2)`, `IsActive BIT`
- Migration: `20260601000002_AddLearningGoal_Phase9.cs` (raw SQL — no EF entity mapping)
- PostgreSQL equivalent: `Docs/PostgreSQLMigration/18_add_learning_goal_module.sql`

**API Endpoints:**
- `POST /api/users/goal` — set/replace active goal; auto-detects level from last 5 sessions
- `GET /api/users/goal` — returns `GoalProgressResponseDto` with sessions completed, score movement, trend, estimated weeks remaining, recommended plan

**Goal level detection:** avg FluencyScore last 5 sessions — < 55 = Beginner, 55–74 = Intermediate, 75+ = Advanced.

**Implementation:**
- `IUserRepository.SetUserGoalAsync` + `GetGoalProgressAsync` — raw ADO.NET (no EF entity)
- `IUserService.SetGoalAsync` + `GetGoalProgressAsync`
- `UserController` — `POST/GET /api/users/goal`
- Frontend: `LearningGoalsComponent` at `/user/goals`
- Dashboard: goal progress panel added (progress bar, sessions/target, trend, weeks remaining). "Set a Learning Goal →" link shown when no active goal.

**Current score calculation inside `GetGoalProgressAsync`:**
- `interview`: avg `OverallScore` from VoiceAnalyses joined with Sessions where `SessionMode IN ('Mock Interview', 'Interview')` since goal start date. Returns 0 if no sessions.
- `vocabulary`: `startingScore + sessionsSinceGoal × 3` (proxy).
- `grammar`/`fluency`: avg `FluencyScore` from COMPLETED sessions since goal start date. Returns `startingScore` if no sessions.

**Notes on Drift Prevented (2026-06-02):**
- Drift type: **EF LINQ untranslatable expression** — `GetGoalProgressAsync` used `.DefaultIfEmpty(value).AverageAsync()` on two EF Core queries. EF Core cannot translate `DefaultIfEmpty(localVariable)` to SQL (error: "LINQ expression could not be translated"). The pattern is not valid for server-side evaluation. Fixed by changing `select (decimal)` to `select (decimal?)` and replacing `.DefaultIfEmpty(x).AverageAsync()` with `.AverageAsync() ?? x`. This translates to SQL `AVG(column)` which returns `NULL` for empty sets; the `??` applies the default in C# after the query executes. File: `UserRepository.cs` → `GetGoalProgressAsync`.

---

### Phase 2 Step 5 — Cross-Session Grammar Error Trend Analysis

**Purpose:** Adds 4-week rolling trend (Improving / Stable / Regressing) to each grammar tag in the grammar progress view.

**API Endpoint:** `GET /api/mistakes/grammar-trends`
- Auth: UserOrAdmin + ActiveUser
- Returns: `List<GrammarProgressResponseDto>` — same shape as grammar-progress but with added fields:
  - `TrendLabel`: Improving / Stable / Regressing / null (insufficient data)
  - `CurrentPeriodAvg`: avg errors/session in the current 4-week window
  - `PreviousPeriodAvg`: avg errors/session in the previous 4-week window
  - `TrendDelta`: current minus previous (positive = more errors = regressing)

**Trend logic:** Improving if current < previous × 0.90; Regressing if current > previous × 1.10; Stable otherwise.

**Implementation:**
- `IMistakeRepository.GetGrammarProgressWithTrendAsync(userId)` — calls existing SP + EF LINQ trend enrichment
- `IMistakeService.GetGrammarProgressWithTrendAsync`
- `MistakeController.GetGrammarProgressWithTrendAsync` — `GET /api/mistakes/grammar-trends`
- Frontend: `ImprovementTrackerComponent` loads trend data from `grammar-trends` endpoint; grammar cards show trend badge (colour-coded); Regressing items get amber border

---

### Phase 2 Step 6 — Filler Phrase Detection

**Purpose:** Extends hesitation detection from single words (um, uh, er) to full multi-word filler phrases (you know, I mean, basically, kind of, sort of, you see, to be honest, at the end of the day). "like" and "actually" flagged only when used 3+ times in a single turn.

**No backend changes.** Detection is entirely frontend-side, in the voice analysis engine.

**Files changed:**
- `Frontend/src/app/core/services/voice/transcript-normalizer.ts` — new `detectFillerPhrases(rawText: string): string[]` method with single-word + multi-word + frequency-gated detection
- `Frontend/src/app/core/services/voice/voice-recognition.engine.ts` — `detectHesitations(transcript)` now delegates entirely to `TranscriptNormalizer.detectFillerPhrases()`; old `hesitationPatterns` array removed

**Confidence penalty:** Unchanged — each detected filler/phrase counts as one hesitation unit, max 25 point total penalty.

**Post-Session Review display:** Filler phrases appear inline in transcript alongside single-word hesitations (same display path — stored in `HesitationWords` CSV field in `tblVoiceAnalysis`).

---

### Phase 2 Step 7 — Admin Script Quality Analytics

**Purpose:** Admin view showing per-script performance metrics: session count, completion rate, avg fluency, avg mistakes, avg duration, re-read rate, repractice conversion rate, last used date. Scripts unused 60+ days flagged as inactive.

**Entry Points:** `/admin/script-analytics` — "Analytics" button added to admin scripts page header.

**API Endpoint:** `GET /api/scripts/analytics?category={optional}`
- Auth: AdminOnly
- Returns: `List<ScriptAnalyticsItemDto>` — per-script metrics

**Data joins:** `tblScript` → `tblSession` → `tblVoiceAnalysis`, `tblMistake`, `tblTurnState`, `tblRepracticeSession`

**Implementation:**
- `IScriptRepository.GetScriptAnalyticsAsync(categoryFilter)` — EF LINQ aggregation
- `IScriptService.GetScriptAnalyticsAsync(categoryFilter)`
- `ScriptController.GetScriptAnalyticsAsync` — `GET /api/scripts/analytics`
- Frontend: `AdminScriptAnalyticsComponent` at `/admin/script-analytics` — summary KPI cards (Total Scripts / Avg Completion / Inactive / Avg Fluency) rendered **above** the table, then a sortable table with a category filter. Loads the full set once via `getScriptAnalytics()` (no category arg); **category filter + sort run client-side off signals** (`selectedCategory`, `sortField` are `signal()`s; `filteredByCategory` + `filtered` computeds). Summary KPIs reflect the active category filter.
  - Notes on Drift (2026-06-08): Two UI bugs fixed. (1) **Sort dropdown was dead** — `selectedCategory`/`sortField` were plain fields while `filtered()` was a `computed()`, so changing them never re-ran the computed (the category dropdown only "worked" via an incidental `loadData()` server reload; `sortData()` was an empty no-op). Fix: made both filter states `signal()`s so the computeds react. (2) **Summary cards rendered after the table** (appeared in the footer) — moved above the table. Bindings switched from `[(ngModel)]` to `[ngModel]="sig()" (ngModelChange)="sig.set($event)"` for signal compatibility.

---

### Phase 2 Step 8 — Script Versioning Rollback and Duplication

**Purpose:** Admins can roll back a script to a previous version, and duplicate an existing script as a draft starting point.

**API Endpoints:**
- `POST /api/scripts/{scriptId}/rollback?version={versionNumber}` — rolls back to a prior version, increments current version number with a "Rolled back to version N" note
- `POST /api/scripts/{scriptId}/duplicate` — creates a new inactive copy of the script with all utterances; returns the new `ScriptId`

**Implementation:**
- `IScriptRepository.RollbackScriptVersionAsync` + `DuplicateScriptAsync` — EF Core
- `IScriptService.RollbackScriptVersionAsync` + `DuplicateScriptAsync`
- `ScriptController` — new endpoints
- `ScriptService.getVersionHistory()` + `ScriptService.rollbackScriptVersion()` + `ScriptService.duplicateScript()` added to Angular service
- Admin Scripts: previously used a slide-out side panel for script details; replaced 2026-06-03 with dedicated detail page `/admin/scripts/:id` — see Admin Frontend Architecture note below.

---

### Admin Frontend Architecture — List → Detail Page Pattern (2026-06-03)

**Pattern:** All admin list pages use a lean table for search/filter/select only. Record details navigate to a dedicated detail page. The Reports module (`/admin/reports` → `/admin/reports/user/:id`) is the reference implementation.

**Applied to:**

| Module | List Route | Detail Route | Component |
|---|---|---|---|
| Reports | `/admin/reports` | `/admin/reports/user/:id` | `UserDetailReportComponent` |
| Scripts | `/admin/scripts` | `/admin/scripts/:id` | `AdminScriptDetailComponent` |
| Users | `/admin/users` | `/admin/users/:id` | `AdminUserDetailComponent` |
| Sessions | `/admin/sessions` | `/admin/sessions/:id` | `AdminSessionDetailComponent` |

**Before (removed):** Scripts, Users, and Sessions used `fixed inset-0 z-50` slide-out side panels (overlays) to show record details within the same route. These broke mobile usability due to full-screen takeover with no scrollable layout.

**After:** Clicking the View/Eye button navigates to the detail route. Detail pages have a back button (`routerLink` to the list). List pages are now lean: only the minimal columns needed for identification and quick actions remain visible.

**Sessions detail navigation (updated 2026-06-06):** The sessions list still passes the session object via Angular router state (`router.navigate([...], { state: { session } })`) — the fast path, no fetch. The detail component reads `history.state.session` when present. On direct navigation / refresh (no router state) it now calls `adminService.getSession(id)` → `GET /api/admin/sessions/{sessionId}` and shows a header + body skeleton (`sessionLoading` signal) while fetching; the "Session not found" state only renders when the fetch completes with no session (`!session() && !sessionLoading()`). Notes on Drift: previously NO single-session endpoint existed, so a refresh always showed "Session not found" — documented as "acceptable" but actually a dead-end. Fixed by adding the endpoint (see Admin endpoints list above) and the fallback fetch.

**Edit modal (Users):** The Add/Edit User modal was kept on the list page (`/admin/users`) since it is a lightweight inline form already embedded in `AdminUsersComponent`. The detail page (`/admin/users/:id`) is view-only with Activate/Deactivate and View Full Report actions.

---

### Phase 2 Step 9 — Claude Prompt Helper with DB-Connected Data (Script Upload UI)

**Purpose:** When an admin downloads a category-specific Excel template, a pre-built, copy-ready Claude prompt appears — enriched with live data from the database (actual grammar tags and context tags already in use for that category). The admin copies the prompt, pastes into claude.ai, and uploads the resulting JSON via the existing wizard.

**Backend:**
- `GET /api/scripts/prompt-data?category=` — new endpoint returning `ScriptPromptDataResponseDto`
  - `grammarTagsInUse`: distinct GrammarFocusTag values from `tblScript` for this category
  - `contextTagsInUse`: distinct ContextTag values from `tblScript` for this category
  - `approvedGrammarTags`: static approved list from ExcelTemplateStandard §9.3
  - `speakerLabels`, `minRows`, `maxRows`, `mandatoryColumns`: static category rules
  - `activeScriptCount`: total active scripts in this category
- `GET /api/scripts/sample-template?category=` — now pulls first 4 utterances from most recent active script in that category (DB-sourced samples). Falls back to hardcoded samples if category has no scripts.

**DB migrations applied 2026-06-01:**
- `tbluservocabulary` (migration 17, fixed to lowercase identifiers) — applied to Supabase
- `tblusergoal` (migration 18, fixed to lowercase identifiers) — applied to Supabase
- Both `VocabularyRepository` and `UserRepository` Goal methods updated to be provider-aware (PostgreSQL lowercase table/column names, `NOW()` vs `GETDATE()`, boolean vs int, `INSERT ON CONFLICT` vs `MERGE`, `LIMIT 1` vs `TOP 1`)

**Frontend:**
- `ScriptService.getPromptData(category)` — calls new endpoint
- Upload wizard: clicking a category button → fetches prompt data from DB → shows "Loading..." state → populates prompt panel with DB-enriched content
- `buildClaudePrompt(category)` uses DB data (grammar tags in use, context tags in use, active script count) when available; falls back to static defaults
- Prompt includes a distinct-content reminder when active scripts already exist in the category

**Workflow:**
1. Admin clicks category button → template downloads + DB prompt data fetched
2. Prompt panel shows: category rules, actual tags in use in the platform, approved tag list
3. "Copy Prompt" → paste into claude.ai → Claude returns JSON consistent with existing content
4. Upload JSON-based Excel via existing wizard

**Files changed:**
- `ExcelExportService.cs` — sample rows now from DB (first 4 utterances of latest active script)
- `ScriptRepository.cs` — `GetPromptDataForCategoryAsync()` added
- `ScriptService.cs` — `GetPromptDataForCategoryAsync()` added
- `ScriptController.cs` — `GET /api/scripts/prompt-data` added
- `VocabularyRepository.cs` — full provider-aware rewrite (SQL Server + PostgreSQL)
- `UserRepository.cs` Goal methods — provider-aware SQL (deactivate + insert + select)
- `script-upload.component.ts` — prompt data fetching + DB-enriched prompt builder
- `ExcelTemplateStandard.md §10` — updated to document DB-connected workflow

---

## Phase 3 Feature Contracts (2026-06-01)

### Phase 3 Step 1 — Spaced Repetition for Grammar Mistakes

**Purpose:** Extend RepracticeRound to schedule follow-up reviews after a mistake is resolved, using increasing intervals. Prevents regression by re-surfacing resolved mistakes before they are forgotten.

**Entry Points:** "Grammar reviews due" banner on user dashboard at `/user/dashboard`. Links to `/user/my-mistakes`.

**API Endpoint:** `GET /api/mistakes/due-for-review`
- Auth: UserOrAdmin + ActiveUser
- Returns: `SpacedRepetitionDueResponseDto`
  - `DueCount` (int): number of reviews currently due
  - `Items` (array): list of `SpacedRepetitionDueItemDto`

**SpacedRepetitionDueItemDto fields:**
- `MistakeId` (long)
- `MistakeType` (string)
- `GrammarTag` (string?)
- `UtteranceText` (string)
- `CorrectionText` (string?)
- `ReviewStage` (byte: 1–4)
- `NextReviewDate` (DateTime)
- `SessionName` (string)
- `ScriptTitle` (string)

**Database Schema Changes:**
`tblMistake` gets 3 new columns:
- `ReviewStage TINYINT NOT NULL DEFAULT(0)` — 0=not scheduled, 1–4=active review stages, 5=long-term retained (no more reviews needed)
- `NextReviewDate DATETIME2 NULL` — next review due date; NULL when stage 0 or stage 5
- `ReviewIntervalDays INT NOT NULL DEFAULT(0)` — current interval in days

**Review Interval Schedule:**
| Stage | After | Days until next |
|-------|-------|----------------|
| 0 → 1 | RepracticeRound completed | +1 day |
| 1 → 2 | Review session passed | +3 days |
| 2 → 3 | Review session passed | +7 days |
| 3 → 4 | Review session passed | +14 days |
| 4 → 5 | Review session passed | +30 days (retained) |

**Stored Procedures:**
- `uspScheduleMistakeReview(@MistakeId, @UpdatedBy, @IPAddress)` — sets stage 1, interval 1, NextReviewDate = +1 day. Only runs if IsResolved=1 and ReviewStage=0.
- `uspAdvanceMistakeReview(@MistakeId, @UpdatedBy, @IPAddress)` — advances stage and sets next interval date.
- `uspResetMistakeReview(@UserId, @GrammarTag, @UpdatedBy, @IPAddress)` — resets all resolved stage 1–4 mistakes with matching GrammarTag back to stage 1 (+1 day). Called when same grammar mistake recurs.
- `uspGetMistakesDueForReview(@UserId)` — returns all IsResolved=1, ReviewStage 1–4, NextReviewDate <= NOW() mistakes with session/script name joins.

**Business Rules:**
- On `POST /api/repractice/{id}/complete`: after marking COMPLETED, `ScheduleMistakeReviewAsync` is called for all resolved RepracticeUtterances' MistakeIds in the session.
- Dashboard "Reviews Due Today" banner only shown when DueCount > 0.
- Review interval resets to stage 1 (+1 day) when the user makes the same GrammarTag mistake again in a new session (via `uspResetMistakeReview`).

**Migration:** `20260601000004_AddSpacedRepetition_Phase11.cs` (SQL Server) + `20_add_spaced_repetition.sql` (PostgreSQL)
**Drift fix:** `25_fix_challenge_columns_and_sp_drift.sql` — applied 2026-06-02

**Files changed:**
- `Mistake.cs` — added `ReviewStage`, `NextReviewDate`, `ReviewIntervalDays` properties
- `MistakeConfiguration.cs` — EF config for new columns
- `IMistakeRepository.cs` — added 4 new methods
- `MistakeRepository.cs` — implemented 4 new methods + `GetByte` helper
- `IMistakeService.cs` — added `GetDueForReviewAsync`
- `MistakeService.cs` — implemented `GetDueForReviewAsync`
- `IRepracticeRepository.cs` — added `GetResolvedMistakeIdsBySessionAsync`
- `RepracticeRepository.cs` — implemented `GetResolvedMistakeIdsBySessionAsync`
- `RepracticeService.cs` — calls `ScheduleMistakeReviewAsync` after session completion
- `SpacedRepetitionDueResponseDto.cs` — new DTO
- `ApiRoutes.cs` — added `DueForReview = "due-for-review"`
- `MistakeController.cs` — added `GET /api/mistakes/due-for-review` endpoint
- `user-dashboard.component.ts` — "Reviews Due Today" banner, loads via `MistakeService.getDueForReview()`
- `mistake.service.ts` — added `getDueForReview()`

**Notes on Drift Prevented (2026-06-02):**
- Drift type: **SP return-type drift** — `uspgetmistakesdueforreview` was created in migration 20 with `firstoccurrence TIMESTAMPTZ` and `lastattempt TIMESTAMPTZ` in its `RETURNS TABLE`, but the actual `tblmistake` columns are `TIMESTAMP` (without timezone) — the project-wide convention established in migration 15. PostgreSQL error 42804 (`structure of query does not match function result type`) was thrown on every `GET /api/mistakes/due-for-review` call. Fixed by DROP + CREATE FUNCTION with `TIMESTAMP` for both columns (migration 25). Source migration 20 updated to use correct types going forward. Note: `nextreviewdate` was added in migration 20 as `TIMESTAMPTZ` and remains `TIMESTAMPTZ` in both the column and the function return type — this is intentional since it was created after migration 15's type-correction sweep.

---

### Phase 3 Step 2 — Cohort Management (B2B)

**Purpose:** Admin-level feature to group users into cohorts (training batches) and view collective analytics. Enables B2B training use cases with batch reporting and group performance tracking.

**Entry Points:**
- `/admin/cohorts` — `AdminCohortsComponent` (list/create). Nav item "Cohorts" in the shared admin bottom nav.
- `/admin/cohorts/:id` — `AdminCohortDetailComponent` (detail). Opened by tapping a cohort card; route lazy-loads the component (`admin.routes.ts`).

**New DB Table:** `tblCohort`
- `CohortId BIGINT IDENTITY(1,1) PK`
- `CohortName NVARCHAR(128) NOT NULL`
- `Description NVARCHAR(256) NULL`
- `IsActive BIT NOT NULL DEFAULT(1)`
- Standard audit columns

**DB Change on tblUser:** `CohortId BIGINT NULL FK → tblCohort(CohortId) ON DELETE SET NULL`

**API Endpoints (all ADMIN only):**
- `GET /api/admin/cohorts` — list all cohorts with member counts
- `POST /api/admin/cohorts` — create a new cohort (`{ cohortName, description }`)
- `PATCH /api/admin/cohorts/assign` — assign or unassign a user (`{ userId, cohortId }`)
- `GET /api/admin/cohorts/{cohortId}/members` — list cohort members with session stats
- `GET /api/admin/cohorts/{cohortId}/analytics` — cohort-level analytics (avg fluency, inactive count, top grammar mistakes, most improved member)

**Stored Procedures:** `uspInsertCohort`, `uspGetAllCohorts`, `uspAssignUserToCohort`, `uspGetCohortMembers`. Analytics uses EF queries (provider-safe).

**Migration:** `20260601000005_AddCohortManagement_Phase12.cs` (SQL Server) + `21_add_cohort_management.sql` (PostgreSQL)

**Files changed:**
- `Cohort.cs` — new domain entity
- `User.cs` — added `CohortId?`, `Cohort?` navigation
- `CohortConfiguration.cs` — new EF config
- `UserConfiguration.cs` — added CohortId column + FK
- `GoWithFlowDbContext.cs` — added `DbSet<Cohort> Cohorts`
- `IAdminRepository.cs` — 5 new cohort methods
- `AdminRepository.cs` — implemented 5 new cohort methods
- `IAdminService.cs` — 5 new cohort methods
- `AdminService.cs` — implemented 5 new cohort methods
- `CohortRequestDto.cs`, `CohortResponseDto.cs` — new DTOs
- `ApiRoutes.cs` — 5 new cohort routes
- `AdminController.cs` — 5 new cohort endpoints
- `admin.service.ts` — 5 new cohort API methods
- `admin-cohorts.component.ts` — new admin cohorts management page
- `admin-cohort-detail.component.ts` — cohort detail screen (2026-06-05): header + status, stat cards (members / avg fluency / inactive / most improved), top-grammar-mistake chips, and a responsive member-card roster (sessions / fluency / mistakes per member). Loads `getCohortAnalytics(id)` + `getCohortMembers(id)` via `forkJoin`; `analytics === null` → not-found state. Member cards (not a wide table) keep it mobile-safe. Cohort `isActive` is inferred from members (analytics DTO does not return it). DTOs: `CohortAnalyticsResponseDto` (cohortName, description, memberCount, avgFluencyScore, inactiveCount, topGrammarMistakes[{grammarTag, mistakeCount}], mostImproved{userId, fullName, improvementDelta}), `CohortMemberDto` (userId, fullName, ageGroup, avatarUrl, isActive, sessionCount, avgFluencyScore, totalMistakes, …).
- `admin.routes.ts` — added `/admin/cohorts` and `/admin/cohorts/:id` routes
- `admin-layout.component.html` — Cohorts tab now via shared `<app-bottom-nav>` (see Shared Footer Navigation Architecture)

**Notes on Known Drift Prevented:** the `/admin/cohorts/:id` route lazy-loaded `AdminCohortDetailComponent` from an **empty** file (TS2306 "not a module"); the screen was routed but never built. Now implemented against the existing analytics/members endpoints — no new API needed.

---

### Phase 3 Step 3 — Speaking Challenge Mode

**Purpose:** Weekly competitive practice mode where admin designates one script as the week's challenge. Users compete for the highest fluency score. Top 20% earn a "Weekly Champion" badge.

**Entry Points:**
- Dashboard "Weekly Challenge" banner (shown when active challenge exists)
- Admin: "Set Challenge" button on each script in the admin scripts detail panel

**New DB Tables:**
- `tblWeeklyChallenge`: `ChallengeId PK`, `ScriptId FK`, `WeekStartDate`, `WeekEndDate`, `IsActive BIT` + full `BaseAuditEntity` columns (`Tag`, `Comments`, `SortOrder`, `IPAddress`, `CreatedBy`, `DateCreated`, `UpdatedBy`, `LastUpdated`, `DeletedBy`, `DateDeleted`, `IsDeleted`)
- `tblChallengeAttempt`: `AttemptId PK`, `ChallengeId FK`, `UserId FK`, `FluencyScore DECIMAL(5,2)`, `AttemptDate` + full `BaseAuditEntity` columns

**API Endpoints:**
- `GET /api/challenge/active` — returns active challenge + user's best score + top 10 leaderboard (auth: UserOrAdmin)
- `POST /api/challenge/attempt` — submit a challenge attempt score (auth: UserOrAdmin)
- `POST /api/challenge/set-weekly` — admin sets current week's challenge script (auth: AdminOnly)

**Business Rules:**
- Only one active challenge at a time — `uspSetWeeklyChallenge` deactivates previous before creating new
- Leaderboard shows top 10 by best score per user for the current week's challenge
- `uspAwardChallengeBadge` awards `WEEKLY_CHAMPION` badge to top 20% of participants
- Challenge week: Monday 00:00 → Sunday 23:59:59

**Migration:** `20260601000006_AddSpeakingChallenge_Phase13.cs` (SQL Server) + `22_add_speaking_challenge.sql` (PostgreSQL)
**Drift fix:** `25_fix_challenge_columns_and_sp_drift.sql` — applied 2026-06-02

**Files changed:**
- `WeeklyChallenge.cs`, `ChallengeAttempt.cs` — new domain entities
- `WeeklyChallengeConfiguration.cs`, `ChallengeAttemptConfiguration.cs` — EF configurations
- `GoWithFlowDbContext.cs` — `WeeklyChallenges`, `ChallengeAttempts` DbSets
- `IChallengeRepository.cs`, `ChallengeRepository.cs` — challenge repository (SP + EF provider-aware)
- `IChallengeService.cs`, `ChallengeService.cs` — challenge service
- `ChallengeRequestDto.cs`, `ChallengeResponseDto.cs` — DTOs
- `ApiRoutes.cs` — Challenge routes
- `ChallengeController.cs` — 3 endpoints
- `Program.cs` — service registrations
- `challenge.service.ts` — frontend challenge API service
- `user-dashboard.component.ts` — Weekly Challenge banner
- `admin-scripts.component.ts` — "Set Challenge" button

**Notes on Drift Prevented (2026-06-02):**
- Drift type: **DB contract drift** — `tag` and `comments` columns (from `BaseAuditEntity`) were omitted from the original `22_add_speaking_challenge.sql` CREATE TABLE for both `tblweeklychallenge` and `tblchallengeattempt`. EF Core includes all `BaseAuditEntity` properties in generated SELECT queries, causing PostgreSQL error 42703 (`column t.comments does not exist`) on every `GET /api/challenge/active` call. Fixed by `ALTER TABLE ... ADD COLUMN IF NOT EXISTS tag/comments` on both tables (migration 25). Source migration 22 updated to include these columns going forward.

---

### Phase 3 Step 4 — Completion Milestones and Certificates

**Purpose:** Six category-specific achievement certificates evaluated after every session completion. Displayed on user profile with downloadable PNG.

**Certificate BadgeCodes (extend tblUserBadge — same table/API):**
| BadgeCode | Criteria |
|-----------|----------|
| `GRAMMAR_FOUNDATION` | 10 GrammarDrill sessions across 5+ distinct GrammarFocusTags |
| `INTERVIEW_READY` | avg Candidate FluencyScore ≥ 75 across 5+ completed MockInterview sessions |
| `VOCABULARY_BUILDER` | 100 FocusWords in vocabulary bank (tblUserVocabulary, correctcount > 0) |
| `FLUENCY_MILESTONE` | 8 FluencyDrill sessions with avg SpeakingSpeedWpm between 80–120 |
| `GRAMMAR_CORRECTOR` | 10 distinct GrammarTag mistake types fully resolved |
| `SCENARIO_MASTER` | 10 Roleplay sessions across 5+ distinct ContextTags with avg FluencyScore ≥ 70 |

**Backend: `uspCheckAndAwardMilestoneBadge(@UserId, @CreatedBy, @IPAddress)`**
- Called by `UserRepository.CheckAndAwardBadgesAsync` after the existing `uspCheckAndAwardBadge` call
- Evaluates all 6 criteria; inserts badges for newly qualified criteria only (NOT EXISTS guard prevents duplicates)

**Frontend: Profile Certificates Section**
- Loads badges via `GET /api/users/badges`
- Filters for `CERTIFICATE_CODES` set: only earned milestone badges shown
- Each certificate card has a "Download" button
- Download generates a Canvas-based PNG with GoWithFlow branding, user name, certificate title, earned date

**Migration:** `20260601000007_AddMilestoneCertificates_Phase14.cs` (SQL Server) + `23_add_milestone_certificates.sql` (PostgreSQL)

**Files changed:**
- `UserRepository.cs` — calls `uspCheckAndAwardMilestoneBadge` after existing badge check
- `profile.component.ts` — certificates section + Canvas PNG download

---

### Phase 3 Step 5 — Live Session Audio Archive (Opt-In)

**Purpose:** Users can opt in to record their own voice turns during a session for personal review. Audio is stored per-turn, accessible for playback in the Post-Session Review screen, and auto-deleted after 90 days.

**Consent:** Stored in `localStorage` (`gwf_audio_archive_consent = 'true'|'false'`). Toggle visible in the Lobby screen before session starts. Default: OFF.

**Privacy Rules:**
- Only the user who opted in can access their own clips
- No other user, admin, or backend process can retrieve another user's clips
- Auto-deletion: 90 days (via `ExpiresAt` column; server-side cleanup uses `ExpiresAt <= GETDATE()` filter)

**New DB Table:** `tblAudioArchive`
- `ArchiveId BIGINT PK`, `SessionId FK`, `UserId FK`, `TurnIndex INT`, `StorageKey NVARCHAR(512)`, `DurationSecs INT`, `ExpiresAt DATETIME2`
- **Phase 10 (R2):** `StorageKey` now stores the R2 object key (e.g. `sessions/{sessionId}/turns/{turnIndex}/{userId}_{timestamp}.webm`). Previous disk-path format (`{userId}/{sessionId}/turn_1_xxx.webm`) is backwards-compatible — `IsR2Key()` detects by checking the value does not start with `/` or `http`.

**API Endpoints (UserOrAdmin + ActiveUser):**
- `POST /api/users/audio-archive` (multipart/form-data: `file`, `sessionId`, `turnIndex`) — **Phase 10:** uploads to `gwf-audio` R2 bucket; saves R2 key to `tblAudioArchive.StorageKey`; returns presigned URL in `AudioUrl`
- `GET /api/users/sessions/{sessionId}/audio-archive` — returns clip list; **Phase 10:** `AudioUrl` is generated as a fresh presigned URL (120-min expiry) for each R2-backed clip
- `DELETE /api/users/audio-archive/{archiveId}` — soft-deletes DB record; **Phase 10:** also calls `IStorageService.DeleteAsync` to remove the R2 object

**File Storage (Phase 10 — R2 only):**
- Old `AudioArchive:StoragePath` config key is no longer read; `IConfiguration` is no longer injected into `AudioArchiveService`
- Storage handled entirely by `IStorageService` / `CloudflareR2StorageService`

**Frontend flow:**
1. **Lobby**: "Record my voice turns" toggle → saves to `localStorage`
2. **SpeakerScreenComponent**: on each turn change, calls `voiceEngine.enableAudioCapture(consent)`. After recording completes, if consent is on and `voiceEngine.lastAudioBlob` is set, uploads clip via `AudioArchiveService.uploadClip()`
3. **VoiceRecognitionEngine**: new `enableAudioCapture(enabled)` + `lastAudioBlob` — when enabled, runs `MediaRecorder` in parallel to `SpeechRecognition`. Blob available after `stopSession()`
4. **SessionReviewComponent**: loads clips via `getSessionClips(sessionId)`, shows play button next to user's own turns with archived audio

**Migration:** `20260601000008_AddAudioArchive_Phase15.cs` (SQL Server) + `24_add_audio_archive.sql` (PostgreSQL)

**Files changed:**
- `voice-recognition.engine.ts` — `enableAudioCapture()`, `lastAudioBlob`, `MediaRecorder` parallel capture
- `lobby.component.ts` + `lobby.component.html` — audio archive consent toggle
- `speaker-screen.component.ts` — upload clip after recording completes
- `audio-archive.service.ts` — new Angular service
- `IAudioArchiveRepository.cs`, `AudioArchiveRepository.cs` — repository
- `IAudioArchiveService.cs`, `AudioArchiveService.cs` — service
- `UserController.cs` — 3 new endpoints
- `ApiRoutes.cs` — 3 new user audio archive routes
- `Program.cs` — service registrations
- `session-review.component.ts` — load clips + play buttons

---

## Phase 10 — Cloudflare R2 Storage Integration (2026-06-02)

### Summary

Replaces all local `wwwroot` disk storage with Cloudflare R2 (S3-compatible, free-egress object storage). No files are written to the server disk after this phase. All file access is via time-limited presigned URLs generated by the API.

### R2 Bucket Design

| Bucket | Contents | Key Pattern |
|---|---|---|
| `gwf-audio` | Voice recordings + audio archive clips | `sessions/{sessionId}/turns/{turnIndex}/{userId}.ogg` / `.webm` |
| `gwf-avatars` | User profile images | `avatars/{userId}/{timestampMs}.{ext}` |
| `gwf-scripts` | Original uploaded Excel files + sample template | `scripts/{scriptId}/v{version}.xlsx` / `scripts/sample/template_v1.xlsx` |
| `gwf-exports` | Admin-generated report Excel files | `exports/{requestedByUserId}/{yyyyMMdd_HHmmss}.xlsx` |

All buckets are **private**. Files are never served via public R2 URLs. All access is via presigned URLs.

### Account Details

- Account ID: `20d4a61f73b1845cd3322d589b62820b`
- S3 Endpoint: `https://20d4a61f73b1845cd3322d589b62820b.r2.cloudflarestorage.com`
- Token Name: `R2 Account Token`
- Credentials stored in: `appsettings.json` and `appsettings.Development.json` under `CloudflareR2` section

### Configuration Block (appsettings.json)

```json
"CloudflareR2": {
  "AccountEndpoint": "https://20d4a61f73b1845cd3322d589b62820b.r2.cloudflarestorage.com",
  "AccessKeyId": "<stored in appsettings>",
  "SecretAccessKey": "<stored in appsettings>",
  "Buckets": {
    "Audio": "gwf-audio",
    "Avatars": "gwf-avatars",
    "Scripts": "gwf-scripts",
    "Exports": "gwf-exports"
  },
  "PresignedUrlExpiryMinutes": {
    "Audio": 120,
    "Avatars": 1440,
    "Scripts": 60,
    "Exports": 30
  }
}
```

### New Infrastructure

#### Settings Model
- `GoWithFlow.Application/Settings/CloudflareR2Settings.cs` — `CloudflareR2Settings`, `BucketSettings`, `PresignedExpirySettings`

#### Custom Exception
- `GoWithFlow.Domain/Exceptions/StorageException.cs` — `Bucket` + `ObjectKey` properties; caught by `ExceptionMiddleware` → HTTP 502 `"File storage operation failed. Please try again."`

#### Storage Interface
- `GoWithFlow.Application/Interfaces/Services/IStorageService.cs`
  - `UploadAsync(stream, bucketName, objectKey, contentType)` → returns objectKey
  - `GetPresignedUrlAsync(bucketName, objectKey, expiryMinutes)` → returns time-limited URL (never log — contains credentials in query string)
  - `DeleteAsync(bucketName, objectKey)` — swallows 404 (object already gone)
  - `ExistsAsync(bucketName, objectKey)` → bool

#### Storage Key Builder
- `GoWithFlow.Application/Helpers/StorageKeyBuilder.cs` — centralised key construction; never build keys inline
  - `VoiceRecording(sessionId, turnIndex, userId, ext="ogg")`
  - `UserAvatar(userId, ext="jpg")`
  - `ScriptExcel(scriptId, version)`
  - `ReportExport(requestedByUserId)`
  - `SampleTemplate()`
  - `AudioArchiveClip(sessionId, turnIndex, userId)`
  - `IsR2Key(value)` — returns true if value does not start with `/` or `http` (used to distinguish R2 keys from old disk paths)

#### Bucket Constants
- `GoWithFlow.Application/Constants/StorageBuckets.cs` — `StorageBucket` enum + `BucketResolver.Resolve(bucket, settings)`

#### R2 Service Implementation
- `GoWithFlow.Infrastructure/ExternalServices/CloudflareR2StorageService.cs`
- SDK: `AWSSDK.S3` v3.7.x (R2 is S3-compatible; no Cloudflare-specific SDK needed)
- `AmazonS3Config.ForcePathStyle = true`, `SignatureVersion = "4"`, `AuthenticationRegion = "auto"`, `UseChunkEncoding = false` on uploads
- `GetPreSignedURL` is synchronous — wrapped in `Task.FromResult`
- **Drift fixed 2026-06-03:** `AuthenticationRegion = "auto"` is mandatory. Without it, `GetPreSignedURL` falls back to SigV2 (`AWSAccessKeyId` query param format) on custom `ServiceURL` endpoints even when `SignatureVersion = "4"` is set. Cloudflare R2 rejects SigV2 with HTTP 401 `"SigV2 authorization is not supported. Please use SigV4 instead."` — all presigned URLs were broken until this was added.

### DB Changes (applied to Supabase PostgreSQL 2026-06-02)

```sql
ALTER TABLE public.tblscript ADD COLUMN IF NOT EXISTS excelstoragkey VARCHAR(256) NULL;
ALTER TABLE public.tblvoiceanalysis ADD COLUMN IF NOT EXISTS audiostoragkey VARCHAR(256) NULL;
-- tblUser.avatarurl already existed — now stores R2 key instead of URL path
```

EF entity column name mapping (via `ApplyProviderConventions`):
- `ExcelStorageKey` (C#) → `excelstoragekey` (PostgreSQL)  [excel + storage + key = 15 chars]
- `AudioStorageKey` (C#) → `audiostoragekey` (PostgreSQL)  [audio + storage + key = 15 chars]
- **Drift fixed 2026-06-02 (migration 27):** original Phase 10 migration created `excelstoragkey` / `audiostoragkey` (14 chars each — missing trailing 'e' from "storage"). EF `ApplyProviderConventions` lowercases property names fully producing the 15-char names. Any query including Script or VoiceAnalysis threw PG 42703. Fixed by `ALTER TABLE ... RENAME COLUMN` in migration 27. Source migration `AddR2StorageKeys_Phase10.sql` updated with correct names.

New repository methods added via EF Core:
- `IScriptRepository.UpdateScriptExcelKeyAsync(scriptId, key)` + `GetScriptExcelKeyAsync(scriptId)`
- `ILiveSessionRepository.UpdateVoiceAnalysisAudioKeyAsync(voiceAnalysisId, key)`

### Services Updated

| Service | Change |
|---|---|
| `UserService` | `UploadAvatarAsync` → uploads to `gwf-avatars`; saves R2 key to DB; returns presigned URL. `GetProfileAsync` → detects R2 key and generates fresh presigned URL on each profile fetch. `IWebHostEnvironment` removed. |
| `ScriptService` | `UploadScriptAsync` → buffers Excel in memory; uploads to `gwf-scripts` after insert; saves key to `ExcelStorageKey`. `GetSampleTemplateAsync` → uploads template to R2 on first call (checks `ExistsAsync`); returns presigned URL string (was `byte[]`). `GetExcelDownloadUrlAsync` → new method, returns presigned URL for stored original Excel. |
| `AdminService` | `ExportReportsAsExcelAsync` → generates Excel via `ClosedXML`; uploads to `gwf-exports`; returns presigned URL string (was `byte[]`). |
| `LiveSessionService` | `SaveVoiceAnalysisAsync` → after UPSERT, if `AudioBase64` is present in request: decodes, uploads to `gwf-audio`, saves key to `AudioStorageKey`. Invalid Base64 silently skipped. |
| `AudioArchiveService` | `UploadClipAsync` → uploads to `gwf-audio`; saves R2 key to `StorageKey`; returns presigned URL. `GetSessionClipsAsync` → fetches items; replaces each R2-backed `AudioUrl` with a fresh presigned URL. `DeleteClipAsync` → soft-deletes DB record + calls `DeleteAsync` on R2. `IConfiguration` injection removed. |

### Controller Changes

| Controller | Endpoint | Change |
|---|---|---|
| `AdminController` | `GET /api/admin/reports/export` | Returns `ApiResponse<string>` (presigned URL); removed `File()` response |
| `ScriptController` | `GET /api/scripts/sample-template` | Returns `ApiResponse<string>` (presigned URL); removed `File()` response |
| `ScriptController` | `GET /api/scripts/{scriptId}/excel-download` | **New endpoint** — returns presigned URL for original uploaded Excel |
| `ApiRoutes.Script` | `ExcelDownload` | **New route constant** `"{scriptId:long}/excel-download"` |

Note: `GET /api/scripts/{scriptId}/download` still returns `File()` bytes — this endpoint generates Excel **in-memory from current utterances** via `ClosedXML` (not R2). It is intentionally kept as a live-regeneration endpoint.

### Key Rules (Never Drift)

1. **Store key, not URL** — only R2 object keys are saved to the database. URLs are generated fresh per-request and never persisted.
2. **All buckets are private** — presigned URLs are the only valid access path. Never make buckets public.
3. **Key builder is the single source** — never construct R2 keys inline. Always use `StorageKeyBuilder.*`.
4. **`AudioBase64` is optional** — voice analysis save never fails due to missing or invalid audio. Audio upload is fire-and-forget after the DB write.
5. **Backwards compatibility** — `AvatarUrl` and `AudioUrl` values that start with `/` or `http` are treated as old disk paths and returned as-is. Only R2 keys (detected via `IsR2Key()`) trigger presigned URL generation.

### Migration SQL Files

- `GoWithFlow.Infrastructure/Migrations/PostgreSQL/AddR2StorageKeys_Phase10.sql`

---

## Flow Audit — Confirmed Bugs and Design Gaps (2026-06-03)

**Reference document:** `Backend/Docs/Dev/FlowAudit_ProductionReady.md`

### Confirmed Bugs

#### Bug-01: Lobby reload incorrectly transitions session to ABANDONED
- **Module:** Session Module — Lobby Flow / SessionHub
- **Root cause:** `SessionHub.OnDisconnectedAsync` calls `LeaveSessionAsync` when status is LOBBY; `uspUpdateSessionMemberLeft` auto-abandons when the last active member leaves. A page reload disconnects the WebSocket, triggering this path for any user who is alone in the lobby.
- **Fix:** Add 15–30s reconnect grace window before persisting leave. Only persist leave after grace expires without reconnect. Session should never transition to ABANDONED solely from a WebSocket disconnect.
- **DB change required:** None for stopgap. For full fix: in-memory or Redis cache tracking disconnected userId+sessionId pairs.

#### Bug-02: MEMBER_READY event not updating host lobby screen in real-time
- **Module:** Session Module — Lobby Flow — Frontend
- **Root cause:** `LobbyComponent` `MEMBER_READY` handler likely not finding the correct member reference (userId lookup mismatch or signal not triggering change detection).
- **Fix:** In `LobbyComponent.ngOnInit()`, register `MEMBER_READY` handler before hub connect; use `members.update()` signal mutation with `userId` key match.

#### Bug-03: Dashboard invitation badge count not updated in real-time
- **Module:** Session Invitation Module — Frontend
- **Root cause:** `UserDashboardComponent` does not subscribe to `INVITATION_RECEIVED` SignalR event. Count only updates on page load.
- **Fix:** In `UserDashboardComponent.ngOnInit()`, subscribe to `INVITATION_RECEIVED`. On event, call `pendingInvitationCount.update(count => count + 1)`.

#### Bug-04: Duplicate readiness action after accepting invitation
- **Module:** Session Invitation Module — Invitation → Lobby
- **Root cause:** `RespondToInvitationAsync` (Accept path) inserts `tblSessionMember` with `IsReady=false`. User then enters lobby and must tap again.
- **Fix:** After inserting member, call `uspUpdateSessionMemberReadyStatus(@IsReady=true)`. Broadcast `MEMBER_READY` alongside `INVITATION_RESPONDED`.

### Confirmed Design Gaps

#### Gap-01: SessionMode collected twice (script upload + session creation)
- SessionMode is implicitly defined by the script's Category. Remove `SessionMode` from `CreateSessionRequestDto`. Backend derives it from `tblScript.Category`.

#### Gap-02: MaxMembers collected twice (session creation should inherit from script)
- MaxMembers should be captured at Script Upload (derived from distinct SpeakerLabel count in Excel) and stored on `tblScript.MaxMembers`. Session Creation auto-inherits it.
- **DB change required:** `ALTER TABLE tblScript ADD MaxMembers TINYINT NOT NULL DEFAULT(2);`

#### Gap-03: Recording toggle shown to all participants
- `IsRecordingEnabled` should be a host-only session-level flag. Remove recording toggle from participant screens. Add `tblSession.IsRecordingEnabled BIT NOT NULL DEFAULT(0)`.
- **DB change required:** `ALTER TABLE tblSession ADD IsRecordingEnabled BIT NOT NULL DEFAULT(0);`

#### Gap-04: GrammarFocusTag and HintLanguage are unnecessary in Script Upload UI
- GrammarFocusTag: derive from GrammarTag column values in Excel; keep in DB for analytics; remove from UI.
- HintLanguage: hardcoded to Telugu — remove dropdown from upload wizard; backend defaults to "Telugu".

### Implementation Status (2026-06-03)

All 7 items implemented:

#### Bug-01 FIXED
- Created `ILobbyReconnectTracker` + `LobbyReconnectTracker` singleton (`GoWithFlow.API/Hubs/LobbyReconnectTracker.cs`)
- `OnDisconnectedAsync`: schedules `LeaveSessionAsync` with 20s grace window instead of immediate call
- `OnConnectedAsync`: calls `CancelPendingLeave(sessionId, userId)` — page reload reconnects cancel the scheduled leave
- `LobbyReconnectTracker` uses `IServiceScopeFactory` + `IHubContext<SessionHub>` for out-of-hub leave execution
- Frontend `lobby.component.ts`: removed `leaveSession` REST call from `ngOnDestroy` (hub grace window is the cleanup mechanism)
- Registered as singleton in `Program.cs`

#### Bug-02 FIXED
- Root cause confirmed: `toggleReady()` called REST `PATCH /api/sessions/ready` which updates DB only — no SignalR broadcast.
  The broadcast only happens via hub method `SetReady`.
- Fix: `toggleReady()` now calls `wsService.emit('SetReady', sessionId, userId, next)` as primary path.
  Hub broadcasts `MEMBER_READY` to entire session group → host screen updates immediately.
  REST API remains as fallback if hub is unavailable.

#### Bug-03 FIXED
- `WebsocketService.connect()` now accepts `sessionId: string | null` — connects without session context.
- `UserDashboardComponent` connects to `/hubs/session` (no sessionId) in `ngOnInit`, subscribes to `INVITATION_RECEIVED`.
  On event: `pendingInvitationCount.update(count => count + 1)` — no API call needed.
  Disconnects in `ngOnDestroy`.

#### Bug-04 FIXED
- `SessionInvitationService.RespondToInvitationAsync` (Accept path): after `JoinSessionAsync`, now calls
  `_sessionRepository.UpdateSessionMemberReadyStatusAsync(@IsReady=true)` and
  `_notifier.NotifyMemberReadyAsync(sessionId, userId, true)` which broadcasts `MEMBER_READY` to session group.
- `ISessionNotifier` + `SessionNotifier`: added `NotifyMemberReadyAsync(sessionId, userId, isReady)` method.
- Result: user who accepts an invitation arrives in lobby already READY. No second tap required.

#### Gap-01/02 FIXED
- `SessionService.CreateSessionAsync`: derives `SessionMode` from `script.Category` via `MapCategoryToSessionMode()`
  (includes all 6 canonical categories + 3 legacy aliases).
  Derives `MaxMembers` from `ExtractSlotNames(script, byte.MaxValue).Count` — all distinct speaker labels.
- `CreateSessionRequestDto`: `SessionMode` and `MaxMembers` fields retained for backward compatibility but no longer used.
- Frontend `CreateSessionComponent`:
  - Removed Practice Mode selector (6-button grid)
  - Removed MaxMembers stepper (± controls)
  - Added read-only "Session Type" + "Members" display after script selection (derived client-side)
  - Duration + Expiry selectors kept; form now requires only Name + Script + Duration + Expiry

#### Gap-03/04 FIXED (Recording host-only)
- `lobby.component.html`: recording toggle wrapped in `@if (isHost())` — guests never see the control.
- Guests see passive "This session is being recorded by the host" notice only when `audioConsentEnabled()` is true.
- Label updated from "Record my voice turns" → "Record Session Audio" (reflects session-level scope).

#### Gap-05/06 FIXED (Upload UI cleanup)
- Script Upload metadata form: removed "Grammar Focus" dropdown from template.
- `grammarFocusTag` + `hintLanguage` retained as hidden form values with defaults ('Have Been' / 'Telugu').
- `metadataForm`: removed `Validators.required` from `hintLanguage` (always 'Telugu', never user-supplied).
- API payload unchanged — backend still receives all fields; no breaking change.
- `GoWithFlow.Infrastructure/Migrations/SqlServer/AddR2StorageKeys_Phase10.sql`

---

## Android Mobile Module — Capacitor Setup (2026-06-03)

### Configuration

- Framework: Capacitor 8.4.0 wrapping Angular 19 + Vite (AnalogJS) web app
- App ID: `com.gowithflow.app`
- App Name: `GoWithFlow`
- **App Version: `1.5` (versionCode 8)** — set in `Frontend/android/app/build.gradle`. History: 1.1.1 (vc 3) → 1.2 (vc 4, 2026-06-05: Web Speech secure-context/capability fix) → 1.2 rebuild (vc 5, 2026-06-05: + lobby `JoinLobby`/`MEMBER_JOINED` realtime fix) → 1.3 (vc 6, 2026-06-05: production distribution build from current `main`, no logic change) → 1.4 (vc 7, 2026-06-06: production distribution build for sharing, includes Session Room UX redesign phases 0–5, no new logic change) → 1.5 (vc 8, 2026-06-08: production distribution build from current `main`, no new logic change). Bump `versionCode` on every distributable build (keep `versionName` for user-facing releases).
- Web Dir: `dist/analog/public` (Vite production build output)
- Android Scheme: `https` — required for JWT cookies and SignalR auth to function correctly on device
- Config file: `Frontend/capacitor.config.ts` — currently in production mode (NO `server` block); app loads from bundled assets and connects to the production API.
- Distribution APK: `Backend/Docs/Dev/GoWithFlow.apk` (latest = 1.5) + versioned copy `GoWithFlow-1.5.apk`. Prior: `GoWithFlow-1.4.apk`, `GoWithFlow-1.3.apk`, `GoWithFlow-1.2.apk` retained.

### Android Project Location

- Android project root: `Frontend/android/`
- Gradle wrapper: `gradle-8.14.3-bin` (local cache at `~/.gradle/wrapper/dists/gradle-8.14.3-bin/`)
- Minimum SDK: 24 (Android 7.0)
- Compile/Target SDK: 36 (Android 16)

### Environment Requirements

- Java 21 required (Capacitor 8.x compiles with `JavaVersion.VERSION_21`)
- `JAVA_HOME=/usr/lib/jvm/java-21-openjdk-amd64` must be set when running Gradle
- Android SDK: `ANDROID_HOME=/root/Android/Sdk` (platforms android-34, android-35, build-tools 34.0.0, 35.0.0)
- Node 24+ for npm scripts

### Permissions (AndroidManifest.xml)

- `INTERNET` — all API and SignalR calls
- `RECORD_AUDIO` — native speech recognition via `@capacitor-community/speech-recognition`
- `MODIFY_AUDIO_SETTINGS` — microphone gain during voice turns
- `ACCESS_NETWORK_STATE` — connectivity detection

### API Connectivity

- Production API: `https://gowithflow-api.onrender.com`
- Environment file: `Frontend/src/app/environments/environment.ts`
- `PROD = true` (production build): connects directly to `https://gowithflow-api.onrender.com`
- `PROD = false` (dev build): uses `window.location.origin/api` → Vite proxy → local .NET backend at `https://localhost:44378`

### Network Security Config

File: `Frontend/android/app/src/main/res/xml/network_security_config.xml`
Allows cleartext HTTP to both PC addresses:
- `10.147.254.186` — secondary PC interface
- `192.168.31.216` — primary WiFi IP (home network, same subnet as Android device)

Required for dev APK to reach Vite dev server over HTTP.

### Two APK Modes

#### Mode 1 — Production APK (distribution and production testing)

`capacitor.config.ts` must NOT have `server.url`. App loads from bundled assets.
Connects to: `https://gowithflow-api.onrender.com` — no local server needed.

Build (WSL2):
```bash
cd /mnt/c/Live/GoWithFlow/Frontend
npm run build
npx cap sync android
cd android
JAVA_HOME=/usr/lib/jvm/java-21-openjdk-amd64 ./gradlew assembleDebug
```

Install (Windows PowerShell):
```powershell
adb install -r "C:\Live\GoWithFlow\Frontend\android\app\build\outputs\apk\debug\app-debug.apk"
```

Copy to share location (PowerShell):
```powershell
Copy-Item "C:\Live\GoWithFlow\Frontend\android\app\build\outputs\apk\debug\app-debug.apk" `
          "C:\Live\GoWithFlow\Backend\Docs\Dev\GoWithFlow.apk"
```

#### Mode 2 — Dev APK with Live Reload (rapid development iteration)

`capacitor.config.ts` must have `server.url: 'http://192.168.31.216:4200'` and `cleartext: true`.
Current state: `capacitor.config.ts` already has this config as of 2026-06-04.

**Workflow:**
1. Verify PC IP is still `192.168.31.216` → `ipconfig | Select-String "IPv4"`
2. Start Angular dev server: `cd Frontend && npm run dev` (binds to `0.0.0.0:4200`)
   - Default: proxies `/api` and `/hubs` to `https://gowithflow-api.onrender.com` — no local backend needed
   - For local backend dev: `$env:API_TARGET='https://localhost:44378'; npm run dev`
3. Only rebuild and reinstall APK when `capacitor.config.ts` changes — otherwise dev server serves changes via HMR
4. Device loads app from dev server → frontend changes reflect without reinstall

**Vite proxy default target (2026-06-04 change):** `https://gowithflow-api.onrender.com`
Previously defaulted to `https://localhost:44378` which required local .NET backend. Changed so dev APK works against production API by default.
Override: `$env:API_TARGET='https://localhost:44378'; npm run dev`

**Current live-reload APK:** Built 2026-06-04, has `server.url: 'http://192.168.31.216:4200'` baked in.
`C:\Live\GoWithFlow\Frontend\android\app\build\outputs\apk\debug\app-debug.apk`

### APK Release Process — Run This Exact Sequence Every Time

**Use case:** Producing a new APK for family/friends distribution or device testing.
**Time:** ~3–5 minutes total. No manual steps beyond running these commands.

---

#### Step 1 — Remove server.url (Windows PowerShell or edit directly)

`capacitor.config.ts` must have NO `server` block for a production APK.
Required content:
```typescript
const config: CapacitorConfig = {
  appId: 'com.gowithflow.app',
  appName: 'GoWithFlow',
  webDir: 'dist/analog/public'
};
```

---

#### Step 2 — Vite production build (Windows PowerShell, inside `Frontend/`)

```powershell
Set-Location "C:\Live\GoWithFlow\Frontend"
npm run build
```

Produces: `dist/analog/public/` — bundled Angular app.

---

#### Step 3 — Capacitor sync (Windows PowerShell, inside `Frontend/`)

```powershell
npx cap sync android
```

Copies `dist/analog/public` into the Android project. Confirm output includes:
- `✔ Copying web assets`
- `✔ Creating capacitor.config.json` (verify NO `server` key in this file)
- `@capacitor-community/speech-recognition@7.0.1` listed under plugins

---

#### Step 4 — Clear old build dir, then Gradle build (WSL)

Always clear the build dir first. WSL2 cannot delete intermediates on the Windows filesystem after a previous build — Gradle fails with "Unable to delete directory after 10 attempts".

**SDK-path gotcha (REQUIRED — see Drift below):** `android/local.properties` is regenerated by Android Studio on Windows with `sdk.dir=C:/Users/<user>/AppData/Local/Android/Sdk` — a **Windows** path that Gradle, running in WSL/Linux, cannot resolve → `SDK location not found`. The WSL build must use the Linux SDK at `/root/Android/Sdk` (verified present: build-tools 34.0.0/35.0.0, platforms android-34/35/36). Setting `ANDROID_HOME` does **not** help: the Android Gradle Plugin gives `local.properties` `sdk.dir` precedence over the env var. So point `local.properties` at the Linux SDK for the build, then restore the Windows path (keeps the Windows/Android-Studio setup working).

**CRITICAL — do NOT write `local.properties` with `printf "...\n"` through PowerShell `wsl --exec` (see Drift 2026-06-05 #2).** PowerShell→`wsl.exe` argument translation strips the backslash in `\n`, so `printf` writes the literal path `sdk.dir=/root/Android/Sdkn` (trailing `n`, nonexistent) → `SDK location not found`. The `cp local.properties local.properties.winbak` step then propagates the corrupt value into the backup, so the restore also writes garbage. **Reliable method:** write `local.properties` directly with an editor/Write tool (one line, `sdk.dir=/root/Android/Sdk`), run Gradle, then write it back to the Windows path. Equivalent build invoked through the **Bash tool** (true Linux shell, no `wsl.exe` arg translation) where `\n` survives:

```bash
# 1. Set Linux SDK path (write file directly — NOT via printf through PowerShell)
#    local.properties content: sdk.dir=/root/Android/Sdk
wsl --exec bash -lc 'cd /mnt/c/Live/GoWithFlow/Frontend/android && rm -rf app/build 2>/dev/null; JAVA_HOME=/usr/lib/jvm/java-21-openjdk-amd64 ./gradlew assembleDebug --no-daemon 2>&1 | tail -15; echo "GRADLE_EXIT=${PIPESTATUS[0]}"'
# 2. After build, restore local.properties to: sdk.dir=C:/Users/mdfay/AppData/Local/Android/Sdk
```

Expected: `BUILD SUCCESSFUL in Xs` then `GRADLE_EXIT=0`. Verify the version with aapt:
`/root/Android/Sdk/build-tools/35.0.0/aapt dump badging app/build/outputs/apk/debug/app-debug.apk | grep package` → `versionCode='6' versionName='1.3'`.

Output APK: `C:\Live\GoWithFlow\Frontend\android\app\build\outputs\apk\debug\app-debug.apk`

---

#### Step 5 — Replace distribution APK (Windows PowerShell)

```powershell
Remove-Item "C:\Live\GoWithFlow\Backend\Docs\Dev\GoWithFlow.apk" -ErrorAction SilentlyContinue
Copy-Item "C:\Live\GoWithFlow\Frontend\android\app\build\outputs\apk\debug\app-debug.apk" `
          "C:\Live\GoWithFlow\Backend\Docs\Dev\GoWithFlow.apk"
Get-Item "C:\Live\GoWithFlow\Backend\Docs\Dev\GoWithFlow.apk" | Select-Object Length, LastWriteTime
```

---

#### Step 6 — Restore live-reload config (Windows PowerShell or edit directly)

After distribution APK is done, restore `server.url` for dev work:
```typescript
const config: CapacitorConfig = {
  appId: 'com.gowithflow.app',
  appName: 'GoWithFlow',
  webDir: 'dist/analog/public',
  server: {
    url: 'http://192.168.31.216:4200',
    cleartext: true
  }
};
```

---

#### Step 7 — Install on device (Windows PowerShell)

```powershell
# If only one ADB device connected:
adb install -r "C:\Live\GoWithFlow\Backend\Docs\Dev\GoWithFlow.apk"

# If both USB and wireless active (specify serial):
adb -s QGCAAETSYXRS95KV install -r "C:\Live\GoWithFlow\Backend\Docs\Dev\GoWithFlow.apk"
```

---

#### Share with family/friends

APK location: `C:\Live\GoWithFlow\Backend\Docs\Dev\GoWithFlow.apk`

Recipients must:
1. Enable **Install from unknown sources** (Settings → Apps → Special app access → Install unknown apps)
2. Open the APK file on device to install

App connects to: `https://gowithflow-api.onrender.com` — no local server needed.

---

#### Full sequence as one block (copy-paste ready)

```powershell
# STEP 1: Remove server.url from capacitor.config.ts (edit manually)

# STEP 2: Build
Set-Location "C:\Live\GoWithFlow\Frontend"
npm run build
npx cap sync android

# STEP 3: (optional) bump version in Frontend/android/app/build.gradle — versionName + versionCode

# STEP 4: Gradle (clears build dir + swaps local.properties to the WSL SDK path, then restores)
wsl --exec bash -lc 'cd /mnt/c/Live/GoWithFlow/Frontend/android && rm -rf app/build 2>/dev/null; cp local.properties local.properties.winbak && printf "sdk.dir=/root/Android/Sdk\n" > local.properties && JAVA_HOME=/usr/lib/jvm/java-21-openjdk-amd64 ./gradlew assembleDebug --no-daemon 2>&1 | tail -8; status=${PIPESTATUS[0]}; mv local.properties.winbak local.properties; echo "GRADLE_EXIT=$status"'

# STEP 5: Replace distribution APK
Remove-Item "C:\Live\GoWithFlow\Backend\Docs\Dev\GoWithFlow.apk" -ErrorAction SilentlyContinue
Copy-Item "C:\Live\GoWithFlow\Frontend\android\app\build\outputs\apk\debug\app-debug.apk" "C:\Live\GoWithFlow\Backend\Docs\Dev\GoWithFlow.apk"
Get-Item "C:\Live\GoWithFlow\Backend\Docs\Dev\GoWithFlow.apk" | Select-Object Length, LastWriteTime

# STEP 6: Restore server.url in capacitor.config.ts (edit manually)

# STEP 7: Install on device
adb -s QGCAAETSYXRS95KV install -r "C:\Live\GoWithFlow\Backend\Docs\Dev\GoWithFlow.apk"
```

### Build Process — Full APK Rebuild

Run from WSL2 (note the `local.properties` swap — see Drift below):
```bash
cd /mnt/c/Live/GoWithFlow/Frontend
npm run build                          # Angular production build → dist/analog/public
npx cap sync android                   # copy assets into Android project
cd android/
cp local.properties local.properties.winbak && printf "sdk.dir=/root/Android/Sdk\n" > local.properties
JAVA_HOME=/usr/lib/jvm/java-21-openjdk-amd64 ./gradlew assembleDebug; mv local.properties.winbak local.properties
```

### Notes on Drift Prevented (Build)

- **Drift (2026-06-05): WSL Gradle build fails with `SDK location not found`** — `Frontend/android/local.properties` is regenerated by Android Studio on Windows with `sdk.dir=C:/Users/<user>/AppData/Local/Android/Sdk` (a Windows path). Gradle runs in WSL/Linux and cannot resolve it, and the Android Gradle Plugin gives `local.properties` `sdk.dir` **precedence over `ANDROID_HOME`** — so exporting the env var does not fix it. **Fix:** before the WSL build, swap `local.properties` to `sdk.dir=/root/Android/Sdk` (the Linux SDK, confirmed present alongside `/usr/lib/android-sdk`), then restore the original Windows path so the Windows/Android-Studio workflow keeps working. Baked into Step 4 and the full-sequence block. Drift type: environment/path contract drift.

- **Drift (2026-06-05) #2: `printf "sdk.dir=/root/Android/Sdk\n"` through PowerShell `wsl --exec` corrupts `local.properties`** — hit during the 1.3 build. PowerShell→`wsl.exe` argument translation strips the backslash in `\n`, so `printf` receives `sdk.dir=/root/Android/Sdk\n` as `sdk.dir=/root/Android/Sdkn` and writes that literal trailing `n`. Result: `sdk.dir` points at the nonexistent `/root/Android/Sdkn` → Gradle `SDK location not found` even though `/root/Android/Sdk` is valid. **Worse:** the documented swap does `cp local.properties local.properties.winbak` *before* overwriting, and on a retry that copies the already-corrupt value into the backup, so the `mv` restore writes garbage back — and the original Windows path was lost. **Fix:** never write `local.properties` via `printf "...\n"` piped through PowerShell `wsl.exe`. Write the file directly (Write tool / editor) as a single line `sdk.dir=/root/Android/Sdk`, build, then write it back to `sdk.dir=C:/Users/mdfay/AppData/Local/Android/Sdk`. The Bash tool (real Linux shell) is also safe because there is no `wsl.exe` arg translation. The `.winbak` swap dance is discouraged — prefer two explicit direct writes. Drift type: build-script / shell-escaping drift. Diagnostic tell: `cat -A local.properties` shows a trailing `n` with no `$` (newline) before it.

- **Drift (2026-06-06): the agent "Bash tool" on this machine is MSYS/git-bash (`MINGW64`), NOT WSL** — it has no `/mnt/c` mount, so `cd /mnt/c/...` and `./gradlew` fail with `No such file or directory` (exit 127). Earlier docs implied the Bash tool was "a real Linux shell"; that is false here. **Fix:** always run the Gradle build through WSL explicitly: `wsl --exec bash -lc '...'` (works from either the Bash tool or PowerShell). Combined with writing `local.properties` directly via the Write tool (single line `sdk.dir=/root/Android/Sdk`), this avoids both the path issue and the `printf \n` corruption. Verified clean with `cat -A local.properties` → `sdk.dir=/root/Android/Sdk$` (newline `$`, no trailing `n`). Drift type: build-environment/shell drift.

Output APK (WSL2 path): `Frontend/android/app/build/outputs/apk/debug/app-debug.apk`
Output APK (Windows path): `C:\Live\GoWithFlow\Frontend\android\app\build\outputs\apk\debug\app-debug.apk`

### Device Installation

USB device is connected to Windows — ADB not accessible from WSL2 directly.
Install from **Windows PowerShell** only:

```powershell
adb install -r "C:\Live\GoWithFlow\Frontend\android\app\build\outputs\apk\debug\app-debug.apk"
```

Wireless ADB works for installation once `adb connect 192.168.31.203:5555` is active.

### Wireless ADB Setup (2026-06-04)

Configured — device accessible over WiFi without USB cable.
Device WiFi IP: `192.168.31.203` | Port: `5555` | Serial: `QGCAAETSYXRS95KV`

**One-time USB setup (already done — repeat only if device is reset):**
```powershell
adb tcpip 5555
adb connect 192.168.31.203:5555
adb devices   # both serial and 192.168.31.203:5555 should appear
```

**After USB removal:**
```powershell
adb connect 192.168.31.203:5555
```

If connection fails: confirm device is on WiFi (same `192.168.31.x` network) and USB debugging is still enabled.

### Debug Session Guide

Full debugging playbook: `Backend/Docs/Dev/MobileDebug_PhaseWise_Guide.md`
Covers: clean install (Phase 1), manual flow walk (Phase 2), logcat capture (Phase 3), targeted logs (Phase 4), backend cross-reference (Phase 5), wireless ADB (Phase 6).

### Native Speech Recognition (FULLY IMPLEMENTED — 2026-06-04)

**Plugin:** `@capacitor-community/speech-recognition` v7.0.1 — installed and wired up.

**Implementation:** `Frontend/src/app/core/services/voice/voice-recognition.engine.ts`

**Platform detection:** `Capacitor.isNativePlatform()` → if true, routes to `startNativeSession()`

**Native path flow:**
1. `checkPermissions()` → `requestPermissions()` if not granted (runtime RECORD_AUDIO request)
2. `NativeSpeechRecognition.start()` fires the Android `SpeechRecognizer` service
3. Partial results stream via listener events → accumulated into `latestTranscript`
4. Silence detection: 2.5s with no new partial → finalizes
5. Hard timeout: 15s max → finalizes with whatever was captured
6. `finalizeNative()` → runs scoring pipeline (pronunciation, fluency, hesitations)

**VAD on native path:** NOT started — would conflict with `SpeechRecognizer` audio pipeline. Silence is handled by the 2.5s silence timer instead.

**Notes on Drift:** ProjectOverview previously stated "Web Speech API does not work in WebView — fix requires plugin, not yet implemented." This was stale. The plugin was installed (`@capacitor-community/speech-recognition@7.0.1`) and the native path fully implemented in the voice engine. Corrected 2026-06-04.

### ADB Device Screenshot Pull (Windows)

Use **PowerShell** only — Bash and cmd mangle the `/sdcard/` path via Git bash path conversion.

**Method (always use this — captures to device then pulls):**
```powershell
adb shell screencap -p /sdcard/screen_tmp.png
adb pull /sdcard/screen_tmp.png "C:\Users\mdfay\AppData\Local\Temp\latest_screenshot.jpg"
```

**Step 3 — Read the image** using the Read tool on:
`C:\Users\mdfay\AppData\Local\Temp\latest_screenshot.jpg`

**From device Screenshots folder:**
```powershell
adb shell "ls -lt /sdcard/Pictures/Screenshots/" | Select-Object -First 5
adb pull /sdcard/Pictures/Screenshots/<filename>.jpg C:\Users\mdfay\AppData\Local\Temp\latest_screenshot.jpg
```

**Critical rules:**
- Always use `adb shell screencap -p /sdcard/file.png` → `adb pull` — never `adb exec-out screencap -p | Set-Content` (PS5.1 cannot pipe binary stdout to file)
- Always run via PowerShell — never Bash or cmd (both corrupt the `/sdcard/` path)
- When both USB and wireless ADB are active simultaneously, `adb devices` shows two entries — all commands must include `-s QGCAAETSYXRS95KV` (serial) to avoid "more than one device/emulator" error
- Device must show `device` (not `offline` or `unauthorized`) in `adb devices` before pulling
- If device is offline: ask user to unlock phone and re-approve USB debugging prompt

**APK Share Location (for family distribution):**
`C:\Live\GoWithFlow\Backend\Docs\Dev\GoWithFlow.apk`
Recipients must enable "Install from unknown sources" on their Android device before installing.

### local.properties (Critical — do not delete)

File location: `Frontend/android/local.properties`
Required content:
```
sdk.dir=/root/Android/Sdk
```
This file is not committed to git. If missing, Gradle fails with "SDK location not found".
Must be recreated manually if the android folder is reset or re-initialized.

### Quick Command Reference (Windows PowerShell)

> **Note:** When both USB and wireless ADB connections are active, all commands need `-s QGCAAETSYXRS95KV`.
> Use `-s 192.168.31.203:5555` instead when connected wirelessly only.

```powershell
# Check ADB and device
adb devices

# Connect wirelessly
adb connect 192.168.31.203:5555

# Install APK (USB only OR wireless only)
adb install -r "C:\Live\GoWithFlow\Frontend\android\app\build\outputs\apk\debug\app-debug.apk"

# Install APK (when both USB and wireless active)
adb -s QGCAAETSYXRS95KV install -r "C:\Live\GoWithFlow\Frontend\android\app\build\outputs\apk\debug\app-debug.apk"

# Uninstall app
adb -s QGCAAETSYXRS95KV uninstall com.gowithflow.app

# Launch app
adb -s QGCAAETSYXRS95KV shell monkey -p com.gowithflow.app -c android.intent.category.LAUNCHER 1

# Clear logcat
adb -s QGCAAETSYXRS95KV logcat -c

# Capture logs to file
adb -s QGCAAETSYXRS95KV logcat -v time > "C:\Live\GoWithFlow\Backend\Docs\Dev\device_log.txt"

# Filter to app debug logs
adb -s QGCAAETSYXRS95KV logcat -v time | findstr "GWF-DEBUG"

# Screenshot (correct method)
adb -s QGCAAETSYXRS95KV shell screencap -p /sdcard/screen_tmp.png
adb -s QGCAAETSYXRS95KV pull /sdcard/screen_tmp.png C:\Users\mdfay\AppData\Local\Temp\latest_screenshot.jpg

# Get device WiFi IP
adb -s QGCAAETSYXRS95KV shell ip route | findstr wlan

# Switch to wireless ADB (while USB connected)
adb tcpip 5555
adb connect 192.168.31.203:5555
```

### Notes on Build Issues Encountered

**2026-06-03:**
- Gradle 8.14.3-all: partial download in cache caused `forceFetch` SSL failure → switched to `gradle-8.14.3-bin` (fully cached)
- Gradle 8.10.2 rejected: AGP requires minimum Gradle 8.13
- Java 17 rejected: Capacitor 8.4.0 sets `sourceCompatibility JavaVersion.VERSION_21` → installed Java 21
- Resolution: `gradle-wrapper.properties` uses `gradle-8.14.3-bin.zip`, Gradle invoked with `JAVA_HOME` pointing to Java 21
- local.properties missing on first build → Gradle failed with "SDK location not found" → created manually with `sdk.dir=/root/Android/Sdk`
- adb pull path mangling: Bash and cmd corrupt `/sdcard/` path via Git bash → use PowerShell only for adb pull commands

**2026-06-04:**
- `adb exec-out screencap -p | Set-Content -Encoding Byte` fails in PowerShell 5.1 — PS5.1 cannot pipe binary stdout; use `adb shell screencap -p /sdcard/file.png` → `adb pull` instead
- Wireless ADB configured: device IP `192.168.31.203`, port `5555` — USB cable no longer required after initial `adb tcpip 5555`
- Native speech recognition confirmed fully implemented — `@capacitor-community/speech-recognition@7.0.1` is installed and the native path in `voice-recognition.engine.ts` is complete
- Gradle fails with "Unable to delete directory after 10 attempts" when `android/app/build` exists from a prior WSL2 build — WSL2 cannot delete Windows-filesystem files held open by previous build. Fix: `Remove-Item -Recurse -Force android\app\build` from Windows PowerShell before each Gradle run
- Gradle fails with "Could not find EOCD in app-debug.apk" when a partial/corrupted APK exists in outputs from a failed previous build — Fix: `Remove-Item -Recurse -Force android\app\build` AND `android\build` from Windows PowerShell before rebuilding

---

## Android Mobile Module — Back Navigation (2026-06-04)

### Entry Points
All screens in the app — hardware back button on Android device.

### UI Trigger
Android physical/gesture Back button press (hardware event, not a UI element).

### Implementation

**Service:** `Frontend/src/app/core/services/back-button.service.ts`
**Package required:** `@capacitor/app@8.1.0` (installed 2026-06-04)
**Initialized by:** `AppComponent.constructor()` → `this.backButton.init()`

`init()` is async and guards with `Capacitor.isNativePlatform()` — no-op on web.
On Android it registers a single `App.addListener('backButton', ...)` listener app-wide.

### Business Rules

The listener fires on every Android back press with a `{ canGoBack: boolean }` payload from Capacitor reflecting the WebView's native history state.

Decision tree (evaluated in order):

1. **Blocked routes** — swallow the event entirely (do nothing):
   - `/live-session/room` — active live session
   - `/repractice/` — active repractice round
   Rationale: accidental back press during a session must not exit or navigate away.

2. **Root routes** — exit the app via `App.exitApp()`:
   - `/auth/login`
   - `/user/dashboard`
   - `/admin/dashboard`
   Also applies if `canGoBack === false` regardless of URL (no WebView history left).

3. **All other routes** — navigate back via `location.back()` (Angular `Location` service).
   This pops one entry from the WebView history, returning to the previous Angular route.

### Route Coverage

| Route | Back behavior |
|---|---|
| `/auth/login` | Exit app |
| `/auth/register` | → `/auth/login` |
| `/user/dashboard` | Exit app |
| `/user/*` (profile, progress, goals, settings, vocabulary, etc.) | → previous screen |
| `/session/history` | → `/user/dashboard` (via history) |
| `/session/create`, `/session/invite` | → previous screen |
| `/session/lobby/:id` | → previous screen |
| `/session/detail/:id`, `/session/report/:id`, `/session/review/:id` | → previous screen |
| `/scripts`, `/scripts/upload`, `/scripts/prepare/:id` | → previous screen |
| `/admin/dashboard` | Exit app |
| `/admin/*` | → previous screen |
| `/live-session/room/:id` | Blocked — no action |
| `/repractice/:id` | Blocked — no action |

### State Transitions
None — navigation only.

### Notes on Known Drift Prevented
Prior to 2026-06-04, no `@capacitor/app` listener existed. Capacitor's default Android behavior when no `backButton` listener is registered is to exit the WebView (close the app) on every back press — regardless of navigation history. This caused the reported issue where every back press closed the app instead of navigating to the previous screen. Fixed by registering a centralized `backButton` listener in `BackButtonService`.

---

## Frontend Loading & Skeleton Framework (2026-06-06)

### Purpose
Project-wide loading strategy eliminating partial renders, layout shift, flicker, and blank
sections on data-driven screens. Applies to Admin, User, Public, mobile and desktop. Three layers:

1. **Branded full-screen loader** — blocking ops only (initial bootstrap, login + redirect, route data).
2. **Top progress bar** — always-on, non-blocking ambient indicator for navigation + foreground HTTP.
3. **Skeletons + LoadingState wrapper** — per-section placeholders for cards/lists/tables/dashboards.

### Components (all standalone, OnPush)
- `LoaderService` (`@core/services/loader.service`) — ref-counted full-screen loader state + message; `show(msg?)`, `hide()`, `reset()`. Use ONLY for blocking flows; never for ordinary list/detail fetches.
- `LoaderComponent` (`app-loader`, `@shared/components/loader`) — branded full-screen overlay (animated brand ring + GoWithFlow wordmark + message). Mounted once in `AppComponent`. Driven by `LoaderService`.
- `HttpActivityService` (`@core/services/http-activity`) — signal counter of in-flight foreground requests; `isActive` computed.
- `loadingInterceptor` (`@core/interceptors/loading.interceptor`) — increments/decrements `HttpActivityService` per request. Skips background/streaming calls (`/hubs/`, `negotiate`, `/voice`, `/audio`) and any request marked via `withBackground()` context or `X-Background` header (header stripped before send). Registered FIRST in `withInterceptors([loadingInterceptor, authInterceptor])`.
- `TopProgressBarComponent` (`app-top-progress-bar`, `@shared/components/top-progress-bar`) — fixed 3px gradient bar; shows on router `NavigationStart` or HTTP activity. Anti-flicker: 120ms show-delay + 400ms min-visible. Mounted at top of `AppComponent` shell (outside scroll area → zero layout shift).
- Skeleton kit (`@shared/ui/skeleton`):
  - `SkeletonComponent` (`app-skeleton`) — primitive shimmer block; inputs `width`, `height`, `rounded`, `block`. Uses global `@keyframes gwf-shimmer` + `--gwf-skeleton-base/--gwf-skeleton-sheen` tokens (styles.scss). Honors `prefers-reduced-motion`.
  - Presets: `app-skeleton-text` (lines), `app-skeleton-card`, `app-skeleton-list` (rows; `header`/`avatar`/`trailing` toggles), `app-skeleton-stat-grid` (cols), `app-skeleton-table` (rows×cols).
- `LoadingStateComponent` (`app-loading-state`, `@shared/ui/loading-state`) — the scalable per-section pattern. Inputs `loading`/`error`/`empty` (+ messages, `retryable`, `hasErrorSlot`/`hasEmptySlot`); `retry` output. State precedence loading → error → empty → content. Content slot is NOT rendered until `loading` is false (Rule 1: no partial data). Projection slots `[skeleton]`, `[error]`, `[empty]`, default.

### Standard pattern for any data-driven screen (Rule 10 — future pages auto-follow)
```html
<app-loading-state [loading]="loading()" [error]="error()" [empty]="rows().length === 0"
                   emptyMessage="No records yet." (retry)="reload()">
  <ng-container skeleton><app-skeleton-list [rows]="6"></app-skeleton-list></ng-container>
  <!-- default slot: real content, rendered only when loaded -->
  @for (row of rows(); track row.id) { ... }
</app-loading-state>
```
Rules for new screens: (1) initialize the `loading` signal to `true` so the FIRST paint is the skeleton, not an empty-state flash; (2) set `loading=false` in BOTH `next` and `error`; (3) pick the skeleton preset whose geometry matches the real content to keep CLS ≈ 0; (4) use `LoaderService` only for blocking/whole-page operations.

### Reference conversions completed (2026-06-06)
- **User Dashboard** (`user-dashboard.component`) — data region gated on `showDashSkeleton = !dashboard() && dashboardLoading()` (skeleton card + 2 list skeletons); static greeting/quick-actions paint instantly; falls through to content + empty states on error (no infinite skeleton).
- **Admin Users** (`admin-users.component`) — **bug fixed**: `loading` initialized `false` and never set `true` on first load, so the initial mount flashed the "No users found" empty state before data; now initialized `true` and uses `app-skeleton-list`.
- **Login** (`login.component`) — branded `LoaderService.show('Signing you in…')` across the auth round-trip + redirect; hidden after `router.navigate(...).finally()`; destination skeleton takes over.

### Rollout completed across all data screens (2026-06-06)
Every screen in the original checklist has been brought to the standard. Disposition per screen
(build verified: `vite build` ✓ exit 0, `tsc --noEmit` ✓ exit 0):

**Converted to gate first paint (added `loading`/`error`, wrapped data region in `app-loading-state`
or gated skeleton, init `loading=true`, set false in next+error):**
- User `improvement-tracker` — was fully ungated (null `data()` flashed every section's empty state);
  now `loading`/`error` signals + `app-loading-state` (stat-grid + 2 card skeletons), retry → `loadData()`.
- User `session-detail` — was ungated (score ring rendered 0%, sections empty then snapped in); now
  `load(id)`/`reload()` with `loading`/`error`, `app-loading-state` (card + list skeleton).
- User `profile` — primary data comes from cached `UserStateService` signals; gated on `!profile()`
  with a skeleton (card + stat-grid + card) so a hard refresh doesn't flash a blank hero. Sign Out
  stays outside the gate (always available). `retryable=false` (no fetch to retry).
- Admin `user-detail-report` — **init bug fixed**: `loading` was `false` (flashed "No report data"
  before fetch); now `true`, header skeleton + body skeleton (2 tables + 2 cards).
- Admin `reports` — **same init bug fixed** (`loading` `false`→`true`); ad-hoc `animate-pulse` rows
  replaced with `app-skeleton-list`.

**Already gated correctly — only swapped spinner/ad-hoc placeholder for the shared skeleton kit:**
- User `my-mistakes` (already compliant, left as-is — uses shared `app-skeleton-list` + custom empty).
- User `interview-performance`, `learning-goals` — spinner → `app-skeleton-card`/`-list`.
- User `invitations` (`my-invitations`) — spinner → `app-skeleton-list`.
- User `session-history`, `script-library` — ad-hoc `animate-pulse` → `app-skeleton-list`.
- Admin `dashboard` — two panel spinners → `app-skeleton-list [bare]` (KPI inline pulses kept; they
  are geometry-matched and cause no shift).
- Admin `session-detail` — recording-panel spinner → `app-skeleton-card` (session data itself is
  passed synchronously via `history.state`, no flash).

**Intentionally NOT wrapped (no first-paint data fetch / special case — documented so a future pass
does not "fix" them):**
- `script-upload`, `create-session` — multi-step / single forms. No initial data region; their async
  ops (validate, save, prompt fetch, create) already show inline button/progress indicators. (Also
  fixed a header typo "UPOLAD"→"UPLOAD" in script-upload.)
- `lobby` — realtime screen. Null state already renders a coherent loading representation (hero
  "Loading session…", room code `———`, dashed "Open Slot" rows) and self-heals via SignalR + 3s
  poll. A hard skeleton gate over a continuously-polling critical path adds risk without UX gain.

The top progress bar + branded loader continue to apply globally with no per-screen work.

### Notes on Drift Prevented
- `LoaderService` previously existed with a `LoaderComponent` mounted in the shell but was NEVER invoked (`show()/hide()` had zero call sites) — dead global loader. Now driven by explicit blocking flows; ambient activity handled by the separate non-blocking top bar so the full-screen loader does not flicker on every XHR.
- Per-screen skeletons were ad-hoc `animate-pulse` divs duplicated across ~16 components with inconsistent geometry. Replaced by the shared skeleton kit; admin-users converted as reference.

---

## Android Mobile Module — Mobile Design Standards (2026-06-04)

### Reference Document
`Backend/Docs/MobileDesignAnalysis.md` — full audit, standards, and implementation priorities.

### Summary of Issues Identified (Pending Implementation Approval)

**Critical:**
- Text sizes `7px–10px` used throughout 30+ components — minimum readable size is 11px for labels/captions.
- Touch targets under 44px: `w-6/w-7/w-8/w-9` icon buttons, `h-10` select elements.
- Lobby hero card: `text-3xl` session name + join code risks overflow on 320–360px screens.
- User page bottom padding uses hardcoded `pb-28` (112px) — does not account for `env(safe-area-inset-bottom)`. Admin layout handles this correctly.

**High:**
- Missing `--gwf-secondary: #3D5A99` color token (used as undocumented second accent throughout live session and speaker screens).
- `hidden sm:block` hides session context info in live session topbar on all phones < 640px.
- Admin screens use HTML tables on mobile — need card-list alternative below 768px.

**Medium:**
- 40 of 51 components have no `.component.scss` file — responsive overrides rely entirely on Tailwind responsive prefixes.
- Hardcoded hex colors in templates bypass CSS variable system.
- `confirm()` browser dialogs should be replaced with in-app confirmation panels.

### Standing Mobile Design Rules (ENFORCED — apply to every new/edited page)
- **No horizontal scroll on list/data views at mobile widths.** A list/grid item must never require sideways scrolling to be read. Wide multi-column HTML `<table>`s (anything with `overflow-x-auto`) are **desktop-only**: gate the table with `hidden md:block` and provide a `md:hidden` **stacked card** alternative — one card per row, header (title + chips + action) over a `grid-cols-2` label/value metrics block. Preserve the table's per-cell color logic in the cards.
- Reuse the existing `@for (...) {} @empty {}` empty-state in both the table and the card list so both layouts handle "no results".
- Rationale: avoids the per-item horizontal scroll that breaks usability on 320–414px screens; satisfies the long-standing "admin tables need a card-list alternative below 768px" finding.
- **Applied:** `AdminScriptAnalyticsComponent` (`/admin/script-analytics`) — 9-column table now `hidden md:block` + `md:hidden` stacked metric cards (2026-06-08). Pattern to follow for all remaining admin tables (`admin-reports`, `admin-users`, etc.).

### Design Token Additions Required (in `styles.scss` and `_variables.scss`)
```css
--gwf-secondary:       #3D5A99
--gwf-secondary-dark:  #2D4580
--gwf-nav-bg:          #0D1526
--gwf-focus-bg-deep:   #121221
$breakpoint-xxs:       360px
```

### What Is Already Correct (do not regress)
- Bottom nav (`bottom-nav.component.scss`) — reference implementation, do not change. Now also powers the Admin shell footer (see "Shared Footer Navigation Architecture" below).
- Admin layout safe-area handling — `calc(84px + env(safe-area-inset-bottom, 0px))`.
- Live session room — `max(24px, env(safe-area-inset-bottom, 24px))` bottom padding.
- Speaker screen `questionFontSize` — clamp() based on utterance length.
- Login screen — clamp() throughout, min(100%, 400px) wrapper.
- Voice recorder mic button — `clamp(58px, 14vw, 70px)` correct responsive sizing.

### Implementation Applied (2026-06-04)

All Priority 1 and Priority 2 items from MobileDesignAnalysis.md were implemented and build verified:

**Global SCSS:**
- Added tokens: `--gwf-secondary: #3D5A99`, `--gwf-secondary-dark`, `--gwf-nav-bg: #0D1526`, `--gwf-focus-bg-deep: #121221`
- Added `.gwf-page-bottom` utility class: `calc(68px + env(safe-area-inset-bottom, 0px) + 16px)`
- Added `.gwf-page-content` and `.gwf-icon-btn` utility classes
- Added `$breakpoint-xxs: 360px` and `@mixin respond-xxs`, `@mixin safe-area-bottom`, `@mixin gwf-icon-btn`
- Corrected `$bottomnav-height` from 64px → 68px (matches actual nav height)

**Typography:**
- All `text-[7px]`, `text-[8px]`, `text-[9px]`, `text-[10px]` → `text-[11px]` across 42 files via mass replacement

**Lobby:**
- Session name: `text-3xl` → CSS class `lobby-session-name` with `clamp(18px, 5vw, 28px)`
- Join code: `text-3xl` → CSS class `lobby-join-code` with `clamp(20px, 5.5vw, 28px)`

**Session Room:**
- Settings + leave buttons: `w-9 h-9` (36px) → `w-11 h-11` (44px)
- Alert dismiss button: `w-6 h-6` (24px) → `w-11 h-11` (44px)
- Session context info: removed `hidden sm:block` — now always visible

**Touch targets:**
- Correction round close button: `w-8 h-8` → `w-11 h-11`
- Admin modal close button: `w-8 h-8` → `w-11 h-11`
- Admin table action buttons: `w-8 h-8` → `w-10 h-10`
- Script library, profile edit buttons: `w-8 h-8` → `w-10 h-10`
- Dashboard recommendation arrow: `w-8 h-8` → `w-10 h-10`

**Form inputs:**
- Create session selects: `h-10` (40px) → `h-12` (48px)
- Admin filter inputs/selects: `h-10` → `h-11` (44px) across 6 admin files

**Safe area:**
- All 16 user pages: `pb-28` (hardcoded 112px) → `gwf-page-bottom` (safe-area-aware dynamic)

**Admin tables:**
- Admin dashboard: card-list pattern added for `< 640px`, table remains for `>= 640px`

**Color tokens:**
- `login.component.scss`: `#3D5A99`, `#2D4580`, `#D32F2F`, `#6B7280`, `#1A1A2E` → CSS vars
- `admin-layout.component.scss`: `#0D1526` → `var(--gwf-nav-bg)`
- `bottom-nav.component.scss`: `#0D1526` → `var(--gwf-nav-bg)`
- Template inline styles: `#F59E0B` → `text-gw-warning`, `#E07B39` → `text-gw-accent`, `#2E7D32` → `text-gw-success`

### Full-Bleed Shell Routes + Login Redesign (2026-06-06)

**Problem fixed:** the user shell (`app.component`) wraps all non-admin routes in
`.user-content-area`, which forces `padding: 16px`. The shell/body background is
`--gwf-bg: #F4F6F9` (near-white). On self-contained full-screen routes (login, register,
live-session, repractice) the header and bottom nav are hidden, but the 16px padding still
showed the near-white shell background as a frame around each page's own full-screen surface,
and the extra vertical padding pushed a `100dvh` page past the viewport → unwanted scroll.

**Shell contract (`app.component.ts`):**
- `fullBleedRoutes = ['/auth', '/live-session', '/repractice']` — single source of truth.
- `isFullBleed()` = url matches that list. `showHeader()` / `showBottomNav()` now derive from it
  (header still also hidden on `'/'`). These three route states can no longer drift apart.
- Template binds `[class.flush]="isFullBleed()"` on `.user-content-area`.
- `.user-content-area.flush { padding: 0; }` — full-bleed pages own their entire layout +
  background; no light frame, no added height. Mirrors how admin routes render full-screen.
- Keep `.no-bottom-pad` (zeroes only bottom padding) for non-full-bleed pages with no nav.

**Login redesign (`auth/login`):** dark gradient theme retained (already audited as correct).
- Page shell: `min-height: 100%` (fills flush content area) with safe-area-aware padding
  (`max(clamp(...), env(safe-area-inset-top/bottom))`). Short-viewport (`max-height: 720px`)
  rule anchors to top + tightens gaps so the form never forces a scroll.
- Card: added "Welcome back" title + subtitle for hierarchy.
- Fields: leading Lucide icons (Smartphone, Lock) via `.field-control`/`.field-icon`; icon
  recolors to accent on input focus. Password field has a show/hide toggle button
  (`showPassword` signal, Eye/EyeOff) with `aria-label` + `title`.
- Mobile input hygiene: `inputmode="numeric"`, `autocomplete="tel" / "current-password"`,
  `enterkeyhint`, `id`/`for` label association.
- Added footer link "Create an account" → `/auth/register` (register already linked back to
  login; this closes the round-trip gap).
- Hardcoded `#5C35A8` → `var(--gwf-primary)` for logo/button.

**Notes on Known Drift Prevented:** header / nav / padding visibility were three independent
`hideOn` lists that could diverge; now all derive from `fullBleedRoutes`. The white-frame issue
was a shell-level bug affecting every full-bleed route, not a login-only CSS problem — fixing it
in the shell cleans up login, register, live-session and repractice in one place.

### Shared Footer Navigation Architecture (2026-06-05)

Single reusable footer used by BOTH the User and Admin shells. Previously the Admin shell
hardcoded its own dark footer (`.footer-nav`/`.footer-nav-item`/`.nav-pill`, Material icons,
`--gwf-nav-bg` background) inline in `admin-layout.component.html` — a second, divergent design
pattern. That inline footer and all its SCSS were removed; both shells now render the same
`<app-bottom-nav>` component and the same `bottom-nav.component.scss` design system.

**Component:** `shared/components/bottom-nav/bottom-nav.component.ts`
- Exports `interface BottomNavItem { label; path; icon (Lucide); exact? }`.
- `@Input() items?: BottomNavItem[]` — optional explicit tab set.
  - Omitted (User, mounted globally in `app.component`): uses default user tabs
    (Home / Review / Progress / History) + internal role gating
    (`role === 'USER'` and url not in `/auth /live-session /repractice /admin`).
  - Supplied (Admin, mounted in `admin-layout`): host owns visibility, so `showNav()` always
    returns true. Admin tabs: Dashboard / Users / Scripts / Reports / Cohorts.
- `gridTemplate` getter = `repeat(<items.length>, 1fr)` bound inline → bar auto-adapts to
  4 (user) or 5 (admin) columns; no hardcoded column count.
- Icons are Lucide for both shells (admin Material icons were mapped:
  dashboard→LayoutDashboard, people→Users, menu_book→BookOpen, bar_chart→BarChart3,
  groups→UsersRound).

**Design system (shared, light theme):** white bar, `height: calc(68px + safe-area-inset-bottom)`,
inactive `#9ca3af`, active `--gwf-primary` text + `--gwf-primary-light` pill, Lucide size 20,
top shadow `0 -2px 12px rgba(0,0,0,.06)`. Responsive: centered 480/520px strip ≥700/1024px,
icon/font shrink ≤360px (handles 5 admin tabs on small phones).

**No double footer:** `app.component` renders the global `<app-bottom-nav>` only when
`!isAdminRoute()`; admin routes render `admin-layout` which mounts its own `<app-bottom-nav [items]>`.

**Notes on Known Drift Prevented:** the two footers had drifted into separate design systems
(icon library, theme, active state, class names, column count, missing ≤360px responsive on admin).
Consolidating onto one component + one SCSS file means future footer changes apply to both shells
and cannot diverge again. Do not reintroduce a shell-specific footer; add/adjust tabs via the
`items` input instead.

---

### Dark-Blue Theme Conversion (2026-06-05) — REVERTED

STATUS: abandoned. The token flips in `index.css` and `styles.scss` were reverted by the user;
the app remains on the LIGHT theme (page `#F4F6F9`, white cards, dark text). The notes below are
kept only as reference for how a future dark-mode pass would be structured. Do NOT assume dark mode
is active — `bg-white` cards are correct in the current light theme.

Direction (originally approved, not shipped): full dark mode — deep-navy page + raised-navy surfaces
+ light text app-wide; eliminate white surfaces. Rollout was foundation + 3 flagship screens first
(Login, Dashboard, Session Room), then mass-apply.

**Two token systems drive the theme (both flipped to dark):**
- `src/index.css` — Tailwind v4 `@theme --color-gw-*` (drives every `bg-gw-*`/`text-gw-*`/`border-gw-*`
  utility) + `:root --gw-*`. Flipping these re-themes all utility-based surfaces automatically.
- `src/styles/styles.scss` — `:root --gwf-*` (drives component `.scss` files). Also fixed hardcoded
  light Material surfaces there (`.mdc-text-field--filled`, `.mat-mdc-header-row`, scrollbar thumb,
  `.badge-neutral`).

**Dark palette:** page `#0D1526`, raised surface `#161F38`, border `#25324F`, text `#EAEDF5`,
muted `#94A1BD`; success `#3DBB6B`, warning `#F5A623`, error `#F2545B`; accents kept (purple/orange/blue).

**Critical drift note — `bg-white` is NOT token-driven:** 256 `bg-white` Tailwind utilities across
41 files are hardcoded white and do NOT flip with the tokens. They must be converted per-screen,
typically `bg-white` → `bg-gw-card-bg`. NOTE: a `bg-white` substring match also catches `bg-white/5`,
`bg-white/[0.04]` etc. (translucent overlays on dark) and toggle-knob whites — those are CORRECT in
dark mode and must NOT be changed. Verify each occurrence is a solid surface before converting.

**Flagships done:**
- Login (`login.component.scss`): white card + light inputs/error banner → dark surfaces (page was already a dark-blue gradient).
- Dashboard (`user-dashboard.component.ts`): 11 `bg-white` cards → `bg-gw-card-bg`; brightened two low-contrast status colors.
- Session Room (`session-room.component.ts`): NO CHANGE — already fully dark (`bg-[#1A1A2E]` + `focus-mode`); all `bg-white` here are translucent overlays/knobs. The live-session/speaker/listener screens are already dark.

**Pending mass rollout (after approval):** convert remaining solid `bg-white` surfaces + hardcoded
light hex on the other ~38 screens (Profile, Review/my-mistakes, all Admin screens, session create/
history/detail, scripts, etc.) + per-screen mobile spacing/viewport tightening. Until then those
screens render dark-page/white-card (intentionally half-converted at the approval checkpoint).

### Admin Mobile Design System (2026-06-05)

All Admin list screens were redesigned to one mobile-first system modeled on the user
`session/history` screen. Single source of truth for Admin list UX — reuse, don't fork.

**Shared component:** `shared/components/admin-load-more/admin-load-more.component.ts`
(`<app-admin-load-more [loading] [hasMore] [loaded] [total] (more)>`). Replaces `mat-paginator`
on EVERY admin list with the same Load-More (append) pager — touch-friendly, no horizontal scroll.
Pagination model changed from page-replace (mat-paginator) to append: screens keep a private
`page`/`pageSize`, reset on filter change, and append on `loadMore()`.

**Standard layout (every admin list):** `max-w-lg mx-auto space-y-4` container → header row
(icon badge + title + count subtitle + primary action) → search/filter pills → loading skeletons
(`h-[68-72px] animate-pulse`) → empty-state card → single rounded list card
(`bg-white rounded-2xl border divide-y divide-gw-bg`) with rows
(`flex items-center gap-3 px-4 py-3.5`: icon/avatar + truncating meta + right-aligned status badge +
compact `w-9 h-9` action buttons) → `<app-admin-load-more>`. No `<table>`/`overflow-x-auto` anywhere.

**Per-screen specifics:**
- `admin/scripts` — table → list card; header/stats(3-up)/search kept; actions view/toggle/download.
- `admin/users` — Age Group FILTER removed (dropdown + `ageFilterControl` deleted; the Add/Edit
  modal's `ageGroup` form field is KEPT). Table → list card; filter is now All / Active-only pills;
  actions view/edit/toggle. Removed horizontal scroll.
- `admin/reports` — summary stat cards moved ABOVE filters; filters (date range + user) below;
  table → list card (avatar + sessions/improvement + score badge + View). Stats computed from
  loaded rows.
- `admin/cohorts` — single-column standardized cards within `max-w-lg`; added client-side search;
  create modal kept. No pager (getCohorts returns all).
- `admin/sessions` — table → list card (icon + name/members/code/date/score + status badge + View);
  filters are now search + status pills (All/Completed/Abandoned/In Progress) + date-range; paginator
  → Load-More (append). Stats (total/completed/avg) computed from loaded rows.

**Build note:** `MatPaginator` is now fully removed from the Admin module — the former shared
~234 kB paginator chunk no longer exists. Not a regression. All five admin list screens
(scripts, users, reports, cohorts, sessions) now share the one design system + `<app-admin-load-more>`.

---

## Session Room UX Redesign (2026-06-06 — ALL PHASES 0–5 IMPLEMENTED, build verified; APK-VERIFIED 2026-06-08)

Full audit + redesign plan: `Backend/Docs/SessionRoomRedesign.md`. Covers the live Session
Room only (`Frontend/src/app/modules/live-session/*`). All six phases are built and
build-verified.

**On-device verification (2026-06-08, Motorola IV2201 / Android 13):** drove a real 2-participant
live session into `/live-session/room/{id}` and screencapped every redesign surface — speaker hero
utterance + small grammar tag (Phase 3); sticky `.action-dock` with "Done Speaking" primary + Try
Again/Skip (Phase 2); first-run orientation hint (Phase 5); settings bottom-sheet exposing ONLY
Auto-Start Mic + Auto Submit with NO "Hear Speaker's Voice" toggle (Phase 0/1/6 capability gating
confirmed on native); in-app Leave sheet (Stay/Leave) replacing `confirm()` (Phase 5); responsive
stage `max-w-[480px] md:max-w-[680px] lg:max-w-[760px]` (Phase 4). All render correctly. NOTE: entering
the room requires 2 live participants (no single-speaker script exists → no solo start); a reusable
test account "Verify Bot" (9911946608 / 123456, userId 12) was provisioned for this.

### Phase 2–5 (layout, hierarchy, responsive, polish) — 2026-06-06
- **Sticky action dock** (`.action-dock`, `position: sticky; bottom: 0`, safe-area gradient)
  in `speaker-screen` + `listener-screen` — primary action always reachable; the stage
  (`flex-1 min-h-0 overflow-y-auto`) is the only scroll region.
- **Responsive stage** — container unlocked from hard `max-w-[480px]` to
  `max-w-[480px] md:max-w-[680px] lg:max-w-[760px]`, `px-4 md:px-6`, `pt-3 md:pt-5`.
- **Settings → bottom-sheet overlay** (`session-room`) — floats above content (grabber,
  backdrop, close) instead of the old in-flow dropdown that pushed the live turn down.
- **Leave → in-app sheet** (`showLeaveConfirm`, Stay/Leave) replacing native `confirm()`.
- **First-run orientation hint** (`showOrientation`, once per device via
  `gwf_session_room_seen`; role-aware copy).
- Removed misleading listener "Your Turn Is Next" footer (no next-speaker data exists).

### Implemented 2026-06-06 (Phase 0 + Phase 1) — build verified

**Phase 0 — capability source of truth.** New `SessionCapabilitiesService`
(`core/services/session-capabilities.service.ts`): `isNative` and
`canBroadcastVoice = !isNative`. Single place that decides whether WebRTC peer voice
broadcast is possible (false on native APK — recognizer owns the mic exclusively).

**Phase 1 — hide non-functional / misleading UI.**
- `session-room.component.ts`: "Hear Speaker's Voice" settings toggle is wrapped in
  `@if (capabilities.canBroadcastVoice)` — hidden on native APK where it was a no-op.
- `listener-screen.component.ts`: the animated sound-wave bars are now wrapped in
  `@if (voiceBroadcast.isReceivingAudio())` — they only render while real peer audio is
  actually streaming (never on native), instead of animating decoratively at all times.
  The "Live Audio" badge was already gated on `isReceivingAudio()`.
- `voice-broadcast.service.ts`: `startBroadcast()` and `handleBroadcastStarted()` now guard
  on `capabilities.canBroadcastVoice` instead of an inline `Capacitor.isNativePlatform()`
  check — removes the duplicated platform check that let "shown" drift from "works".

Refinement vs. original plan: Auto-Start Mic / Auto Submit are NOT hidden from listeners.
Turns rotate, so every participant is a speaker on their own turns → those prefs are
universally relevant. Only the platform-broken broadcast control was gated.

Key confirmed findings (validated against source):
- **No tablet/desktop layout exists** — room is locked to `max-w-[480px]` centered
  (`session-room.component.ts:167`).
- **"Hear Speaker's Voice" is dead on the APK.** `VoiceBroadcastService.startBroadcast()`
  returns early on `Capacitor.isNativePlatform()` (`voice-broadcast.service.ts:45-48`)
  because the native Google `SpeechRecognizer` (pronunciation scoring) needs exclusive mic
  access and Android cannot reliably share the mic. The settings toggle + listener "Live
  Audio" badge are still rendered on native → users enable a feature that can never work.
  This is the reported "voice/speaker feature shown on mobile but broken" bug. Fix = gate
  toggle + badge on a `canBroadcastVoice` capability (false on native).
- Listener sound-wave bars (`listener-screen.component.ts:71-79`) are a **fake CSS
  animation**, not real audio — misleading.
- `SessionPreferencesService` prefs are **not role-aware**: listeners see speaker-only
  toggles (Auto-Start Mic, Auto Submit); speakers on native see a dead broadcast toggle.

Proposed direction: 3-zone CSS grid shell (`auto / 1fr / auto`) with a pinned action dock
(fixes primary-action-below-fold scroll bug), role×platform-filtered settings, a single
`SessionCapabilities` source of truth, and responsive mobile/tablet/desktop layouts.

---

## Backend Session Recording Module — Consolidated Session Recording (Phase 16, 2026-06-05)

Backend implemented and building. Replaces the fragmented per-turn admin recordings view with ONE
consolidated `.m4a` per session, merged server-side from the existing per-turn audio segments
(shared with the personal Audio Archive). Design: `Backend/Docs/Dev/SessionRecordingArchitecture.md`.

### Entry Points
- Host toggle (lobby): `PATCH /api/sessions/{sessionId}/recording`
- Admin view: `GET /api/admin/sessions/{sessionId}/recordings`
- Trigger: `LiveSessionService.CompleteSessionAsync` (session completion) enqueues the merge.

### UI Trigger
Host enables "Record Session" in the lobby → persists `tblSession.recordingenabled = true`.
On session completion the merge is queued automatically. Admin opens a session → sees one recording.

### Request / Response Contracts
- `PATCH /api/sessions/{sessionId}/recording` — host-only. Body: `{ "enabled": bool }`.
  Success `200 { data: bool }`; non-host → `403` `"Only the session host can change recording…"`.
- `GET /api/admin/sessions/{sessionId}/recordings` — ADMIN+ActiveUser. Returns **single**
  `SessionRecordingDto` (or `data: null` if none):
  `{ recordingId, sessionId, status, audioUrl, format, durationSecs, sizeBytes, segmentCount,
  participants:[{userId,name,turns}], failureReason, createdAt, completedAt }`.
  `status` ∈ CAPTURING | PENDING_MERGE | PROCESSING | READY | FAILED. `audioUrl` is a presigned
  R2 URL (120-min) only when `status = READY`; null otherwise.

### Validation
- Toggle: caller must be the session host (`tblsession.hostuserid` match) — enforced in the UPDATE.
- Admin read: `[AdminOnly]` + `[ActiveUser]` policies.

### Database / Stored Procedures
- `ALTER TABLE tblsession ADD recordingenabled BOOLEAN NOT NULL DEFAULT FALSE` — the host flag was
  previously **frontend/localStorage only** (Gap-03 was never persisted server-side); the backend
  had no way to know recording was enabled. This column is required for any merge to fire.
- New table `tblsessionrecording` (one row per session, partial-unique on `sessionid WHERE isdeleted=false`):
  `recordingid, sessionid, storagekey, status, format, durationsecs, sizebytes, segmentcount,
  participantsjson(jsonb), failurereason, attemptcount, createdat, completedat, expiresat, isdeleted`.
- Migrations: `Migrations/PostgreSQL/AddSessionRecording_Phase16.sql` (production/Supabase) +
  `Migrations/SqlServer/AddSessionRecording_Phase16.sql` (dev parity). **Not yet run against Supabase.**
- Segments source: reuses `IAudioArchiveRepository.GetAllBySessionAsync` (ordered by turn) — no new
  segment table. `AudioArchiveItemDto` gained `UserId` (mapped from `aa.userid`) for participant rollup.
- Raw-SQL repository (`SessionRecordingRepository`) targets PostgreSQL, matching the
  `GetAllBySessionAsync` precedent (lowercase columns, `now() AT TIME ZONE 'utc'`, `::jsonb` cast).

### Business Rules (merge pipeline)
1. On completion, if `recordingenabled` → `CreateOrGetAsync` inserts a `PENDING_MERGE` row
   (`createdat` = session `StartedDate`, `expiresat` = +90 days) and enqueues `sessionId`.
2. Merge worker claims the row via `TryClaimForMergeAsync` (PENDING_MERGE/FAILED → PROCESSING,
   `attemptcount++`). READY/already-claimed rows are skipped (single-merge idempotency).
3. Download ordered segments from `gwf-audio`; ffmpeg concat filter
   (`[0:a][1:a]…concat=n=N:v=0:a=1,loudnorm[out]`) → AAC 96 kbps, 48 kHz mono, `+faststart` `.m4a`.
4. ffprobe duration; upload final to `sessions/{sessionId}/recording/session_{sessionId}.m4a`;
   `MarkReadyAsync` sets storagekey, duration, size, segmentcount, participantsjson, `completedat`.
5. Zero segments → `MarkFailedAsync "No audio segments were captured for this session."`.

### State Transitions
`tblsessionrecording.status`: (insert) PENDING_MERGE → PROCESSING → READY | FAILED.
FAILED is re-claimable (retry) until `attemptcount >= 3`.

### Realtime Events
None. Admin polls/refreshes; merge is async and may be PROCESSING when first viewed.

### Failure Cases / Recovery
- ffmpeg missing on host → `MarkFailedAsync` with "Is ffmpeg installed and on PATH?"; row retryable.
- Merge exception → row FAILED with truncated reason; segments retained for re-merge.
- Worker restart → `SessionRecordingMergeWorker.RecoverPendingAsync` re-enqueues PENDING_MERGE/FAILED
  rows (`attemptcount < 3`) on startup (queue is in-process, non-durable).
- Recording never breaks session completion: `EnsureRecordingQueuedAsync` is fully self-guarding.

### Retention
`SessionRecordingRetentionWorker` (daily): deletes expired finals from R2 then soft-deletes the row
(`expiresat <= now`, 90-day default).
DI lifetime: the worker is a singleton hosted service. It constructor-injects ONLY singleton/options
deps (`IServiceScopeFactory`, `IOptions<CloudflareR2Settings>`, `ILogger`). Every scoped dependency —
`ISessionRecordingRepository` AND `IStorageService` — is resolved from `_scopeFactory.CreateScope()`
inside `PurgeExpiredAsync`, never via the constructor.

### Ops / Config
- ffmpeg + ffprobe must be installed on the API host (or bundled). Paths overridable via
  `appsettings`: `Ffmpeg:FfmpegPath`, `Ffmpeg:FfprobePath` (default `ffmpeg`/`ffprobe` on PATH).
- DI: `ISessionRecordingRepository`/`Service` (scoped), `IStorageService` (scoped),
  `ISessionRecordingMergeQueue` (singleton), two `AddHostedService` workers (singletons).
  Storage gained `IStorageService.DownloadToAsync`.
- DI lifetime rule for BOTH workers: a hosted service is a singleton, so it must NOT constructor-inject
  any scoped service (`IStorageService`, repositories, `ISessionRecordingService`). Resolve those from a
  per-iteration `IServiceScopeFactory.CreateScope()`. Violating this throws at startup:
  "Cannot consume scoped service ... from singleton IHostedService" (DI validate-on-build).

### Notes on Known Drift Prevented
- The host "Record Session" flag was documented (Gap-03) but never persisted server-side — corrected
  here with `recordingenabled` + the host-only PATCH endpoint. Without it the feature is inert.
- Personal Audio Archive (per-user, opt-in, private) is unchanged and coexists; the consolidated
  recording is a separate admin/host artifact built from the same segments.
- DI drift (fixed): `SessionRecordingRetentionWorker` originally constructor-injected the scoped
  `IStorageService`, which crashed API startup with `InvalidOperationException: Cannot consume scoped
  service 'IStorageService' from singleton IHostedService`. Fixed by resolving `IStorageService` from
  the same per-sweep scope as the repository. `SessionRecordingMergeWorker` was already correct
  (resolves `ISessionRecordingService` per scope). Rule documented above to prevent recurrence.

### Frontend (Angular — implemented, `vite build` green)
- **Lobby state** now surfaces `recordingEnabled` (`SessionService.GetLobbyStateAsync` reads
  `ISessionRecordingRepository.GetRecordingEnabledAsync` — no stored-proc change). `LobbyStateResponseDto.RecordingEnabled`.
- **Host toggle** (`lobby.component.ts`) persists via `PATCH /api/sessions/{id}/recording`
  (optimistic + revert on failure) and sets `AudioArchiveService.sessionRecordingEnabled`.
- **All-participant capture:** `AudioArchiveService.shouldCapture()` = host session recording OR
  personal consent. `speaker-screen` gates `enableAudioCapture` + clip upload on `shouldCapture()`,
  so every participant uploads turn clips when the host records (was: only opted-in users).
- **Facilitator capture:** `VoiceRecognitionEngine.startStandaloneCapture/stopStandaloneCapture`
  (standalone `MediaRecorder`, no recognition) — `speaker-screen` starts it on facilitator
  read-aloud turns and uploads on "Done". (On Capacitor native this now uses
  `capacitor-voice-recorder` instead of getUserMedia — see "KNOWN FAILURE … / Fix applied (2026-06-06)".)
- **Admin UI** (`admin-session-detail.component.ts`): music-player redesign of the single
  `SessionRecordingDto` — play/pause, scrubber, ±10s, status states (Ready/Processing/Failed/None),
  participant chips, download. `AdminService.getSessionRecording` returns the single object.
  - FAILED state: when `failureReason` contains "no audio segments" (computed `isNoSegmentsFailure`),
    an extra italic hint is shown ("…participants may have used an app version that couldn't capture
    audio") so admins don't misread the capture-platform limitation as a server/merge bug. (2026-06-06)
    A manual "Retry merge" admin action was considered but NOT implemented (retries remain automatic, capped at 3).

### Ops status (2026-06-05)
- **Migration: DONE** — `AddSessionRecording_Phase16.sql` applied & verified on production Supabase
  (`recordingenabled` column, `tblsessionrecording` table, unique index all confirmed). Idempotent.
  (SQL Server file is dev-parity only; this deployment is PostgreSQL.)
- **ffmpeg on Render: DONE in image** — `Backend/Dockerfile` runtime stage now `apt-get install -y ffmpeg`.
  Requires a **Render redeploy** to take effect. `appsettings.json` has an `Ffmpeg` block
  (`FfmpegPath`/`FfprobePath` default to PATH).
- **Local dev ffmpeg** — install for local testing only (`winget install Gyan.FFmpeg`); not needed for prod.

### KNOWN FAILURE — No audio captured on the mobile app (verified 2026-06-06, session 97)

**Symptom:** `GET /api/admin/sessions/{id}/recordings` returns `status=FAILED`,
`failureReason="No audio segments were captured for this session."` even though recording was
enabled and a full session ran.

**Verified diagnosis (production Supabase + R2, session 97):**
- `tblsession.recordingenabled = TRUE`, `status=COMPLETED`, `actualdurationsec=618` (real 10-min session).
- `tblvoiceanalysis` = **28 rows** for session 97 → turns happened and **recognition worked**.
- `tblaudioarchive` for 97 = **0 rows**; R2 `gwf-audio/sessions/97/` = **0 objects** → **no clip was ever
  uploaded**. `tblsessionrecording` = FAILED, `attemptcount=3` (retries exhausted, working as designed).
- Contrast: session 95 (web) = 16 turns + 7 clips ✓; sessions 96 & 97 (app) = turns but 0 clips ✗.

**Root cause (SAVE/capture side, NOT get side — the merge query is correct):** turn-audio capture
relies on the browser `MediaRecorder` fed by `navigator.mediaDevices.getUserMedia({audio:true})`,
opened **concurrently with the active speech recognizer** in
`voice-recognition.engine.ts` (`startRecording`, captureAudio branch ~L360, and
`startStandaloneCapture` which already early-returns on native). On the **Capacitor native Android
app** the native `@capacitor-community/speech-recognition` plugin owns the mic, so the WebView
`getUserMedia` call throws and is **silently swallowed** (`catch {}`) → `lastAudioBlob` stays null →
the upload at `speaker-screen.component.ts` L233 is skipped → zero segments → merge FAILED. Android
does not allow a second `AudioRecord`/`MediaRecorder` session while the native `SpeechRecognizer`
holds the mic, so per-turn capture + native recognition cannot run simultaneously as currently
architected. Net: **consolidated session recording cannot capture audio on the native app**; it only
works on the web client. No native voice-recorder plugin is installed (only speech-recognition).

**Secondary fragility (web) — FIXED 2026-06-08:** the capture flag
`AudioArchiveService.sessionRecordingEnabled` is an in-memory root-singleton previously set ONLY in
`lobby.component` from lobby state, so a mid-session page reload / deep-link into `/live-session`
(which bypasses the lobby) reset it to false → web turn-clip capture silently stopped (unless personal
archive consent was on). Fixed by re-deriving it from the server on every room entry:
`session-room.component.ts` `initSession()` now calls `SessionService.getLobbyState(sessionId)` and
sets `audioArchiveSvc.setSessionRecordingEnabled(state.recordingEnabled)`. Best-effort — a failed
lookup leaves the existing flag/consent untouched. No backend/DTO change (lobby endpoint already
returns `recordingEnabled` post-start). Web build verified green.

**Fix applied (2026-06-06) — native audio capture added; ON-DEVICE TESTED 2026-06-08 on IV2201/Android 13 → CONCURRENT CAPTURE NOT VIABLE (starves recognition; see validation result below):**
- Added dependency `capacitor-voice-recorder@7.0.6` (peer `@capacitor/core >=7.0.0`, satisfied by 8.4.0).
  Web build green. Android `RECORD_AUDIO` + `MODIFY_AUDIO_SETTINGS` already in the manifest — no manifest change.
- `voice-recognition.engine.ts` now captures turn audio on native via the plugin (web path unchanged):
  - New `startNativeClipCapture()` / `stopNativeClipCapture()` (base64 → Blob → `lastAudioBlob`),
    plus `base64ToBlob`. Field `nativeClipCapturing`.
  - **Recognition turns:** recorder is started ONLY after recognition is confirmed running
    (inside `confirmLanguage`) and stopped/collected in the single `settle()` finalize gate
    BEFORE the promise resolves (so the speaker screen's upload at L233 sees the blob). Strictly
    best-effort and wrapped in try/catch — if a device disallows capture concurrent with the
    native `SpeechRecognizer`, recognition/scoring is unaffected (degrades to the old no-capture
    behaviour). `settle()` adds zero delay when no capture was active.
  - **Facilitator read-aloud turns:** no recognizer runs, so the mic is free — `startStandaloneCapture`
    /`stopStandaloneCapture` now use the plugin on native (was a hard no-op). Reliable here.
  - `stopSession()` also stops any active native capture (idempotent).
- **Open risk / next step:** Android generally disallows a second mic capture while the system
  `SpeechRecognizer` is active, so concurrent capture on recognition turns is DEVICE-DEPENDENT.
  Facilitator turns should always capture. Requires `npx cap sync android` + APK rebuild + on-device
  test (run a recording-enabled session, then confirm `tblaudioarchive`/R2 `sessions/{id}/turns/` fill
  and the recording goes READY). If recognition turns don't capture on target devices, the fallback
  is a record-then-recognize re-architecture (single mic owner) — not done here.
- Clip key/format: native blob is `audio/aac`; stored under the existing `.webm`-suffixed key
  (`StorageKeyBuilder.AudioArchiveClip`). The ffmpeg merge re-encodes and detects container by
  content, so the cosmetic extension mismatch is harmless.

**ON-DEVICE CAPTURE HARNESS (added 2026-06-08) — repeatable test without a live session:**
The Speech Debug screen (`modules/user/speech-debug/speech-debug.component.ts`, route
`/user/speech-debug`, reachable via Settings → Diagnostics → "Speech & Capture Test") now drives the
EXACT native concurrent-capture path in isolation — no lobby / second participant / full session
needed. It has a "Capture turn audio" toggle that calls `engine.enableAudioCapture()` before
`startSession()`, then after the turn reads `engine.lastAudioBlob` and reports captured size + MIME,
plays it back in-app (proves real audio, not silence), and optionally uploads it to the real
`POST /api/users/audio-archive` endpoint (Session ID + Turn inputs; FK requires a real session the
user owns) to validate the full save path that was returning zero segments. Use this to verify item
#4 below instead of running a recording-enabled multi-person session each time.

**>>> ON-DEVICE VALIDATION RESULT (2026-06-08) — CONCURRENT CAPTURE STARVES RECOGNITION (NOT VIABLE):**
Validated via the capture harness on **Motorola IV2201, Android 13** (`navigator.language=en-GB`,
en-GB SODA offline pack v3071 installed). Three runs, decisive A/B:
- **Capture ON:** `capacitor-voice-recorder` DID produce an `audio/aac` clip, BUT the native SODA
  `SpeechRecognizer` returned `NO_SPEECH_DETECTED` (logcat: `RecognitionClient #onRecognitionFinished
  no speech - erroring`; the captured base64 was long runs of constant bytes ≈ silence). Recognition
  failed to the 30 s hard ceiling → "No speech detected."
- **Capture OFF (control):** recognition worked perfectly — interim streamed
  "good"→"good morning how are you", **Overall 84 / Fluency 83 / Confidence 85**, all words matched,
  finalized in 3.8 s.
**Conclusion: the native recorder and the SODA recognizer CONTEND for the single mic.** When the
recorder opens the mic, the recognizer is starved (gets silence). The engine's "best-effort capture
never affects recognition" assumption is FALSE on this device — enabling session recording would break
recognition/scoring for EVERY speaker turn. Android only grants live mic input to one consumer at a
time. **The concurrent-capture approach is NOT viable; the record-then-recognize fallback (item #5)
IS required.** (Earlier same-day note claiming capture worked concurrently was WRONG — recognition was
empty in that run too, due to the same starvation, not the Stop button.)

**>>> DESIGN DECISION REQUIRED (architecture — do not silently re-architect):** native session
recording of SPEAKER turns cannot coexist with on-device recognition. Options:
  (a) **Facilitator-only capture on native** — only the facilitator read-aloud turns (no recognizer)
      are captured; speaker-turn audio is omitted from the consolidated recording on the app. Safe,
      simple, recognition never affected. Consolidated recording is incomplete (no learner audio).
  (b) **Record-then-recognize** — recorder owns the mic for the whole turn; recognition no longer uses
      the live SODA mic but transcribes the recorded clip (requires a file-based/cloud STT — large
      change; current pipeline is live-mic only).
  (c) **Disable consolidated session recording on native entirely** — document as a platform limit;
      only the web client (MediaRecorder, no native recognizer) supports it.
  (d) **Research** whether `@capacitor-community/speech-recognition` (or a custom plugin) can emit the
      recognizer's OWN audio buffer (single mic owner, no second AudioRecord). Unknown if supported.

**>>> PENDING ACTIONS:**
1. [x] DONE — `npm install` / `capacitor-voice-recorder@7.0.6` present.
2. [x] DONE — `npm run build` + `npx cap sync android` (3 native plugins registered incl. voice-recorder).
3. [x] DONE — debug APK rebuilt + installed on IV2201 (Java 21).
4. [x] DONE (via harness) — on-device behaviour characterised: capture works in isolation BUT starves
       recognition when concurrent (see validation result above). The original goal (capture speaker
       audio while recognising) is BLOCKED by mic contention.
5. [x] DONE 2026-06-08 — chose option (a) Facilitator-only + protect recognition. The native
       concurrent-capture call was removed from `confirmLanguage()` in `voice-recognition.engine.ts`
       (speaker/recognition turns no longer open a native recorder), so the recognizer always owns the
       mic. Facilitator read-aloud capture via `startStandaloneCapture` (no recognizer) is unchanged;
       web capture (MediaRecorder) unchanged. **Verified on IV2201 with capture toggle ON:** recognition
       succeeded (Final "good morning how are you", Overall 84, finalized 3.85 s) AND
       `lastAudioBlob` was null (no capture) — recognition protected. Net: on the app, the consolidated
       session recording contains FACILITATOR audio only; speaker-turn audio is intentionally omitted.
       To capture speaker audio in future, a single-mic-owner re-architecture (option b/d) is required.
6. [x] DONE 2026-06-08 (separate web bug) — `AudioArchiveService.sessionRecordingEnabled` is now
       re-derived from the server on every room entry via `SessionService.getLobbyState` in
       `session-room.component.ts` `initSession()`, so a mid-session reload no longer stops web
       capture. Frontend-only, build verified. Independent of the native work above.

Note: session 97 (and 96) cannot be recovered — their source turn clips were never captured. This
fix only affects sessions recorded AFTER the new APK is installed.

---

## Secret Management & Security Architecture (Review 2026-06-06 — Remediation PENDING)

STATUS: **Security review complete. Remediation NOT yet applied. P0 credential rotation is mandatory
and must be done by a human in the Cloudflare / Supabase dashboards.** This section is the single
source of truth for the next "secrets hardening" phase — it captures findings, evidence, the target
architecture, and the migration plan so the work can be executed without re-auditing.

### Scope reviewed
`appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json`,
Frontend `environment.ts` / `environment.prod.ts`, `Backend/Dockerfile`, `Backend/.gitignore`,
`Program.cs` config wiring (`IConfiguration` → `IOptions<JwtSettings>`/`<CloudflareR2Settings>`).

### CRITICAL findings — live secrets committed in plaintext
The following live credentials are present in source-controlled config and MUST be treated as
**compromised** (Render deploys from Git → assume they are in remote history):

| Secret | Location | Notes |
|---|---|---|
| Supabase Postgres password (full DSN, prod DB, **superuser** `postgres.<project>`) | `appsettings.json` ConnectionStrings:PostgreSQL; `appsettings.Development.json` | grants full DB access to all user data |
| Cloudflare R2 `AccessKeyId` + `SecretAccessKey` | `appsettings.json` CloudflareR2; `appsettings.Development.json` | read/write/delete on gwf-audio, gwf-avatars, gwf-scripts, gwf-exports |
| JWT `SecretKey` (HS256 symmetric, weak/guessable literal) | `appsettings.json` JwtSettings | **forge any user/ADMIN token → bypasses ALL authorization**; highest impact |

Root cause: `Backend/.gitignore` deliberately force-tracks the secret files:
```
appsettings.*.json
!appsettings.json              # re-includes base (live R2 + DB + JWT)
!appsettings.Development.json  # re-includes Development (live R2 + DB)
```

### Secondary findings
- **Docker image leakage:** `Dockerfile` does `COPY . .` then `dotnet publish`, baking
  `appsettings.json` (with secrets) into image layers. Env-var overrides change *values used* at
  runtime, not *files shipped* — anyone pulling the image can extract the secrets. Need a
  `.dockerignore`.
- **Prod connection-string drift:** `Program.cs` reads `GetConnectionString(databaseProvider)` where
  `databaseProvider == "PostgreSQL"` (`builder.Configuration["DatabaseProvider"]`). But
  `appsettings.Production.json` defines `ConnectionStrings:DefaultConnection` — which is **never read**.
  Production MUST set env var `ConnectionStrings__PostgreSQL`; otherwise the app silently falls back to
  the committed Supabase prod DSN. Fix the placeholder name to `PostgreSQL`.
- **Least privilege:** app logs in as the Supabase superuser; should use a scoped app role.
- **Data Protection keys:** ASP.NET Core key ring is likely on Render's ephemeral filesystem → resets
  across deploys (invalidates issued tokens). Persist to a Render Disk or DB.
- **No secret scanning** in pre-commit / CI.

### Verified SAFE (do not regress)
- **Frontend bundle is clean.** `environment.ts` / `environment.prod.ts` contain only public API/WS
  URLs (`https://gowithflow-api.onrender.com`). No keys reach the browser. JWT key is symmetric and
  used server-side only — never sent to the client. (Requirement: "no secret in frontend bundle" — PASS.)
- `appsettings.Production.json` already uses placeholders and is git-ignored.
- Env-var override path works: `WebApplication.CreateBuilder` layers Environment Variables over
  appsettings (double-underscore = section nesting, e.g. `JwtSettings__SecretKey`,
  `CloudflareR2__SecretAccessKey`, `ConnectionStrings__PostgreSQL`).

### Sensitive-data inventory (never plaintext / never in Git)
1. DB connection string + password (Supabase Postgres)
2. JWT `SecretKey` (+ any refresh-token encryption key if added)
3. Cloudflare R2 `AccessKeyId` + `SecretAccessKey`
4. Future third-party keys (OTP/SMS/email provider, payments, server-side LLM/Claude key for the
   prompt helper, hosted log-sink credentials)

Non-secret (safe in config/Git): bucket names, endpoints/URLs, token TTLs, CORS allow-list,
file-size limits, ffmpeg paths, JWT `Issuer`/`Audience`.

### Target architecture (decision)
- **Secret store:** Baseline = **Render Environment Groups + Secret Files** (native to host, free,
  already supported by the config layering). Target = **Doppler or Infisical** as the central source
  of truth that syncs to Render + CI + local dev (versioning, RBAC, audit, rotation reminders).
- **Rejected for current scale:** Azure Key Vault / AWS Secrets Manager (not on those clouds),
  HashiCorp Vault (ops overhead). Revisit only if consolidating onto a single cloud.
- **Local dev:** **.NET User Secrets** (`dotnet user-secrets`, `UserSecretsId` in the API csproj) or
  Doppler local injection — never shared appsettings files.
- **App-level encryption:** NOT required for the secrets themselves (the store encrypts at rest;
  custom crypto just adds another key to protect). DO use ASP.NET Core **Data Protection** (persisted
  key ring) for app-issued tokens, and consider **column-level encryption** (pgcrypto / envelope) for
  sensitive user PII at rest (mobile numbers used for login, audio metadata) — a separate data-at-rest
  concern from secret management.
- **Rotation:** JWT — dual-key overlap (accept old+new, sign new, then drop old) on a 90-day cadence;
  R2 — new key pair → deploy → revoke old; DB — rotate in Supabase + switch to least-privilege role.

### Migration plan (phased — next phase executes Phase 0/1 first)
- **Phase 0 — Contain (HUMAN, do first):** rotate JWT key, R2 key pair, and DB password (all burned);
  create a least-privilege Supabase app role (stop using superuser).
- **Phase 1 — Repo hygiene (code/config, agent can do):** replace real values in `appsettings.json` +
  `appsettings.Development.json` with placeholders; fix `.gitignore` (drop the `!appsettings.json` /
  `!appsettings.Development.json` re-includes, keep base appsettings secret-free); add `.dockerignore`
  excluding dev/local secret files; add `gitleaks` pre-commit + CI gate. Purge Git history
  (`git filter-repo`/BFG) OR rely on Phase 0 rotation to neutralize leaked values.
- **Phase 2 — Local dev:** move dev secrets to .NET User Secrets (`UserSecretsId`).
- **Phase 3 — Runtime config:** set Render env vars (`ConnectionStrings__PostgreSQL`,
  `JwtSettings__SecretKey`, `CloudflareR2__AccessKeyId`, `CloudflareR2__SecretAccessKey`) in an
  Environment Group; fix the `DefaultConnection`→`PostgreSQL` placeholder mismatch; then (target) wire
  Doppler/Infisical → Render.
- **Phase 4 — Harden:** persist Data Protection key ring off ephemeral disk; implement JWT dual-key
  rotation; confirm prod relies on env only (no secret files in image).

### Risk register
| # | Risk | Severity |
|---|---|---|
| R1 | Forgeable admin JWTs via committed weak symmetric key | Critical |
| R2 | Live R2 storage credentials in Git | Critical |
| R3 | Live prod DB superuser password in Git | Critical |
| R4 | `.gitignore` force-tracks secret files (root cause R1–R3) | High |
| R5 | Secrets baked into Docker image layers | High |
| R6 | Prod falls back to committed DSN if `ConnectionStrings__PostgreSQL` unset (placeholder mismatch) | High |
| R7 | App DB login uses superuser, not least-privilege role | Medium |
| R8 | Data Protection keys likely ephemeral on Render | Medium |
| R9 | No secret scanning in CI/pre-commit | Medium |
| — | Frontend bundle secret exposure | None found (PASS) |

### Notes on Drift Prevented
- Documents the committed-secret exposure and the `appsettings.Production.json` `DefaultConnection`
  vs. code `PostgreSQL` mismatch so the next phase does not re-discover them. When Phase 1/3 land,
  update the CRITICAL findings table to "remediated" and record the new env-var contract here.
