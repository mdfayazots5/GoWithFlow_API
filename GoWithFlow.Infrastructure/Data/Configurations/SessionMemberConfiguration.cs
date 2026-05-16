using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class SessionMemberConfiguration : IEntityTypeConfiguration<SessionMember>
{
	public void Configure(EntityTypeBuilder<SessionMember> builder)
	{
		builder.ToTable("tblSessionMember");

		builder.HasKey(sessionMember => sessionMember.SessionMemberId)
			.HasName("PK_tblSessionMember_SessionMemberId");

		builder.Property(sessionMember => sessionMember.SessionMemberId)
			.ValueGeneratedOnAdd();

		builder.Property(sessionMember => sessionMember.SlotName)
			.HasMaxLength(64)
			.IsRequired();

		builder.Property(sessionMember => sessionMember.IsReady)
			.HasDefaultValue(false);

		builder.Property(sessionMember => sessionMember.IsHost)
			.HasDefaultValue(false);

		builder.Property(sessionMember => sessionMember.JoinedAt)
			.HasColumnType("datetime2");

		builder.Property(sessionMember => sessionMember.LeftAt)
			.HasColumnType("datetime2");

		builder.Property(sessionMember => sessionMember.IsActive)
			.HasDefaultValue(true);

		ConfigureAuditColumns(builder);

		builder.HasIndex(sessionMember => new { sessionMember.SessionId, sessionMember.SlotIndex })
			.HasDatabaseName("UK_tblSessionMember_SessionId_SlotIndex")
			.IsUnique()
			.HasFilter("[IsDeleted] = 0 AND [IsActive] = 1");

		builder.HasIndex(sessionMember => sessionMember.SessionId)
			.HasDatabaseName("IDX_tblSessionMember_SessionId");

		builder.HasIndex(sessionMember => sessionMember.UserId)
			.HasDatabaseName("IDX_tblSessionMember_UserId");

		builder.HasOne(sessionMember => sessionMember.User)
			.WithMany()
			.HasForeignKey(sessionMember => sessionMember.UserId)
			.HasConstraintName("FK_tblSessionMember_UserId_tblUser_UserId");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<SessionMember> builder)
	{
		builder.Property(sessionMember => sessionMember.Tag).HasMaxLength(64);
		builder.Property(sessionMember => sessionMember.Comments).HasMaxLength(256);
		builder.Property(sessionMember => sessionMember.SortOrder).HasDefaultValue(0);
		builder.Property(sessionMember => sessionMember.IPAddress).HasMaxLength(64).IsRequired().HasDefaultValue("127.0.0.1");
		builder.Property(sessionMember => sessionMember.CreatedBy).HasMaxLength(128).IsRequired().HasDefaultValue("Admin");
		builder.Property(sessionMember => sessionMember.DateCreated).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
		builder.Property(sessionMember => sessionMember.UpdatedBy).HasMaxLength(128);
		builder.Property(sessionMember => sessionMember.LastUpdated).HasColumnType("datetime2");
		builder.Property(sessionMember => sessionMember.DeletedBy).HasMaxLength(128);
		builder.Property(sessionMember => sessionMember.DateDeleted).HasColumnType("datetime2");
		builder.Property(sessionMember => sessionMember.IsDeleted).IsRequired().HasDefaultValue(false);
	}
}
