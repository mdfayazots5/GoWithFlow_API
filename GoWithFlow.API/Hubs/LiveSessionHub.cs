using System.Security.Claims;
using GoWithFlow.Application.DTOs.Requests.LiveSession;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.API.Constants;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GoWithFlow.API.Hubs;

[Authorize(Policy = AuthorizationPolicies.UserOrAdmin)]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
public sealed class LiveSessionHub : Hub
{
	private readonly ILiveSessionService _liveSessionService;
	private readonly ILiveSessionRepository _liveSessionRepository;
	private readonly IHubConnectionTracker _connectionTracker;
	private readonly ILogger<LiveSessionHub> _logger;

	public LiveSessionHub(
		ILiveSessionService liveSessionService,
		ILiveSessionRepository liveSessionRepository,
		IHubConnectionTracker connectionTracker,
		ILogger<LiveSessionHub> logger)
	{
		_liveSessionService = liveSessionService;
		_liveSessionRepository = liveSessionRepository;
		_connectionTracker = connectionTracker;
		_logger = logger;
	}

	public override async Task OnConnectedAsync()
	{
		var connectionInfo = await TryBuildConnectionMetadataAsync(Context.ConnectionAborted);

		if (connectionInfo is not null)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, connectionInfo.GroupName, Context.ConnectionAborted);
			_connectionTracker.TrackConnection(Context.ConnectionId, connectionInfo);

			_logger.LogInformation(
				"Live session hub connected. ConnectionId {ConnectionId}, SessionId {SessionId}, UserId {UserId}.",
				Context.ConnectionId,
				connectionInfo.SessionId,
				connectionInfo.UserId);
		}

		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		if (_connectionTracker.TryRemoveConnection(Context.ConnectionId, out var connectionInfo) && connectionInfo is not null)
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, connectionInfo.GroupName);
			await Clients.Group(connectionInfo.GroupName).SendAsync(
				"MEMBER_LEFT",
				new
				{
					userId = connectionInfo.UserId,
					slotIndex = connectionInfo.SlotIndex
				});

			// Best-effort DB update — ensures IsActive=0, IsReady=0 even on browser close or network loss
			await _liveSessionService.MarkMemberLeftAsync(connectionInfo.SessionId, connectionInfo.UserId);
		}

		_logger.LogInformation("Live session hub disconnected. ConnectionId {ConnectionId}.", Context.ConnectionId);
		await base.OnDisconnectedAsync(exception);
	}

	public async Task JoinLiveSession(string sessionId, string userId)
	{
		var parsedSessionId = ParseSessionId(sessionId);
		var parsedUserId = ParseAndValidateCallerUserId(userId);

		var connectionInfo = await EnsureGroupMembershipAsync(parsedSessionId, parsedUserId);

		await Clients.Group(connectionInfo.GroupName).SendAsync(
			"MEMBER_JOINED",
			new
			{
				userId = connectionInfo.UserId,
				name = connectionInfo.FullName,
				slotIndex = connectionInfo.SlotIndex
			},
			Context.ConnectionAborted);
	}

	public async Task CompleteTurn(string sessionId, string memberId, int turnIndex, decimal score)
	{
		var parsedSessionId = ParseSessionId(sessionId);
		var parsedMemberId = ParseAndValidateCallerUserId(memberId);

		var response = await _liveSessionService.ShiftTurnAsync(
			new TurnShiftRequestDto
			{
				SessionId = parsedSessionId,
				MemberId = parsedMemberId,
				TurnIndex = turnIndex,
				AnalysisScore = score
			},
			parsedMemberId,
			Context.ConnectionAborted);

		if (response.Success == false || response.Data is null)
		{
			throw new HubException(response.Message);
		}

		await Clients.Group(BuildGroupName(parsedSessionId)).SendAsync(
			"TURN_SHIFT",
			new
			{
				newActiveMemberId = response.Data.ActiveMemberId,
				slotIndex = response.Data.ActiveSlotIndex,
				turnIndex = response.Data.TurnIndex,
				nextUtterance = response.Data.Utterance
			},
			Context.ConnectionAborted);
	}

	public async Task SubmitListenerFeedback(string sessionId, string tag, int targetTurnIndex)
	{
		var parsedSessionId = ParseSessionId(sessionId);
		var callerUserId = ParseCallerUserId();

		var response = await _liveSessionService.SubmitListenerFeedbackByTurnIndexAsync(
			parsedSessionId,
			tag,
			targetTurnIndex,
			callerUserId,
			Context.ConnectionAborted);

		if (response.Success == false)
		{
			throw new HubException(response.Message);
		}

		await Clients.Group(BuildGroupName(parsedSessionId)).SendAsync(
			"LISTENER_TAG",
			new
			{
				tag,
				fromUserId = callerUserId
			},
			Context.ConnectionAborted);
	}

	public async Task RequestReRead(string sessionId, string requesterId)
	{
		var parsedSessionId = ParseSessionId(sessionId);
		var parsedRequesterId = ParseAndValidateCallerUserId(requesterId);

		var response = await _liveSessionService.RequestReReadAsync(parsedSessionId, parsedRequesterId, Context.ConnectionAborted);

		if (response.Success == false)
		{
			throw new HubException(response.Message);
		}

		var currentTurnResponse = await _liveSessionService.GetCurrentTurnAsync(parsedSessionId, Context.ConnectionAborted);

		if (currentTurnResponse.Success == false || currentTurnResponse.Data is null)
		{
			throw new HubException(currentTurnResponse.Message);
		}

		await Clients.Group(BuildGroupName(parsedSessionId)).SendAsync(
			"RE_READ_REQUESTED",
			new
			{
				requesterId = parsedRequesterId,
				reReadCount = currentTurnResponse.Data.ReReadCount
			},
			Context.ConnectionAborted);
	}

	public async Task EndSession(string sessionId)
	{
		var parsedSessionId = ParseSessionId(sessionId);
		var response = await _liveSessionService.CompleteSessionAsync(parsedSessionId, Context.ConnectionAborted);

		if (response.Success == false || response.Data is null)
		{
			throw new HubException(response.Message);
		}

		await Clients.Group(BuildGroupName(parsedSessionId)).SendAsync(
			"SESSION_ENDED",
			new
			{
				sessionId = parsedSessionId,
				summary = response.Data
			},
			Context.ConnectionAborted);
	}

	// ── WebRTC voice broadcast relay methods (stateless, no DB interaction) ──

	public async Task VoiceBroadcastStart(string sessionId, string speakerId)
	{
		var parsedSessionId = ParseSessionId(sessionId);
		ParseAndValidateCallerUserId(speakerId);

		await Clients.Group(BuildGroupName(parsedSessionId)).SendAsync(
			"VOICE_BROADCAST_STARTED",
			new { speakerId },
			Context.ConnectionAborted);
	}

	public async Task VoiceBroadcastStop(string sessionId, string speakerId)
	{
		var parsedSessionId = ParseSessionId(sessionId);
		ParseAndValidateCallerUserId(speakerId);

		await Clients.Group(BuildGroupName(parsedSessionId)).SendAsync(
			"VOICE_BROADCAST_STOPPED",
			new { speakerId },
			Context.ConnectionAborted);
	}

	public async Task RequestVoiceStream(string sessionId, string listenerUserId)
	{
		var parsedSessionId = ParseSessionId(sessionId);
		ParseAndValidateCallerUserId(listenerUserId);

		await Clients.Group(BuildGroupName(parsedSessionId)).SendAsync(
			"VOICE_STREAM_REQUESTED",
			new { listenerUserId },
			Context.ConnectionAborted);
	}

	public async Task SendWebRTCOffer(string sessionId, string toUserId, string offerJson)
	{
		var parsedSessionId = ParseSessionId(sessionId);
		var fromUserId = ParseCallerUserId().ToString();

		await Clients.Group(BuildGroupName(parsedSessionId)).SendAsync(
			"WEBRTC_OFFER",
			new { fromUserId, toUserId, offerJson },
			Context.ConnectionAborted);
	}

	public async Task SendWebRTCAnswer(string sessionId, string toUserId, string answerJson)
	{
		var parsedSessionId = ParseSessionId(sessionId);
		var fromUserId = ParseCallerUserId().ToString();

		await Clients.Group(BuildGroupName(parsedSessionId)).SendAsync(
			"WEBRTC_ANSWER",
			new { fromUserId, toUserId, answerJson },
			Context.ConnectionAborted);
	}

	public async Task SendICECandidate(string sessionId, string toUserId, string candidateJson)
	{
		var parsedSessionId = ParseSessionId(sessionId);
		var fromUserId = ParseCallerUserId().ToString();

		await Clients.Group(BuildGroupName(parsedSessionId)).SendAsync(
			"ICE_CANDIDATE",
			new { fromUserId, toUserId, candidateJson },
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

	private async Task<HubConnectionMetadata> EnsureGroupMembershipAsync(long sessionId, long userId)
	{
		var metadata = await ResolveConnectionMetadataAsync(sessionId, userId, Context.ConnectionAborted);

		await Groups.AddToGroupAsync(Context.ConnectionId, metadata.GroupName, Context.ConnectionAborted);
		_connectionTracker.TrackConnection(Context.ConnectionId, metadata);

		return metadata;
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
		return $"live_{sessionId}";
	}

	private async Task<HubConnectionMetadata?> TryBuildConnectionMetadataAsync(CancellationToken cancellationToken)
	{
		var sessionIdValue = Context.GetHttpContext()?.Request.Query["sessionId"].ToString();

		if (string.IsNullOrWhiteSpace(sessionIdValue))
		{
			return null;
		}

		var sessionId = ParseSessionId(sessionIdValue);
		var userId = ParseCallerUserId();

		return await ResolveConnectionMetadataAsync(sessionId, userId, cancellationToken);
	}

	private async Task<HubConnectionMetadata> ResolveConnectionMetadataAsync(long sessionId, long userId, CancellationToken cancellationToken)
	{
		var sessionMember = await _liveSessionRepository.GetActiveSessionMemberByUserIdAsync(sessionId, userId, cancellationToken);

		if (sessionMember is null)
		{
			throw new HubException("Active session member was not found for the authenticated connection.");
		}

		return new HubConnectionMetadata(
			sessionId,
			userId,
			BuildGroupName(sessionId),
			GetCallerFullName(),
			sessionMember.SlotIndex);
	}

	private string? GetCallerFullName()
	{
		return Context.User?.FindFirst("FullName")?.Value
			?? Context.User?.FindFirst(ClaimTypes.Name)?.Value;
	}
}
