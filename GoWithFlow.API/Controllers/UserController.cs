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
	private readonly IAudioArchiveService _audioArchiveService;

	public UserController(IUserService userService, IAudioArchiveService audioArchiveService)
	{
		_userService         = userService;
		_audioArchiveService = audioArchiveService;
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

	[HttpPost(ApiRoutes.User.Goal)]
	public async Task<IActionResult> SetGoalAsync([FromBody] SetGoalRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _userService.SetGoalAsync(GetUserId(), dto, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpGet(ApiRoutes.User.Goal)]
	public async Task<IActionResult> GetGoalProgressAsync(CancellationToken cancellationToken)
	{
		var response = await _userService.GetGoalProgressAsync(GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpPost(ApiRoutes.User.AudioArchive)]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> UploadAudioClipAsync(
		[FromForm] IFormFile file,
		[FromForm] long sessionId,
		[FromForm] int turnIndex,
		CancellationToken cancellationToken)
	{
		var userId = GetUserId();
		var ip     = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
		var response = await _audioArchiveService.UploadClipAsync(file, sessionId, turnIndex, userId, ip, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status201Created);
	}

	[HttpGet(ApiRoutes.User.SessionAudioArchive)]
	public async Task<IActionResult> GetSessionAudioClipsAsync(long sessionId, CancellationToken cancellationToken)
	{
		var response = await _audioArchiveService.GetSessionClipsAsync(sessionId, GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpDelete(ApiRoutes.User.AudioArchiveById)]
	public async Task<IActionResult> DeleteAudioClipAsync(long archiveId, CancellationToken cancellationToken)
	{
		var userId = GetUserId();
		var name   = User.FindFirstValue("FullName") ?? userId.ToString();
		var response = await _audioArchiveService.DeleteClipAsync(archiveId, userId, name, cancellationToken);
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
