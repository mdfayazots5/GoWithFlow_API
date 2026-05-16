using GoWithFlow.Application.DTOs.Requests.Session;
using GoWithFlow.API.Constants;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GoWithFlow.API.Hubs;

[Authorize(Policy = AuthorizationPolicies.UserOrAdmin)]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
public sealed class SessionHub : Hub
{
	private readonly ISessionService _sessionService;
	private readonly IHubConnectionTracker _connectionTracker;
	private readonly ILogger<SessionHub> _logger;

	public SessionHub(
		ISessionService sessionService,
		IHubConnectionTracker connectionTracker,
		ILogger<SessionHub> logger)
	{
		_sessionService = sessionService;
		_connectionTracker = connectionTracker;
		_logger = logger;
	}

	public override async Task OnConnectedAsync()
	{
		var connectionInfo = TryBuildConnectionMetadata();

		if (connectionInfo is not null)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, connectionInfo.GroupName, Context.ConnectionAborted);
			_connectionTracker.TrackConnection(Context.ConnectionId, connectionInfo);

			_logger.LogInformation(
				"Session hub connected. ConnectionId {ConnectionId}, SessionId {SessionId}, UserId {UserId}.",
				Context.ConnectionId,
				connectionInfo.SessionId,
				connectionInfo.UserId);
		}
		else
		{
			_logger.LogInformation("Session hub connected without a session query parameter. ConnectionId {ConnectionId}.", Context.ConnectionId);
		}

		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		if (_connectionTracker.TryRemoveConnection(Context.ConnectionId, out var connectionInfo) && connectionInfo is not null)
		{
			try
			{
				await Groups.RemoveFromGroupAsync(Context.ConnectionId, connectionInfo.GroupName);

				var lobbyStateResponse = await _sessionService.GetLobbyStateAsync(connectionInfo.SessionId, CancellationToken.None);
				var leavingMember = lobbyStateResponse.Data?.Members.FirstOrDefault(member => member.UserId == connectionInfo.UserId);
				var leaveResponse = await _sessionService.LeaveSessionAsync(connectionInfo.SessionId, connectionInfo.UserId, CancellationToken.None);

				if (leaveResponse.Success && leavingMember is not null)
				{
					await Clients.Group(connectionInfo.GroupName).SendAsync(
						"MEMBER_LEFT",
						new
						{
							userId = connectionInfo.UserId,
							slotIndex = leavingMember.SlotIndex
						});
				}
			}
			catch (Exception disconnectException)
			{
				_logger.LogWarning(
					disconnectException,
					"Session hub disconnect cleanup failed. ConnectionId {ConnectionId}, SessionId {SessionId}, UserId {UserId}.",
					Context.ConnectionId,
					connectionInfo.SessionId,
					connectionInfo.UserId);
			}
		}

		_logger.LogInformation("Session hub disconnected. ConnectionId {ConnectionId}.", Context.ConnectionId);
		await base.OnDisconnectedAsync(exception);
	}

	public async Task JoinLobby(string sessionId, string userId)
	{
		var parsedSessionId = ParseSessionId(sessionId);
		var parsedUserId = ParseAndValidateCallerUserId(userId);

		await EnsureGroupMembershipAsync(parsedSessionId, parsedUserId);

		var lobbyStateResponse = await _sessionService.GetLobbyStateAsync(parsedSessionId, Context.ConnectionAborted);

		if (lobbyStateResponse.Success == false || lobbyStateResponse.Data is null)
		{
			throw new HubException(lobbyStateResponse.Message);
		}

		var joinedMember = lobbyStateResponse.Data.Members.FirstOrDefault(member => member.UserId == parsedUserId);

		if (joinedMember is null)
		{
			throw new HubException("Session member was not found in the lobby.");
		}

		await Clients.Group(BuildGroupName(parsedSessionId)).SendAsync(
			"MEMBER_JOINED",
			new
			{
				userId = joinedMember.UserId,
				name = joinedMember.FullName,
				slotIndex = joinedMember.SlotIndex
			},
			Context.ConnectionAborted);
	}

	public async Task SetReady(string sessionId, string userId, bool isReady)
	{
		var parsedSessionId = ParseSessionId(sessionId);
		var parsedUserId = ParseAndValidateCallerUserId(userId);

		var response = await _sessionService.UpdateReadyStatusAsync(
			new UpdateReadyStatusRequestDto
			{
				SessionId = parsedSessionId,
				IsReady = isReady
			},
			parsedUserId,
			Context.ConnectionAborted);

		if (response.Success == false)
		{
			throw new HubException(response.Message);
		}

		await Clients.Group(BuildGroupName(parsedSessionId)).SendAsync(
			"MEMBER_READY",
			new
			{
				userId = parsedUserId,
				isReady
			},
			Context.ConnectionAborted);
	}

	public async Task StartSession(string sessionId)
	{
		var parsedSessionId = ParseSessionId(sessionId);
		var callerUserId = ParseCallerUserId();

		var startResponse = await _sessionService.StartSessionAsync(parsedSessionId, callerUserId, Context.ConnectionAborted);

		if (startResponse.Success == false)
		{
			throw new HubException(startResponse.Message);
		}

		var lobbyStateResponse = await _sessionService.GetLobbyStateAsync(parsedSessionId, Context.ConnectionAborted);

		if (lobbyStateResponse.Success == false || lobbyStateResponse.Data is null)
		{
			throw new HubException("Lobby state could not be loaded after session start.");
		}

		var firstSpeakerId = lobbyStateResponse.Data.Members
			.OrderBy(member => member.SlotIndex)
			.Select(member => member.UserId)
			.FirstOrDefault();

		await Clients.Group(BuildGroupName(parsedSessionId)).SendAsync(
			"SESSION_STARTED",
			new
			{
				sessionId = parsedSessionId,
				firstSpeakerId
			},
			Context.ConnectionAborted);
	}

	public async Task LeaveLobby(string sessionId, string userId)
	{
		var parsedSessionId = ParseSessionId(sessionId);
		var parsedUserId = ParseAndValidateCallerUserId(userId);
		var lobbyStateResponse = await _sessionService.GetLobbyStateAsync(parsedSessionId, Context.ConnectionAborted);

		if (lobbyStateResponse.Success == false || lobbyStateResponse.Data is null)
		{
			throw new HubException(lobbyStateResponse.Message);
		}

		var leavingMember = lobbyStateResponse.Data.Members.FirstOrDefault(member => member.UserId == parsedUserId);

		if (leavingMember is null)
		{
			throw new HubException("Session member was not found in the lobby.");
		}

		var leaveResponse = await _sessionService.LeaveSessionAsync(parsedSessionId, parsedUserId, Context.ConnectionAborted);

		if (leaveResponse.Success == false)
		{
			throw new HubException(leaveResponse.Message);
		}

		var groupName = BuildGroupName(parsedSessionId);
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
		_connectionTracker.TryRemoveConnection(Context.ConnectionId, out _);

		await Clients.Group(groupName).SendAsync(
			"MEMBER_LEFT",
			new
			{
				userId = parsedUserId,
				slotIndex = leavingMember.SlotIndex
			},
			Context.ConnectionAborted);
	}

	private long ParseAndValidateCallerUserId(string userId)
	{
		var parsedCallerUserId = ParseCallerUserId();

		if (long.TryParse(userId, out var parsedUserId) == false || parsedUserId != parsedCallerUserId)
		{
			throw new HubException("User identifier does not match the authenticated connection.");
		}

		return parsedCallerUserId;
	}

	private long ParseCallerUserId()
	{
		if (long.TryParse(Context.UserIdentifier, out var userId))
		{
			return userId;
		}

		throw new HubException("Authenticated user identifier is missing.");
	}

	private async Task EnsureGroupMembershipAsync(long sessionId, long userId)
	{
		var groupName = BuildGroupName(sessionId);

		await Groups.AddToGroupAsync(Context.ConnectionId, groupName, Context.ConnectionAborted);
		_connectionTracker.TrackConnection(Context.ConnectionId, new HubConnectionMetadata(sessionId, userId, groupName));
	}

	private static long ParseSessionId(string sessionId)
	{
		if (long.TryParse(sessionId, out var parsedSessionId) && parsedSessionId > 0)
		{
			return parsedSessionId;
		}

		throw new HubException("Session identifier is invalid.");
	}

	private static string BuildGroupName(long sessionId)
	{
		return $"session_{sessionId}";
	}

	private HubConnectionMetadata? TryBuildConnectionMetadata()
	{
		var sessionIdValue = Context.GetHttpContext()?.Request.Query["sessionId"].ToString();

		if (string.IsNullOrWhiteSpace(sessionIdValue))
		{
			return null;
		}

		var sessionId = ParseSessionId(sessionIdValue);
		var userId = ParseCallerUserId();

		return new HubConnectionMetadata(sessionId, userId, BuildGroupName(sessionId));
	}
}
