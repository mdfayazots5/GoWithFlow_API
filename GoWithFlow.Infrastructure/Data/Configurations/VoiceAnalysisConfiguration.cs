using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class VoiceAnalysisConfiguration : IEntityTypeConfiguration<VoiceAnalysis>
{
	public void Configure(EntityTypeBuilder<VoiceAnalysis> builder)
	{
		builder.ToTable("tblVoiceAnalysis");

		builder.HasKey(voiceAnalysis => voiceAnalysis.VoiceAnalysisId)
			.HasName("PK_tblVoiceAnalysis_VoiceAnalysisId");

		builder.Property(voiceAnalysis => voiceAnalysis.VoiceAnalysisId)
			.ValueGeneratedOnAdd();

		builder.Property(voiceAnalysis => voiceAnalysis.TranscribedText)
			.HasMaxLength(512);

		builder.Property(voiceAnalysis => voiceAnalysis.ExpectedText)
			.HasMaxLength(512)
			.IsRequired();

		builder.Property(voiceAnalysis => voiceAnalysis.FluencyScore)
			.HasColumnType("decimal(5,2)")
			.HasDefaultValue(0m);

		builder.Property(voiceAnalysis => voiceAnalysis.ConfidenceScore)
			.HasColumnType("decimal(5,2)")
			.HasDefaultValue(0m);

		builder.Property(voiceAnalysis => voiceAnalysis.SpeakingSpeedWpm)
			.HasDefaultValue(0);

		builder.Property(voiceAnalysis => voiceAnalysis.PauseCount)
			.HasDefaultValue(0);

		builder.Property(voiceAnalysis => voiceAnalysis.HesitationWords)
			.HasMaxLength(256);

		builder.Property(voiceAnalysis => voiceAnalysis.RepeatedWords)
			.HasMaxLength(256);

		builder.Property(voiceAnalysis => voiceAnalysis.GrammarErrorsJson)
			.HasMaxLength(512);

		builder.Property(voiceAnalysis => voiceAnalysis.PronunciationJson)
			.HasMaxLength(512);

		builder.Property(voiceAnalysis => voiceAnalysis.OverallScore)
			.HasColumnType("decimal(5,2)")
			.HasDefaultValue(0m);

		builder.Property(voiceAnalysis => voiceAnalysis.RecordedAt)
			.HasColumnType("datetime2")
			.HasDefaultValueSql("GETDATE()");

		ConfigureAuditColumns(builder);

		builder.HasIndex(voiceAnalysis => voiceAnalysis.SessionId)
			.HasDatabaseName("IDX_tblVoiceAnalysis_SessionId");

		builder.HasIndex(voiceAnalysis => voiceAnalysis.UserId)
			.HasDatabaseName("IDX_tblVoiceAnalysis_UserId");

		builder.HasOne(voiceAnalysis => voiceAnalysis.Session)
			.WithMany()
			.HasForeignKey(voiceAnalysis => voiceAnalysis.SessionId)
			.HasConstraintName("FK_tblVoiceAnalysis_SessionId_tblSession_SessionId");

		builder.HasOne(voiceAnalysis => voiceAnalysis.User)
			.WithMany()
			.HasForeignKey(voiceAnalysis => voiceAnalysis.UserId)
			.HasConstraintName("FK_tblVoiceAnalysis_UserId_tblUser_UserId");

		builder.HasOne(voiceAnalysis => voiceAnalysis.Utterance)
			.WithMany()
			.HasForeignKey(voiceAnalysis => voiceAnalysis.UtteranceId)
			.HasConstraintName("FK_tblVoiceAnalysis_UtteranceId_tblUtterance_UtteranceId");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<VoiceAnalysis> builder)
	{
		builder.Property(voiceAnalysis => voiceAnalysis.Tag).HasMaxLength(64);
		builder.Property(voiceAnalysis => voiceAnalysis.Comments).HasMaxLength(256);
		builder.Property(voiceAnalysis => voiceAnalysis.SortOrder).HasDefaultValue(0);
		builder.Property(voiceAnalysis => voiceAnalysis.IPAddress).HasMaxLength(64).IsRequired().HasDefaultValue("127.0.0.1");
		builder.Property(voiceAnalysis => voiceAnalysis.CreatedBy).HasMaxLength(128).IsRequired().HasDefaultValue("Admin");
		builder.Property(voiceAnalysis => voiceAnalysis.DateCreated).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
		builder.Property(voiceAnalysis => voiceAnalysis.UpdatedBy).HasMaxLength(128);
		builder.Property(voiceAnalysis => voiceAnalysis.LastUpdated).HasColumnType("datetime2");
		builder.Property(voiceAnalysis => voiceAnalysis.DeletedBy).HasMaxLength(128);
		builder.Property(voiceAnalysis => voiceAnalysis.DateDeleted).HasColumnType("datetime2");
		builder.Property(voiceAnalysis => voiceAnalysis.IsDeleted).IsRequired().HasDefaultValue(false);
	}
}
