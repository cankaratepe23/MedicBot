using LiteDB;
using MedicBot.Model;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.Repository;

public static class UserMuteRepository
{
    static UserMuteRepository()
    {
        // Ensure db and collection is created.
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        db.GetCollection<UserMute>();
        Log.Information(Constants.DbCollectionInitializedUserMutes);
    }

    public static UserMute? Get(ulong userId)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        return db.GetCollection<UserMute>().FindOne(p => p.Id == userId);
    }

    public static DateTime GetEndDateTime(ulong userId)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        return db.GetCollection<UserMute>().FindOne(p => p.Id == userId).EndDateTime;
    }

    public static void Delete(ulong userId)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        db.GetCollection<UserMute>().DeleteMany(u => u.Id == userId);
    }
}