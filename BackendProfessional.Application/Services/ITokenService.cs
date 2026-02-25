using System;

namespace BackendProfessional.Application.Services;

public interface ITokenService
{
  TokenResult CreateToken(Guid userId, string userName, IEnumerable<string> roles);
}
