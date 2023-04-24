using MedicBot.EventHandler;
using MedicBot.Manager;
using MedicBot.Model;
using MedicBot.Utils;
using MongoDB.Driver;
using Serilog;

namespace MedicBot.Repository;

public static class SettingsRepository
{
    private static readonly IMongoCollection<BotSetting> SettingsCollection;

    static SettingsRepository()
    {
        // TODO Ensure db and collection is created.
        SettingsCollection = MongoDbManager.Database.GetCollection<BotSetting>(BotSetting.CollectionName);
        Init(Constants.MinNumberOfUsersNeededToEarnPoints, 2);
        Init(Constants.DefaultScore, 10);
        Log.Information(Constants.DbCollectionInitializedBotSettings);
    }

    public static BotSetting? Get(string key)
    {
        return SettingsCollection.Find(s => s.Key == key).FirstOrDefault();
    }

    public static T? GetValue<T>(string key)
    {
        var botSetting = Get(key);

        return (T?) botSetting?.Value;
    }

    public static IEnumerable<BotSetting> All()
    {
        return SettingsCollection.Find(FilterDefinition<BotSetting>.Empty).ToEnumerable();
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

        SettingsCollection.ReplaceOne(s => s.Key == botSetting.Key, botSetting, new ReplaceOptions {IsUpsert = true});
        if (Constants.ObservedSettingKeys.Contains(key))
        {
            BotSettingHandler.BotSettingChangedHandler(key);
        }
    }

    private static void Init(string key, object value)
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

        SettingsCollection.InsertOne(new BotSetting(key, value));
    }

    public static void Delete(string key)
    {
        SettingsCollection.DeleteMany(s => s.Key == key);
    }
}