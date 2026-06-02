using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.Session;
using GoWithFlow.Application.DTOs.Responses.Session;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;

namespace GoWithFlow.Application.Services;

public sealed class SessionInvitationService : ISessionInvitationService
{
	private static readonly HashSet<string> ValidResponseStatuses = new(StringComparer.OrdinalIgnoreCase)
	{
		"ACCEPTED",
		"DECLINED"
	};

	private readonly ISessionRepository _sessionRepository;
	private readonly ISessionInvitationRepository _invitationRepository;
	private readonly IUserRepository _userRepository;
	private readonly ISessionNotifier _notifier;

	public SessionInvitationService(
		ISessionRepository sessionRepository,
		ISessionInvitationRepository invitationRepository,
		IUserRepository userRepository,
		ISessionNotifier notifier)
	{
		_sessionRepository = sessionRepository;
		_invitationRepository = invitationRepository;
		_userRepository = userRepository;
		_notifier = notifier;
	}

	public async Task<ApiResponse<List<SessionInvitationDto>>> SendInvitationsAsync(
		SendInvitationsRequestDto dto,
		long hostUserId,
		CancellationToken cancellationToken = default)
	{
		if (dto.SessionId <= 0 || dto.Assignments.Count == 0)
		{
			return ApiResponse<List<SessionInvitationDto>>.FailureResult(
				new[] { "SessionId and at least one slot assignment are required." }, "Validation failed.");
		}

		var session = await _sessionRepository.GetSessionBySessionIdAsync(dto.SessionId, cancellationToken);

		if (session is null)
		{
			return ApiResponse<List<SessionInvitationDto>>.FailureResult(
				new[] { "Session was not found." }, "Send invitations failed.");
		}

		if (session.HostUserId != hostUserId)
		{
			return ApiResponse<List<SessionInvitationDto>>.FailureResult(
				new[] { "Only the session host can send invitations." }, "Send invitations failed.");
		}

		if (session.Status != "LOBBY")
		{
			return ApiResponse<List<SessionInvitationDto>>.FailureResult(
				new[] { "Invitations can only be sent for sessions in LOBBY status." }, "Send invitations failed.");
		}

		var host = await _userRepository.GetByUserIdAsync(hostUserId, cancellationToken);
		var slots = await _sessionRepository.GetAvailableSlotsBySessionIdAsync(dto.SessionId, cancellationToken);
		var slotMap = slots.ToDictionary(s => s.SlotIndex);

		var expiresAt = session.ScheduledAt.HasValue
			? session.ScheduledAt.Value.AddHours(1)
			: DateTime.UtcNow.AddHours(24);

		foreach (var assignment in dto.Assignments)
		{
			if (assignment.UserId <= 0 || assignment.UserId == hostUserId)
			{
				continue;
			}

			if (slotMap.TryGetValue(assignment.SlotIndex, out var slot) == false)
			{
				continue;
			}

			await _invitationRepository.InsertInvitationAsync(
				dto.SessionId,
				assignment.UserId,
				assignment.SlotIndex,
				slot.SlotName,
				expiresAt,
				host?.FullName ?? "Host",
				"127.0.0.1",
				cancellationToken);
		}

		var invitations = await _invitationRepository.GetInvitationsBySessionIdAsync(dto.SessionId, cancellationToken);

		// Push real-time notification to each invited user
		foreach (var inv in invitations.Where(i => i.Status == "PENDING"))
		{
			try
			{
				await _notifier.NotifyInvitationReceivedAsync(inv.UserId, new
				{
					invitationId  = inv.InvitationId,
					sessionId     = inv.SessionId,
					sessionName   = session.SessionName,
					sessionMode   = session.SessionMode,
					slotName      = inv.SlotName,
					hostName      = host?.FullName ?? "Host",
					scheduledAt   = session.ScheduledAt
				}, cancellationToken);
			}
			catch
			{
				// Non-fatal — user may not be connected; they will see it on next dashboard load
			}
		}

		return ApiResponse<List<SessionInvitationDto>>.SuccessResult(
			invitations, "Invitations sent successfully.");
	}

	public async Task<ApiResponse<bool>> RespondToInvitationAsync(
		long invitationId,
		RespondToInvitationRequestDto dto,
		long userId,
		CancellationToken cancellationToken = default)
	{
		if (invitationId <= 0 || userId <= 0)
		{
			return ApiResponse<bool>.FailureResult(
				new[] { "InvitationId and UserId must be greater than zero." }, "Validation failed.");
		}

		var normalizedStatus = dto.Status?.Trim().ToUpperInvariant() ?? string.Empty;

		if (ValidResponseStatuses.Contains(normalizedStatus) == false)
		{
			return ApiResponse<bool>.FailureResult(
				new[] { "Status must be ACCEPTED or DECLINED." }, "Validation failed.");
		}

		var invitation = await _invitationRepository.GetInvitationByIdAsync(invitationId, cancellationToken);

		if (invitation is null || invitation.Value.UserId != userId)
		{
			return ApiResponse<bool>.FailureResult(
				new[] { "Invitation was not found." }, "Response failed.");
		}

		if (invitation.Value.Status != "PENDING")
		{
			return ApiResponse<bool>.FailureResult(
				new[] { $"This invitation has already been {invitation.Value.Status.ToLower()}." }, "Response failed.");
		}

		var user = await _userRepository.GetByUserIdAsync(userId, cancellationToken);

		await _invitationRepository.UpdateInvitationStatusAsync(
			invitationId,
			userId,
			normalizedStatus,
			user?.FullName ?? "User",
			"127.0.0.1",
			cancellationToken);

		if (normalizedStatus == "ACCEPTED")
		{
			var hasMember = await _sessionRepository.HasSessionMemberAsync(invitation.Value.SessionId, userId, cancellationToken);

			if (hasMember == false)
			{
				await _sessionRepository.JoinSessionAsync(
					new Domain.Entities.SessionMember
					{
						SessionId = invitation.Value.SessionId,
						UserId = userId,
						SlotIndex = invitation.Value.SlotIndex,
						SlotName = invitation.Value.SlotName,
						IsReady = false,
						IsHost = false,
						CreatedBy = user?.FullName ?? "User",
						IPAddress = "127.0.0.1"
					},
					cancellationToken);
			}
		}

		// Notify the session host about the response
		try
		{
			await _notifier.NotifyInvitationRespondedAsync(0, invitation.Value.SessionId, new
			{
				invitationId = invitationId,
				userId       = userId,
				fullName     = user?.FullName,
				slotName     = invitation.Value.SlotName,
				status       = normalizedStatus
			}, cancellationToken);
		}
		catch { }

		return ApiResponse<bool>.SuccessResult(true, $"Invitation {normalizedStatus.ToLower()} successfully.");
	}

	public async Task<ApiResponse<bool>> CancelInvitationAsync(
		long invitationId,
		long sessionId,
		long hostUserId,
		CancellationToken cancellationToken = default)
	{
		if (invitationId <= 0 || sessionId <= 0)
		{
			return ApiResponse<bool>.FailureResult(
				new[] { "InvitationId and SessionId must be greater than zero." }, "Validation failed.");
		}

		var session = await _sessionRepository.GetSessionBySessionIdAsync(sessionId, cancellationToken);

		if (session is null || session.HostUserId != hostUserId)
		{
			return ApiResponse<bool>.FailureResult(
				new[] { "Session not found or you are not the host." }, "Cancel failed.");
		}

		var host = await _userRepository.GetByUserIdAsync(hostUserId, cancellationToken);

		var invitation = await _invitationRepository.GetInvitationByIdAsync(invitationId, cancellationToken);

		await _invitationRepository.CancelInvitationAsync(
			invitationId, sessionId, host?.FullName ?? "Host", cancellationToken);

		if (invitation is not null)
		{
			try
			{
				await _notifier.NotifyInvitationCancelledAsync(invitation.Value.UserId, new
				{
					invitationId = invitationId,
					sessionId    = sessionId,
					sessionName  = session.SessionName
				}, cancellationToken);
			}
			catch { }
		}

		return ApiResponse<bool>.SuccessResult(true, "Invitation cancelled.");
	}

	public async Task<ApiResponse<List<SessionInvitationDto>>> GetSessionInvitationsAsync(
		long sessionId,
		long hostUserId,
		CancellationToken cancellationToken = default)
	{
		if (sessionId <= 0)
		{
			return ApiResponse<List<SessionInvitationDto>>.FailureResult(
				new[] { "SessionId must be greater than zero." }, "Validation failed.");
		}

		var session = await _sessionRepository.GetSessionBySessionIdAsync(sessionId, cancellationToken);

		if (session is null || session.HostUserId != hostUserId)
		{
			return ApiResponse<List<SessionInvitationDto>>.FailureResult(
				new[] { "Session not found or you are not the host." }, "Get invitations failed.");
		}

		var invitations = await _invitationRepository.GetInvitationsBySessionIdAsync(sessionId, cancellationToken);

		return ApiResponse<List<SessionInvitationDto>>.SuccessResult(invitations, "Invitations retrieved successfully.");
	}

	public async Task<ApiResponse<List<UserInvitationDto>>> GetMyInvitationsAsync(
		long userId,
		CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<List<UserInvitationDto>>.FailureResult(
				new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var invitations = await _invitationRepository.GetPendingInvitationsByUserIdAsync(userId, cancellationToken);

		return ApiResponse<List<UserInvitationDto>>.SuccessResult(invitations, "Invitations retrieved successfully.");
	}

	public async Task<ApiResponse<List<UserSearchResultDto>>> SearchUsersAsync(
		string searchTerm,
		long excludeUserId,
		CancellationToken cancellationToken = default)
	{
		var trimmed = searchTerm?.Trim() ?? string.Empty;

		if (trimmed.Length < 2)
		{
			return ApiResponse<List<UserSearchResultDto>>.FailureResult(
				new[] { "Search term must be at least 2 characters." }, "Validation failed.");
		}

		var results = await _userRepository.SearchUsersByNameAsync(trimmed, excludeUserId, cancellationToken);

		return ApiResponse<List<UserSearchResultDto>>.SuccessResult(results, "Users retrieved successfully.");
	}
}
