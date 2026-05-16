using System.Security.Claims;
using GoWithFlow.API.Constants;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoWithFlow.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.UserOrAdmin)]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
[Route(ApiRoutes.Repractice.Base)]
public sealed class RepracticeController : ControllerBase
{
	private readonly IRepracticeService _repracticeService;

	public RepracticeController(IRepracticeService repracticeService)
	{
		_repracticeService = repracticeService;
	}

	[HttpPost(ApiRoutes.Repractice.Generate)]
	public async Task<IActionResult> GenerateRepracticeSessionAsync([FromBody] GenerateRepracticeRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _repracticeService.GenerateRepracticeSessionAsync(dto, GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status201Created);
	}

	[HttpGet(ApiRoutes.Repractice.Detail)]
	public async Task<IActionResult> GetRepracticeSessionAsync(long repracticeSessionId, CancellationToken cancellationToken)
	{
		var response = await _repracticeService.GetRepracticeSessionAsync(repracticeSessionId, GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK, StatusCodes.Status404NotFound);
	}

	[HttpGet(ApiRoutes.Repractice.History)]
	public async Task<IActionResult> GetRepracticeHistoryAsync([FromQuery] int page = 1, [FromQuery] int size = 10, CancellationToken cancellationToken = default)
	{
		var response = await _repracticeService.GetRepracticeHistoryAsync(GetUserId(), page, size, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpPatch(ApiRoutes.Repractice.Attempt)]
	public async Task<IActionResult> UpdateAttemptAsync([FromBody] UpdateAttemptRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _repracticeService.UpdateAttemptAsync(dto, GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpPost(ApiRoutes.Repractice.Complete)]
	public async Task<IActionResult> CompleteRepracticeSessionAsync(long repracticeSessionId, CancellationToken cancellationToken)
	{
		var response = await _repracticeService.CompleteRepracticeSessionAsync(repracticeSessionId, GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
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
