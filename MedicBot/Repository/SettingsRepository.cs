using LiteDB;
using MedicBot.Model;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.Repository;

public static class SettingsRepository
{
    static SettingsRepository()
    {
        // Ensure db and collection is created.
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        db.GetCollection<BotSetting>();
        Init(Constants.MinNumberOfUsersNeededToEarnPoints, 2);
        Log.Information(Constants.DbCollectionInitializedBotSettings);
    }

    public static BotSetting? Get(string key)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        return db.GetCollection<BotSetting>().FindOne(s => s.Key == key);
    }
    
    public static T? GetValue<T>(string key)
    {
        var botSetting = Get(key);

        return (T?) botSetting?.Value;
    }

    public static IEnumerable<BotSetting> All()
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        return db.GetCollection<BotSetting>().FindAll();
    }

    public static void Set(string key, object value)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        var botSetting = Get(key);
        if (Constants.IntegerSettingKeys.Contains(key))
        {
            value = Convert.ToInt32(value);
        }
        if (botSetting == null)
        {
            botSetting = new BotSetting(key, value);
        }
        else
        {
            botSetting.Value = value;
        }
        db.GetCollection<BotSetting>().Upsert(botSetting);
    }
    
    public static void Init(string key, object value)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        var botSetting = Get(key);
        if (Constants.IntegerSettingKeys.Contains(key))
        {
            value = Convert.ToInt32(value);
        }
        if (botSetting != null)
        {
            return;
        }
        db.GetCollection<BotSetting>().Insert(new BotSetting(key, value));
    }

    public static void Delete(string key)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        db.GetCollection<BotSetting>().DeleteMany(s => s.Key == key);
    }
}