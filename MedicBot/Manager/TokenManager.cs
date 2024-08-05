using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace MedicBot.Manager;

public static class TokenManager
{
    private static string JwtSecret { get; set; } = null!;

    public static void Init(string jwtSecret)
    {
        JwtSecret = jwtSecret;
    }
    public static string GenerateTemporaryToken(string userId, TimeSpan? duration = null)
    {
        var durationValue = duration ?? TimeSpan.FromMinutes(15);

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
}