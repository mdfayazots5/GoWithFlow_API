using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class AdminNoteConfiguration : IEntityTypeConfiguration<AdminNote>
{
	public void Configure(EntityTypeBuilder<AdminNote> builder)
	{
		builder.ToTable("tblAdminNote");

		builder.HasKey(adminNote => adminNote.AdminNoteId)
			.HasName("PK_tblAdminNote_AdminNoteId");

		builder.Property(adminNote => adminNote.AdminNoteId)
			.ValueGeneratedOnAdd();

		builder.Property(adminNote => adminNote.NoteText)
			.HasMaxLength(512)
			.IsRequired();

		builder.Property(adminNote => adminNote.NoteDate)
			.HasColumnType("datetime2")
			.HasDefaultValueSql("GETDATE()");

		ConfigureAuditColumns(builder);

		builder.HasIndex(adminNote => adminNote.TargetUserId)
			.HasDatabaseName("IDX_tblAdminNote_TargetUserId");

		builder.HasOne<User>()
			.WithMany()
			.HasForeignKey(adminNote => adminNote.AdminUserId)
			.OnDelete(DeleteBehavior.Restrict)
			.HasConstraintName("FK_tblAdminNote_AdminUserId_tblUser_UserId");

		builder.HasOne<User>()
			.WithMany()
			.HasForeignKey(adminNote => adminNote.TargetUserId)
			.OnDelete(DeleteBehavior.Restrict)
			.HasConstraintName("FK_tblAdminNote_TargetUserId_tblUser_UserId");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<AdminNote> builder)
	{
		builder.Property(adminNote => adminNote.Tag)
			.HasMaxLength(64);

		builder.Property(adminNote => adminNote.Comments)
			.HasMaxLength(256);

		builder.Property(adminNote => adminNote.SortOrder)
			.HasDefaultValue(0);

		builder.Property(adminNote => adminNote.IPAddress)
			.HasMaxLength(64)
			.IsRequired()
			.HasDefaultValue("127.0.0.1");

		builder.Property(adminNote => adminNote.CreatedBy)
			.HasMaxLength(128)
			.IsRequired()
			.HasDefaultValue("Admin");

		builder.Property(adminNote => adminNote.DateCreated)
			.HasColumnType("datetime2")
			.HasDefaultValueSql("GETDATE()");

		builder.Property(adminNote => adminNote.UpdatedBy)
			.HasMaxLength(128);

		builder.Property(adminNote => adminNote.LastUpdated)
			.HasColumnType("datetime2");

		builder.Property(adminNote => adminNote.DeletedBy)
			.HasMaxLength(128);

		builder.Property(adminNote => adminNote.DateDeleted)
			.HasColumnType("datetime2");

		builder.Property(adminNote => adminNote.IsDeleted)
			.IsRequired()
			.HasDefaultValue(false);
	}
}
