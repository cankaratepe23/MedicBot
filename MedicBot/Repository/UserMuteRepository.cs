using MedicBot.Manager;
using MedicBot.Model;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.Repository;

public static class UserMuteRepository
{
    static UserMuteRepository()
    {
        // Ensure db and collection is created.
        LiteDbManager.Database.GetCollection<UserMute>();
        Log.Information(Constants.DbCollectionInitializedUserMutes);
    }

    public static UserMute? Get(ulong userId)
    {
        return LiteDbManager.Database.GetCollection<UserMute>().FindOne(p => p.Id == userId);
    }

    public static DateTime GetEndDateTime(ulong userId)
    {
        return LiteDbManager.Database.GetCollection<UserMute>().FindOne(p => p.Id == userId).EndDateTime;
    }

    public static void Delete(ulong userId)
    {
        LiteDbManager.Database.GetCollection<UserMute>().DeleteMany(u => u.Id == userId);
    }
}