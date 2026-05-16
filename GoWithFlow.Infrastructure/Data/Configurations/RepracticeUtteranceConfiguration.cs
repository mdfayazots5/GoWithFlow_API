using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class RepracticeUtteranceConfiguration : IEntityTypeConfiguration<RepracticeUtterance>
{
	public void Configure(EntityTypeBuilder<RepracticeUtterance> builder)
	{
		builder.ToTable("tblRepracticeUtterance");

		builder.HasKey(repracticeUtterance => repracticeUtterance.RepracticeUtteranceId)
			.HasName("PK_tblRepracticeUtterance_RepracticeUtteranceId");

		builder.Property(repracticeUtterance => repracticeUtterance.RepracticeUtteranceId)
			.ValueGeneratedOnAdd();

		builder.Property(repracticeUtterance => repracticeUtterance.EnglishText)
			.HasMaxLength(512)
			.IsRequired();

		builder.Property(repracticeUtterance => repracticeUtterance.HintText)
			.HasMaxLength(512);

		builder.Property(repracticeUtterance => repracticeUtterance.MistakeType)
			.HasMaxLength(32)
			.IsRequired();

		builder.Property(repracticeUtterance => repracticeUtterance.MistakeDetail)
			.HasMaxLength(256);

		builder.Property(repracticeUtterance => repracticeUtterance.CorrectionNote)
			.HasMaxLength(512);

		builder.Property(repracticeUtterance => repracticeUtterance.AttemptCount)
			.HasDefaultValue(0);

		builder.Property(repracticeUtterance => repracticeUtterance.BestScore)
			.HasColumnType("decimal(5,2)")
			.HasDefaultValue(0m);

		builder.Property(repracticeUtterance => repracticeUtterance.LastScore)
			.HasColumnType("decimal(5,2)")
			.HasDefaultValue(0m);

		builder.Property(repracticeUtterance => repracticeUtterance.IsResolved)
			.HasDefaultValue(false);

		ConfigureAuditColumns(builder);

		builder.HasIndex(repracticeUtterance => repracticeUtterance.RepracticeSessionId)
			.HasDatabaseName("IDX_tblRepracticeUtterance_RepracticeSessionId");

		builder.HasOne(repracticeUtterance => repracticeUtterance.Mistake)
			.WithMany(mistake => mistake.RepracticeUtterances)
			.HasForeignKey(repracticeUtterance => repracticeUtterance.MistakeId)
			.HasConstraintName("FK_tblRepracticeUtterance_MistakeId_tblMistake_MistakeId");

		builder.HasOne(repracticeUtterance => repracticeUtterance.OriginalUtterance)
			.WithMany()
			.HasForeignKey(repracticeUtterance => repracticeUtterance.OriginalUtteranceId)
			.HasConstraintName("FK_tblRepracticeUtterance_OriginalUtteranceId_tblUtterance_UtteranceId");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<RepracticeUtterance> builder)
	{
		builder.Property(repracticeUtterance => repracticeUtterance.Tag).HasMaxLength(64);
		builder.Property(repracticeUtterance => repracticeUtterance.Comments).HasMaxLength(256);
		builder.Property(repracticeUtterance => repracticeUtterance.SortOrder).HasDefaultValue(0);
		builder.Property(repracticeUtterance => repracticeUtterance.IPAddress).HasMaxLength(64).IsRequired().HasDefaultValue("127.0.0.1");
		builder.Property(repracticeUtterance => repracticeUtterance.CreatedBy).HasMaxLength(128).IsRequired().HasDefaultValue("Admin");
		builder.Property(repracticeUtterance => repracticeUtterance.DateCreated).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
		builder.Property(repracticeUtterance => repracticeUtterance.UpdatedBy).HasMaxLength(128);
		builder.Property(repracticeUtterance => repracticeUtterance.LastUpdated).HasColumnType("datetime2");
		builder.Property(repracticeUtterance => repracticeUtterance.DeletedBy).HasMaxLength(128);
		builder.Property(repracticeUtterance => repracticeUtterance.DateDeleted).HasColumnType("datetime2");
		builder.Property(repracticeUtterance => repracticeUtterance.IsDeleted).IsRequired().HasDefaultValue(false);
	}
}
