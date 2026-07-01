using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using cerberus_securitygate.Models;
using Microsoft.IdentityModel.Tokens;

namespace cerberus_securitygate.Services;

public class JwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config) => _config = config;

    public (string token, DateTimeOffset expiresAt) Generate(User user)
    {
        var secret  = _config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured");
        var issuer  = _config["Jwt:Issuer"]  ?? "cerberus";
        var hours   = int.TryParse(_config["Jwt:ExpiresHours"], out var h) ? h : 8;
        var expires = DateTimeOffset.UtcNow.AddHours(hours);

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role",                        user.Role),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           issuer,
            claims:             claims,
            expires:            expires.UtcDateTime,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
