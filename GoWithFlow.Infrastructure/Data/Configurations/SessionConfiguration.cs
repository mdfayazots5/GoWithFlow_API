using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
	public void Configure(EntityTypeBuilder<Session> builder)
	{
		builder.ToTable("tblSession");

		builder.HasKey(session => session.SessionId)
			.HasName("PK_tblSession_SessionId");

		builder.Property(session => session.SessionId)
			.ValueGeneratedOnAdd();

		builder.Property(session => session.SessionName)
			.HasMaxLength(128)
			.IsRequired();

		builder.Property(session => session.JoinCode)
			.HasMaxLength(8)
			.IsRequired();

		builder.Property(session => session.SessionMode)
			.HasMaxLength(64)
			.IsRequired();

		builder.Property(session => session.MaxMembers)
			.HasDefaultValue((byte)4);

		builder.Property(session => session.Status)
			.HasMaxLength(16)
			.IsRequired()
			.HasDefaultValue("LOBBY");

		builder.Property(session => session.RoomExpiresAt)
			.HasColumnType("datetime2");

		builder.Property(session => session.StartedDate)
			.HasColumnType("datetime2");

		builder.Property(session => session.EndedDate)
			.HasColumnType("datetime2");

		ConfigureAuditColumns(builder);

		builder.HasIndex(session => session.Status)
			.HasDatabaseName("IDX_tblSession_Status");

		builder.HasIndex(session => session.HostUserId)
			.HasDatabaseName("IDX_tblSession_HostUserId");

		builder.HasIndex(session => session.JoinCode)
			.HasDatabaseName("IDX_tblSession_JoinCode");

		builder.HasIndex(session => session.JoinCode)
			.HasDatabaseName("UK_tblSession_JoinCode")
			.IsUnique()
			.HasFilter("[IsDeleted] = 0");

		builder.HasOne(session => session.Host)
			.WithMany()
			.HasForeignKey(session => session.HostUserId)
			.HasConstraintName("FK_tblSession_HostUserId_tblUser_UserId");

		builder.HasOne(session => session.Script)
			.WithMany()
			.HasForeignKey(session => session.ScriptId)
			.HasConstraintName("FK_tblSession_ScriptId_tblScript_ScriptId");

		builder.HasMany(session => session.Members)
			.WithOne(member => member.Session)
			.HasForeignKey(member => member.SessionId)
			.HasConstraintName("FK_tblSessionMember_SessionId_tblSession_SessionId");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<Session> builder)
	{
		builder.Property(session => session.Tag).HasMaxLength(64);
		builder.Property(session => session.Comments).HasMaxLength(256);
		builder.Property(session => session.SortOrder).HasDefaultValue(0);
		builder.Property(session => session.IPAddress).HasMaxLength(64).IsRequired().HasDefaultValue("127.0.0.1");
		builder.Property(session => session.CreatedBy).HasMaxLength(128).IsRequired().HasDefaultValue("Admin");
		builder.Property(session => session.DateCreated).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
		builder.Property(session => session.UpdatedBy).HasMaxLength(128);
		builder.Property(session => session.LastUpdated).HasColumnType("datetime2");
		builder.Property(session => session.DeletedBy).HasMaxLength(128);
		builder.Property(session => session.DateDeleted).HasColumnType("datetime2");
		builder.Property(session => session.IsDeleted).IsRequired().HasDefaultValue(false);
	}
}
