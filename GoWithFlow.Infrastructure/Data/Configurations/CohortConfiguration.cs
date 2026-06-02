using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class CohortConfiguration : IEntityTypeConfiguration<Cohort>
{
	public void Configure(EntityTypeBuilder<Cohort> builder)
	{
		builder.ToTable("tblCohort");

		builder.HasKey(cohort => cohort.CohortId)
			.HasName("PK_tblCohort_CohortId");

		builder.Property(cohort => cohort.CohortId)
			.ValueGeneratedOnAdd();

		builder.Property(cohort => cohort.CohortName)
			.HasMaxLength(128)
			.IsRequired();

		builder.Property(cohort => cohort.Description)
			.HasMaxLength(256);

		builder.Property(cohort => cohort.IsActive)
			.HasDefaultValue(true);

		builder.HasMany(cohort => cohort.Members)
			.WithOne(user => user.Cohort)
			.HasForeignKey(user => user.CohortId)
			.HasConstraintName("FK_tblUser_CohortId_tblCohort_CohortId")
			.IsRequired(false)
			.OnDelete(DeleteBehavior.SetNull);

		ConfigureAuditColumns(builder);

		builder.HasIndex(cohort => cohort.IsActive)
			.HasDatabaseName("IDX_tblCohort_IsActive");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<Cohort> builder)
	{
		builder.Property(cohort => cohort.Tag).HasMaxLength(64);
		builder.Property(cohort => cohort.Comments).HasMaxLength(256);
		builder.Property(cohort => cohort.SortOrder).HasDefaultValue(0);
		builder.Property(cohort => cohort.IPAddress).HasMaxLength(64).IsRequired().HasDefaultValue("127.0.0.1");
		builder.Property(cohort => cohort.CreatedBy).HasMaxLength(128).IsRequired().HasDefaultValue("Admin");
		builder.Property(cohort => cohort.DateCreated).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
		builder.Property(cohort => cohort.UpdatedBy).HasMaxLength(128);
		builder.Property(cohort => cohort.LastUpdated).HasColumnType("datetime2");
		builder.Property(cohort => cohort.DeletedBy).HasMaxLength(128);
		builder.Property(cohort => cohort.DateDeleted).HasColumnType("datetime2");
		builder.Property(cohort => cohort.IsDeleted).IsRequired().HasDefaultValue(false);
	}
}
