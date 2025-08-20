using CreditAnalyzer.Application.Abstractions;
using CreditAnalyzer.Application.Abstractions.Security;

namespace CreditAnalyzer.Infrastructure.Security;

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}