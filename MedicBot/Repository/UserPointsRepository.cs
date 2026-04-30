using MedicBot.Model;
using MedicBot.Utils;
using MongoDB.Driver;
using Serilog;

namespace MedicBot.Repository;

public class UserPointsRepository : IUserPointsRepository
{
    private readonly IMongoCollection<UserPoints> _collection;
    private readonly ISettingsRepository _settingsRepository;

    public UserPointsRepository(IMongoDatabase database, ISettingsRepository settingsRepository)
    {
        _collection = database.GetCollection<UserPoints>(UserPoints.CollectionName);
        _settingsRepository = settingsRepository;
        Log.Information(Constants.DbCollectionInitializedUserPoints);
    }

    public UserPoints? Get(ulong userId)
    {
        return _collection.Find(p => p.Id == userId).FirstOrDefault();
    }

    public int GetPoints(ulong userId)
    {
        var userPoints = Get(userId) ??
                         AddPoints(userId, _settingsRepository.GetValue<int>(Constants.DefaultScore) * 100);
        return userPoints.Score;
    }

    public UserPoints AddPoints(ulong userId, int score)
    {
        var currentPoints = Get(userId);

        if (currentPoints == null)
        {
            currentPoints = new UserPoints(userId, score);
        }
        else
        {
            currentPoints.Score += score;
        }

        _collection.ReplaceOne(p => p.Id == currentPoints.Id, currentPoints,
            new ReplaceOptions {IsUpsert = true});
        return currentPoints;
    }
}