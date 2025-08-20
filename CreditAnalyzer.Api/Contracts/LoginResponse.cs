namespace CreditAnalyzer.Api.Contracts;

public class LoginResponse
{
    public string Token { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public string Role { get; set; } = default!;
}