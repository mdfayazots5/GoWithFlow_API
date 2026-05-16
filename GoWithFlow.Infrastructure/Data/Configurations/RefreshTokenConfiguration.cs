using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoWithFlow.Infrastructure.Data.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
	public void Configure(EntityTypeBuilder<RefreshToken> builder)
	{
		builder.ToTable("tblRefreshToken");

		builder.HasKey(refreshToken => refreshToken.RefreshTokenId)
			.HasName("PK_tblRefreshToken_RefreshTokenId");

		builder.Property(refreshToken => refreshToken.RefreshTokenId)
			.ValueGeneratedOnAdd();

		builder.Property(refreshToken => refreshToken.UserId)
			.IsRequired();

		builder.Property(refreshToken => refreshToken.Token)
			.HasMaxLength(512)
			.IsRequired();

		builder.Property(refreshToken => refreshToken.ExpiresAt)
			.HasColumnType("datetime2")
			.IsRequired();

		builder.Property(refreshToken => refreshToken.IsRevoked)
			.IsRequired()
			.HasDefaultValue(false);

		builder.Property(refreshToken => refreshToken.RevokedAt)
			.HasColumnType("datetime2");

		builder.Property(refreshToken => refreshToken.DeviceInfo)
			.HasMaxLength(256);

		ConfigureAuditColumns(builder);

		builder.HasIndex(refreshToken => refreshToken.UserId)
			.HasDatabaseName("IDX_tblRefreshToken_UserId");

		builder.HasOne(refreshToken => refreshToken.User)
			.WithMany(user => user.RefreshTokens)
			.HasForeignKey(refreshToken => refreshToken.UserId)
			.HasConstraintName("FK_tblRefreshToken_UserId_tblUser_UserId");
	}

	private static void ConfigureAuditColumns(EntityTypeBuilder<RefreshToken> builder)
	{
		builder.Property(refreshToken => refreshToken.Tag)
			.HasMaxLength(64);

		builder.Property(refreshToken => refreshToken.Comments)
			.HasMaxLength(256);

		builder.Property(refreshToken => refreshToken.SortOrder)
			.HasDefaultValue(0);

		builder.Property(refreshToken => refreshToken.IPAddress)
			.HasMaxLength(64)
			.IsRequired()
			.HasDefaultValue("127.0.0.1");

		builder.Property(refreshToken => refreshToken.CreatedBy)
			.HasMaxLength(128)
			.IsRequired()
			.HasDefaultValue("Admin");

		builder.Property(refreshToken => refreshToken.DateCreated)
			.HasColumnType("datetime2")
			.HasDefaultValueSql("GETDATE()");

		builder.Property(refreshToken => refreshToken.UpdatedBy)
			.HasMaxLength(128);

		builder.Property(refreshToken => refreshToken.LastUpdated)
			.HasColumnType("datetime2");

		builder.Property(refreshToken => refreshToken.DeletedBy)
			.HasMaxLength(128);

		builder.Property(refreshToken => refreshToken.DateDeleted)
			.HasColumnType("datetime2");

		builder.Property(refreshToken => refreshToken.IsDeleted)
			.IsRequired()
			.HasDefaultValue(false);
	}
}
