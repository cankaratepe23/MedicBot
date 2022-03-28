using LiteDB;
using MedicBot.Model;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.Repository;

public static class UserPointsRepository
{
    static UserPointsRepository()
    {
        // Ensure db and collection is created.
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        db.GetCollection<UserPoints>();
        Log.Information(Constants.DbCollectionInitializedUserPoints);
    }

    public static UserPoints? Get(ulong userId)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        return db.GetCollection<UserPoints>().FindOne(p => p.Id == userId);
    }

    public static int GetPoints(ulong userId)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        return db.GetCollection<UserPoints>().FindOne(p => p.Id == userId).Score;
    }

    public static void AddPoints(ulong userId, int score)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        var currentPoints = Get(userId);

        if (currentPoints == null)
        {
            currentPoints = new UserPoints(userId, score);
        }
        else
        {
            currentPoints.Score += score;
        }

        db.GetCollection<UserPoints>().Upsert(currentPoints);
    }
}