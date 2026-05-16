using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class OtpVerificationConfiguration : IEntityTypeConfiguration<OtpVerification>
{
	public void Configure(EntityTypeBuilder<OtpVerification> builder)
	{
		builder.ToTable("tblOtpVerification");

		builder.HasKey(otpVerification => otpVerification.OtpVerificationId)
			.HasName("PK_tblOtpVerification_OtpVerificationId");

		builder.Property(otpVerification => otpVerification.OtpVerificationId)
			.ValueGeneratedOnAdd();

		builder.Property(otpVerification => otpVerification.MobileNumber)
			.HasMaxLength(16)
			.IsRequired();

		builder.Property(otpVerification => otpVerification.OtpCode)
			.HasMaxLength(8)
			.IsRequired();

		builder.Property(otpVerification => otpVerification.ExpiresAt)
			.HasColumnType("datetime2")
			.IsRequired();

		builder.Property(otpVerification => otpVerification.IsVerified)
			.IsRequired()
			.HasDefaultValue(false);

		builder.Property(otpVerification => otpVerification.VerifiedAt)
			.HasColumnType("datetime2");

		builder.Property(otpVerification => otpVerification.AttemptCount)
			.HasDefaultValue(0);

		ConfigureAuditColumns(builder);

		builder.HasIndex(otpVerification => otpVerification.MobileNumber)
			.HasDatabaseName("IDX_tblOtpVerification_MobileNumber");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<OtpVerification> builder)
	{
		builder.Property(otpVerification => otpVerification.Tag)
			.HasMaxLength(64);

		builder.Property(otpVerification => otpVerification.Comments)
			.HasMaxLength(256);

		builder.Property(otpVerification => otpVerification.SortOrder)
			.HasDefaultValue(0);

		builder.Property(otpVerification => otpVerification.IPAddress)
			.HasMaxLength(64)
			.IsRequired()
			.HasDefaultValue("127.0.0.1");

		builder.Property(otpVerification => otpVerification.CreatedBy)
			.HasMaxLength(128)
			.IsRequired()
			.HasDefaultValue("Admin");

		builder.Property(otpVerification => otpVerification.DateCreated)
			.HasColumnType("datetime2")
			.HasDefaultValueSql("GETDATE()");

		builder.Property(otpVerification => otpVerification.UpdatedBy)
			.HasMaxLength(128);

		builder.Property(otpVerification => otpVerification.LastUpdated)
			.HasColumnType("datetime2");

		builder.Property(otpVerification => otpVerification.DeletedBy)
			.HasMaxLength(128);

		builder.Property(otpVerification => otpVerification.DateDeleted)
			.HasColumnType("datetime2");

		builder.Property(otpVerification => otpVerification.IsDeleted)
			.IsRequired()
			.HasDefaultValue(false);
	}
}
