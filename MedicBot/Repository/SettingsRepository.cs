using MedicBot.EventHandler;
using MedicBot.Manager;
using MedicBot.Model;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.Repository;

public static class SettingsRepository
{
    static SettingsRepository()
    {
        // Ensure db and collection is created.
        LiteDbManager.Database.GetCollection<BotSetting>();
        Init(Constants.MinNumberOfUsersNeededToEarnPoints, 2);
        Init(Constants.DefaultScore, 10);
        Log.Information(Constants.DbCollectionInitializedBotSettings);
    }

    public static BotSetting? Get(string key)
    {
        return LiteDbManager.Database.GetCollection<BotSetting>().FindOne(s => s.Key == key);
    }

    public static T? GetValue<T>(string key)
    {
        var botSetting = Get(key);

        return (T?) botSetting?.Value;
    }

    public static IEnumerable<BotSetting> All()
    {
        return LiteDbManager.Database.GetCollection<BotSetting>().FindAll();
    }

    public static void Set(string key, object value)
    {
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

        LiteDbManager.Database.GetCollection<BotSetting>().Upsert(botSetting);
        if (Constants.ObservedSettingKeys.Contains(key))
        {
            BotSettingHandler.BotSettingChangedHandler(key);
        }
    }

    public static void Init(string key, object value)
    {
        var botSetting = Get(key);
        if (Constants.IntegerSettingKeys.Contains(key))
        {
            value = Convert.ToInt32(value);
        }

        if (botSetting != null)
        {
            return;
        }

        LiteDbManager.Database.GetCollection<BotSetting>().Insert(new BotSetting(key, value));
    }

    public static void Delete(string key)
    {
        LiteDbManager.Database.GetCollection<BotSetting>().DeleteMany(s => s.Key == key);
    }
}