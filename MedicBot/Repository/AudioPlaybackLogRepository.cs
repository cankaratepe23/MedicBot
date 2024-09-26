using System;
using MedicBot.Manager;
using MedicBot.Model;
using MedicBot.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace MedicBot.Repository;

public class AudioPlaybackLogRepository
{
    private static readonly IMongoCollection<AudioPlaybackLog> AudioPlaybackLogCollection;

    static AudioPlaybackLogRepository()
    {
        var collection = MongoDbManager.Database.GetCollection<AudioPlaybackLog>(AudioPlaybackLog.CollectionName);
        // TODO Create TTL index?
        AudioPlaybackLogCollection = collection;
        Log.Information(string.Format(Constants.DbCollectionInitializedFormatString, AudioPlaybackLog.CollectionName));
    }

    public static IEnumerable<AudioPlaybackLog> GetUserRecents(ulong userId)
    {
        return AudioPlaybackLogCollection.Find(t => t.DiscordMember.Id == userId).ToEnumerable();
    }

    public static async void Add(AudioPlaybackLog AudioPlaybackLog)
    {
        await AudioPlaybackLogCollection.InsertOneAsync(AudioPlaybackLog);
    }

    public static async void Delete(ObjectId id)
    {
        await AudioPlaybackLogCollection.DeleteOneAsync(t => t.Id == id);
    }
}
