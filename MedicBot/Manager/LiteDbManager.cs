using LiteDB;
using MedicBot.Utils;

namespace MedicBot.Manager;

public static class LiteDbManager
{
    static LiteDbManager()
    {
        Database = new LiteDatabase(Constants.LiteDatabasePath);
    }

    public static LiteDatabase Database { get; }
}