using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class MistakeConfiguration : IEntityTypeConfiguration<Mistake>
{
	public void Configure(EntityTypeBuilder<Mistake> builder)
	{
		builder.ToTable("tblMistake");

		builder.HasKey(mistake => mistake.MistakeId)
			.HasName("PK_tblMistake_MistakeId");

		builder.Property(mistake => mistake.MistakeId)
			.ValueGeneratedOnAdd();

		builder.Property(mistake => mistake.UtteranceText)
			.HasMaxLength(512)
			.IsRequired();

		builder.Property(mistake => mistake.SpokenText)
			.HasMaxLength(512);

		builder.Property(mistake => mistake.MistakeType)
			.HasMaxLength(32)
			.IsRequired();

		builder.Property(mistake => mistake.MistakeDetail)
			.HasMaxLength(256);

		builder.Property(mistake => mistake.GrammarTag)
			.HasMaxLength(64);

		builder.Property(mistake => mistake.ContextTag)
			.HasMaxLength(64);

		builder.Property(mistake => mistake.CorrectionText)
			.HasMaxLength(512);

		builder.Property(mistake => mistake.PracticeCount)
			.HasDefaultValue(0);

		builder.Property(mistake => mistake.IsResolved)
			.HasDefaultValue(false);

		builder.Property(mistake => mistake.FirstOccurrence)
			.HasColumnType("datetime2")
			.HasDefaultValueSql("GETDATE()");

		builder.Property(mistake => mistake.LastAttempt)
			.HasColumnType("datetime2");

		ConfigureAuditColumns(builder);

		builder.HasIndex(mistake => mistake.UserId)
			.HasDatabaseName("IDX_tblMistake_UserId");

		builder.HasIndex(mistake => mistake.SessionId)
			.HasDatabaseName("IDX_tblMistake_SessionId");

		builder.HasIndex(mistake => new { mistake.UserId, mistake.IsResolved })
			.HasDatabaseName("IDX_tblMistake_UserId_IsResolved");

		builder.HasIndex(mistake => mistake.GrammarTag)
			.HasDatabaseName("IDX_tblMistake_GrammarTag");

		builder.HasOne(mistake => mistake.User)
			.WithMany()
			.HasForeignKey(mistake => mistake.UserId)
			.HasConstraintName("FK_tblMistake_UserId_tblUser_UserId");

		builder.HasOne(mistake => mistake.Session)
			.WithMany()
			.HasForeignKey(mistake => mistake.SessionId)
			.HasConstraintName("FK_tblMistake_SessionId_tblSession_SessionId");

		builder.HasOne(mistake => mistake.Utterance)
			.WithMany()
			.HasForeignKey(mistake => mistake.UtteranceId)
			.HasConstraintName("FK_tblMistake_UtteranceId_tblUtterance_UtteranceId");

		builder.HasOne(mistake => mistake.Script)
			.WithMany()
			.HasForeignKey(mistake => mistake.ScriptId)
			.HasConstraintName("FK_tblMistake_ScriptId_tblScript_ScriptId");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<Mistake> builder)
	{
		builder.Property(mistake => mistake.Tag).HasMaxLength(64);
		builder.Property(mistake => mistake.Comments).HasMaxLength(256);
		builder.Property(mistake => mistake.SortOrder).HasDefaultValue(0);
		builder.Property(mistake => mistake.IPAddress).HasMaxLength(64).IsRequired().HasDefaultValue("127.0.0.1");
		builder.Property(mistake => mistake.CreatedBy).HasMaxLength(128).IsRequired().HasDefaultValue("Admin");
		builder.Property(mistake => mistake.DateCreated).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
		builder.Property(mistake => mistake.UpdatedBy).HasMaxLength(128);
		builder.Property(mistake => mistake.LastUpdated).HasColumnType("datetime2");
		builder.Property(mistake => mistake.DeletedBy).HasMaxLength(128);
		builder.Property(mistake => mistake.DateDeleted).HasColumnType("datetime2");
		builder.Property(mistake => mistake.IsDeleted).IsRequired().HasDefaultValue(false);
	}
}
