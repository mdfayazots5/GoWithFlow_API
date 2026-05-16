using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class ListenerFeedbackConfiguration : IEntityTypeConfiguration<ListenerFeedback>
{
	public void Configure(EntityTypeBuilder<ListenerFeedback> builder)
	{
		builder.ToTable("tblListenerFeedback");

		builder.HasKey(listenerFeedback => listenerFeedback.ListenerFeedbackId)
			.HasName("PK_tblListenerFeedback_ListenerFeedbackId");

		builder.Property(listenerFeedback => listenerFeedback.ListenerFeedbackId)
			.ValueGeneratedOnAdd();

		builder.Property(listenerFeedback => listenerFeedback.FeedbackTag)
			.HasMaxLength(32)
			.IsRequired();

		builder.Property(listenerFeedback => listenerFeedback.FeedbackAt)
			.HasColumnType("datetime2")
			.HasDefaultValueSql("GETDATE()");

		ConfigureAuditColumns(builder);

		builder.HasIndex(listenerFeedback => new { listenerFeedback.SessionId, listenerFeedback.TurnIndex })
			.HasDatabaseName("IDX_tblListenerFeedback_SessionId_TurnIndex");

		builder.HasOne(listenerFeedback => listenerFeedback.Session)
			.WithMany()
			.HasForeignKey(listenerFeedback => listenerFeedback.SessionId)
			.HasConstraintName("FK_tblListenerFeedback_SessionId_tblSession_SessionId");

		builder.HasOne(listenerFeedback => listenerFeedback.FromUser)
			.WithMany()
			.HasForeignKey(listenerFeedback => listenerFeedback.FromUserId)
			.HasConstraintName("FK_tblListenerFeedback_FromUserId_tblUser_UserId")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(listenerFeedback => listenerFeedback.TargetUser)
			.WithMany()
			.HasForeignKey(listenerFeedback => listenerFeedback.TargetUserId)
			.HasConstraintName("FK_tblListenerFeedback_TargetUserId_tblUser_UserId")
			.OnDelete(DeleteBehavior.Restrict);
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<ListenerFeedback> builder)
	{
		builder.Property(listenerFeedback => listenerFeedback.Tag).HasMaxLength(64);
		builder.Property(listenerFeedback => listenerFeedback.Comments).HasMaxLength(256);
		builder.Property(listenerFeedback => listenerFeedback.SortOrder).HasDefaultValue(0);
		builder.Property(listenerFeedback => listenerFeedback.IPAddress).HasMaxLength(64).IsRequired().HasDefaultValue("127.0.0.1");
		builder.Property(listenerFeedback => listenerFeedback.CreatedBy).HasMaxLength(128).IsRequired().HasDefaultValue("Admin");
		builder.Property(listenerFeedback => listenerFeedback.DateCreated).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
		builder.Property(listenerFeedback => listenerFeedback.UpdatedBy).HasMaxLength(128);
		builder.Property(listenerFeedback => listenerFeedback.LastUpdated).HasColumnType("datetime2");
		builder.Property(listenerFeedback => listenerFeedback.DeletedBy).HasMaxLength(128);
		builder.Property(listenerFeedback => listenerFeedback.DateDeleted).HasColumnType("datetime2");
		builder.Property(listenerFeedback => listenerFeedback.IsDeleted).IsRequired().HasDefaultValue(false);
	}
}
