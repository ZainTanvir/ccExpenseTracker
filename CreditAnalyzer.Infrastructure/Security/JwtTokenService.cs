// CreditAnalyzer.Infrastructure/Security/JwtTokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CreditAnalyzer.Application.Abstractions;
using CreditAnalyzer.Application.Abstractions.Security;
using CreditAnalyzer.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public class JwtTokenService : IJwtTokenService
{
    private readonly IOptionsMonitor<JwtOptions> _opts;
    public JwtTokenService(IOptionsMonitor<JwtOptions> opts) => _opts = opts;

    public (string token, DateTimeOffset expiresAt) CreateToken(Guid userId, string email, string role)
    {
        var o = _opts.CurrentValue;
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Key)), SecurityAlgorithms.HmacSha256);
        var expires = DateTimeOffset.UtcNow.AddMinutes(o.ExpiresMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(o.Issuer, o.Audience, claims, expires: expires.UtcDateTime, signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}