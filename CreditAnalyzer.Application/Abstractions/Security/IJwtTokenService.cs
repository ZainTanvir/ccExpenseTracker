namespace CreditAnalyzer.Application.Abstractions.Security;

public interface IJwtTokenService
{
    (string token, DateTimeOffset expiresAt) CreateToken(Guid userId, string email, string role);
}