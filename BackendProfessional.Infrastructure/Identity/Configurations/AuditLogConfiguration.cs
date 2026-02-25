using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProfessional.Infrastructure.Identity.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
  public void Configure(EntityTypeBuilder<AuditLog> builder)
  {
    builder.ToTable("audit_logs");
    builder.HasKey(a => a.Id);

    builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
    builder.Property(a => a.EntityName).HasMaxLength(200);
    builder.Property(a => a.EntityId).HasMaxLength(100);
    builder.Property(a => a.CorrelationId).HasMaxLength(100);
    builder.Property(a => a.CreatedAt).IsRequired();

    builder.HasIndex(a => a.UserId).HasDatabaseName("ix_audit_logs_userid");
    builder.HasIndex(a => a.CreatedAt).HasDatabaseName("ix_audit_logs_createdat");
  }
}