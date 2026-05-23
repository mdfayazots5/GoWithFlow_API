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
  - URL segment versioning at `/api/v{version}`
  - default API version `1.0`
  - reports supported/deprecated versions in response headers
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

- Self-service user APIs under `/api/v1/users`
- Dedicated dashboard API under `/api/v1/dashboard`
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

#### GET /api/v1/dashboard

- Returns: user name, streak, today date, active session banner, pending repractice count, last 3 sessions, last 3 unresolved mistakes
- `activeSession` prefers latest unexpired session where `tblSession.Status` is `LOBBY` or `ACTIVE`
- Dashboard fallback ignores `tblSessionMember.IsActive` — banner visible until `tblSession.RoomExpiresAt` passes
- `ACTIVE` sessions preferred over `LOBBY`; null if neither

#### GET /api/v1/users/profile

- Returns: profile fields plus computed totals (sessions, last-30-day average fluency, resolved mistakes)

#### PUT /api/v1/users/profile

- Request DTO: `UpdateProfileRequestDto`
- Updates: full name, email, age group, preferred hint language, avatar URL

#### POST /api/v1/users/profile/avatar

- Multipart form file; saves to `wwwroot/avatars`; updates `tblUser.AvatarUrl`; returns relative avatar URL

#### GET /api/v1/users/sessions/{sessionId}/detail

- Returns: session header, caller performance summary, caller mistake list, listener feedback received, all member scores

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

#### GET /api/v1/users/streak

- Returns: current streak, longest streak, last 30 streak rows

#### GET /api/v1/users/badges

- Returns: all earned badges for the authenticated user

### User Business Logic

- `DailyStreakCount` represents practice streak from completed session dates, not login count
- `CompleteSessionAsync` upserts one streak record per active member when session moves to `COMPLETED`
- `uspUpsertUserStreak` increments `tblUser.TotalSessionsPlayed`
- Badge rules: `7_DAY_STREAK` when `DailyStreakCount >= 7`; `10_SESSIONS` when `TotalSessionsPlayed >= 10`; `50_MISTAKES_FIXED` when resolved mistakes reach 50

### Application and Infrastructure Wiring

- Dashboard: `UserDashboardController`, `IUserDashboardService`, `UserDashboardService`
- User: `UserController`, `IUserService`, `UserService`, `IUserRepository`, `UserRepository`
- `FileStorageSettings` controls avatar directory and max upload size
- Session completion in `LiveSessionService` calls streak upsert and badge evaluation
- **Important:** PostgreSQL SPs are NOT managed via EF migrations. Apply `Docs/PostgreSQLMigration/*.sql` scripts directly to Supabase when SP changes are needed.

### Migration State

- `AddUserModule_Phase7`
- `RelaxImprovementSP_IncludeAbandoned` (SQL Server only — now has PostgreSQL guard)
- `FixImprovementSP_PostgreSQLTypeSafety` (PostgreSQL only — fixes 500 on /api/users/progress)

---

## Backend Admin Module

### Module Scope

- API base route: `api/v1/admin`
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

- `GET /api/v1/admin/dashboard` — global totals, recent activities, top grammar mistakes
- `GET /api/v1/admin/users` — paginated admin user list
- `GET /api/v1/admin/users/{userId}` — full user profile with averages and recent sessions
- `PATCH /api/v1/admin/users/status` — updates `tblUser.IsActive`
- `POST /api/v1/admin/users/notes` — inserts admin note using admin JWT claim
- `GET /api/v1/admin/users/{userId}/notes` — active admin notes for target user
- `GET /api/v1/admin/reports` — paginated user report summaries; `ImprovementPercent = (resolved / total) * 100`
- `GET /api/v1/admin/reports/users/{userId}` — user header, session history, mistake breakdown, weekly scores
- `GET /api/v1/admin/reports/export` — exports summary rows as Excel bytes via `ClosedXML`

### Admin Users List — Stable Flow Contract

#### Entry Points
`/admin/users` — Angular standalone route `AdminUsersComponent`

#### Request Contract
Endpoint: `GET /api/v1/admin/users`
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

- API base route: `api/v1/scripts`
- Purpose: validate admin-uploaded Excel content, upload script metadata and utterances, expose script library and version history, generate sample `.xlsx` template

### Database Schema

#### tblScript

- Primary key: `ScriptId BIGINT IDENTITY(1,1)`
- Business columns: `ScriptTitle NVARCHAR(128) NOT NULL`, `Category NVARCHAR(64) NOT NULL`, `GrammarFocusTag NVARCHAR(64) NOT NULL`, `ContextTag NVARCHAR(64) NOT NULL`, `ComplexityLevel TINYINT NOT NULL`, `TargetAgeGroup NVARCHAR(32) NOT NULL`, `HintLanguage NVARCHAR(32) NOT NULL`, `IsActive BIT NOT NULL DEFAULT(1)`, `UploadedDate DATETIME2 NOT NULL DEFAULT(GETDATE())`, `UploadedByUserId BIGINT NOT NULL`, `Version INT NOT NULL DEFAULT(1)`, `UtteranceCount INT NOT NULL DEFAULT(0)`
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
- Six registered categories: `Grammar Drill`, `Roleplay`, `Mock Interview`, `Vocabulary Sprint`, `Fluency Drill`, `Repractice Round`
- Each category defines: fixed speaker labels, mandatory columns (D/G/H), row count limits, content rules, metadata defaults, and sample data
- Speaker labels by category: GrammarDrill → `Speaker A/B`; Roleplay → role-based; MockInterview → `Interviewer/Candidate`; VocabularySprint → `Tutor/Learner`; FluencyDrill → `Speaker A/B`; RepracticeRound → `Coach/Learner`
- Column D (HintText) mandatory for: `Vocabulary Sprint`, `Repractice Round`
- Column G (FocusWord) mandatory for: `Mock Interview`, `Vocabulary Sprint`
- Column H (PronunciationNote) mandatory on Tutor rows for: `Vocabulary Sprint`
- File naming convention: `[CategoryCode]_[slug]_v[Version]_[YYYY-MM-DD].xlsx`
- AI generation output format: JSON with `metadata` block and `rows` array — spec in `ExcelTemplateStandard.md §10`
- Row limits: GrammarDrill 12–30; Roleplay 16–40; MockInterview 20–50; VocabularySprint 20–40; FluencyDrill 30–60; RepracticeRound 14–28

### API Surface

- `POST /api/v1/scripts/validate` — ADMIN; validates Excel; returns parse result
- `POST /api/v1/scripts/upload` — ADMIN; validates file, parses, inserts `tblScript`, bulk inserts `tblUtterance`, updates `UtteranceCount`, inserts `tblScriptVersion`; returns `ScriptId`, `ScriptTitle`, `Version`, `UtteranceCount`
- `GET /api/v1/scripts` — authenticated; paginated script list
- `GET /api/v1/scripts/{scriptId}` — authenticated; script metadata and ordered utterances
- `PATCH /api/v1/scripts/status` — ADMIN; updates `tblScript.IsActive`
- `GET /api/v1/scripts/{scriptId}/versions` — ADMIN; version history
- `GET /api/v1/scripts/sample-template` — ADMIN; returns generated `.xlsx`

### Admin Script Upload Wizard — Stable Flow Contract

#### Entry Points

- Route: `/admin/scripts/upload`

#### UI Trigger

- Step 1 validation starts when the admin selects or drops an `.xlsx` file and clicks `Validate Excel`
- Step 2 progression starts when the admin clicks `Continue` after metadata entry

#### Request Contract

Endpoint: `POST /api/v1/scripts/validate`
Headers: authenticated admin session; multipart form upload
Body:
- `file` (`File`, required): `.xlsx` only, max size `5 MB`

Endpoint: `POST /api/v1/scripts/upload`
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
- Constraints: `PK_tblSessionMember_SessionMemberId`, `FK_tblSessionMember_SessionId_tblSession_SessionId`, `FK_tblSessionMember_UserId_tblUser_UserId`, `UK_tblSessionMember_SessionId_SlotIndex` (filtered active rows), `IDX_tblSessionMember_SessionId`, `IDX_tblSessionMember_UserId`

### Stored Procedure Contracts

| SP | Input Parameters | Output / Return |
|---|---|---|
| `uspInsertSession` | `@SessionName`, `@SessionMode`, `@MaxMembers`, `@SessionDuration`, `@HostUserId`, `@ScriptId`, `@RoomExpiryMinutes`, `@CreatedBy`, `@IPAddress` | OUTPUT `@SessionId BIGINT`, `@JoinCode NVARCHAR(8)` |
| `uspInsertSessionMember` | `@SessionId`, `@UserId`, `@SlotIndex`, `@SlotName`, `@IsHost`, `@CreatedBy`, `@IPAddress` | `SessionMemberId` (non-query) |
| `uspGetSessionByJoinCode` | `@JoinCode` | RS1: `SessionId`, `SessionName`, `SessionMode`, `ScriptTitle`, `ScriptGrammarTag`, `Duration`, `MaxMembers`, `CurrentMemberCount`, `Status`; RS2: `SlotIndex`, `SlotName`, `IsOccupied`, `UserFullName`, `IsReady` |
| `uspGetSessionBySessionId` | `@SessionId` | RS1: `SessionId`, `SessionName`, `JoinCode`, `SessionMode`, `ScriptTitle`, `MaxMembers`, `SessionDuration`, `Status` (may be absent — see drift note); RS2: `UserId`, `FullName`, `AvatarUrl`, `SlotIndex`, `SlotName`, `IsReady`, `IsHost` (active members only) |
| `uspValidateJoinCode` | `@JoinCode` | OUTPUT: `@IsValid BIT`, `@SessionId BIGINT`, `@SessionName NVARCHAR(128)`, `@Status NVARCHAR(16)`, `@CurrentMemberCount INT` |
| `uspGetAvailableSlotsBySessionId` | `@SessionId` | Rows: `SlotIndex`, `SlotName`, `IsOccupied`, `UserFullName`, `IsReady` |
| `uspUpdateSessionMemberReadyStatus` | `@SessionId`, `@UserId`, `@IsReady`, `@UpdatedBy`, `@IPAddress` | non-query |
| `uspUpdateSessionStatus` | `@SessionId`, `@Status`, `@UpdatedBy`, `@IPAddress` | non-query |
| `uspUpdateSessionMemberLeft` | `@SessionId`, `@UserId`, `@UpdatedBy`, `@IPAddress` | non-query; auto-abandons session if host leaves or no active members remain |
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
POST /api/v1/sessions
Authorization: Bearer {accessToken}
Body (CreateSessionRequestDto):
  - SessionName (string, required, min 3 max 60)
  - SessionMode (int enum, required): 1=GrammarDrill 2=Roleplay 3=MockInterview 4=VocabularySprint 5=FluencyDrill 6=RepracticeRound
  - MaxMembers (byte, required, 2–5)
  - SessionDuration (int, required): must be one of [15, 30, 45, 60, 90]
  - ScriptId (long, required, > 0)
  - RoomExpiryMinutes (int, required): must be one of [60, 120, 360, 1440]
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
- `SessionMode`: valid enum value only
- `MaxMembers`: 2–5 inclusive
- `SessionDuration`: exact values only — `[15, 30, 45, 60, 90]`
- `ScriptId`: > 0
- `RoomExpiryMinutes`: exact values only — `[60, 120, 360, 1440]`
- Script must exist and have at least `MaxMembers` distinct `SpeakerLabel` values
- Host user must exist and be active

**Database Tables:** `tblSession` (insert), `tblSessionMember` (insert)

**Stored Procedures:**
- `uspInsertSession` → returns OUTPUT `@SessionId`, `@JoinCode`
- `uspInsertSessionMember` (host member)
- Both calls wrapped in a single DB transaction; rollback on failure

**Business Rules:**
- Host is automatically inserted as slot 1 member; `IsReady = true`, `IsHost = true`
- Slot names derived from script `SpeakerLabel` values ordered by first `SequenceId` appearance then alphabetically; limited to `MaxMembers` count
- If script has fewer distinct speaker labels than `MaxMembers`, creation fails with: `"Selected script does not contain enough distinct speaker slots for the requested MaxMembers."`
- `SessionMode` is stored as human-readable string (e.g., `"Grammar Drill"`), not the enum integer

**State Transitions:** `tblSession.Status` starts as `LOBBY`

**SignalR / Realtime Events:** None at creation; hub join happens after navigation to lobby

**Failure Cases:**
- Host user not found → 200 `Success:false` "Session creation failed."
- Script not found → 200 `Success:false` "Session creation failed."
- Not enough script slots → 200 `Success:false` "Session creation failed."
- SP fails to return `@SessionId` or `@JoinCode` → throws `InvalidOperationException`

**Recovery / Fallback Logic:** DB transaction rollback on any exception during session or member insert

**Notes on Known Drift Prevented:**
- Frontend must send numeric `SessionMode` (1–6), not a string; backend maps to stored string
- `SessionDuration` and `RoomExpiryMinutes` are exact-value whitelists, not ranges

---

### Flow: Validate Join Code

**Purpose:** Guest retrieves session preview before joining to see slots and session info.

**Entry Points:** Frontend join-session screen

**UI Route / Screen:** Join session entry screen

**UI Trigger:** User enters join code and presses Validate / Preview

**Preconditions:** User is authenticated; join code is a 6-character string

**Request Contract:**
```
GET /api/v1/sessions/validate/{joinCode}
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
- After validate succeeds, frontend navigates to join page with sessionId carried from preview

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

### Flow: Join Session

**Purpose:** Guest selects a slot and joins the session lobby. Repeat join (user already in lobby) is idempotent.

**Entry Points:**
- Frontend join-session screen; page route uses `SessionPreview` data from validate step
- `Confirm & Join` button placed directly below the role-selection list (not in a sticky footer)
- Labels, badges, helper text, and CTA rendered in black on light surfaces

**UI Route / Screen:** Join session screen; navigates to `/session/lobby/{sessionId}` on success

**UI Trigger:** `Confirm & Join` button click

**Preconditions:** Validate join code completed; user has selected a slot

**Request Contract:**
```
POST /api/v1/sessions/join
Authorization: Bearer {accessToken}
Body (JoinSessionRequestDto):
  - JoinCode (string, required, exactly 6 chars)
  - SlotIndex (byte, required, 1–5)
```

**Response Contract:**
```
HTTP 200 — ApiResponse<LobbyStateResponseDto>
  - SessionId (long)
  - SessionName (string)
  - JoinCode (string)
  - SessionMode (string)
  - ScriptTitle (string)
  - MaxMembers (byte)
  - SessionDuration (int)
  - Status (string)
  - Members (List<LobbyMemberDto>):
      - UserId (long)
      - FullName (string)
      - AvatarUrl (string, nullable)
      - SlotIndex (byte)
      - SlotName (string)
      - IsReady (bool)
      - IsHost (bool)
  - CanStart (bool)
```

**Validation Rules (FluentValidation):**
- `JoinCode`: not empty, exactly 6 chars
- `SlotIndex`: 1–5 inclusive

**Business Rules (Service):**
1. Join code is normalized (trim + uppercase)
2. `uspValidateJoinCode` called — output params: `@IsValid BIT`, `@SessionId BIGINT`, `@SessionName NVARCHAR(128)`, `@Status NVARCHAR(16)`, `@CurrentMemberCount INT`
3. If `IsValid = false` → fail: `"Join code is invalid, expired, or the room is already full."`
4. `GetLobbyStateBySessionIdAsync` — if user is already in `Members` list, return existing lobby state (idempotent)
5. `GetAvailableSlotsBySessionIdAsync` → `uspGetAvailableSlotsBySessionId` → slot list with `IsOccupied`
6. If `SlotIndex` not found in available slots → fail: `"Selected slot does not exist for this session."`
7. If selected slot `IsOccupied = true` → fail: `"Selected slot is already occupied."`
8. Insert member via `uspInsertSessionMember`: `@SessionId`, `@UserId`, `@SlotIndex`, `@SlotName`, `@IsHost = false`, `@CreatedBy`, `@IPAddress`
9. Reload lobby state via `uspGetSessionBySessionId`
10. `CanStart = Members.Count >= 2 AND All(IsReady)`

**Database Tables:** `tblSessionMember` (insert), `tblSession` + `tblSessionMember` (reads)

**Stored Procedures:**
- `uspValidateJoinCode` (output params)
- `uspGetAvailableSlotsBySessionId`
- `uspInsertSessionMember`
- `uspGetSessionBySessionId` (to build lobby response)

**State Transitions:** None — session remains `LOBBY`

**SignalR / Realtime Events:**
- After joining via REST, frontend calls `JoinLobby(sessionId, userId)` on `/hubs/session`
- Hub broadcasts `MEMBER_JOINED` to group `session_{sessionId}`: `{ userId, name, slotIndex }`

**Failure Cases:**
- Invalid/expired/full join code → `"Join code is invalid, expired, or the room is already full."`
- Slot not in available list → `"Selected slot does not exist for this session."`
- Slot occupied → `"Selected slot is already occupied."`
- Lobby state null after join → `"Lobby state could not be loaded after joining."`

**Recovery / Fallback Logic:**
- Idempotent repeat join: if user is already in Members, returns existing lobby state without re-inserting
- `uspGetSessionBySessionId` Status drift fallback: if `Status` column is missing from RS1, repository exits the `DbDataReader` scope and disposes it before running the EF fallback query on `tblSession.Status` (returns `"LOBBY"` if still null)

**Notes on Known Drift Prevented:**
- `uspGetSessionBySessionId` must emit `Status` column in RS1 — added in migration `AddStatusToLobbyStateSP`; without it, `ResolveLobbyStatusAsync` falls back to EF to prevent `IndexOutOfRangeException`
- The status fallback must run only after the lobby SP reader scope has ended and the reader is disposed; merely finishing `ReadAsync`/`NextResultAsync` is not enough, and querying EF before disposal causes SQL Server error: `There is already an open DataReader associated with this Connection`
- Numeric fields (`SlotIndex`, `MaxMembers`, counts, durations) use `Convert.To*()` not direct CLR cast — prevents type mismatch errors when SP returns different numeric types
- Frontend uses `sessionId` from the `join` response (not the validate response) for lobby navigation

---

### Flow: Get Lobby State

**Purpose:** Returns current lobby snapshot including members, readiness, and `CanStart` flag.

**Request Contract:**
```
GET /api/v1/sessions/lobby/{sessionId}
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
PATCH /api/v1/sessions/ready
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

### Flow: Start Session

**Purpose:** Host starts the session, transitions it to ACTIVE, and initializes the first turn.

**Request Contract:**
```
POST /api/v1/sessions/{sessionId}/start
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
- **Host fallback recovery**: if the hub invoke resolves late, rejects after the backend already changed state, or the event is missed, the lobby component polls `GET /api/v1/sessions/lobby/{sessionId}` for a short window; `status = ACTIVE` forces navigation to `/live-session/room/{sessionId}` and clears the `Starting...` state
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
POST /api/v1/sessions/{sessionId}/leave
Authorization: Bearer {accessToken}
Path param: sessionId (long)
```

**Response Contract:** `ApiResponse<bool>` — `Data: true` on success

**Business Rules:**
- User must be a currently **active** member (`IsActive = 1`) of the session — enforced via `GetLobbyStateBySessionIdAsync` LINQ filter
- Calls `uspUpdateSessionMemberLeft(@SessionId, @UserId, @UpdatedBy, @IPAddress)`
- SP sets `IsActive = 0`, `IsReady = 0`, `LeftAt = GETDATE()` for the leaving member
- SP auto-abandons session (`Status = 'ABANDONED'`) when host leaves OR when no active members remain
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
- Frontend `confirmLeave()`: calls `POST /api/v1/sessions/{sessionId}/leave` then navigates. Navigation always happens (success or error). `OnDisconnectedAsync` acts as the safety net for browser-close/network-loss.

**Rejoin flow (after leave):**
1. User navigates back to `/session/join` or dashboard
2. `JoinSessionAsync` calls `GetLobbyStateBySessionIdAsync` → with IsActive filter, left member is NOT in Members list → check at line 165 passes → rejoin allowed
3. `GetAvailableSlotsBySessionIdAsync` (via SP `uspGetAvailableSlotsBySessionId`) filters `AND IsActive = 1` — slot appears as available after leave
4. `uspInsertSessionMember` inserts a NEW row (previous inactive row kept for history)

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
- **Drift 2:** `session-room.component.ts` `confirmLeave()` only called `router.navigate()` — no DB update on leave. Fixed 2026-05-22: now calls `POST /api/v1/sessions/{sessionId}/leave` before navigating.
- **Drift 3:** `LiveSessionHub.OnDisconnectedAsync` only broadcast `MEMBER_LEFT` but did not call `MarkMemberLeftAsync` — DB state not updated on browser close or network loss. Fixed 2026-05-22.
- **Drift 4:** `uspUpdateSessionMemberLeft` previously did not reset `IsReady = 0`; leaving a lobby while ready preserved the flag — on rejoin the member appeared ready without toggling. Fixed in migration `FixMemberLeftSP_ResetIsReady`.
- **Drift 5:** `LeaveSessionAsync` returned 400 "Session member was not found in the lobby" when `IsActive = 0`, which happens legitimately when `OnDisconnectedAsync` (SessionHub or LiveSessionHub) fires on a WebSocket drop/reconnect cycle BEFORE the user manually clicks Leave. Frontend received 400 but user was already left — idempotent path was missing. Fixed 2026-05-23: added `HasSessionMemberAsync` check; if member exists (any state) but not active → return 200 success. Only returns 400 when member has NO record at all in the session.

---

### Flow: End Session (REST)

**Purpose:** Marks session as COMPLETED without generating a summary. Distinct from live-session complete.

**Request Contract:**
```
POST /api/v1/sessions/{sessionId}/end
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
GET /api/v1/sessions/history?statusFilter=&pageNumber=1&pageSize=20
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

### Migration State

- `InitialCreate_Phase1`, `AddAdminModule_Phase2`, `AddScriptModule_Phase3`, `AddSessionModule_Phase4`
- `AddStatusToLobbyStateSP` — adds `ses.Status` to `uspGetSessionBySessionId` RS1; required for `SessionHub` disconnect guard and `ResolveLobbyStatusAsync` fallback
- `FixMemberLeftSP_ResetIsReady` — adds `IsReady = 0` to `uspUpdateSessionMemberLeft` UPDATE; prevents stale ready-flag on member re-join

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
GET /api/v1/turns/{sessionId}/current
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
  - ActiveSlotIndex (byte)
  - Utterance (UtteranceResponseDto): full utterance record for this turn
  - ReReadAllowed (bool)
  - ReReadCount (int)
  - MaxReReads (int): always 2 for new turns
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
- REST: `POST /api/v1/turns/{sessionId}/shift`
- SignalR hub: `CompleteTurn(sessionId, memberId, turnIndex, score)`

**Request Contract:**
```
POST /api/v1/turns/{sessionId}/shift
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

**Business Rules:**
1. `MemberId` must equal authenticated `userId` — only the active speaker can shift
2. Current turn must have `ActiveMemberId == MemberId` AND `TurnIndex == dto.TurnIndex`
3. Session must be `ACTIVE`
4. Mark current turn `COMPLETED` via `uspUpdateTurnStatusByTurnStateId`
5. Create next turn at `TurnIndex + 1`
6. Next speaker resolved by matching `tblUtterance.SpeakerLabel` to `tblSessionMember.SlotName` (case-insensitive trim match)
7. If `nextTurnIndex > orderedUtterances.Count` → no more turns; return error `"No further turns remain in this session. Complete the session."`
8. `MaxReReads` is always 2 for new turns

**Next Speaker Resolution:**
```
nextUtterance = orderedUtterances[nextTurnIndex - 1]
activeMember = activeMembers.FirstOrDefault(m =>
    m.SlotName.Trim().Equals(nextUtterance.SpeakerLabel.Trim(), OrdinalIgnoreCase))
```
If no member matches → error: `"No active member holds the slot '{speakerLabel}'. Active slots: [{slots}]. Check that the script speaker labels match the session slot names."`

**SignalR / Realtime Events:**
- Hub `CompleteTurn(sessionId, memberId, turnIndex, score)` calls `ShiftTurnAsync`
- Broadcasts `TURN_SHIFT` to `live_{sessionId}`: `{ newActiveMemberId, slotIndex, turnIndex, nextUtterance }`

**Frontend Transition Contract:**
- `TURN_SHIFT` is a partial event, not a full `TurnStateResponseDto`
- The active speaker must submit turn completion through hub method `CompleteTurn`, not the REST `POST /api/v1/turns/{sessionId}/shift` endpoint, so every connected client receives `TURN_SHIFT` without needing a manual page reload
- After receiving `TURN_SHIFT`, the live-session room must refresh `GET /api/v1/turns/{sessionId}/current` to hydrate the canonical state for all clients
- The room may optimistically swap `activeMemberId`, `turnIndex`, and `utterance`; `activeSlotIndex` is optional in the temporary client-side transition because the canonical current-turn reload runs immediately afterward
- The shared frontend `TurnState` model should still include `activeSlotIndex` to match the backend contract, but the live room must not depend on that field in the optimistic `TURN_SHIFT` patch path

**Failure Cases:**
- `MemberId != userId` → `"Only the active speaker can complete the current turn."`
- Session, current turn, or user not found → `"Session, current turn, or user was not found."`
- Session not ACTIVE → `"Session must be active to shift turns."`
- Turn mismatch → `"The provided turn does not match the active turn."`
- No further turns → `"No further turns remain in this session. Complete the session."`
- Speaker label not matched → slot mismatch error with active slot list

**Notes on Known Drift Prevented:**
- `SpeakerLabel` and `SlotName` matched case-insensitively with trim — prevents mismatches from whitespace or casing differences in script upload vs. session slot assignment
- Treating `TURN_SHIFT` as a full turn DTO leaves listeners on stale speaker text and blocks the next speaker from seeing the recorder; clients must re-fetch current turn after the event

---

### Flow: Save Voice Analysis

**Purpose:** Active speaker submits their voice analysis result for a completed turn.

**Request Contract:**
```
POST /api/v1/turns/{sessionId}/voice-analysis
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
- REST: `POST /api/v1/turns/{sessionId}/listener-feedback`
- SignalR hub: `SubmitListenerFeedback(sessionId, tag, targetTurnIndex)`

**Request Contract:**
```
POST /api/v1/turns/{sessionId}/listener-feedback
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
- REST `POST /api/v1/turns/{sessionId}/listener-feedback` remains valid as a data endpoint, but using it directly in the live room bypasses the realtime broadcast and leaves other clients stale until reload

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
- REST: `POST /api/v1/turns/{sessionId}/re-read`
- SignalR hub: `RequestReRead(sessionId, requesterId)`

**Request Contract:**
```
POST /api/v1/turns/{sessionId}/re-read
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
- REST: `POST /api/v1/sessions/{sessionId}/complete`
- SignalR hub: `EndSession(sessionId)`

**Request Contract:**
```
POST /api/v1/sessions/{sessionId}/complete
Authorization: Bearer {accessToken}
Path param: sessionId (long)
```

**Response Contract:**
```
ApiResponse<SessionSummaryResponseDto>
  - MemberScores (List<MemberScoreDto>):
      - UserId (long)
      - FullName (string)
      - FluencyScore (decimal)
      - ConfidenceScore (decimal)
      - MistakeCount (int)
      - ListenerRating (decimal)
  - TotalTurns (int)
  - ScriptTitle (string)
  - GrammarFocusTag (string)
  - TotalMistakesAllMembers (int)
```

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

**Failure Cases:**
- `sessionId <= 0` → validation fail
- Session not found → `"Session was not found."`
- Summary could not be generated → `"Session summary could not be generated."`

---

### Flow: Live Session Entry and Transition

**Purpose:** Members transition from lobby to live session room after `SESSION_STARTED` fires.

**Sequence:**
1. Frontend receives `SESSION_STARTED` from `/hubs/session` — both host and guest navigate to live session screen
2. Frontend connects to `/hubs/live-session?sessionId={id}&access_token={token}`
3. Hub `OnConnectedAsync` calls `GetActiveSessionMemberByUserIdAsync` to resolve slot and fullName for the connection
4. Frontend calls `JoinLiveSession(sessionId, userId)` → broadcasts `MEMBER_JOINED`: `{ userId, name, slotIndex }`
5. Frontend calls `GET /api/v1/turns/{sessionId}/current` to load the first turn
6. On each `TURN_SHIFT`, every connected client reloads `GET /api/v1/turns/{sessionId}/current` before deciding whether to render speaker or listener UI

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

**OnDisconnect behavior:** Hub broadcasts `MEMBER_LEFT`: `{ userId, slotIndex }` to group unconditionally; also calls `MarkMemberLeftAsync` (sets IsActive = 0, IsReady = 0) — best-effort safety net for browser close / genuine leave

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
| `TURN_SHIFT` | `{ newActiveMemberId: long, slotIndex: byte, turnIndex: int, nextUtterance: UtteranceResponseDto }` |
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
| `defaultVoiceStarter` | bool | `true` | ⚠ INACTIVE — was auto-start mic 400ms after `ngOnChanges`. Removed from `SpeakerScreenComponent` in Voice Recognition Upgrade. Pref key retained in storage but no longer consumed. |
| `autoSubmitOnStop` | bool | `false` | ⚠ INACTIVE — was `stopRecording()` auto-firing `onConfirm()`. Removed in Voice Recognition Upgrade. `onDoneSpeaking()` requires explicit user tap. |
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

### Failure Cases
- Missing `sessionId` route param → room init does not run
- Guard rejection → navigation blocked before room render
- Compiler fallback removed while a dependency still requests JIT → browser runtime failure on room navigation
- Current turn API failure after successful route load → room renders retry state with `loadError`

### Recovery / Fallback Logic
- `main.ts` imports `@angular/compiler` so JIT-required dependencies do not hard-fail bootstrap
- Child live-session route resolves the room through a static component reference inside the lazy child route file
- `SessionRoomComponent.retryLoad()` re-runs `GET /api/v1/turns/{sessionId}/current` after transient API failure
- Session preference toggle states use `ngClass` string switching for `bg-white/15`, `bg-white/5`, `text-white/40`, and `translate-x-0.5` style utilities instead of `[class.*]` bindings
- Session preference toggle thumbs use explicit `left-0.5` anchoring plus inline transform distance instead of class-only translation so the knob stays visually aligned in rendered HTML

### Notes on Known Drift Prevented
- Stale docs drift: `ProjectOverview.md` previously stated that `Frontend/src/main.ts` imported `@angular/compiler`, but source had drifted and the import was missing
- Route compilation drift: the live-session room is documented to resolve through `component: SessionRoomComponent` within the lazy child route contract to reduce runtime JIT-only failures on this page
- Template compilation drift: session-room settings toggles previously used escaped `[class.bg-white\/15]` and `[class.translate-x-0\.5]` bindings, which broke Angular template parsing and surfaced as unclosed `button` tags during JIT compilation
- UI drift: session-room preference toggles previously rendered with class-driven thumb translation but no fixed left anchor, producing an unstable knob position in the actual DOM
- **Drift 1 (2026-05-23): Page-refresh breaks hub reconnect + voice analysis + leave** — `OnDisconnectedAsync` sets `IsActive = 0` on every disconnect including page refreshes. On page reload: (a) hub `OnConnectedAsync` → `GetActiveSessionMemberByUserIdAsync` → null → HubException → connection fails; (b) `SaveVoiceAnalysisAsync` → same null check → 400 "Voice analysis save failed."; (c) `LeaveSessionAsync` → same null check → 400 "Session leave failed." Fixed 2026-05-23 via three-part fix: hub `ResolveConnectionMetadataAsync` now falls back to `GetSessionMemberByUserIdAsync` and calls `ReactivateMemberAsync` on reconnect; `SaveVoiceAnalysisAsync` now uses `GetSessionMemberByUserIdAsync` (turn ownership enforced by `ActiveMemberId == userId`); `LeaveSessionAsync` already made idempotent in Drift 5 of Session Module.
- **Drift 4 (2026-05-23): Single skipped word cascades entire alignment → all subsequent words score near-zero** — `alignAndScore` only detected *insertions* (extra spoken words) in its look-ahead, never *deletions* (skipped expected words). When a speaker skips one word (e.g. "I have **[been]** waiting..."), the algorithm did a substitution ("waiting" scored against "been"), then compared every following word against the wrong expected slot. Entire sentence offset by 1 position → fluency=15 for a near-perfect sentence. **Mathematical proof from screen:** spoken missed only "been", yet fluency=15 and "[project]" showed as missing at the end (exhausted spokenIdx one slot early). **Fix:** Added deletion look-ahead in `alignAndScore`: when `wordSimilarity(spokenWord, expected[expectedIdx+1]) >= 0.7 AND > current similarity`, mark `expected[expectedIdx]` as `isMissing`, advance `expectedIdx` only (keep `spokenIdx` on same word). Priority: insertion checked first, deletion second, substitution last. **Verified trace:** with fix, "I have waiting to see it how long have you been working on this project" vs expected gives 15/16 matched, [been] missing, fluency≈94, overall≈93 — correct.
- **Drift 3 (2026-05-23): 8000ms fallback timer fires before isFinal for long sentences → all scores wrong** — Timer started at mic press; interim results did not reset it. For a 12-word sentence where the user takes 2–3s to prepare then speaks for 4–5s, the 8000ms timer fired just before the browser emitted `isFinal=true`. `finalize()` ran with `allFinalTranscripts=[]` → `transcribedText=""`, `confidenceScore=70` (hardcoded fallback), `fluencyScore=0`, `overallScore=18` (= `0.25 × 70`), all words `[missing]`. **Fix:** In the `onresult` handler's `else` (interim) branch, when `allFinalTranscripts.length===0`, call `resetSilenceTimeout(8000)`. This makes the fallback deadline "8s from last speech activity" not "8s from mic press". Once a `isFinal` arrives the 2500ms timer takes over. **Secondary fix:** `VoiceFeedbackComponent.speedLabel` now returns `'— Not detected'` for `wpm===0` instead of `'🐢 Too slow'` — 0 WPM means capture failed, not that the speaker was too slow.
- **Drift 2 (2026-05-23): Page-refresh after recording-complete causes 400 on re-submit** — Drift 1 fixed hub reconnect and member lookup, but did not address: the voice analysis is saved immediately at `onRecordingComplete` (before `CompleteTurn`). If the user refreshes at that point, `ngOnChanges` resets phase to `'recording'` (in-memory state lost). The user re-records the same active turn and the backend rejects with 400 "already exists". **Two-part fix applied 2026-05-23:** (a) **Backend UPSERT**: `SaveVoiceAnalysisAsync` now calls `GetVoiceAnalysisByUserTurnAsync`; if a record exists for this session+user+turn, it calls `UpdateVoiceAnalysisAsync` (EF `ExecuteUpdateAsync` on all score fields) and returns 200 `"Voice analysis updated successfully."` — no more 400 on re-record. New repo methods: `GetVoiceAnalysisByUserTurnAsync(sessionId, userId, turnIndex)`, `UpdateVoiceAnalysisAsync(voiceAnalysisId, updates, updatedBy)`. (b) **Frontend sessionStorage persistence**: `SpeakerScreenComponent` stores the `VoiceSessionResult` in `sessionStorage` under key `gwf_va_{sessionId}_{turnIndex}_{userId}` on `onRecordingComplete`. On `ngOnChanges`, `tryRestoreFromStorage()` checks this key and if found, restores `sessionResult` and sets `analysisPhase = 'feedback'` — user lands back on their score screen after refresh, not a blank recorder. Entry is cleared on `CompleteTurn` success or `Skip` success.

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
- Reset on each `ngOnChanges(turnState)` → `resetPhase()`.

**Turn Start:**
1. `ngOnChanges(turnState)` → `words.set(...)` → `resetPhase()` → `analysisPhase = 'recording'`, `sessionResult = null`
2. User taps mic inside `VoiceRecorderComponent` → `VoiceRecognitionEngine.startSession()` begins

**Recording Phase — VoiceRecognitionEngine flow:**
1. `requestMicPermission()` — `navigator.mediaDevices.getUserMedia` before recognition starts
2. `AudioActivityDetector.start()` — opens AudioContext, waveform + VAD starts
3. `SpeechRecognition` configured: `lang = 'en-IN'`, `continuous = !isIOS`, `interimResults = true`, `maxAlternatives = 3`
4. `onresult` — interim buffered for display only; final chunks collected in `allFinalTranscripts[]`; **interim results reset the 8000ms fallback timer** (when no finals yet) so the deadline is "8s from last speech activity" not "8s from mic press"
5. Silence detected (< -50dB for 1200ms) → `recognition.stop()` after 400ms buffer
6. Fallback timeout: **8000ms from last speech activity** (interim resets timer if no finals yet); 2500ms from last final chunk → auto-finalize
7. Finalize: join all final chunks → `TranscriptNormalizer.normalize()` both sides → `PronunciationScorer.score()` → emit `VoiceSessionResult`
8. Retry on `network` / `audio-capture` / `no-speech` errors up to 3 times (500ms delay)
9. iOS: `continuous = false`, restart on `onend` manually until VAD detects silence

**onRecordingStarted():**
- Calls `VoiceBroadcastService.startBroadcast()` (fire-and-forget)

**onRecordingComplete(result: VoiceSessionResult):**
- Sets `sessionResult = result`, `analysisPhase = 'feedback'`
- Calls `VoiceBroadcastService.stopBroadcast()`
- Maps `VoiceSessionResult` → voice analysis payload
- POST `/api/v1/turns/{sessionId}/voice-analysis` — non-blocking (`.subscribe()` no handler)

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
- `defaultVoiceStarter` and `autoSubmitOnStop` session prefs no longer active — new flow requires explicit user tap for both start and confirm.

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

| Setting | Value | Tuning Note |
|---|---|---|
| `silenceThresholdDB` | `-50` | Quiet room: lower to -55. Noisy room: raise to -45 |
| `silenceDurationMs` | `1200` | Raise to 1800 for users who pause between words |
| `fftSize` | `256` | Low latency — do not increase |
| `sampleRate` | `16000` | Optimal for speech recognition |
| Audio constraints | `echoCancellation`, `noiseSuppression`, `autoGainControl` all `true` | Built-in browser noise reduction |

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

| Browser | Platform | Support |
|---|---|---|
| Chrome 100+ | Android / Desktop | ✅ Full — use `lang=en-IN`, `continuous=true` |
| Edge 100+ | Desktop / Android | ✅ Full |
| Safari 15+ | iOS / iPadOS | ⚠ Partial — `continuous=false`, restart on `onend` |
| Samsung Internet | Android | ✅ Full (Chrome engine) |
| Firefox | Any | ❌ None — show "Please use Chrome or Edge" |

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

### Migration State

- `InitialCreate_Phase1`, `AddAdminModule_Phase2`, `AddScriptModule_Phase3`, `AddSessionModule_Phase4`, `AddLiveSessionModule_Phase5`, `AddUserModule_Phase7`

---

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
- Response DTOs: `MistakeResponseDto`, `MistakeSummaryResponseDto`, `RepracticeSessionResponseDto`, `RepracticeUtteranceResponseDto`, `GrammarProgressResponseDto`

### API Surface

- All endpoints require authentication. `UserId` always sourced from JWT — not from request body.

#### GET /api/v1/mistakes

- Returns paginated mistakes; supports `MistakeType` and `IsResolved` filters

#### GET /api/v1/mistakes/summary

- Returns: total, resolved, pending, improvement-percentage for authenticated user

#### GET /api/v1/mistakes/grammar-progress

- Returns: grammar-tag level progress with resolved counts and progress-bar percentages

#### POST /api/v1/repractice/generate

- Loads unresolved mistakes for authenticated user and source session; creates one repractice session with one repractice utterance per mistake

#### GET /api/v1/repractice/{repracticeSessionId}

- Validates ownership against authenticated user; returns session metadata + ordered repractice utterances

#### GET /api/v1/repractice/history

- Returns paginated repractice history

#### PATCH /api/v1/repractice/attempt

- Updates: `AttemptCount`, `BestScore`, `LastScore`, linked mistake `PracticeCount`
- Resolves both repractice utterance and linked mistake after 2 consecutive scores > 80

#### POST /api/v1/repractice/{repracticeSessionId}/complete

- Validates ownership; recalculates improvement percent; marks session `COMPLETED`; re-runs badge evaluation

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
