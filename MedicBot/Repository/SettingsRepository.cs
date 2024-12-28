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
        SettingsCollection = MongoDbManager.Database.GetCollection<BotSetting>(BotSetting.CollectionName);
        Init(Constants.MinNumberOfUsersNeededToEarnPoints, 2);
        Init(Constants.DefaultScore, 10);
        Init(Constants.PriceIncreasePerUse, 1);
        Init(Constants.PriceDecreasePerMinute, 1);
        Init(Constants.PriceMaximum, 100);
        Init(Constants.RandomTimeout, -1);
        Init(Constants.SillyZonkaWonka, false);
        Log.Information(Constants.DbCollectionInitializedBotSettings);
    }

    public static BotSetting? Get(string key, bool includeHidden = false)
    {
        var result = SettingsCollection.Find(s => s.Key == key).FirstOrDefault();
        if (result != null && result.Key != null)
        {
            if (!includeHidden && Constants.HiddenSettingsKeys.Contains(result.Key))
            {
                result = null;
            }
        }

        return result;
    }

    public static T? GetValue<T>(string key)
    {
        var botSetting = Get(key, true);

        return (T?) botSetting?.Value;
    }

    public static IEnumerable<BotSetting> All(bool includeHidden = false)
    {
        var allSettings = SettingsCollection.Find(FilterDefinition<BotSetting>.Empty).ToEnumerable();
        if (!includeHidden)
        {
            allSettings = allSettings.Where(s => s.Key != null && !Constants.HiddenSettingsKeys.Contains(s.Key));
        }
        return allSettings;
    }

    public static void Set(string key, object value, bool includeHidden = false)
    {
        var botSetting = Get(key, includeHidden);
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
        var botSetting = Get(key, true);
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