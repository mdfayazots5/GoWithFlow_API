using System.Security.Claims;
using GoWithFlow.API.Constants;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoWithFlow.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.UserOrAdmin)]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
[Route(ApiRoutes.Vocabulary.Base)]
public sealed class VocabularyController : ControllerBase
{
	private readonly IVocabularyService _vocabularyService;

	public VocabularyController(IVocabularyService vocabularyService)
	{
		_vocabularyService = vocabularyService;
	}

	[HttpGet(ApiRoutes.Vocabulary.Bank)]
	public async Task<IActionResult> GetVocabularyBankAsync(CancellationToken cancellationToken)
	{
		var response = await _vocabularyService.GetVocabularyBankAsync(GetUserId(), cancellationToken);
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

	private IActionResult BuildActionResult<T>(GoWithFlow.Application.Common.ApiResponse<T> response, int successStatusCode)
	{
		if (response.Success)
			return StatusCode(successStatusCode, response);

		return StatusCode(StatusCodes.Status400BadRequest, response);
	}
}
