using MedicBot.Model;
using MedicBot.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace MedicBot.Repository;

public class UserFavoritesRepository : IUserFavoritesRepository
{
    private readonly IMongoCollection<UserFavorite> _collection;

    public UserFavoritesRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<UserFavorite>(UserFavorite.CollectionName);
        Log.Information(Constants.DbCollectionInitializedUserFavorites);
    }

    public IEnumerable<UserFavorite> GetUserFavorites(ulong userId)
    {
        return _collection.Find(f => f.UserId == userId).ToEnumerable();
    }

    public bool IsFavorited(ulong userId, ObjectId trackId)
    {
        return _collection.Find(f => f.UserId == userId && f.TrackId == trackId).Any();
    }

    public async Task AddAsync(UserFavorite userFavorite)
    {
        await _collection.InsertOneAsync(userFavorite);
    }

    public async Task DeleteAsync(ObjectId id)
    {
        await _collection.DeleteOneAsync(u => u.Id == id);
    }

    public async Task DeleteByUserAndTrackIdAsync(ulong userId, ObjectId trackId)
    {
        await _collection.DeleteOneAsync(u => u.UserId == userId && u.TrackId == trackId);
    }
}
