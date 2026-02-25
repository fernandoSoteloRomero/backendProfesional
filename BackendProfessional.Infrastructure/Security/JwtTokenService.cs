using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BackendProfessional.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BackendProfessional.Infrastructure.Security;

public class JwtTokenService : ITokenService
{
  private readonly IConfiguration _config;

  public JwtTokenService(IConfiguration config) => _config = config;

  public TokenResult CreateToken(Guid userId, string userName, IEnumerable<string> roles)
  {
    var key = _config["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(key))
      throw new InvalidOperationException("Jwt:Key is not configured.");

    var expireMinutes = int.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m : 15;

    var jti = Guid.NewGuid().ToString();

    var claims = new List<Claim>
    {
      new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
      new Claim(JwtRegisteredClaimNames.UniqueName, userName ?? string.Empty),
      new Claim(JwtRegisteredClaimNames.Jti, jti)
    };

    if (roles != null)
    {
      foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));
    }

    var keyBytes = Encoding.UTF8.GetBytes(key);
    var securityKey = new SymmetricSecurityKey(keyBytes);
    var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var expires = DateTime.UtcNow.AddMinutes(expireMinutes);

    var token = new JwtSecurityToken(
      claims: claims,
      expires: expires,
      signingCredentials: creds);

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return new TokenResult(tokenString, jti, expires);
  }
}