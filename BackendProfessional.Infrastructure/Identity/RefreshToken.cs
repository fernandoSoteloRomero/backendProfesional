using System;

namespace BackendProfessional.Infrastructure.Identity;

public class RefreshToken
{
  public Guid Id { get; private set; } = Guid.NewGuid();
  public Guid UserId { get; set; }

  // Guardaremos el HASH del token (SHA-256) — no el token en claro.
  public string TokenHash { get; set; } = null!;

  // Opcional: JTI del access token asociado (para trazabilidad).
  public string? AccessTokenJti { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime ExpiresAt { get; set; }
  public DateTime? RevokedAt { get; set; }
  public Guid? ReplacedByTokenId { get; set; }

  // Metadata para sesiones (device/ip/user-agent)
  public string? Device { get; set; }
  public string? IpAddress { get; set; }
  public string? UserAgent { get; set; }

  public bool IsRevoked => RevokedAt != null;
  public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;

  public virtual ApplicationUser? User { get; set; }
}
