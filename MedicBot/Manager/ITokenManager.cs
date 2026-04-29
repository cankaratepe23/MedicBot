namespace MedicBot.Manager;

public interface ITokenManager
{
    TimeSpan AccessTokenLifetime { get; }
    TimeSpan RefreshTokenLifetime { get; }
    string GenerateTemporaryToken(string userId, TimeSpan? duration = null);
    string GenerateRefreshToken();
    string HashToken(string token);
}
