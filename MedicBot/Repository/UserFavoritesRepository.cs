using MedicBot.Manager;
using MedicBot.Model;
using MedicBot.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace MedicBot.Repository;

public static class UserFavoritesRepository
{
    private static readonly IMongoCollection<UserFavorite> UserFavoritesCollection;

    static UserFavoritesRepository()
    {
        // Ensure db and collection is created.
        UserFavoritesCollection = MongoDbManager.Database.GetCollection<UserFavorite>(UserFavorite.CollectionName);
        Log.Information(Constants.DbCollectionInitializedUserFavorites);
    }

    public static IEnumerable<UserFavorite> GetUserFavorites(ulong userId)
    {
        return UserFavoritesCollection.Find(f => f.UserId == userId).ToEnumerable();
    }

    public static bool IsFavorited(ulong userId, ObjectId trackId)
    {
        return UserFavoritesCollection.Find(f => f.UserId == userId && f.TrackId == trackId).Any();
    }

    public static async void Add(UserFavorite userFavorite)
    {
        await UserFavoritesCollection.InsertOneAsync(userFavorite);
    }

    public static async void Delete(ObjectId id)
    {
        await UserFavoritesCollection.DeleteOneAsync(u => u.Id == id);
    }

    public static async void DeleteByUserAndTrackId(ulong userId, ObjectId trackId)
    {
        await UserFavoritesCollection.DeleteOneAsync(u => u.UserId == userId && u.TrackId == trackId);
    }
}
