using System;
using System.Security.Cryptography;
using System.Text;
using BackendProfessional.Application.Services;
using BackendProfessional.Infrastructure.Identity;
using BackendProfessional.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BackendProfessional.Infrastructure.Security;

public class AuthService : IAuthService
{
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly AppDbContext _db;
  private readonly ITokenService _tokenService;
  private readonly IConfiguration _config;
  private readonly ILogger<AuthService> _logger;
  private readonly int _refreshTokenDays;

  public AuthService(UserManager<ApplicationUser> userManager, AppDbContext db, ITokenService tokenService,
    IConfiguration config, ILogger<AuthService> logger)
  {
    _userManager = userManager;
    _db = db;
    _tokenService = tokenService;
    _config = config;
    _logger = logger;
    _refreshTokenDays = int.TryParse(_config["Auth:RefreshTokenDays"], out var d) ? d : 30;
  }


  public async Task<AuthResult> LoginAsync(string email, string password, string? device = null, string? ip = null,
    string? userAgent = null)
  {
    var user = await _userManager.FindByEmailAsync(email);
    if (user == null) throw new UnauthorizedAccessException("Credenciales invalidas");
    if (!user.IsActive) throw new UnauthorizedAccessException("Cuenta inactiva");

    if (!await _userManager.CheckPasswordAsync(user, password))
      throw new UnauthorizedAccessException("Credenciales invalidas");

    var roles = await _userManager.GetRolesAsync(user);
    var tokenResult = _tokenService.CreateToken(user.Id, user.UserName ?? user.Email!, roles);

    var plainRefresh = GenerateRandomToken();
    var hash = HashToken(plainRefresh);

    var refreshEntity = new RefreshToken()
    {
      UserId = user.Id,
      TokenHash = hash,
      AccessTokenJti = tokenResult.Jti,
      CreatedAt = DateTime.UtcNow,
      ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays),
      Device = device,
      IpAddress = ip,
      UserAgent = userAgent
    };

    _db.RefreshTokens.Add(refreshEntity);
    await _db.SaveChangesAsync();
    return new AuthResult(tokenResult, plainRefresh);
  }

  public async Task LogoutAsync(string refreshToken, string? correlationId = null)
  {
    var hash = HashToken(refreshToken);
    var existing = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash);

    if (existing == null)
    {
      // Idempotente: nada que hacer
      return;
    }

    // Usamos transacción para evitar carreras concurrentes
    using var tx = await _db.Database.BeginTransactionAsync();
    try
    {
      // Recargamos la entidad (tracking) por su Id
      existing = await _db.RefreshTokens
        .Where(r => r.Id == existing.Id)
        .FirstOrDefaultAsync();

      if (existing == null)
      {
        await tx.CommitAsync();
        return;
      }

      if (!existing.IsRevoked)
      {
        existing.RevokedAt = DateTime.UtcNow;
        _db.RefreshTokens.Update(existing);

        _db.AuditLogs.Add(new AuditLog
        {
          UserId = existing.UserId,
          Action = "Logout",
          DataJson = $"{{\"refreshTokenId\":\"{existing.Id}\"}}",
          CorrelationId = correlationId,
          CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
      }

      await tx.CommitAsync();
    }
    catch
    {
      await tx.RollbackAsync();
      throw;
    }
  }

  public async Task<AuthResult> RefreshTokenAync(string refreshToken)
  {
    var hash = HashToken(refreshToken);

    var existing = await _db.RefreshTokens
      .Include(r => r.User)
      .FirstOrDefaultAsync(r => r.TokenHash == hash);

    if (existing == null || !existing.IsActive)
      throw new UnauthorizedAccessException("Invalid or expired refresh token");

    var user = existing.User ?? throw new UnauthorizedAccessException("User not found");

    if (!user.IsActive)
      throw new UnauthorizedAccessException("User disabled");

    var roles = await _userManager.GetRolesAsync(user);

    var newAccess = _tokenService.CreateToken(user.Id, user.UserName ?? user.Email!, roles);

    var newPlain = GenerateRandomToken();
    var newHash = HashToken(newPlain);

    var newRefresh = new RefreshToken
    {
      UserId = user.Id,
      TokenHash = newHash,
      AccessTokenJti = newAccess.Jti,
      CreatedAt = DateTime.UtcNow,
      ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays),
      Device = existing.Device,
      IpAddress = existing.IpAddress,
      UserAgent = existing.UserAgent
    };

    using var tx = await _db.Database.BeginTransactionAsync();
    try
    {
      existing.RevokedAt = DateTime.UtcNow;
      existing.ReplacedByTokenId = newRefresh.Id;

      _db.RefreshTokens.Add(newRefresh);
      _db.RefreshTokens.Update(existing);

      _db.AuditLogs.Add(new AuditLog
      {
        UserId = user.Id,
        Action = "RefreshTokenRotated",
        DataJson = $"{{\"replaced\":\"{existing.Id}\",\"new\":\"{newRefresh.Id}\"}}",
        CreatedAt = DateTime.UtcNow
      });

      await _db.SaveChangesAsync();
      await tx.CommitAsync();
    }
    catch (Exception ex)
    {
      await tx.RollbackAsync();
      _logger.LogError(ex, "Error rotating refresh token");
      throw;
    }

    return new AuthResult(newAccess, newPlain);
  }


  //* -- helpers --

  private static string GenerateRandomToken()
  {
    var bytes = new byte[64];
    RandomNumberGenerator.Fill(bytes);
    return Base64UrlEncode(bytes);
  }

  private static string HashToken(string token)
  {
    using var sha = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(token);
    var hash = sha.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
  }

  private static string Base64UrlEncode(byte[] bytes) =>
    Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
