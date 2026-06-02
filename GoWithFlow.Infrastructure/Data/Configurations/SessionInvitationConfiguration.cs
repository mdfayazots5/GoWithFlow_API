using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class SessionInvitationConfiguration : IEntityTypeConfiguration<SessionInvitation>
{
	public void Configure(EntityTypeBuilder<SessionInvitation> builder)
	{
		builder.ToTable("tblSessionInvitation");

		builder.HasKey(inv => inv.InvitationId)
			.HasName("PK_tblSessionInvitation_InvitationId");

		builder.Property(inv => inv.InvitationId)
			.ValueGeneratedOnAdd();

		builder.Property(inv => inv.SlotName)
			.HasMaxLength(64)
			.IsRequired();

		builder.Property(inv => inv.Status)
			.HasMaxLength(16)
			.IsRequired()
			.HasDefaultValue("PENDING");

		builder.Property(inv => inv.SentAt)
			.HasColumnType("datetime2")
			.HasDefaultValueSql("GETDATE()");

		builder.Property(inv => inv.RespondedAt)
			.HasColumnType("datetime2");

		builder.Property(inv => inv.ExpiresAt)
			.HasColumnType("datetime2");

		ConfigureAuditColumns(builder);

		builder.HasIndex(inv => inv.SessionId)
			.HasDatabaseName("IDX_tblSessionInvitation_SessionId");

		builder.HasIndex(inv => inv.UserId)
			.HasDatabaseName("IDX_tblSessionInvitation_UserId");

		builder.HasOne(inv => inv.Session)
			.WithMany(s => s.Invitations)
			.HasForeignKey(inv => inv.SessionId)
			.HasConstraintName("FK_tblSessionInvitation_SessionId_tblSession_SessionId");

		builder.HasOne(inv => inv.User)
			.WithMany()
			.HasForeignKey(inv => inv.UserId)
			.HasConstraintName("FK_tblSessionInvitation_UserId_tblUser_UserId");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<SessionInvitation> builder)
	{
		builder.Property(inv => inv.Tag).HasMaxLength(64);
		builder.Property(inv => inv.Comments).HasMaxLength(256);
		builder.Property(inv => inv.SortOrder).HasDefaultValue(0);
		builder.Property(inv => inv.IPAddress).HasMaxLength(64).IsRequired().HasDefaultValue("127.0.0.1");
		builder.Property(inv => inv.CreatedBy).HasMaxLength(128).IsRequired().HasDefaultValue("Admin");
		builder.Property(inv => inv.DateCreated).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
		builder.Property(inv => inv.UpdatedBy).HasMaxLength(128);
		builder.Property(inv => inv.LastUpdated).HasColumnType("datetime2");
		builder.Property(inv => inv.DeletedBy).HasMaxLength(128);
		builder.Property(inv => inv.DateDeleted).HasColumnType("datetime2");
		builder.Property(inv => inv.IsDeleted).IsRequired().HasDefaultValue(false);
	}
}
