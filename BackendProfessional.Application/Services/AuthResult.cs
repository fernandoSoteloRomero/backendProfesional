using System;

namespace BackendProfessional.Application.Services
{
  public sealed record AuthResult(TokenResult Token, string RefreshToken);
}
