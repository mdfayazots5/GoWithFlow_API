using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class UserStreakConfiguration : IEntityTypeConfiguration<UserStreak>
{
	public void Configure(EntityTypeBuilder<UserStreak> builder)
	{
		builder.ToTable("tblUserStreak");

		builder.HasKey(userStreak => userStreak.UserStreakId)
			.HasName("PK_tblUserStreak_UserStreakId");

		builder.Property(userStreak => userStreak.UserStreakId)
			.ValueGeneratedOnAdd();

		builder.Property(userStreak => userStreak.StreakDate)
			.HasColumnType("date")
			.IsRequired();

		builder.Property(userStreak => userStreak.SessionCount)
			.IsRequired()
			.HasDefaultValue(0);

		builder.Property(userStreak => userStreak.PracticeMinutes)
			.IsRequired()
			.HasDefaultValue(0);

		builder.HasOne(userStreak => userStreak.User)
			.WithMany(user => user.UserStreaks)
			.HasForeignKey(userStreak => userStreak.UserId)
			.HasConstraintName("FK_tblUserStreak_UserId_tblUser_UserId");

		builder.HasIndex(userStreak => new { userStreak.UserId, userStreak.StreakDate })
			.IsUnique()
			.HasDatabaseName("UK_tblUserStreak_UserId_StreakDate");

		builder.HasIndex(userStreak => userStreak.UserId)
			.HasDatabaseName("IDX_tblUserStreak_UserId");

		ConfigureAuditColumns(builder);
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<UserStreak> builder)
	{
		builder.Property(userStreak => userStreak.Tag)
			.HasMaxLength(64);

		builder.Property(userStreak => userStreak.Comments)
			.HasMaxLength(256);

		builder.Property(userStreak => userStreak.SortOrder)
			.HasDefaultValue(0);

		builder.Property(userStreak => userStreak.IPAddress)
			.HasMaxLength(64)
			.IsRequired()
			.HasDefaultValue("127.0.0.1");

		builder.Property(userStreak => userStreak.CreatedBy)
			.HasMaxLength(128)
			.IsRequired()
			.HasDefaultValue("Admin");

		builder.Property(userStreak => userStreak.DateCreated)
			.HasColumnType("datetime2")
			.HasDefaultValueSql("GETDATE()");

		builder.Property(userStreak => userStreak.UpdatedBy)
			.HasMaxLength(128);

		builder.Property(userStreak => userStreak.LastUpdated)
			.HasColumnType("datetime2");

		builder.Property(userStreak => userStreak.DeletedBy)
			.HasMaxLength(128);

		builder.Property(userStreak => userStreak.DateDeleted)
			.HasColumnType("datetime2");

		builder.Property(userStreak => userStreak.IsDeleted)
			.IsRequired()
			.HasDefaultValue(false);
	}
}
