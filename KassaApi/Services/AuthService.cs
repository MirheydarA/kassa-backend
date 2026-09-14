using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KassaApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace KassaApi.Services;

public class AuthService
{
    private readonly IConfiguration _config;
    private readonly PasswordHasher<AppUser> _hasher = new();

    public AuthService(IConfiguration config)
    {
        _config = config;
    }

    public string HashPassword(AppUser user, string password) => _hasher.HashPassword(user, password);

    public bool VerifyPassword(AppUser user, string password)
    {
        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }

    public (string token, DateTime expiresAt) GenerateToken(AppUser user)
    {
        var jwtSection = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresMinutes = int.Parse(jwtSection["ExpiresMinutes"] ?? "480");
        var expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
