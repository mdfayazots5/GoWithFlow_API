using System.Security.Claims;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.API.Constants;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoWithFlow.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.UserOrAdmin)]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
[Route(ApiRoutes.User.Base)]
public sealed class UserController : ControllerBase
{
	private readonly IUserService _userService;

	public UserController(IUserService userService)
	{
		_userService = userService;
	}

	[HttpGet(ApiRoutes.User.Profile)]
	public async Task<IActionResult> GetProfileAsync(CancellationToken cancellationToken)
	{
		var response = await _userService.GetProfileAsync(GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK, StatusCodes.Status404NotFound);
	}

	[HttpPut(ApiRoutes.User.Profile)]
	public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateProfileRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _userService.UpdateProfileAsync(GetUserId(), dto, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK, StatusCodes.Status404NotFound);
	}

	[HttpPost(ApiRoutes.User.Avatar)]
	public async Task<IActionResult> UploadAvatarAsync([FromForm] IFormFile file, CancellationToken cancellationToken)
	{
		var response = await _userService.UploadAvatarAsync(GetUserId(), file, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpGet(ApiRoutes.User.SessionDetail)]
	public async Task<IActionResult> GetSessionDetailAsync(long sessionId, CancellationToken cancellationToken)
	{
		var response = await _userService.GetSessionDetailAsync(sessionId, GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK, StatusCodes.Status404NotFound);
	}

	[HttpGet(ApiRoutes.User.Progress)]
	public async Task<IActionResult> GetImprovementDataAsync(CancellationToken cancellationToken)
	{
		var response = await _userService.GetImprovementDataAsync(GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK, StatusCodes.Status404NotFound);
	}

	[HttpGet(ApiRoutes.User.Streak)]
	public async Task<IActionResult> GetStreakDataAsync(CancellationToken cancellationToken)
	{
		var response = await _userService.GetStreakDataAsync(GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpGet(ApiRoutes.User.Badges)]
	public async Task<IActionResult> GetBadgesAsync(CancellationToken cancellationToken)
	{
		var response = await _userService.GetBadgesAsync(GetUserId(), cancellationToken);
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
