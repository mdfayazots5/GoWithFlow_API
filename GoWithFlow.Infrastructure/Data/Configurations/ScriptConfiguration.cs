using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class ScriptConfiguration : IEntityTypeConfiguration<Script>
{
	public void Configure(EntityTypeBuilder<Script> builder)
	{
		builder.ToTable("tblScript");

		builder.HasKey(script => script.ScriptId)
			.HasName("PK_tblScript_ScriptId");

		builder.Property(script => script.ScriptId)
			.ValueGeneratedOnAdd();

		builder.Property(script => script.ScriptTitle)
			.HasMaxLength(128)
			.IsRequired();

		builder.Property(script => script.Category)
			.HasMaxLength(64)
			.IsRequired();

		builder.Property(script => script.GrammarFocusTag)
			.HasMaxLength(64)
			.IsRequired();

		builder.Property(script => script.ContextTag)
			.HasMaxLength(64)
			.IsRequired();

		builder.Property(script => script.TargetAgeGroup)
			.HasMaxLength(32)
			.IsRequired();

		builder.Property(script => script.HintLanguage)
			.HasMaxLength(32)
			.IsRequired();

		builder.Property(script => script.IsActive)
			.HasDefaultValue(true);

		builder.Property(script => script.UploadedDate)
			.HasColumnType("datetime2")
			.HasDefaultValueSql("GETDATE()");

		builder.Property(script => script.Version)
			.HasDefaultValue(1);

		builder.Property(script => script.UtteranceCount)
			.HasDefaultValue(0);

		ConfigureAuditColumns(builder);

		builder.HasIndex(script => script.GrammarFocusTag)
			.HasDatabaseName("IDX_tblScript_GrammarFocusTag");

		builder.HasIndex(script => script.Category)
			.HasDatabaseName("IDX_tblScript_Category");

		builder.HasIndex(script => script.IsActive)
			.HasDatabaseName("IDX_tblScript_IsActive");

		builder.HasOne<User>()
			.WithMany()
			.HasForeignKey(script => script.UploadedByUserId)
			.HasConstraintName("FK_tblScript_UploadedByUserId_tblUser_UserId");

		builder.HasMany(script => script.Utterances)
			.WithOne(utterance => utterance.Script)
			.HasForeignKey(utterance => utterance.ScriptId)
			.HasConstraintName("FK_tblUtterance_ScriptId_tblScript_ScriptId");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<Script> builder)
	{
		builder.Property(script => script.Tag).HasMaxLength(64);
		builder.Property(script => script.Comments).HasMaxLength(256);
		builder.Property(script => script.SortOrder).HasDefaultValue(0);
		builder.Property(script => script.IPAddress).HasMaxLength(64).IsRequired().HasDefaultValue("127.0.0.1");
		builder.Property(script => script.CreatedBy).HasMaxLength(128).IsRequired().HasDefaultValue("Admin");
		builder.Property(script => script.DateCreated).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
		builder.Property(script => script.UpdatedBy).HasMaxLength(128);
		builder.Property(script => script.LastUpdated).HasColumnType("datetime2");
		builder.Property(script => script.DeletedBy).HasMaxLength(128);
		builder.Property(script => script.DateDeleted).HasColumnType("datetime2");
		builder.Property(script => script.IsDeleted).IsRequired().HasDefaultValue(false);
	}
}
