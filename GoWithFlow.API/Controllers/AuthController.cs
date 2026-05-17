using GoWithFlow.Application.DTOs.Requests;
using GoWithFlow.API.Constants;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GoWithFlow.API.Controllers;

[ApiController]
[Route(ApiRoutes.Auth.Base)]
[EnableRateLimiting(RateLimitPolicyNames.AuthEndpoints)]
public sealed class AuthController : ControllerBase
{
	private readonly IAuthService _authService;

	public AuthController(IAuthService authService)
	{
		_authService = authService;
	}

	[HttpPost(ApiRoutes.Auth.Login)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _authService.LoginAsync(dto, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK, StatusCodes.Status401Unauthorized);
	}

	[HttpPost(ApiRoutes.Auth.Register)]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _authService.RegisterAsync(dto, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status201Created, StatusCodes.Status409Conflict);
	}

	[HttpPost(ApiRoutes.Auth.RefreshToken)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _authService.RefreshTokenAsync(dto, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK, StatusCodes.Status401Unauthorized);
	}

	[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
	[HttpPost(ApiRoutes.Auth.Logout)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> LogoutAsync([FromBody] RefreshTokenRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _authService.LogoutAsync(dto.RefreshToken, cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	private IActionResult BuildActionResult<T>(GoWithFlow.Application.Common.ApiResponse<T> response, int successStatusCode, int? failureStatusCode = null)
	{
		if (response.Success)
		{
			return StatusCode(successStatusCode, response);
		}

		var statusCode = failureStatusCode ?? ResolveFailureStatusCode(response.Message);
		return StatusCode(statusCode, response);
	}

	private static int ResolveFailureStatusCode(string message)
	{
		if (message.Contains("validation", StringComparison.OrdinalIgnoreCase))
		{
			return StatusCodes.Status400BadRequest;
		}

		if (message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
			message.Contains("invalid credentials", StringComparison.OrdinalIgnoreCase) ||
			message.Contains("refresh token", StringComparison.OrdinalIgnoreCase) ||
			message.Contains("authentication", StringComparison.OrdinalIgnoreCase))
		{
			return StatusCodes.Status401Unauthorized;
		}

		if (message.Contains("register", StringComparison.OrdinalIgnoreCase))
		{
			return StatusCodes.Status409Conflict;
		}

		return StatusCodes.Status400BadRequest;
	}
}
