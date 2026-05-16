using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class DashboardMetricConfiguration : IEntityTypeConfiguration<DashboardMetric>
{
	public void Configure(EntityTypeBuilder<DashboardMetric> builder)
	{
		builder.ToTable("tblDashboardMetric");

		builder.HasKey(dashboardMetric => dashboardMetric.DashboardMetricId)
			.HasName("PK_tblDashboardMetric_DashboardMetricId");

		builder.Property(dashboardMetric => dashboardMetric.DashboardMetricId)
			.ValueGeneratedOnAdd();

		builder.Property(dashboardMetric => dashboardMetric.MetricDate)
			.HasColumnType("date")
			.IsRequired();

		builder.Property(dashboardMetric => dashboardMetric.TotalUsers)
			.HasDefaultValue(0);

		builder.Property(dashboardMetric => dashboardMetric.ActiveSessionsToday)
			.HasDefaultValue(0);

		builder.Property(dashboardMetric => dashboardMetric.TotalScriptsUploaded)
			.HasDefaultValue(0);

		builder.Property(dashboardMetric => dashboardMetric.TotalMistakesRecorded)
			.HasDefaultValue(0);

		ConfigureAuditColumns(builder);

		builder.HasIndex(dashboardMetric => dashboardMetric.MetricDate)
			.IsUnique()
			.HasDatabaseName("UK_tblDashboardMetric_MetricDate");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<DashboardMetric> builder)
	{
		builder.Property(dashboardMetric => dashboardMetric.Tag)
			.HasMaxLength(64);

		builder.Property(dashboardMetric => dashboardMetric.Comments)
			.HasMaxLength(256);

		builder.Property(dashboardMetric => dashboardMetric.SortOrder)
			.HasDefaultValue(0);

		builder.Property(dashboardMetric => dashboardMetric.IPAddress)
			.HasMaxLength(64)
			.IsRequired()
			.HasDefaultValue("127.0.0.1");

		builder.Property(dashboardMetric => dashboardMetric.CreatedBy)
			.HasMaxLength(128)
			.IsRequired()
			.HasDefaultValue("Admin");

		builder.Property(dashboardMetric => dashboardMetric.DateCreated)
			.HasColumnType("datetime2")
			.HasDefaultValueSql("GETDATE()");

		builder.Property(dashboardMetric => dashboardMetric.UpdatedBy)
			.HasMaxLength(128);

		builder.Property(dashboardMetric => dashboardMetric.LastUpdated)
			.HasColumnType("datetime2");

		builder.Property(dashboardMetric => dashboardMetric.DeletedBy)
			.HasMaxLength(128);

		builder.Property(dashboardMetric => dashboardMetric.DateDeleted)
			.HasColumnType("datetime2");

		builder.Property(dashboardMetric => dashboardMetric.IsDeleted)
			.IsRequired()
			.HasDefaultValue(false);
	}
}
