using MedicBot.Model;
using MedicBot.Utils;
using MongoDB.Driver;
using Serilog;

namespace MedicBot.Repository;

public class UserMuteRepository : IUserMuteRepository
{
    private readonly IMongoCollection<UserMute> _collection;

    public UserMuteRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<UserMute>(UserMute.CollectionName);
        Log.Information(Constants.DbCollectionInitializedUserMutes);
    }

    public UserMute? Get(ulong userId)
    {
        return _collection.Find(p => p.Id == userId).FirstOrDefault();
    }

    public DateTime? GetEndDateTime(ulong userId)
    {
        return _collection.Find(p => p.Id == userId).FirstOrDefault()?.EndDateTime;
    }

    public async Task SetAsync(ulong userId, DateTime endTime)
    {
        await _collection.ReplaceOneAsync(d => d.Id == userId, new UserMute(userId, endTime), new ReplaceOptions {IsUpsert = true});
    }

    public void Delete(ulong userId)
    {
        _collection.DeleteMany(u => u.Id == userId);
    }
}