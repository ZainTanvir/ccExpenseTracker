using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CreditAnalyzer.Application.Abstractions.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CreditAnalyzer.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;
    public JwtTokenService(IConfiguration config) => _config = config;

    public (string token, DateTimeOffset expiresAt) CreateToken(Guid userId, string email, string role)
    {
        var issuer  = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60"));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var token = new JwtSecurityToken(issuer, audience, claims, expires: expires.UtcDateTime, signingCredentials: creds);
        var jwt   = new JwtSecurityTokenHandler().WriteToken(token);
        return (jwt, expires);
    }
}