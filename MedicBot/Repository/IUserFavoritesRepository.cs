using MedicBot.Model;
using MongoDB.Bson;

namespace MedicBot.Repository;

public interface IUserFavoritesRepository
{
    IEnumerable<UserFavorite> GetUserFavorites(ulong userId);
    bool IsFavorited(ulong userId, ObjectId trackId);
    Task AddAsync(UserFavorite userFavorite);
    Task DeleteAsync(ObjectId id);
    Task DeleteByUserAndTrackIdAsync(ulong userId, ObjectId trackId);
}
