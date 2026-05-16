using System.Security.Claims;
using GoWithFlow.Application.DTOs.Requests.LiveSession;
using GoWithFlow.API.Constants;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoWithFlow.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.UserOrAdmin)]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
[Route(ApiRoutes.LiveSession.Base)]
public sealed class LiveSessionController : ControllerBase
{
	private readonly ILiveSessionService _liveSessionService;

	public LiveSessionController(ILiveSessionService liveSessionService)
	{
		_liveSessionService = liveSessionService;
	}

	[HttpGet(ApiRoutes.LiveSession.Current)]
	public async Task<IActionResult> GetCurrentTurnAsync(long sessionId, CancellationToken cancellationToken)
	{
		var response = await _liveSessionService.GetCurrentTurnAsync(sessionId, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK, StatusCodes.Status404NotFound);
	}

	[HttpPost(ApiRoutes.LiveSession.Shift)]
	public async Task<IActionResult> ShiftTurnAsync(long sessionId, [FromBody] TurnShiftRequestDto dto, CancellationToken cancellationToken)
	{
		if (dto.SessionId != sessionId)
		{
			return BadRequest(GoWithFlow.Application.Common.ApiResponse<object>.FailureResult(new[] { "Route sessionId does not match body SessionId." }, "Validation failed."));
		}

		var response = await _liveSessionService.ShiftTurnAsync(dto, GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpPost(ApiRoutes.LiveSession.VoiceAnalysis)]
	public async Task<IActionResult> SaveVoiceAnalysisAsync(long sessionId, [FromBody] SaveVoiceAnalysisRequestDto dto, CancellationToken cancellationToken)
	{
		if (dto.SessionId != sessionId)
		{
			return BadRequest(GoWithFlow.Application.Common.ApiResponse<object>.FailureResult(new[] { "Route sessionId does not match body SessionId." }, "Validation failed."));
		}

		var response = await _liveSessionService.SaveVoiceAnalysisAsync(dto, GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status201Created);
	}

	[HttpPost(ApiRoutes.LiveSession.ListenerFeedback)]
	public async Task<IActionResult> SubmitListenerFeedbackAsync(long sessionId, [FromBody] ListenerFeedbackRequestDto dto, CancellationToken cancellationToken)
	{
		if (dto.SessionId != sessionId)
		{
			return BadRequest(GoWithFlow.Application.Common.ApiResponse<object>.FailureResult(new[] { "Route sessionId does not match body SessionId." }, "Validation failed."));
		}

		var response = await _liveSessionService.SubmitListenerFeedbackAsync(dto, GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpPost(ApiRoutes.LiveSession.ReRead)]
	public async Task<IActionResult> RequestReReadAsync(long sessionId, CancellationToken cancellationToken)
	{
		var response = await _liveSessionService.RequestReReadAsync(sessionId, GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpPost(ApiRoutes.Session.CompleteAbsolute)]
	public async Task<IActionResult> CompleteSessionAsync(long sessionId, CancellationToken cancellationToken)
	{
		var response = await _liveSessionService.CompleteSessionAsync(sessionId, cancellationToken);
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
