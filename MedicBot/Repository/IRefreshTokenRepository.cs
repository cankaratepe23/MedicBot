using MedicBot.Model;

namespace MedicBot.Repository;

public interface IRefreshTokenRepository
{
    void Add(RefreshToken refreshToken);
    RefreshToken? FindByHash(string tokenHash);
    bool Update(RefreshToken refreshToken);
}
