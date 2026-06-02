using System.Security.Claims;
using GoWithFlow.API.Constants;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoWithFlow.API.Controllers;

[ApiController]
[Route(ApiRoutes.Challenge.Base)]
public sealed class ChallengeController : ControllerBase
{
	private readonly IChallengeService _challengeService;

	public ChallengeController(IChallengeService challengeService)
	{
		_challengeService = challengeService;
	}

	[HttpGet(ApiRoutes.Challenge.Active)]
	[Authorize(Policy = AuthorizationPolicies.UserOrAdmin)]
	[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
	public async Task<IActionResult> GetActiveChallengeAsync(CancellationToken cancellationToken)
	{
		var response = await _challengeService.GetActiveChallengeAsync(GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpPost(ApiRoutes.Challenge.Attempt)]
	[Authorize(Policy = AuthorizationPolicies.UserOrAdmin)]
	[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
	public async Task<IActionResult> SubmitAttemptAsync([FromBody] SubmitChallengeAttemptRequestDto dto, CancellationToken cancellationToken)
	{
		var user = User.FindFirstValue("FullName") ?? "User";
		var response = await _challengeService.SubmitAttemptAsync(dto, GetUserId(), user, "127.0.0.1", cancellationToken);
		return BuildActionResult(response, StatusCodes.Status201Created);
	}

	[HttpPost(ApiRoutes.Challenge.SetWeekly)]
	[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
	[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
	public async Task<IActionResult> SetWeeklyChallengeAsync([FromBody] SetWeeklyChallengeRequestDto dto, CancellationToken cancellationToken)
	{
		var admin = User.FindFirstValue("FullName") ?? "Admin";
		var response = await _challengeService.SetWeeklyChallengeAsync(dto, admin, "127.0.0.1", cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	private long GetUserId()
	{
		var claimValue = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (long.TryParse(claimValue, out var userId)) return userId;
		throw new UnauthorizedAccessException("User claim is missing.");
	}

	private IActionResult BuildActionResult<T>(GoWithFlow.Application.Common.ApiResponse<T> response, int successStatusCode)
	{
		return response.Success
			? StatusCode(successStatusCode, response)
			: StatusCode(StatusCodes.Status400BadRequest, response);
	}
}
