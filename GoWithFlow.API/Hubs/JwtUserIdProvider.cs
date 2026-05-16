using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace GoWithFlow.API.Hubs;

public sealed class JwtUserIdProvider : IUserIdProvider
{
	public string? GetUserId(HubConnectionContext connection)
	{
		return connection.User?.FindFirst("UserId")?.Value
			?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
	}
}
