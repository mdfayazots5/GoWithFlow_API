using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class UserBadgeConfiguration : IEntityTypeConfiguration<UserBadge>
{
	public void Configure(EntityTypeBuilder<UserBadge> builder)
	{
		builder.ToTable("tblUserBadge");

		builder.HasKey(userBadge => userBadge.UserBadgeId)
			.HasName("PK_tblUserBadge_UserBadgeId");

		builder.Property(userBadge => userBadge.UserBadgeId)
			.ValueGeneratedOnAdd();

		builder.Property(userBadge => userBadge.BadgeCode)
			.HasMaxLength(64)
			.IsRequired();

		builder.Property(userBadge => userBadge.BadgeName)
			.HasMaxLength(128)
			.IsRequired();

		builder.Property(userBadge => userBadge.EarnedDate)
			.HasColumnType("datetime2")
			.IsRequired()
			.HasDefaultValueSql("GETDATE()");

		builder.HasOne(userBadge => userBadge.User)
			.WithMany(user => user.UserBadges)
			.HasForeignKey(userBadge => userBadge.UserId)
			.HasConstraintName("FK_tblUserBadge_UserId_tblUser_UserId");

		builder.HasIndex(userBadge => new { userBadge.UserId, userBadge.BadgeCode })
			.IsUnique()
			.HasDatabaseName("UK_tblUserBadge_UserId_BadgeCode");

		builder.HasIndex(userBadge => userBadge.UserId)
			.HasDatabaseName("IDX_tblUserBadge_UserId");

		ConfigureAuditColumns(builder);
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<UserBadge> builder)
	{
		builder.Property(userBadge => userBadge.Tag)
			.HasMaxLength(64);

		builder.Property(userBadge => userBadge.Comments)
			.HasMaxLength(256);

		builder.Property(userBadge => userBadge.SortOrder)
			.HasDefaultValue(0);

		builder.Property(userBadge => userBadge.IPAddress)
			.HasMaxLength(64)
			.IsRequired()
			.HasDefaultValue("127.0.0.1");

		builder.Property(userBadge => userBadge.CreatedBy)
			.HasMaxLength(128)
			.IsRequired()
			.HasDefaultValue("Admin");

		builder.Property(userBadge => userBadge.DateCreated)
			.HasColumnType("datetime2")
			.HasDefaultValueSql("GETDATE()");

		builder.Property(userBadge => userBadge.UpdatedBy)
			.HasMaxLength(128);

		builder.Property(userBadge => userBadge.LastUpdated)
			.HasColumnType("datetime2");

		builder.Property(userBadge => userBadge.DeletedBy)
			.HasMaxLength(128);

		builder.Property(userBadge => userBadge.DateDeleted)
			.HasColumnType("datetime2");

		builder.Property(userBadge => userBadge.IsDeleted)
			.IsRequired()
			.HasDefaultValue(false);
	}
}
