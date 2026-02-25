using BackendProfessional.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackendProfessional.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth) => _auth = auth;

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var device = Request.Headers["X-Device"].FirstOrDefault() ??
                             Request.Headers["User-Agent"].FirstOrDefault();
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].FirstOrDefault();

                var res = await _auth.LoginAsync(dto.Email, dto.Password, device, ip, userAgent);
                return Ok(new
                {
                    access_token = res.Token.AccessToken,
                    expires_at = res.Token.ExpiresAt,
                    refresh_token = res.RefreshToken
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { error = "Invalid credentials" });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshDto dto)
        {
            try
            {
                var res = await _auth.RefreshTokenAync(dto.RefreshToken);
                return Ok(new
                {
                    access_token = res.Token.AccessToken,
                    expires_at = res.Token.ExpiresAt,
                    refresh_token = res.RefreshToken
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { error = "Invalid refresh token" });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshDto dto)
        {
            await _auth.LogoutAsync(dto.RefreshToken);
            return NoContent();
        }
    }

    public class LoginDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class RefreshDto
    {
        public string RefreshToken { get; set; } = "";
    }
}
