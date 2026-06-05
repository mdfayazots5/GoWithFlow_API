using System.Data;
using System.Data.Common;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoWithFlow.Infrastructure.Repositories;

public sealed class AudioArchiveRepository : IAudioArchiveRepository
{
	private readonly GoWithFlowDbContext _dbContext;

	public AudioArchiveRepository(GoWithFlowDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<long> InsertAsync(long sessionId, long userId, int turnIndex, string storageKey, int durationSecs, DateTime expiresAt, string createdBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateSpCommandAsync("dbo.uspInsertAudioArchive", cancellationToken);
		command.Parameters.Add(CreateParameter("@SessionId", sessionId));
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@TurnIndex", turnIndex));
		command.Parameters.Add(CreateParameter("@StorageKey", storageKey));
		command.Parameters.Add(CreateParameter("@DurationSecs", durationSecs));
		command.Parameters.Add(CreateParameter("@ExpiresAt", expiresAt));
		command.Parameters.Add(CreateParameter("@CreatedBy", createdBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		var result = await DbCommandHelper.ExecuteScalarAsync(command, cancellationToken);
		return Convert.ToInt64(result);
	}

	public async Task<List<AudioArchiveItemDto>> GetBySessionAndUserAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateSpCommandAsync("dbo.uspGetAudioArchiveBySessionAndUser", cancellationToken);
		command.Parameters.Add(CreateParameter("@SessionId", sessionId));
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
		var items = new List<AudioArchiveItemDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(new AudioArchiveItemDto
			{
				ArchiveId   = reader.GetInt64(reader.GetOrdinal("ArchiveId")),
				SessionId   = reader.GetInt64(reader.GetOrdinal("SessionId")),
				TurnIndex   = reader.GetInt32(reader.GetOrdinal("TurnIndex")),
				AudioUrl    = reader.GetString(reader.GetOrdinal("StorageKey")),
				DurationSecs = reader.GetInt32(reader.GetOrdinal("DurationSecs")),
				ExpiresAt   = reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
				DateCreated = reader.GetDateTime(reader.GetOrdinal("DateCreated"))
			});
		}

		return items;
	}

	public async Task<List<AudioArchiveItemDto>> GetAllBySessionAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		var conn = _dbContext.Database.GetDbConnection();
		if (conn.State != System.Data.ConnectionState.Open)
			await conn.OpenAsync(cancellationToken);

		await using var cmd = conn.CreateCommand();
		cmd.CommandText = """
			SELECT aa.archiveid, aa.sessionid, aa.userid, aa.turnindex, aa.storagekey,
			       aa.durationsecs, aa.expiresat, aa.datecreated, u.fullname
			FROM   tblaudioarchive aa
			INNER JOIN tbluser u ON u.userid = aa.userid AND u.isdeleted = FALSE
			WHERE  aa.sessionid = @sid AND aa.isdeleted = FALSE
			ORDER  BY aa.turnindex ASC, aa.datecreated ASC
			""";
		var p = cmd.CreateParameter(); p.ParameterName = "@sid"; p.Value = sessionId; cmd.Parameters.Add(p);

		await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
		var items = new List<AudioArchiveItemDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(new AudioArchiveItemDto
			{
				ArchiveId    = reader.GetInt64(reader.GetOrdinal("archiveid")),
				SessionId    = reader.GetInt64(reader.GetOrdinal("sessionid")),
				UserId       = reader.GetInt64(reader.GetOrdinal("userid")),
				TurnIndex    = reader.GetInt32(reader.GetOrdinal("turnindex")),
				AudioUrl     = reader.GetString(reader.GetOrdinal("storagekey")),
				DurationSecs = reader.GetInt32(reader.GetOrdinal("durationsecs")),
				ExpiresAt    = reader.GetDateTime(reader.GetOrdinal("expiresat")),
				DateCreated  = reader.GetDateTime(reader.GetOrdinal("datecreated")),
				UserName     = reader.GetString(reader.GetOrdinal("fullname"))
			});
		}

		return items;
	}

	public async Task DeleteAsync(long archiveId, long userId, string deletedBy, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateSpCommandAsync("dbo.uspDeleteAudioArchive", cancellationToken);
		command.Parameters.Add(CreateParameter("@ArchiveId", archiveId));
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@DeletedBy", deletedBy));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task<string?> GetStorageKeyAsync(long archiveId, long userId, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		if (connection.State != ConnectionState.Open)
			await connection.OpenAsync(cancellationToken);

		await using var cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT StorageKey FROM dbo.tblAudioArchive WHERE ArchiveId = @id AND UserId = @uid AND IsDeleted = 0";
		cmd.CommandType = CommandType.Text;
		var p1 = cmd.CreateParameter(); p1.ParameterName = "@id";  p1.Value = archiveId; cmd.Parameters.Add(p1);
		var p2 = cmd.CreateParameter(); p2.ParameterName = "@uid"; p2.Value = userId;    cmd.Parameters.Add(p2);
		var result = await cmd.ExecuteScalarAsync(cancellationToken);
		return result is DBNull or null ? null : result.ToString();
	}

	private async Task<DbCommand> CreateSpCommandAsync(string spName, CancellationToken ct)
	{
		var conn = _dbContext.Database.GetDbConnection();
		if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);
		var cmd = conn.CreateCommand();
		cmd.CommandText = DbCommandHelper.QualifyRoutineName(_dbContext.DatabaseProvider, spName);
		cmd.CommandType = CommandType.StoredProcedure;
		return cmd;
	}

	private DbParameter CreateParameter(string name, object? value)
		=> DbCommandHelper.CreateParameter(_dbContext.DatabaseProvider, name, value);
}
