using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.Session;
using GoWithFlow.Application.DTOs.Responses.Session;
using GoWithFlow.Application.Helpers;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Application.Settings;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Domain.Enums;
using Microsoft.Extensions.Options;

namespace GoWithFlow.Application.Services;

public sealed class SessionService : ISessionService
{
	// Sentinel mobile number of the reserved AI Voice Participant system user (Phase 17). UserId is
	// IDENTITY/BIGSERIAL and differs per environment, so the AI user is always resolved by this value.
	private const string AiParticipantMobileNumber = "AI_PARTICIPANT";

	private static readonly HashSet<string> AllowedStatusFilters = new(StringComparer.OrdinalIgnoreCase)
	{
		"LOBBY",
		"ACTIVE",
		"PAUSED",
		"COMPLETED",
		"ABANDONED"
	};

	private readonly IUserRepository _userRepository;
	private readonly IScriptRepository _scriptRepository;
	private readonly ISessionRepository _sessionRepository;
	private readonly ISessionRecordingRepository _sessionRecordingRepository;
	private readonly ILiveSessionService _liveSessionService;
	private readonly IStorageService _storageService;
	private readonly CloudflareR2Settings _r2Settings;

	public SessionService(
		IUserRepository userRepository,
		IScriptRepository scriptRepository,
		ISessionRepository sessionRepository,
		ISessionRecordingRepository sessionRecordingRepository,
		ILiveSessionService liveSessionService,
		IStorageService storageService,
		IOptions<CloudflareR2Settings> r2Options)
	{
		_userRepository = userRepository;
		_scriptRepository = scriptRepository;
		_sessionRepository = sessionRepository;
		_sessionRecordingRepository = sessionRecordingRepository;
		_liveSessionService = liveSessionService;
		_storageService = storageService;
		_r2Settings = r2Options.Value;
	}

	public async Task<ApiResponse<CreateSessionResponseDto>> CreateSessionAsync(CreateSessionRequestDto dto, long hostUserId, CancellationToken cancellationToken = default)
	{
		if (hostUserId <= 0)
		{
			return ApiResponse<CreateSessionResponseDto>.FailureResult(new[] { "HostUserId must be greater than zero." }, "Validation failed.");
		}

		var hostUser = await _userRepository.GetByUserIdAsync(hostUserId, cancellationToken);

		if (hostUser is null)
		{
			return ApiResponse<CreateSessionResponseDto>.FailureResult(new[] { "Host user was not found." }, "Session creation failed.");
		}

		var script = await _scriptRepository.GetScriptByIdAsync(dto.ScriptId, cancellationToken);

		if (script is null)
		{
			return ApiResponse<CreateSessionResponseDto>.FailureResult(new[] { "Script was not found." }, "Session creation failed.");
		}

		// Derive MaxMembers and SessionMode from the script so the host cannot mismatch them.
		// ExtractSlotNames with a large cap returns all distinct speaker labels in order.
		var allSlots = ExtractSlotNames(script, byte.MaxValue);

		if (allSlots.Count < 2)
		{
			return ApiResponse<CreateSessionResponseDto>.FailureResult(
				new[] { "The selected script must have at least 2 distinct speaker labels." },
				"Session creation failed.");
		}

		var derivedMaxMembers = (byte)allSlots.Count;
		var derivedSessionMode = MapCategoryToSessionMode(script.Category);
		var slotNames = allSlots;

		var session = new Session
		{
			SessionName = dto.SessionName.Trim(),
			SessionMode = MapSessionMode(derivedSessionMode),
			MaxMembers = derivedMaxMembers,
			SessionDuration = dto.SessionDuration,
			HostUserId = hostUserId,
			ScriptId = dto.ScriptId,
			Status = SessionStatusType.LOBBY.ToString(),
			RoomExpiryMinutes = dto.RoomExpiryMinutes,
			CreatedBy = hostUser.FullName,
			IPAddress = "127.0.0.1"
		};

		var hostMember = new SessionMember
		{
			UserId = hostUserId,
			SlotIndex = 1,
			SlotName = slotNames[0],
			IsReady = true,
			IsHost = true,
			CreatedBy = hostUser.FullName,
			IPAddress = "127.0.0.1"
		};

		// AI Voice Participant (Phase 17): when enabled, the AI fills EVERY non-host slot so the
		// candidate can run the session solo. AI members share the one reserved system user and are
		// inserted (auto-ready) in the same transaction as the session + host member.
		var aiMembers = new List<SessionMember>();

		if (dto.AiEnabled)
		{
			var aiUser = await _userRepository.GetByMobileNumberAsync(AiParticipantMobileNumber, cancellationToken);

			if (aiUser is null)
			{
				return ApiResponse<CreateSessionResponseDto>.FailureResult(
					new[] { "The AI Voice Participant is not available. The reserved system user is missing — run the Phase 17 migration." },
					"Session creation failed.");
			}

			session.AiEnabled = true;
			session.AiVoiceGender = dto.AiVoiceGender;
			session.AiSpeechRate = dto.AiSpeechRate;
			session.AiQuestionDelaySec = dto.AiQuestionDelaySec;

			for (var slot = 1; slot < slotNames.Count; slot++)
			{
				aiMembers.Add(new SessionMember
				{
					UserId = aiUser.UserId,
					SlotIndex = (byte)(slot + 1),
					SlotName = slotNames[slot],
					IsReady = true,
					IsHost = false,
					IsAi = true,
					CreatedBy = "System",
					IPAddress = "127.0.0.1"
				});
			}
		}

		var result = await _sessionRepository.CreateSessionAsync(session, hostMember, aiMembers, cancellationToken);

		return ApiResponse<CreateSessionResponseDto>.SuccessResult(
			new CreateSessionResponseDto
			{
				SessionId = result.SessionId,
				SessionName = session.SessionName,
				JoinCode = result.JoinCode,
				Status = session.Status,
				ScriptTitle = script.ScriptTitle
			},
			"Session created successfully.");
	}

	public async Task<ApiResponse<SessionPreviewResponseDto>> ValidateJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
	{
		var normalizedJoinCode = NormalizeJoinCode(joinCode);

		if (string.IsNullOrWhiteSpace(normalizedJoinCode))
		{
			return ApiResponse<SessionPreviewResponseDto>.FailureResult(new[] { "JoinCode is required." }, "Validation failed.");
		}

		var preview = await _sessionRepository.GetSessionPreviewByJoinCodeAsync(normalizedJoinCode, cancellationToken);

		if (preview is null)
		{
			var reason = await _sessionRepository.CheckJoinCodeStatusAsync(normalizedJoinCode, cancellationToken);

			if (reason is null)
				return ApiResponse<SessionPreviewResponseDto>.FailureResult(new[] { "This join code does not exist. Please check and try again." }, "Session not found.");

			if (reason.Value.IsExpired)
				return ApiResponse<SessionPreviewResponseDto>.FailureResult(new[] { "This session has expired. Ask the host to create a new session." }, "Session expired.");

			if (reason.Value.Status is "ENDED" or "CANCELLED")
				return ApiResponse<SessionPreviewResponseDto>.FailureResult(new[] { $"This session has already {reason.Value.Status.ToLower()}." }, "Session unavailable.");

			if (reason.Value.CurrentMemberCount >= reason.Value.MaxMembers)
				return ApiResponse<SessionPreviewResponseDto>.FailureResult(new[] { "This session is full. No available slots." }, "Session full.");

			return ApiResponse<SessionPreviewResponseDto>.FailureResult(new[] { "This session is no longer available." }, "Session unavailable.");
		}

		return ApiResponse<SessionPreviewResponseDto>.SuccessResult(preview, "Session preview retrieved successfully.");
	}

	public async Task<ApiResponse<LobbyStateResponseDto>> GetLobbyStateAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		if (sessionId <= 0)
		{
			return ApiResponse<LobbyStateResponseDto>.FailureResult(new[] { "SessionId must be greater than zero." }, "Validation failed.");
		}

		var lobbyState = await _sessionRepository.GetLobbyStateBySessionIdAsync(sessionId, cancellationToken);

		if (lobbyState is null)
		{
			return ApiResponse<LobbyStateResponseDto>.FailureResult(new[] { "Session lobby was not found." }, "Lobby state not found.");
		}

		lobbyState.CanStart = ResolveCanStart(lobbyState);
		lobbyState.RecordingEnabled = await _sessionRecordingRepository.GetRecordingEnabledAsync(sessionId, cancellationToken);
		await ResolveLobbyAvatarsAsync(lobbyState, cancellationToken);

		return ApiResponse<LobbyStateResponseDto>.SuccessResult(lobbyState, "Lobby state retrieved successfully.");
	}

	public async Task<ApiResponse<bool>> UpdateReadyStatusAsync(UpdateReadyStatusRequestDto dto, long userId, CancellationToken cancellationToken = default)
	{
		if (dto.SessionId <= 0 || userId <= 0)
		{
			return ApiResponse<bool>.FailureResult(new[] { "SessionId and UserId must be greater than zero." }, "Validation failed.");
		}

		var user = await _userRepository.GetByUserIdAsync(userId, cancellationToken);

		if (user is null)
		{
			return ApiResponse<bool>.FailureResult(new[] { "User was not found." }, "Session ready status update failed.");
		}

		var lobbyState = await _sessionRepository.GetLobbyStateBySessionIdAsync(dto.SessionId, cancellationToken);

		if (lobbyState is null || lobbyState.Members.Any(member => member.UserId == userId) == false)
		{
			return ApiResponse<bool>.FailureResult(new[] { "Session member was not found in the lobby." }, "Session ready status update failed.");
		}

		await _sessionRepository.UpdateSessionMemberReadyStatusAsync(dto.SessionId, userId, dto.IsReady, user.FullName, "127.0.0.1", cancellationToken);

		return ApiResponse<bool>.SuccessResult(true, "Session member ready status updated successfully.");
	}

	public async Task<ApiResponse<bool>> StartSessionAsync(long sessionId, long hostUserId, CancellationToken cancellationToken = default)
	{
		var session = await _sessionRepository.GetSessionBySessionIdAsync(sessionId, cancellationToken);

		if (session is null)
		{
			return ApiResponse<bool>.FailureResult(new[] { "Session was not found." }, "Session start failed.");
		}

		if (session.HostUserId != hostUserId)
		{
			return ApiResponse<bool>.FailureResult(new[] { "Only the host can start this session." }, "Session start failed.");
		}

		if (string.Equals(session.Status, SessionStatusType.LOBBY.ToString(), StringComparison.OrdinalIgnoreCase) == false)
		{
			return ApiResponse<bool>.FailureResult(new[] { "Only lobby sessions can be started." }, "Session start failed.");
		}

		var hostUser = await _userRepository.GetByUserIdAsync(hostUserId, cancellationToken);
		var lobbyState = await _sessionRepository.GetLobbyStateBySessionIdAsync(sessionId, cancellationToken);

		if (hostUser is null || lobbyState is null)
		{
			return ApiResponse<bool>.FailureResult(new[] { "Session lobby could not be loaded." }, "Session start failed.");
		}

		if (ResolveCanStart(lobbyState) == false)
		{
			return ApiResponse<bool>.FailureResult(new[] { "All active lobby members must be ready before the session can start." }, "Session start failed.");
		}

		await _sessionRepository.UpdateSessionStatusAsync(sessionId, SessionStatusType.ACTIVE.ToString(), hostUser.FullName, "127.0.0.1", cancellationToken);
		var currentTurnResponse = await _liveSessionService.GetCurrentTurnAsync(sessionId, cancellationToken);

		if (currentTurnResponse.Success == false || currentTurnResponse.Data is null)
		{
			await _sessionRepository.UpdateSessionStatusAsync(sessionId, SessionStatusType.LOBBY.ToString(), hostUser.FullName, "127.0.0.1", cancellationToken);

				return ApiResponse<bool>.FailureResult(
					currentTurnResponse.Errors ?? new List<string> { "The session started, but the first turn could not be initialized." },
					"Session start failed.");
			}

		return ApiResponse<bool>.SuccessResult(true, "Session started successfully.");
	}

	public async Task<ApiResponse<bool>> EndSessionAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		var session = await _sessionRepository.GetSessionBySessionIdAsync(sessionId, cancellationToken);

		if (session is null)
		{
			return ApiResponse<bool>.FailureResult(new[] { "Session was not found." }, "Session end failed.");
		}

		if (string.Equals(session.Status, SessionStatusType.COMPLETED.ToString(), StringComparison.OrdinalIgnoreCase) ||
			string.Equals(session.Status, SessionStatusType.ABANDONED.ToString(), StringComparison.OrdinalIgnoreCase))
		{
			return ApiResponse<bool>.FailureResult(new[] { "Session has already ended." }, "Session end failed.");
		}

		await _sessionRepository.UpdateSessionStatusAsync(sessionId, SessionStatusType.COMPLETED.ToString(), "System", "127.0.0.1", cancellationToken);

		return ApiResponse<bool>.SuccessResult(true, "Session ended successfully.");
	}

	public async Task<ApiResponse<PagedResult<SessionListItemResponseDto>>> GetSessionHistoryAsync(long userId, string? statusFilter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<PagedResult<SessionListItemResponseDto>>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var normalizedStatusFilter = NormalizeStatusFilter(statusFilter);

		if (statusFilter is not null && normalizedStatusFilter is null)
		{
			return ApiResponse<PagedResult<SessionListItemResponseDto>>.FailureResult(new[] { "StatusFilter is invalid." }, "Validation failed.");
		}

		var history = await _sessionRepository.GetSessionHistoryAsync(
			userId,
			normalizedStatusFilter,
			pageNumber <= 0 ? 1 : pageNumber,
			pageSize <= 0 ? 20 : pageSize,
			cancellationToken);

		return ApiResponse<PagedResult<SessionListItemResponseDto>>.SuccessResult(history, "Session history retrieved successfully.");
	}

	public async Task<ApiResponse<bool>> LeaveSessionAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		if (sessionId <= 0 || userId <= 0)
		{
			return ApiResponse<bool>.FailureResult(new[] { "SessionId and UserId must be greater than zero." }, "Validation failed.");
		}

		var user = await _userRepository.GetByUserIdAsync(userId, cancellationToken);
		var lobbyState = await _sessionRepository.GetLobbyStateBySessionIdAsync(sessionId, cancellationToken);

		if (user is null || lobbyState is null)
		{
			return ApiResponse<bool>.FailureResult(new[] { "Session or user was not found." }, "Session leave failed.");
		}

		if (lobbyState.Members.Any(member => member.UserId == userId) == false)
		{
			// Member is not in the active list. Two possible causes:
			// 1. They were never a member of this session → return error.
			// 2. They were already marked inactive by OnDisconnectedAsync (WebSocket drop/reconnect race)
			//    → treat as idempotent success so the frontend leave flow completes cleanly.
			var hasMemberRecord = await _sessionRepository.HasSessionMemberAsync(sessionId, userId, cancellationToken);
			if (hasMemberRecord == false)
			{
				return ApiResponse<bool>.FailureResult(new[] { "Session member was not found in the lobby." }, "Session leave failed.");
			}

			// Already left — idempotent success.
			return ApiResponse<bool>.SuccessResult(true, "Session member left successfully.");
		}

		await _sessionRepository.UpdateSessionMemberLeftAsync(sessionId, userId, user.FullName, "127.0.0.1", cancellationToken);

		return ApiResponse<bool>.SuccessResult(true, "Session member left successfully.");
	}

	private static List<string> ExtractSlotNames(GoWithFlow.Application.DTOs.Responses.Script.ScriptDetailResponseDto script, byte maxMembers)
	{
		return script.Utterances
			.Where(utterance => string.IsNullOrWhiteSpace(utterance.SpeakerLabel) == false)
			.GroupBy(utterance => utterance.SpeakerLabel.Trim(), StringComparer.OrdinalIgnoreCase)
			.OrderBy(group => group.Min(utterance => utterance.SequenceId))
			.ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First().SpeakerLabel.Trim())
			.Take(maxMembers)
			.ToList();
	}

	private static bool ResolveCanStart(LobbyStateResponseDto lobbyState)
	{
		var activeMembers = lobbyState.Members;
		return activeMembers.Count >= 2 && activeMembers.All(member => member.IsReady);
	}

	private async Task ResolveLobbyAvatarsAsync(LobbyStateResponseDto lobbyState, CancellationToken cancellationToken)
	{
		var tasks = lobbyState.Members
			.Where(m => IsR2Key(m.AvatarUrl))
			.Select(async m =>
			{
				m.AvatarUrl = await _storageService.GetPresignedUrlAsync(
					_r2Settings.Buckets.Avatars,
					m.AvatarUrl!,
					_r2Settings.PresignedUrlExpiryMinutes.Avatars,
					cancellationToken);
			});

		await Task.WhenAll(tasks);
	}

	private static bool IsR2Key(string? value) =>
		!string.IsNullOrEmpty(value)
		&& !value.StartsWith('/')
		&& !value.StartsWith("http", StringComparison.OrdinalIgnoreCase);

	private static string NormalizeJoinCode(string joinCode)
	{
		return string.IsNullOrWhiteSpace(joinCode)
			? string.Empty
			: joinCode.Trim().ToUpperInvariant();
	}

	private static string? NormalizeStatusFilter(string? statusFilter)
	{
		if (string.IsNullOrWhiteSpace(statusFilter))
		{
			return null;
		}

		var normalizedStatus = statusFilter.Trim().ToUpperInvariant();
		return AllowedStatusFilters.Contains(normalizedStatus) ? normalizedStatus : null;
	}

	private static string MapSessionMode(SessionModeType sessionMode)
	{
		return sessionMode switch
		{
			SessionModeType.GrammarDrill => "Grammar Drill",
			SessionModeType.Roleplay => "Roleplay",
			SessionModeType.MockInterview => "Mock Interview",
			SessionModeType.VocabularySprint => "Vocabulary Sprint",
			SessionModeType.FluencyDrill => "Fluency Drill",
			SessionModeType.RepracticeRound => "Repractice Round",
			_ => throw new ArgumentOutOfRangeException(nameof(sessionMode), sessionMode, "Unsupported session mode.")
		};
	}

	private static SessionModeType MapCategoryToSessionMode(string category)
	{
		return category.Trim() switch
		{
			"Grammar Drill"    => SessionModeType.GrammarDrill,
			"Roleplay"         => SessionModeType.Roleplay,
			"Mock Interview"   => SessionModeType.MockInterview,
			"Interview"        => SessionModeType.MockInterview,   // legacy alias
			"Vocabulary Sprint" => SessionModeType.VocabularySprint,
			"Vocabulary"       => SessionModeType.VocabularySprint, // legacy alias
			"Fluency Drill"    => SessionModeType.FluencyDrill,
			"Repractice Round" => SessionModeType.RepracticeRound,
			"Repetition"       => SessionModeType.RepracticeRound,  // legacy alias
			_                  => SessionModeType.GrammarDrill
		};
	}
}
