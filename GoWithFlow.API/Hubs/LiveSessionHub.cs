using System.Security.Claims;
using GoWithFlow.Application.Common;
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
	private readonly ILiveSessionReconnectTracker _reconnectTracker;
	private readonly ILogger<LiveSessionHub> _logger;

	public LiveSessionHub(
		ILiveSessionService liveSessionService,
		ILiveSessionRepository liveSessionRepository,
		IHubConnectionTracker connectionTracker,
		ILiveSessionReconnectTracker reconnectTracker,
		ILogger<LiveSessionHub> logger)
	{
		_liveSessionService = liveSessionService;
		_liveSessionRepository = liveSessionRepository;
		_connectionTracker = connectionTracker;
		_reconnectTracker = reconnectTracker;
		_logger = logger;
	}

	public override async Task OnConnectedAsync()
	{
		var connectionInfo = await TryBuildConnectionMetadataAsync(Context.ConnectionAborted);

		if (connectionInfo is not null)
		{
			// Cancel any pending grace-window leave for this user+session (transient drop / reconnect path).
			// This is what prevents a momentary WebSocket drop from ever surfacing as MEMBER_LEFT.
			_reconnectTracker.CancelPendingLeave(connectionInfo.SessionId, connectionInfo.UserId);

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

			// Defer the MEMBER_LEFT broadcast + IsActive=0 update behind a 20s grace window instead of
			// firing immediately. A transient drop (mobile network handoff, app backgrounded, WebView/page
			// reconnect) otherwise made the OTHER participants see this member as "left" while they were
			// only reconnecting — and the sticky "speaker has left" banner never cleared on rejoin.
			// OnConnectedAsync cancels this if the member reconnects within the window, so a real leave
			// (browser close / genuine exit) still emits MEMBER_LEFT after the grace period, while a blip
			// emits nothing. Mirrors the lobby hub's LobbyReconnectTracker.
			_reconnectTracker.ScheduleLeave(
				connectionInfo.SessionId,
				connectionInfo.UserId,
				connectionInfo.SlotIndex,
				connectionInfo.FullName,
				connectionInfo.GroupName);
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

		_logger.LogInformation(
			"CompleteTurn received. SessionId={SessionId} MemberId={MemberId} TurnIndex={TurnIndex} Score={Score}",
			parsedSessionId, parsedMemberId, turnIndex, score);

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
			// Auto-complete is ONLY triggered when ShiftTurnAsync explicitly signals the
			// script is finished. Every other failure reason (turn index mismatch, wrong user,
			// session not active, speaker slot not found, duplicate submit) must surface as a
			// HubException so the client can react appropriately without ending the session.
			//
			// Premature auto-complete is the primary cause of sessions ending unexpectedly:
			// a stale duplicate CompleteTurn call (same turnIndex after turn already shifted)
			// returns "The provided turn does not match the active turn" — that is a client
			// sync error, not a signal that the script is done.
			//
			// The completion signal is the shared TurnShiftSignals.SessionComplete constant,
			// carried by the service in ApiResponse.Message. (Do NOT switch this back to matching
			// Errors or an inline literal — that drift caused the last turn to throw instead of
			// completing.)
			if (response.Message?.Contains(TurnShiftSignals.SessionComplete, StringComparison.OrdinalIgnoreCase) == true)
			{
				_logger.LogInformation(
					"Final turn completed — no further turns remain for SessionId={SessionId} TurnIndex={TurnIndex} MemberId={MemberId}. Completing session automatically.",
					parsedSessionId, turnIndex, parsedMemberId);

				var completeResponse = await _liveSessionService.CompleteSessionAsync(
					parsedSessionId,
					Context.ConnectionAborted);

				if (completeResponse.Success && completeResponse.Data is not null)
				{
					_logger.LogInformation(
						"Session auto-completed. SessionId={SessionId} TotalTurns={TotalTurns}",
						parsedSessionId, completeResponse.Data.TotalTurns);

					await Clients.Group(BuildGroupName(parsedSessionId)).SendAsync(
						"SESSION_ENDED",
						new { sessionId = parsedSessionId, summary = completeResponse.Data },
						Context.ConnectionAborted);
					return;
				}

				// CompleteSession itself failed — throw a clear error rather than silently dropping.
				_logger.LogError(
					"Auto-complete failed after no-further-turns. SessionId={SessionId} Message={Message}",
					parsedSessionId, completeResponse.Message);
				throw new HubException($"Session could not be completed: {completeResponse.Message}");
			}

			// ShiftTurn failed for a transient or client-side reason — surface it.
			// This is NOT a session-ending condition.
			//
			// IMPORTANT: the specific cause lives in response.Errors. response.Message is the
			// generic bucket label ("Turn shift failed.") that ShiftTurnAsync stamps on every
			// failure. Surfacing Message alone hides the real reason (turn mismatch, wrong user,
			// "No active member holds the slot 'X'", etc.) from both the client and the logs.
			var failureReason = DescribeFailure(response);
			_logger.LogWarning(
				"CompleteTurn rejected (non-completion reason). SessionId={SessionId} TurnIndex={TurnIndex} Reason={Reason}",
				parsedSessionId, turnIndex, failureReason);
			throw new HubException(failureReason);
		}

		_logger.LogInformation(
			"Turn shifted. SessionId={SessionId} NewTurnIndex={NewTurnIndex} NextSpeakerId={NextSpeakerId}",
			parsedSessionId, response.Data.TurnIndex, response.Data.ActiveMemberId);

		await BroadcastTurnShiftAsync(parsedSessionId, response.Data);
	}

	// Phase 17: advances a turn currently held by the AI Voice Participant. Any active HUMAN member
	// may call this (the AI holds no hub connection). No score is submitted; the service writes no
	// voice analysis. Completion / failure handling mirrors CompleteTurn exactly so the end-of-script
	// auto-complete and the specific-error surfacing behave identically.
	public async Task AdvanceAiTurn(string sessionId, int turnIndex)
	{
		var parsedSessionId = ParseSessionId(sessionId);
		var callerUserId = ParseCallerUserId();

		_logger.LogInformation(
			"AdvanceAiTurn received. SessionId={SessionId} TurnIndex={TurnIndex} CallerUserId={CallerUserId}",
			parsedSessionId, turnIndex, callerUserId);

		var response = await _liveSessionService.AdvanceAiTurnAsync(parsedSessionId, turnIndex, callerUserId, Context.ConnectionAborted);

		if (response.Success == false || response.Data is null)
		{
			// End-of-script auto-complete uses the same shared signal as CompleteTurn.
			if (response.Message?.Contains(TurnShiftSignals.SessionComplete, StringComparison.OrdinalIgnoreCase) == true)
			{
				var completeResponse = await _liveSessionService.CompleteSessionAsync(parsedSessionId, Context.ConnectionAborted);

				if (completeResponse.Success && completeResponse.Data is not null)
				{
					await Clients.Group(BuildGroupName(parsedSessionId)).SendAsync(
						"SESSION_ENDED",
						new { sessionId = parsedSessionId, summary = completeResponse.Data },
						Context.ConnectionAborted);
					return;
				}

				_logger.LogError(
					"AI advance auto-complete failed. SessionId={SessionId} Message={Message}",
					parsedSessionId, completeResponse.Message);
				throw new HubException($"Session could not be completed: {completeResponse.Message}");
			}

			var failureReason = DescribeFailure(response);
			_logger.LogWarning(
				"AdvanceAiTurn rejected. SessionId={SessionId} TurnIndex={TurnIndex} Reason={Reason}",
				parsedSessionId, turnIndex, failureReason);
			throw new HubException(failureReason);
		}

		await BroadcastTurnShiftAsync(parsedSessionId, response.Data);
	}

	private Task BroadcastTurnShiftAsync(long sessionId, GoWithFlow.Application.DTOs.Responses.LiveSession.TurnStateResponseDto turn)
	{
		return Clients.Group(BuildGroupName(sessionId)).SendAsync(
			"TURN_SHIFT",
			new
			{
				newActiveMemberId = turn.ActiveMemberId,
				newActiveMemberName = turn.ActiveMemberName,
				activeMemberAvatarUrl = turn.ActiveMemberAvatarUrl,
				slotIndex = turn.ActiveSlotIndex,
				turnIndex = turn.TurnIndex,
				nextUtterance = turn.Utterance,
				isAi = turn.IsAi
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
			throw new HubException(DescribeFailure(response));
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
			throw new HubException(DescribeFailure(response));
		}

		var currentTurnResponse = await _liveSessionService.GetCurrentTurnAsync(parsedSessionId, Context.ConnectionAborted);

		if (currentTurnResponse.Success == false || currentTurnResponse.Data is null)
		{
			throw new HubException(DescribeFailure(currentTurnResponse));
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
			throw new HubException(DescribeFailure(response));
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

	/// <summary>
	/// Extracts the specific, actionable failure reason from a service response.
	/// Service methods stamp a generic bucket label on <c>ApiResponse.Message</c>
	/// (e.g. "Turn shift failed.") while the real cause lives in <c>ApiResponse.Errors</c>.
	/// Surfacing only Message hides the diagnostic detail from the client and the logs,
	/// so prefer the joined Errors and fall back to Message when no errors are present.
	/// </summary>
	private static string DescribeFailure<T>(ApiResponse<T> response)
	{
		if (response.Errors is { Count: > 0 } errors)
		{
			return string.Join(" ", errors);
		}

		return string.IsNullOrWhiteSpace(response.Message) ? "Operation failed." : response.Message;
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
			// Member may be temporarily inactive due to the OnDisconnectedAsync race on a page refresh:
			//   1. Browser refreshes → WebSocket drops → OnDisconnectedAsync → MarkMemberLeftAsync → IsActive = 0
			//   2. Page reloads → new connection → OnConnectedAsync → here
			// If a member record exists (any state) they are rejoining a session they belong to.
			// Reactivate them so the session can continue without requiring a manual rejoin flow.
			var anyMember = await _liveSessionRepository.GetSessionMemberByUserIdAsync(sessionId, userId, cancellationToken);

			if (anyMember is null)
			{
				throw new HubException("Active session member was not found for the authenticated connection.");
			}

			// Reactivate — best-effort, does not throw if it fails
			await _liveSessionService.ReactivateMemberAsync(sessionId, userId, cancellationToken);
			sessionMember = anyMember;
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
