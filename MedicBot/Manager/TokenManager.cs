using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MedicBot.Manager;

public static class TokenManager
{
    private static string JwtSecret { get; set; } = null!;
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public static void Init(string jwtSecret)
    {
        JwtSecret = jwtSecret;
    }
    public static string GenerateTemporaryToken(string userId, TimeSpan? duration = null)
    {
        var durationValue = duration ?? AccessTokenLifetime;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Convert.FromBase64String(JwtSecret);
        var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId)}),
            Expires = DateTime.UtcNow.Add(durationValue),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        });
        return tokenHandler.WriteToken(token);
    }

    public static string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(randomBytes);
    }

    public static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = sha256.ComputeHash(tokenBytes);
        return Convert.ToBase64String(hashBytes);
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
