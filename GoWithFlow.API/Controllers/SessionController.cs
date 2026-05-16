using System.Security.Claims;
using GoWithFlow.Application.DTOs.Requests.Session;
using GoWithFlow.API.Constants;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoWithFlow.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.UserOrAdmin)]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
[Route(ApiRoutes.Session.Base)]
public sealed class SessionController : ControllerBase
{
	private readonly ISessionService _sessionService;

	public SessionController(ISessionService sessionService)
	{
		_sessionService = sessionService;
	}

	[HttpPost]
	public async Task<IActionResult> CreateSessionAsync([FromBody] CreateSessionRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _sessionService.CreateSessionAsync(dto, GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status201Created);
	}

	[HttpGet(ApiRoutes.Session.ValidateJoinCode)]
	public async Task<IActionResult> ValidateJoinCodeAsync(string joinCode, CancellationToken cancellationToken)
	{
		var response = await _sessionService.ValidateJoinCodeAsync(joinCode, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK, StatusCodes.Status404NotFound);
	}

	[HttpPost(ApiRoutes.Session.Join)]
	public async Task<IActionResult> JoinSessionAsync([FromBody] JoinSessionRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _sessionService.JoinSessionAsync(dto, GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpGet(ApiRoutes.Session.Lobby)]
	public async Task<IActionResult> GetLobbyStateAsync(long sessionId, CancellationToken cancellationToken)
	{
		var response = await _sessionService.GetLobbyStateAsync(sessionId, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK, StatusCodes.Status404NotFound);
	}

	[HttpPatch(ApiRoutes.Session.Ready)]
	public async Task<IActionResult> UpdateReadyStatusAsync([FromBody] UpdateReadyStatusRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _sessionService.UpdateReadyStatusAsync(dto, GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpPost(ApiRoutes.Session.Start)]
	public async Task<IActionResult> StartSessionAsync(long sessionId, CancellationToken cancellationToken)
	{
		var response = await _sessionService.StartSessionAsync(sessionId, GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpPost(ApiRoutes.Session.End)]
	public async Task<IActionResult> EndSessionAsync(long sessionId, CancellationToken cancellationToken)
	{
		var response = await _sessionService.EndSessionAsync(sessionId, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpGet(ApiRoutes.Session.History)]
	public async Task<IActionResult> GetSessionHistoryAsync([FromQuery] string? statusFilter, [FromQuery] int pageNumber, [FromQuery] int pageSize, CancellationToken cancellationToken)
	{
		var response = await _sessionService.GetSessionHistoryAsync(GetUserId(), statusFilter, pageNumber, pageSize, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpPost(ApiRoutes.Session.Leave)]
	public async Task<IActionResult> LeaveSessionAsync(long sessionId, CancellationToken cancellationToken)
	{
		var response = await _sessionService.LeaveSessionAsync(sessionId, GetUserId(), cancellationToken);
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
