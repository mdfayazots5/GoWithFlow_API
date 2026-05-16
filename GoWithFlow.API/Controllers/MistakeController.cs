using System.Security.Claims;
using GoWithFlow.API.Constants;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoWithFlow.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.UserOrAdmin)]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
[Route(ApiRoutes.Mistake.Base)]
public sealed class MistakeController : ControllerBase
{
	private readonly IMistakeService _mistakeService;
	private readonly IRepracticeService _repracticeService;

	public MistakeController(IMistakeService mistakeService, IRepracticeService repracticeService)
	{
		_mistakeService = mistakeService;
		_repracticeService = repracticeService;
	}

	[HttpGet]
	public async Task<IActionResult> GetMistakesAsync([FromQuery] MistakeFilterRequestDto dto, CancellationToken cancellationToken)
	{
		var response = await _mistakeService.GetMistakesAsync(dto, GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpGet(ApiRoutes.Mistake.Summary)]
	public async Task<IActionResult> GetMistakeSummaryAsync(CancellationToken cancellationToken)
	{
		var response = await _mistakeService.GetMistakeSummaryAsync(GetUserId(), cancellationToken);
		return BuildActionResult(response, StatusCodes.Status200OK);
	}

	[HttpGet(ApiRoutes.Mistake.GrammarProgress)]
	public async Task<IActionResult> GetGrammarProgressAsync(CancellationToken cancellationToken)
	{
		var response = await _mistakeService.GetGrammarProgressAsync(GetUserId(), cancellationToken);
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
