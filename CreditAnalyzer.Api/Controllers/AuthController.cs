using System.Net;
using CreditAnalyzer.Api.Contracts;
using CreditAnalyzer.Application.Abstractions;
using CreditAnalyzer.Application.Abstractions.Security;
using CreditAnalyzer.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditAnalyzer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IRepository<User> _users;
    private readonly IJwtTokenService _jwt;
    private readonly IPasswordHasher _hasher;

    public AuthController(IRepository<User> users, IJwtTokenService jwt, IPasswordHasher hasher)
    {
        _users = users;
        _jwt = jwt;
        _hasher = hasher;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash)) 
            return Unauthorized();

        var (token, expiresAt) = _jwt.CreateToken(user.Id, user.Email, user.Role);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Role = user.Role
        });
    }
}