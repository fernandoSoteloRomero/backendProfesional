using System;
using BackendProfessional.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BackendProfessional.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
  {
  }

  public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
  public DbSet<AuditLog> AuditLogs { get; set; } = null!;

  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    builder.Entity<ApplicationUser>(b =>
    {
      b.Property(u => u.DisplayName).HasMaxLength(200);
      b.Property(u => u.IsActive).HasDefaultValue(true);
    });

    // * configuracion existente para applicationUser
    builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
  }
}
