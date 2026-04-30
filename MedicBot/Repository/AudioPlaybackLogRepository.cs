using MedicBot.Model;
using MedicBot.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace MedicBot.Repository;

public class AudioPlaybackLogRepository : IAudioPlaybackLogRepository
{
    private readonly IMongoCollection<AudioPlaybackLog> _collection;

    public AudioPlaybackLogRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<AudioPlaybackLog>(AudioPlaybackLog.CollectionName);
        Log.Information(string.Format(Constants.DbCollectionInitializedFormatString, AudioPlaybackLog.CollectionName));
    }

    public IEnumerable<AudioPlaybackLog> GetGlobalLog()
    {
        return _collection.Find(_ => true).ToEnumerable();
    }

    public IEnumerable<AudioPlaybackLog> GetUserLog(ulong userId)
    {
        return _collection.Find(t => t.UserId == userId).ToEnumerable();
    }

    public async Task AddAsync(AudioPlaybackLog audioPlaybackLog)
    {
        await _collection.InsertOneAsync(audioPlaybackLog);
    }

    public async Task DeleteAsync(ObjectId id)
    {
        await _collection.DeleteOneAsync(t => t.Id == id);
    }
}
