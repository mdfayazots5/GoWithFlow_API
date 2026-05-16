using System.Data;
using GoWithFlow.Application.Common;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GoWithFlow.Infrastructure.Repositories;

public sealed class OtpRepository : GenericRepository<OtpVerification>, IOtpRepository
{
	public OtpRepository(GoWithFlowDbContext dbContext)
		: base(dbContext)
	{
	}

	public async Task<long> InsertOtpAsync(OtpVerification otpVerification, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = connection.CreateCommand();
		command.CommandText = "dbo.uspInsertOtpVerification";
		command.CommandType = CommandType.StoredProcedure;

		command.Parameters.Add(CreateParameter("@MobileNumber", otpVerification.MobileNumber));
		command.Parameters.Add(CreateParameter("@OtpCode", otpVerification.OtpCode));
		command.Parameters.Add(CreateParameter("@ExpiresAt", otpVerification.ExpiresAt));
		command.Parameters.Add(CreateParameter("@CreatedBy", otpVerification.CreatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", otpVerification.IPAddress));

		var result = await command.ExecuteScalarAsync(cancellationToken);

		return Convert.ToInt64(result);
	}

	public async Task<OtpVerificationResult> VerifyOtpAsync(string mobileNumber, string otpCode, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = connection.CreateCommand();
		command.CommandText = "dbo.uspVerifyOtp";
		command.CommandType = CommandType.StoredProcedure;

		command.Parameters.Add(CreateParameter("@MobileNumber", mobileNumber));
		command.Parameters.Add(CreateParameter("@OtpCode", otpCode));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));
		var isValidParameter = CreateOutputParameter("@IsValid", SqlDbType.Bit);
		var userIdParameter = CreateOutputParameter("@UserId", SqlDbType.BigInt);
		command.Parameters.Add(isValidParameter);
		command.Parameters.Add(userIdParameter);

		await command.ExecuteNonQueryAsync(cancellationToken);

		return new OtpVerificationResult
		{
			IsValid = isValidParameter.Value is bool isValid && isValid,
			UserId = userIdParameter.Value == DBNull.Value
				? 0
				: Convert.ToInt64(userIdParameter.Value)
		};
	}

	private static SqlParameter CreateParameter(string parameterName, object? value)
	{
		return new SqlParameter(parameterName, value ?? DBNull.Value);
	}

	private static SqlParameter CreateOutputParameter(string parameterName, SqlDbType sqlDbType)
	{
		return new SqlParameter(parameterName, sqlDbType)
		{
			Direction = ParameterDirection.Output
		};
	}

	private static async Task EnsureConnectionOpenAsync(System.Data.Common.DbConnection connection, CancellationToken cancellationToken)
	{
		if (connection.State != ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken);
		}
	}
}
