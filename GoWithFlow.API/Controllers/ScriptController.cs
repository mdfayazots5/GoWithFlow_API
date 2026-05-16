using System.Security.Claims;
using GoWithFlow.Application.DTOs.Requests.Script;
using GoWithFlow.API.Constants;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoWithFlow.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
[Route(ApiRoutes.Script.Base)]
public sealed class ScriptController : ControllerBase
{
	private readonly IScriptService _scriptService;

	public ScriptController(IScriptService scriptService)
	{
		_scriptService = scriptService;
	}

	[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
	[HttpPost(ApiRoutes.Script.Validate)]
	public async Task<IActionResult> ValidateExcelAsync([FromForm] IFormFile file, CancellationToken cancellationToken)
	{
		var response = await _scriptService.ValidateExcelAsync(file, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
	[HttpPost(ApiRoutes.Script.Upload)]
	public async Task<IActionResult> UploadScriptAsync([FromForm] IFormFile file, [FromForm] ScriptUploadRequestDto dto, CancellationToken cancellationToken)
	{
		var uploadedByUserId = GetUserId();
		var response = await _scriptService.UploadScriptAsync(file, dto, uploadedByUserId, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status201Created);
	}

	[Authorize(Policy = AuthorizationPolicies.UserOrAdmin)]
	[HttpGet]
	public async Task<IActionResult> GetScriptsAsync([FromQuery] ScriptSearchRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _scriptService.GetScriptsAsync(dto, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[Authorize(Policy = AuthorizationPolicies.UserOrAdmin)]
	[HttpGet(ApiRoutes.Script.Detail)]
	public async Task<IActionResult> GetScriptByIdAsync(long scriptId, CancellationToken cancellationToken)
	{
		var response = await _scriptService.GetScriptByIdAsync(scriptId, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK, StatusCodes.Status404NotFound);
	}

	[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
	[HttpPatch(ApiRoutes.Script.Status)]
	public async Task<IActionResult> UpdateScriptStatusAsync([FromBody] ScriptStatusUpdateRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _scriptService.UpdateScriptStatusAsync(dto, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
	[HttpGet(ApiRoutes.Script.Versions)]
	public async Task<IActionResult> GetVersionHistoryAsync(long scriptId, CancellationToken cancellationToken)
	{
		var response = await _scriptService.GetVersionHistoryAsync(scriptId, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
	[HttpGet(ApiRoutes.Script.SampleTemplate)]
	public async Task<IActionResult> GetSampleTemplateAsync(CancellationToken cancellationToken)
	{
		var response = await _scriptService.GetSampleTemplateAsync(cancellationToken);

		if (response.Success == false || response.Data is null)
		{
			return StatusCode(StatusCodes.Status400BadRequest, response);
		}

		return File(
			response.Data,
			"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
			"GoWithFlow_ScriptTemplate.xlsx");
	}

	private long GetUserId()
	{
		var claimValue = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (long.TryParse(claimValue, out var userId))
		{
			return userId;
		}

		throw new UnauthorizedAccessException("User claim is missing.");
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
