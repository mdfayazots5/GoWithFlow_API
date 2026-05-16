using System.Security.Claims;
using GoWithFlow.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace GoWithFlow.API.Authorization;

public sealed class ActiveUserRequirementHandler : AuthorizationHandler<ActiveUserRequirement>
{
	private readonly IUserRepository _userRepository;
	private readonly ILogger<ActiveUserRequirementHandler> _logger;

	public ActiveUserRequirementHandler(
		IUserRepository userRepository,
		ILogger<ActiveUserRequirementHandler> logger)
	{
		_userRepository = userRepository;
		_logger = logger;
	}

	protected override async Task HandleRequirementAsync(
		AuthorizationHandlerContext context,
		ActiveUserRequirement requirement)
	{
		var claimValue = context.User.FindFirstValue("UserId") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (long.TryParse(claimValue, out var userId) == false)
		{
			_logger.LogWarning("Active user authorization failed because the user identifier claim was missing.");
			return;
		}

		var user = await _userRepository.GetByUserIdAsync(userId);

		if (user?.IsActive == true)
		{
			context.Succeed(requirement);
			return;
		}

		_logger.LogWarning("Active user authorization failed for UserId {UserId}.", userId);
	}
}
