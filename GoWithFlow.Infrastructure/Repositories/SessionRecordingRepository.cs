using System.Data;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoWithFlow.Infrastructure.Repositories;

/// <summary>
/// tblSessionRecording access. PostgreSQL raw SQL (production = Supabase), matching the
/// AudioArchiveRepository.GetAllBySessionAsync precedent.
/// </summary>
public sealed class SessionRecordingRepository : ISessionRecordingRepository
{
	private readonly GoWithFlowDbContext _dbContext;

	public SessionRecordingRepository(GoWithFlowDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<bool> GetRecordingEnabledAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		var conn = await OpenAsync(cancellationToken);
		await using var cmd = conn.CreateCommand();
		cmd.CommandText = "SELECT recordingenabled FROM public.tblsession WHERE sessionid = @sid";
		AddParam(cmd, "@sid", sessionId);

		var result = await cmd.ExecuteScalarAsync(cancellationToken);
		return result is bool b && b;
	}

	public async Task<bool> SetRecordingEnabledAsync(long sessionId, long hostUserId, bool enabled, CancellationToken cancellationToken = default)
	{
		var conn = await OpenAsync(cancellationToken);
		await using var cmd = conn.CreateCommand();
		cmd.CommandText = "UPDATE public.tblsession SET recordingenabled = @e WHERE sessionid = @sid AND hostuserid = @uid";
		AddParam(cmd, "@e", enabled);
		AddParam(cmd, "@sid", sessionId);
		AddParam(cmd, "@uid", hostUserId);
		var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
		return affected > 0;
	}

	public async Task<long> CreateOrGetAsync(long sessionId, DateTime createdAt, DateTime expiresAt, CancellationToken cancellationToken = default)
	{
		var conn = await OpenAsync(cancellationToken);

		await using (var existing = conn.CreateCommand())
		{
			existing.CommandText = "SELECT recordingid FROM public.tblsessionrecording WHERE sessionid = @sid AND isdeleted = FALSE";
			AddParam(existing, "@sid", sessionId);
			var found = await existing.ExecuteScalarAsync(cancellationToken);
			if (found is not (null or DBNull))
			{
				return Convert.ToInt64(found);
			}
		}

		await using var insert = conn.CreateCommand();
		insert.CommandText = """
			INSERT INTO public.tblsessionrecording (sessionid, status, createdat, expiresat)
			VALUES (@sid, 'PENDING_MERGE', @createdat, @expiresat)
			RETURNING recordingid
			""";
		AddParam(insert, "@sid", sessionId);
		AddParam(insert, "@createdat", createdAt);
		AddParam(insert, "@expiresat", expiresAt);
		var inserted = await insert.ExecuteScalarAsync(cancellationToken);
		return Convert.ToInt64(inserted);
	}

	public async Task<SessionRecordingRow?> GetBySessionAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		var conn = await OpenAsync(cancellationToken);
		await using var cmd = conn.CreateCommand();
		cmd.CommandText = """
			SELECT recordingid, sessionid, storagekey, status, format, durationsecs, sizebytes,
			       segmentcount, participantsjson, failurereason, createdat, completedat
			FROM   public.tblsessionrecording
			WHERE  sessionid = @sid AND isdeleted = FALSE
			""";
		AddParam(cmd, "@sid", sessionId);

		await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
		if (!await reader.ReadAsync(cancellationToken))
		{
			return null;
		}

		return new SessionRecordingRow
		{
			RecordingId      = reader.GetInt64(reader.GetOrdinal("recordingid")),
			SessionId        = reader.GetInt64(reader.GetOrdinal("sessionid")),
			StorageKey       = GetNullableString(reader, "storagekey"),
			Status           = reader.GetString(reader.GetOrdinal("status")),
			Format           = GetNullableString(reader, "format"),
			DurationSecs     = GetNullableInt(reader, "durationsecs"),
			SizeBytes        = GetNullableLong(reader, "sizebytes"),
			SegmentCount     = GetNullableInt(reader, "segmentcount"),
			ParticipantsJson = GetNullableString(reader, "participantsjson"),
			FailureReason    = GetNullableString(reader, "failurereason"),
			CreatedAt        = reader.GetDateTime(reader.GetOrdinal("createdat")),
			CompletedAt      = GetNullableDateTime(reader, "completedat")
		};
	}

	public async Task<bool> TryClaimForMergeAsync(long recordingId, CancellationToken cancellationToken = default)
	{
		var conn = await OpenAsync(cancellationToken);
		await using var cmd = conn.CreateCommand();
		cmd.CommandText = """
			UPDATE public.tblsessionrecording
			SET    status = 'PROCESSING', attemptcount = attemptcount + 1
			WHERE  recordingid = @id AND isdeleted = FALSE AND status IN ('PENDING_MERGE', 'FAILED')
			""";
		AddParam(cmd, "@id", recordingId);
		var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
		return affected > 0;
	}

	public async Task MarkReadyAsync(long recordingId, string storageKey, string format, int durationSecs, long sizeBytes, int segmentCount, string participantsJson, CancellationToken cancellationToken = default)
	{
		var conn = await OpenAsync(cancellationToken);
		await using var cmd = conn.CreateCommand();
		cmd.CommandText = """
			UPDATE public.tblsessionrecording
			SET    status = 'READY', storagekey = @key, format = @fmt, durationsecs = @dur,
			       sizebytes = @size, segmentcount = @count, participantsjson = @parts::jsonb,
			       failurereason = NULL, completedat = (now() AT TIME ZONE 'utc')
			WHERE  recordingid = @id AND isdeleted = FALSE
			""";
		AddParam(cmd, "@key", storageKey);
		AddParam(cmd, "@fmt", format);
		AddParam(cmd, "@dur", durationSecs);
		AddParam(cmd, "@size", sizeBytes);
		AddParam(cmd, "@count", segmentCount);
		AddParam(cmd, "@parts", participantsJson);
		AddParam(cmd, "@id", recordingId);
		await cmd.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task MarkFailedAsync(long recordingId, string failureReason, CancellationToken cancellationToken = default)
	{
		var conn = await OpenAsync(cancellationToken);
		await using var cmd = conn.CreateCommand();
		cmd.CommandText = """
			UPDATE public.tblsessionrecording
			SET    status = 'FAILED', failurereason = @reason
			WHERE  recordingid = @id AND isdeleted = FALSE
			""";
		AddParam(cmd, "@reason", failureReason.Length > 512 ? failureReason[..512] : failureReason);
		AddParam(cmd, "@id", recordingId);
		await cmd.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<long>> GetResumableSessionIdsAsync(int maxAttempts, CancellationToken cancellationToken = default)
	{
		var conn = await OpenAsync(cancellationToken);
		await using var cmd = conn.CreateCommand();
		cmd.CommandText = """
			SELECT sessionid FROM public.tblsessionrecording
			WHERE  isdeleted = FALSE AND status IN ('PENDING_MERGE', 'FAILED') AND attemptcount < @max
			ORDER  BY createdat ASC
			""";
		AddParam(cmd, "@max", maxAttempts);

		var ids = new List<long>();
		await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
		while (await reader.ReadAsync(cancellationToken))
		{
			ids.Add(reader.GetInt64(0));
		}
		return ids;
	}

	public async Task<IReadOnlyList<(long RecordingId, string? StorageKey)>> GetExpiredAsync(int batchSize, CancellationToken cancellationToken = default)
	{
		var conn = await OpenAsync(cancellationToken);
		await using var cmd = conn.CreateCommand();
		cmd.CommandText = """
			SELECT recordingid, storagekey FROM public.tblsessionrecording
			WHERE  isdeleted = FALSE AND expiresat IS NOT NULL AND expiresat <= (now() AT TIME ZONE 'utc')
			ORDER  BY expiresat ASC
			LIMIT  @batch
			""";
		AddParam(cmd, "@batch", batchSize);

		var results = new List<(long, string?)>();
		await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
		while (await reader.ReadAsync(cancellationToken))
		{
			results.Add((reader.GetInt64(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
		}
		return results;
	}

	public async Task SoftDeleteAsync(long recordingId, CancellationToken cancellationToken = default)
	{
		var conn = await OpenAsync(cancellationToken);
		await using var cmd = conn.CreateCommand();
		cmd.CommandText = "UPDATE public.tblsessionrecording SET isdeleted = TRUE WHERE recordingid = @id";
		AddParam(cmd, "@id", recordingId);
		await cmd.ExecuteNonQueryAsync(cancellationToken);
	}

	private async Task<System.Data.Common.DbConnection> OpenAsync(CancellationToken ct)
	{
		var conn = _dbContext.Database.GetDbConnection();
		if (conn.State != ConnectionState.Open)
		{
			await conn.OpenAsync(ct);
		}
		return conn;
	}

	private static void AddParam(System.Data.Common.DbCommand cmd, string name, object? value)
	{
		var p = cmd.CreateParameter();
		p.ParameterName = name;
		p.Value = value ?? DBNull.Value;
		cmd.Parameters.Add(p);
	}

	private static string? GetNullableString(System.Data.Common.DbDataReader r, string col)
	{
		var i = r.GetOrdinal(col);
		return r.IsDBNull(i) ? null : r.GetString(i);
	}

	private static int? GetNullableInt(System.Data.Common.DbDataReader r, string col)
	{
		var i = r.GetOrdinal(col);
		return r.IsDBNull(i) ? null : r.GetInt32(i);
	}

	private static long? GetNullableLong(System.Data.Common.DbDataReader r, string col)
	{
		var i = r.GetOrdinal(col);
		return r.IsDBNull(i) ? null : r.GetInt64(i);
	}

	private static DateTime? GetNullableDateTime(System.Data.Common.DbDataReader r, string col)
	{
		var i = r.GetOrdinal(col);
		return r.IsDBNull(i) ? null : r.GetDateTime(i);
	}
}
