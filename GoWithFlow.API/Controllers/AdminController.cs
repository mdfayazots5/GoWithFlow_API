using System.Security.Claims;
using GoWithFlow.Application.DTOs.Requests.Admin;
using GoWithFlow.API.Constants;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoWithFlow.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
[Route(ApiRoutes.Admin.Base)]
public sealed class AdminController : ControllerBase
{
	private readonly IAdminService _adminService;

	public AdminController(IAdminService adminService)
	{
		_adminService = adminService;
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

	[HttpGet(ApiRoutes.Admin.UserDetail)]
	public async Task<IActionResult> GetUserDetailAsync(long userId, CancellationToken cancellationToken)
	{
		var response = await _adminService.GetUserDetailAsync(userId, cancellationToken);
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

	[HttpGet(ApiRoutes.Admin.ExportReports)]
	public async Task<IActionResult> ExportReportsAsExcelAsync([FromQuery] AdminReportFilterRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _adminService.ExportReportsAsExcelAsync(dto, cancellationToken);

		if (response.Success == false || response.Data is null)
		{
			return StatusCode(StatusCodes.Status400BadRequest, response);
		}

		return File(
			response.Data,
			"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
			$"GoWithFlow_AdminReports_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
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

	private IActionResult BuildActionResult<T>(GoWithFlow.Application.Common.ApiResponse<T> response, int successStatusCode, int? failureStatusCode = null)
	{
		if (response.Success)
		{
			return StatusCode(successStatusCode, response);
		}

		return StatusCode(failureStatusCode ?? StatusCodes.Status400BadRequest, response);
	}
}
