using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MedicBot.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MedicBot.Manager;

public class TokenManager : ITokenManager
{
    private readonly string _jwtSecret;

    public TimeSpan AccessTokenLifetime { get; }
    public TimeSpan RefreshTokenLifetime { get; }

    public TokenManager(IOptions<AuthOptions> authOptions)
    {
        _jwtSecret = authOptions.Value.JwtSecret;
        AccessTokenLifetime = authOptions.Value.AccessTokenLifetime;
        RefreshTokenLifetime = authOptions.Value.RefreshTokenLifetime;
    }

    public string GenerateTemporaryToken(string userId, TimeSpan? duration = null)
    {
        var durationValue = duration ?? AccessTokenLifetime;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Convert.FromBase64String(_jwtSecret);
        var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId)}),
            Expires = DateTime.UtcNow.Add(durationValue),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        });
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(randomBytes);
    }

    public string HashToken(string token)
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
