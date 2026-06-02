using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class ChallengeAttemptConfiguration : IEntityTypeConfiguration<ChallengeAttempt>
{
	public void Configure(EntityTypeBuilder<ChallengeAttempt> builder)
	{
		builder.ToTable("tblChallengeAttempt");

		builder.HasKey(a => a.AttemptId).HasName("PK_tblChallengeAttempt_AttemptId");
		builder.Property(a => a.AttemptId).ValueGeneratedOnAdd();
		builder.Property(a => a.FluencyScore).HasColumnType("decimal(5,2)").HasDefaultValue(0m);
		builder.Property(a => a.AttemptDate).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");

		builder.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId)
			.HasConstraintName("FK_tblChallengeAttempt_UserId_tblUser_UserId");

		builder.HasIndex(a => a.ChallengeId).HasDatabaseName("IDX_tblChallengeAttempt_ChallengeId");
		builder.HasIndex(a => a.UserId).HasDatabaseName("IDX_tblChallengeAttempt_UserId");
		builder.HasIndex(a => new { a.ChallengeId, a.UserId }).HasDatabaseName("IDX_tblChallengeAttempt_ChallengeId_User");

		ConfigureAudit(builder);
	}

	private static void ConfigureAudit(EntityTypeBuilder<ChallengeAttempt> builder)
	{
		builder.Property(e => e.Tag).HasMaxLength(64);
		builder.Property(e => e.Comments).HasMaxLength(256);
		builder.Property(e => e.SortOrder).HasDefaultValue(0);
		builder.Property(e => e.IPAddress).HasMaxLength(64).IsRequired().HasDefaultValue("127.0.0.1");
		builder.Property(e => e.CreatedBy).HasMaxLength(128).IsRequired().HasDefaultValue("System");
		builder.Property(e => e.DateCreated).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
		builder.Property(e => e.UpdatedBy).HasMaxLength(128);
		builder.Property(e => e.LastUpdated).HasColumnType("datetime2");
		builder.Property(e => e.DeletedBy).HasMaxLength(128);
		builder.Property(e => e.DateDeleted).HasColumnType("datetime2");
		builder.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
	}
}
