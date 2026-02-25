using System;

namespace BackendProfessional.Infrastructure.Identity;

public class AuditLog
{
  public Guid Id { get; private set; } = Guid.NewGuid();
  public Guid? UserId { get; set; }
  public string Action { get; set; } = null!; // e.g. "Logout", "RefreshTokenRevoked"
  public string? EntityName { get; set; }
  public string? EntityId { get; set; }
  public string? DataJson { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public string? CorrelationId { get; set; }
}