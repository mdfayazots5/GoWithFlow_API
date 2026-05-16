using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class TurnStateConfiguration : IEntityTypeConfiguration<TurnState>
{
	public void Configure(EntityTypeBuilder<TurnState> builder)
	{
		builder.ToTable("tblTurnState");

		builder.HasKey(turnState => turnState.TurnStateId)
			.HasName("PK_tblTurnState_TurnStateId");

		builder.Property(turnState => turnState.TurnStateId)
			.ValueGeneratedOnAdd();

		builder.Property(turnState => turnState.ReReadAllowed)
			.HasDefaultValue(true);

		builder.Property(turnState => turnState.ReReadCount)
			.HasDefaultValue(0);

		builder.Property(turnState => turnState.MaxReReads)
			.HasDefaultValue(2);

		builder.Property(turnState => turnState.TurnStatus)
			.HasMaxLength(16)
			.IsRequired()
			.HasDefaultValue("ACTIVE");

		builder.Property(turnState => turnState.TurnStartedAt)
			.HasColumnType("datetime2");

		builder.Property(turnState => turnState.TurnCompletedAt)
			.HasColumnType("datetime2");

		ConfigureAuditColumns(builder);

		builder.HasIndex(turnState => turnState.SessionId)
			.HasDatabaseName("IDX_tblTurnState_SessionId");

		builder.HasIndex(turnState => new { turnState.SessionId, turnState.TurnIndex })
			.HasDatabaseName("IDX_tblTurnState_SessionId_TurnIndex");

		builder.HasOne(turnState => turnState.Session)
			.WithMany()
			.HasForeignKey(turnState => turnState.SessionId)
			.HasConstraintName("FK_tblTurnState_SessionId_tblSession_SessionId");

		builder.HasOne(turnState => turnState.ActiveMember)
			.WithMany()
			.HasForeignKey(turnState => turnState.ActiveMemberId)
			.HasConstraintName("FK_tblTurnState_ActiveMemberId_tblUser_UserId");

		builder.HasOne(turnState => turnState.Utterance)
			.WithMany()
			.HasForeignKey(turnState => turnState.UtteranceId)
			.HasConstraintName("FK_tblTurnState_UtteranceId_tblUtterance_UtteranceId");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<TurnState> builder)
	{
		builder.Property(turnState => turnState.Tag).HasMaxLength(64);
		builder.Property(turnState => turnState.Comments).HasMaxLength(256);
		builder.Property(turnState => turnState.SortOrder).HasDefaultValue(0);
		builder.Property(turnState => turnState.IPAddress).HasMaxLength(64).IsRequired().HasDefaultValue("127.0.0.1");
		builder.Property(turnState => turnState.CreatedBy).HasMaxLength(128).IsRequired().HasDefaultValue("Admin");
		builder.Property(turnState => turnState.DateCreated).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
		builder.Property(turnState => turnState.UpdatedBy).HasMaxLength(128);
		builder.Property(turnState => turnState.LastUpdated).HasColumnType("datetime2");
		builder.Property(turnState => turnState.DeletedBy).HasMaxLength(128);
		builder.Property(turnState => turnState.DateDeleted).HasColumnType("datetime2");
		builder.Property(turnState => turnState.IsDeleted).IsRequired().HasDefaultValue(false);
	}
}
