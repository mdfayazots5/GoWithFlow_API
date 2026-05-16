using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.ToTable("tblUser");

		builder.HasKey(user => user.UserId)
			.HasName("PK_tblUser_UserId");

		builder.Property(user => user.UserId)
			.ValueGeneratedOnAdd();

		builder.Property(user => user.FullName)
			.HasMaxLength(128)
			.IsRequired();

		builder.Property(user => user.MobileNumber)
			.HasMaxLength(16)
			.IsRequired();

		builder.Property(user => user.Email)
			.HasMaxLength(128);

		builder.Property(user => user.PasswordHash)
			.HasMaxLength(512);

		builder.Property(user => user.AgeGroup)
			.HasMaxLength(32)
			.IsRequired();

		builder.Property(user => user.PreferredHintLanguage)
			.HasMaxLength(32)
			.IsRequired();

		builder.Property(user => user.AvatarUrl)
			.HasMaxLength(256);

		builder.Property(user => user.GroupCode)
			.HasMaxLength(32);

		builder.Property(user => user.Role)
			.HasMaxLength(16)
			.IsRequired()
			.HasDefaultValue("USER");

		builder.Property(user => user.DailyStreakCount)
			.HasDefaultValue(0);

		builder.Property(user => user.TotalSessionsPlayed)
			.HasDefaultValue(0);

		builder.Property(user => user.LastLoginDate)
			.HasColumnType("datetime2");

		builder.Property(user => user.IsActive)
			.HasDefaultValue(true);

		builder.Property(user => user.RegistrationDate)
			.HasColumnType("datetime2")
			.HasDefaultValueSql("GETDATE()");

		ConfigureAuditColumns(builder);

		builder.HasIndex(user => user.MobileNumber)
			.IsUnique()
			.HasDatabaseName("UK_tblUser_MobileNumber");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<User> builder)
	{
		builder.Property(user => user.Tag)
			.HasMaxLength(64);

		builder.Property(user => user.Comments)
			.HasMaxLength(256);

		builder.Property(user => user.SortOrder)
			.HasDefaultValue(0);

		builder.Property(user => user.IPAddress)
			.HasMaxLength(64)
			.IsRequired()
			.HasDefaultValue("127.0.0.1");

		builder.Property(user => user.CreatedBy)
			.HasMaxLength(128)
			.IsRequired()
			.HasDefaultValue("Admin");

		builder.Property(user => user.DateCreated)
			.HasColumnType("datetime2")
			.HasDefaultValueSql("GETDATE()");

		builder.Property(user => user.UpdatedBy)
			.HasMaxLength(128);

		builder.Property(user => user.LastUpdated)
			.HasColumnType("datetime2");

		builder.Property(user => user.DeletedBy)
			.HasMaxLength(128);

		builder.Property(user => user.DateDeleted)
			.HasColumnType("datetime2");

		builder.Property(user => user.IsDeleted)
			.IsRequired()
			.HasDefaultValue(false);
	}
}
