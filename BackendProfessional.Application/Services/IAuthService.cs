using System;

namespace BackendProfessional.Application.Services;

public interface IAuthService
{
  Task<AuthResult> LoginAsync(string email, string password, string? device = null, string? ip = null,
    string? userAgent = null);

  Task<AuthResult> RefreshTokenAync(string refreshToken);

  Task LogoutAsync(string refreshToken, string? correlationId = null);
}
