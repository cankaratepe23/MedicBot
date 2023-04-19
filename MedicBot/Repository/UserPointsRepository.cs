using MedicBot.Manager;
using MedicBot.Model;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.Repository;

public static class UserPointsRepository
{
    static UserPointsRepository()
    {
        // Ensure db and collection is created.
        LiteDbManager.Database.GetCollection<UserPoints>();
        Log.Information(Constants.DbCollectionInitializedUserPoints);
    }

    public static UserPoints? Get(ulong userId)
    {
        return LiteDbManager.Database.GetCollection<UserPoints>().FindOne(p => p.Id == userId);
    }

    public static int GetPoints(ulong userId)
    {
        return LiteDbManager.Database.GetCollection<UserPoints>().FindOne(p => p.Id == userId).Score;
    }

    public static void AddPoints(ulong userId, int score)
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

        LiteDbManager.Database.GetCollection<UserPoints>().Upsert(currentPoints);
    }
}