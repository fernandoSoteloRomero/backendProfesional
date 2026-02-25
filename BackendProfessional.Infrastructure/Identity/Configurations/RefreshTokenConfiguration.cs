using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProfessional.Infrastructure.Identity.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
  public void Configure(EntityTypeBuilder<RefreshToken> builder)
  {
    builder.ToTable("refresh_tokens");

    builder.HasKey(r => r.Id);

    builder.Property(r => r.TokenHash)
      .IsRequired()
      .HasMaxLength(200);

    builder.Property(r => r.AccessTokenJti).HasMaxLength(100);
    builder.Property(r => r.Device).HasMaxLength(200);
    builder.Property(r => r.IpAddress).HasMaxLength(50);
    builder.Property(r => r.UserAgent).HasMaxLength(500);

    builder.Property(r => r.CreatedAt).IsRequired();
    builder.Property(r => r.ExpiresAt).IsRequired();

    builder.HasIndex(r => r.UserId).HasDatabaseName("ix_refresh_tokens_userid");
    builder.HasIndex(r => r.TokenHash).HasDatabaseName("ix_refresh_tokens_tokenhash");

    builder.HasOne(r => r.User)
      .WithMany(u => u.RefreshTokens)
      .HasForeignKey(r => r.UserId)
      .OnDelete(DeleteBehavior.Restrict);
  }
}