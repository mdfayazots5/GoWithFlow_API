using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class WeeklyChallengeConfiguration : IEntityTypeConfiguration<WeeklyChallenge>
{
	public void Configure(EntityTypeBuilder<WeeklyChallenge> builder)
	{
		builder.ToTable("tblWeeklyChallenge");

		builder.HasKey(c => c.ChallengeId).HasName("PK_tblWeeklyChallenge_ChallengeId");
		builder.Property(c => c.ChallengeId).ValueGeneratedOnAdd();
		builder.Property(c => c.WeekStartDate).HasColumnType("datetime2");
		builder.Property(c => c.WeekEndDate).HasColumnType("datetime2");
		builder.Property(c => c.IsActive).HasDefaultValue(true);

		builder.HasOne(c => c.Script).WithMany().HasForeignKey(c => c.ScriptId)
			.HasConstraintName("FK_tblWeeklyChallenge_ScriptId_tblScript_ScriptId");

		builder.HasMany(c => c.Attempts).WithOne(a => a.Challenge).HasForeignKey(a => a.ChallengeId)
			.HasConstraintName("FK_tblChallengeAttempt_ChallengeId_tblWeeklyChallenge_ChallengeId");

		builder.HasIndex(c => c.IsActive).HasDatabaseName("IDX_tblWeeklyChallenge_IsActive");
		builder.HasIndex(c => c.WeekStartDate).HasDatabaseName("IDX_tblWeeklyChallenge_WeekStartDate");

		ConfigureAudit(builder);
	}

	private static void ConfigureAudit(EntityTypeBuilder<WeeklyChallenge> builder)
	{
		builder.Property(e => e.Tag).HasMaxLength(64);
		builder.Property(e => e.Comments).HasMaxLength(256);
		builder.Property(e => e.SortOrder).HasDefaultValue(0);
		builder.Property(e => e.IPAddress).HasMaxLength(64).IsRequired().HasDefaultValue("127.0.0.1");
		builder.Property(e => e.CreatedBy).HasMaxLength(128).IsRequired().HasDefaultValue("Admin");
		builder.Property(e => e.DateCreated).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
		builder.Property(e => e.UpdatedBy).HasMaxLength(128);
		builder.Property(e => e.LastUpdated).HasColumnType("datetime2");
		builder.Property(e => e.DeletedBy).HasMaxLength(128);
		builder.Property(e => e.DateDeleted).HasColumnType("datetime2");
		builder.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
	}
}
