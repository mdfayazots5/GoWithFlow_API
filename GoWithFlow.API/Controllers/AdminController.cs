using System.Security.Claims;
using GoWithFlow.Application.DTOs.Requests.Admin;
using GoWithFlow.Application.DTOs.Responses.Admin;
using GoWithFlow.API.Constants;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace GoWithFlow.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
[Route(ApiRoutes.Admin.Base)]
public sealed class AdminController : ControllerBase
{
	private readonly IAdminService _adminService;
	private readonly ISessionRecordingService _sessionRecordingService;

	public AdminController(
		IAdminService adminService,
		ISessionRecordingService sessionRecordingService)
	{
		_adminService = adminService;
		_sessionRecordingService = sessionRecordingService;
	}

	[HttpGet(ApiRoutes.Admin.Dashboard)]
	public async Task<IActionResult> GetDashboardAsync(CancellationToken cancellationToken)
	{
		var response = await _adminService.GetDashboardSummaryAsync(cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpGet(ApiRoutes.Admin.Users)]
	public async Task<IActionResult> GetUsersAsync([FromQuery] AdminUserSearchRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _adminService.GetUsersAsync(dto, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpPost(ApiRoutes.Admin.Users)]
	public async Task<IActionResult> CreateUserAsync([FromForm] AdminCreateUserRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _adminService.CreateUserAsync(dto, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status201Created);
	}

	[HttpGet(ApiRoutes.Admin.UserDetail)]
	public async Task<IActionResult> GetUserDetailAsync(long userId, CancellationToken cancellationToken)
	{
		var response = await _adminService.GetUserDetailAsync(userId, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK, StatusCodes.Status404NotFound);
	}

	[HttpPut(ApiRoutes.Admin.UserDetail)]
	public async Task<IActionResult> UpdateUserAsync(long userId, [FromForm] AdminUpdateUserRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _adminService.UpdateUserAsync(userId, dto, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK, StatusCodes.Status404NotFound);
	}

	[HttpPatch(ApiRoutes.Admin.UserStatus)]
	public async Task<IActionResult> UpdateUserStatusAsync([FromBody] UpdateUserStatusRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _adminService.UpdateUserStatusAsync(dto, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpPost(ApiRoutes.Admin.UserNotes)]
	public async Task<IActionResult> AddAdminNoteAsync([FromBody] AdminNoteRequestDto dto, CancellationToken cancellationToken)
	{
		var adminUserId = GetAdminUserId();
		var response = await _adminService.AddAdminNoteAsync(dto, adminUserId, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status201Created);
	}

	[HttpGet(ApiRoutes.Admin.UserNotesByUser)]
	public async Task<IActionResult> GetAdminNotesByUserAsync(long userId, CancellationToken cancellationToken)
	{
		var response = await _adminService.GetAdminNotesByUserAsync(userId, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpGet(ApiRoutes.Admin.Reports)]
	public async Task<IActionResult> GetReportSummaryAsync([FromQuery] AdminReportFilterRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _adminService.GetReportSummaryAsync(dto, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpGet(ApiRoutes.Admin.UserReport)]
	public async Task<IActionResult> GetUserFullReportAsync(long userId, CancellationToken cancellationToken)
	{
		var response = await _adminService.GetUserFullReportAsync(userId, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK, StatusCodes.Status404NotFound);
	}

	[HttpGet(ApiRoutes.Admin.SessionHistory)]
	public async Task<IActionResult> GetSessionHistoryAsync([FromQuery] AdminSessionHistoryFilterRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _adminService.GetSessionHistoryAsync(dto, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpGet(ApiRoutes.Admin.SessionById)]
	public async Task<IActionResult> GetSessionByIdAsync(long sessionId, CancellationToken cancellationToken)
	{
		var response = await _adminService.GetSessionByIdAsync(sessionId, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK, StatusCodes.Status404NotFound);
	}

	[HttpGet(ApiRoutes.Admin.ExportReports)]
	public async Task<IActionResult> ExportReportsAsExcelAsync([FromQuery] AdminReportFilterRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _adminService.ExportReportsAsExcelAsync(dto, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpGet(ApiRoutes.Admin.Cohorts)]
	public async Task<IActionResult> GetAllCohortsAsync(CancellationToken cancellationToken)
	{
		var response = await _adminService.GetAllCohortsAsync(cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpPost(ApiRoutes.Admin.Cohorts)]
	public async Task<IActionResult> CreateCohortAsync([FromBody] GoWithFlow.Application.DTOs.Requests.Admin.CreateCohortRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _adminService.CreateCohortAsync(dto, GetAdminDisplayName(), "127.0.0.1", cancellationToken);
		return BuildActionResult(response, StatusCodes.Status201Created);
	}

	[HttpPatch(ApiRoutes.Admin.CohortAssign)]
	public async Task<IActionResult> AssignUserToCohortAsync([FromBody] GoWithFlow.Application.DTOs.Requests.Admin.AssignUserToCohortRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _adminService.AssignUserToCohortAsync(dto, GetAdminDisplayName(), "127.0.0.1", cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpGet(ApiRoutes.Admin.CohortMembers)]
	public async Task<IActionResult> GetCohortMembersAsync(long cohortId, CancellationToken cancellationToken)
	{
		var response = await _adminService.GetCohortMembersAsync(cohortId, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpGet(ApiRoutes.Admin.CohortAnalytics)]
	public async Task<IActionResult> GetCohortAnalyticsAsync(long cohortId, CancellationToken cancellationToken)
	{
		var response = await _adminService.GetCohortAnalyticsAsync(cohortId, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK, StatusCodes.Status404NotFound);
	}

	// One consolidated recording per session (Phase 16). Replaces the per-turn clip list.
	[HttpGet(ApiRoutes.Admin.SessionRecordings)]
	public async Task<IActionResult> GetSessionRecordingsAsync(long sessionId, CancellationToken cancellationToken)
	{
		var response = await _sessionRecordingService.GetAdminSessionRecordingAsync(sessionId, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	private long GetAdminUserId()
	{
		var claimValue = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (long.TryParse(claimValue, out var adminUserId))
		{
			return adminUserId;
		}

		throw new UnauthorizedAccessException("Admin user claim is missing.");
	}

	private string GetAdminDisplayName()
	{
		return User.FindFirstValue("FullName") ?? User.Identity?.Name ?? "Admin";
	}

	private IActionResult BuildActionResult<T>(GoWithFlow.Application.Common.ApiResponse<T> response, int successStatusCode, int? failureStatusCode = null)
	{
		if (response.Success)
		{
			return StatusCode(successStatusCode, response);
		}

		return StatusCode(failureStatusCode ?? StatusCodes.Status400BadRequest, response);
	}
}
