using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class UtteranceConfiguration : IEntityTypeConfiguration<Utterance>
{
	public void Configure(EntityTypeBuilder<Utterance> builder)
	{
		builder.ToTable("tblUtterance");

		builder.HasKey(utterance => utterance.UtteranceId)
			.HasName("PK_tblUtterance_UtteranceId");

		builder.Property(utterance => utterance.UtteranceId)
			.ValueGeneratedOnAdd();

		builder.Property(utterance => utterance.SpeakerLabel)
			.HasMaxLength(64)
			.IsRequired();

		builder.Property(utterance => utterance.EnglishText)
			.HasMaxLength(512)
			.IsRequired();

		builder.Property(utterance => utterance.HintText)
			.HasMaxLength(512);

		builder.Property(utterance => utterance.GrammarTag)
			.HasMaxLength(64);

		builder.Property(utterance => utterance.ContextTag)
			.HasMaxLength(64);

		builder.Property(utterance => utterance.FocusWord)
			.HasMaxLength(64);

		builder.Property(utterance => utterance.PronunciationNote)
			.HasMaxLength(256);

		ConfigureAuditColumns(builder);

		builder.HasIndex(utterance => utterance.ScriptId)
			.HasDatabaseName("IDX_tblUtterance_ScriptId");

		builder.HasIndex(utterance => new { utterance.ScriptId, utterance.SequenceId })
			.IsUnique()
			.HasDatabaseName("UK_tblUtterance_ScriptId_SequenceId");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<Utterance> builder)
	{
		builder.Property(utterance => utterance.Tag).HasMaxLength(64);
		builder.Property(utterance => utterance.Comments).HasMaxLength(256);
		builder.Property(utterance => utterance.SortOrder).HasDefaultValue(0);
		builder.Property(utterance => utterance.IPAddress).HasMaxLength(64).IsRequired().HasDefaultValue("127.0.0.1");
		builder.Property(utterance => utterance.CreatedBy).HasMaxLength(128).IsRequired().HasDefaultValue("Admin");
		builder.Property(utterance => utterance.DateCreated).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
		builder.Property(utterance => utterance.UpdatedBy).HasMaxLength(128);
		builder.Property(utterance => utterance.LastUpdated).HasColumnType("datetime2");
		builder.Property(utterance => utterance.DeletedBy).HasMaxLength(128);
		builder.Property(utterance => utterance.DateDeleted).HasColumnType("datetime2");
		builder.Property(utterance => utterance.IsDeleted).IsRequired().HasDefaultValue(false);
	}
}
