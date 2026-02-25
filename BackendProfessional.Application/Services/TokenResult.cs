using System;

namespace BackendProfessional.Application.Services
{
  public sealed record TokenResult(string AccessToken, string Jti, DateTime ExpiresAt);
}