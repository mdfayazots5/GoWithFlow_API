using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class RepracticeSessionConfiguration : IEntityTypeConfiguration<RepracticeSession>
{
	public void Configure(EntityTypeBuilder<RepracticeSession> builder)
	{
		builder.ToTable("tblRepracticeSession");

		builder.HasKey(repracticeSession => repracticeSession.RepracticeSessionId)
			.HasName("PK_tblRepracticeSession_RepracticeSessionId");

		builder.Property(repracticeSession => repracticeSession.RepracticeSessionId)
			.ValueGeneratedOnAdd();

		builder.Property(repracticeSession => repracticeSession.TotalMistakes)
			.HasDefaultValue(0);

		builder.Property(repracticeSession => repracticeSession.CompletedRounds)
			.HasDefaultValue(0);

		builder.Property(repracticeSession => repracticeSession.ImprovementPercent)
			.HasColumnType("decimal(5,2)")
			.HasDefaultValue(0m);

		builder.Property(repracticeSession => repracticeSession.Status)
			.HasMaxLength(16)
			.IsRequired()
			.HasDefaultValue("PENDING");

		builder.Property(repracticeSession => repracticeSession.GeneratedDate)
			.HasColumnType("datetime2")
			.HasDefaultValueSql("GETDATE()");

		ConfigureAuditColumns(builder);

		builder.HasIndex(repracticeSession => repracticeSession.UserId)
			.HasDatabaseName("IDX_tblRepracticeSession_UserId");

		builder.HasOne(repracticeSession => repracticeSession.User)
			.WithMany()
			.HasForeignKey(repracticeSession => repracticeSession.UserId)
			.HasConstraintName("FK_tblRepracticeSession_UserId_tblUser_UserId");

		builder.HasOne(repracticeSession => repracticeSession.SourceSession)
			.WithMany()
			.HasForeignKey(repracticeSession => repracticeSession.SourceSessionId)
			.HasConstraintName("FK_tblRepracticeSession_SourceSessionId_tblSession_SessionId");

		builder.HasMany(repracticeSession => repracticeSession.Utterances)
			.WithOne(repracticeUtterance => repracticeUtterance.RepracticeSession)
			.HasForeignKey(repracticeUtterance => repracticeUtterance.RepracticeSessionId)
			.HasConstraintName("FK_tblRepracticeUtterance_RepracticeSessionId_tblRepracticeSession_RepracticeSessionId");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<RepracticeSession> builder)
	{
		builder.Property(repracticeSession => repracticeSession.Tag).HasMaxLength(64);
		builder.Property(repracticeSession => repracticeSession.Comments).HasMaxLength(256);
		builder.Property(repracticeSession => repracticeSession.SortOrder).HasDefaultValue(0);
		builder.Property(repracticeSession => repracticeSession.IPAddress).HasMaxLength(64).IsRequired().HasDefaultValue("127.0.0.1");
		builder.Property(repracticeSession => repracticeSession.CreatedBy).HasMaxLength(128).IsRequired().HasDefaultValue("Admin");
		builder.Property(repracticeSession => repracticeSession.DateCreated).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
		builder.Property(repracticeSession => repracticeSession.UpdatedBy).HasMaxLength(128);
		builder.Property(repracticeSession => repracticeSession.LastUpdated).HasColumnType("datetime2");
		builder.Property(repracticeSession => repracticeSession.DeletedBy).HasMaxLength(128);
		builder.Property(repracticeSession => repracticeSession.DateDeleted).HasColumnType("datetime2");
		builder.Property(repracticeSession => repracticeSession.IsDeleted).IsRequired().HasDefaultValue(false);
	}
}
