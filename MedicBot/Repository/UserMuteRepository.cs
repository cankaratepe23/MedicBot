using MedicBot.Manager;
using MedicBot.Model;
using MedicBot.Utils;
using MongoDB.Driver;
using Serilog;

namespace MedicBot.Repository;

public static class UserMuteRepository
{
    private static readonly IMongoCollection<UserMute> UserMuteCollection;

    static UserMuteRepository()
    {
        // Ensure db and collection is created.
        UserMuteCollection = MongoDbManager.Database.GetCollection<UserMute>(UserMute.CollectionName);
        Log.Information(Constants.DbCollectionInitializedUserMutes);
    }

    public static UserMute? Get(ulong userId)
    {
        return UserMuteCollection.Find(p => p.Id == userId).FirstOrDefault();
    }

    public static DateTime GetEndDateTime(ulong userId)
    {
        // TODO NullReferenceException here for non-muted users
        return UserMuteCollection.Find(p => p.Id == userId).FirstOrDefault().EndDateTime;
    }

    public static async void Set(ulong userId, DateTime endTime)
    {
        await UserMuteCollection.ReplaceOneAsync(d => d.Id == userId, new UserMute(userId, endTime), new ReplaceOptions {IsUpsert = true});
    }

    public static void Delete(ulong userId)
    {
        UserMuteCollection.DeleteMany(u => u.Id == userId);
    }
}