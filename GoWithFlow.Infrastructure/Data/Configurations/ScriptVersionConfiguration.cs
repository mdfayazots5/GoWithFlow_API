using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class ScriptVersionConfiguration : IEntityTypeConfiguration<ScriptVersion>
{
	public void Configure(EntityTypeBuilder<ScriptVersion> builder)
	{
		builder.ToTable("tblScriptVersion");

		builder.HasKey(scriptVersion => scriptVersion.ScriptVersionId)
			.HasName("PK_tblScriptVersion_ScriptVersionId");

		builder.Property(scriptVersion => scriptVersion.ScriptVersionId)
			.ValueGeneratedOnAdd();

		builder.Property(scriptVersion => scriptVersion.VersionNotes)
			.HasMaxLength(256);

		builder.Property(scriptVersion => scriptVersion.UploadedDate)
			.HasColumnType("datetime2")
			.HasDefaultValueSql("GETDATE()");

		ConfigureAuditColumns(builder);

		builder.HasIndex(scriptVersion => scriptVersion.ScriptId)
			.HasDatabaseName("IDX_tblScriptVersion_ScriptId");

		builder.HasOne<Script>()
			.WithMany()
			.HasForeignKey(scriptVersion => scriptVersion.ScriptId)
			.HasConstraintName("FK_tblScriptVersion_ScriptId_tblScript_ScriptId");

		builder.HasOne<User>()
			.WithMany()
			.HasForeignKey(scriptVersion => scriptVersion.UploadedByUserId)
			.HasConstraintName("FK_tblScriptVersion_UploadedByUserId_tblUser_UserId");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<ScriptVersion> builder)
	{
		builder.Property(scriptVersion => scriptVersion.Tag).HasMaxLength(64);
		builder.Property(scriptVersion => scriptVersion.Comments).HasMaxLength(256);
		builder.Property(scriptVersion => scriptVersion.SortOrder).HasDefaultValue(0);
		builder.Property(scriptVersion => scriptVersion.IPAddress).HasMaxLength(64).IsRequired().HasDefaultValue("127.0.0.1");
		builder.Property(scriptVersion => scriptVersion.CreatedBy).HasMaxLength(128).IsRequired().HasDefaultValue("Admin");
		builder.Property(scriptVersion => scriptVersion.DateCreated).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
		builder.Property(scriptVersion => scriptVersion.UpdatedBy).HasMaxLength(128);
		builder.Property(scriptVersion => scriptVersion.LastUpdated).HasColumnType("datetime2");
		builder.Property(scriptVersion => scriptVersion.DeletedBy).HasMaxLength(128);
		builder.Property(scriptVersion => scriptVersion.DateDeleted).HasColumnType("datetime2");
		builder.Property(scriptVersion => scriptVersion.IsDeleted).IsRequired().HasDefaultValue(false);
	}
}
