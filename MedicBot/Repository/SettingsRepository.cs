using MedicBot.Model;
using MedicBot.Utils;
using MongoDB.Driver;
using Serilog;

namespace MedicBot.Repository;

public class SettingsRepository : ISettingsRepository
{
    private readonly IMongoCollection<BotSetting> _collection;

    public SettingsRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<BotSetting>(BotSetting.CollectionName);
        InitDefaults();
        Log.Information(Constants.DbCollectionInitializedBotSettings);
    }

    private void InitDefaults()
    {
        InitDefault(Constants.MinNumberOfUsersNeededToEarnPoints, 2);
        InitDefault(Constants.DefaultScore, 10);
        InitDefault(Constants.PriceIncreasePerUse, 1);
        InitDefault(Constants.PriceDecreasePerMinute, 1);
        InitDefault(Constants.PriceMaximum, 100);
        InitDefault(Constants.RandomTimeout, -1);
        InitDefault(Constants.SillyZonkaWonka, false);
    }

    public BotSetting? Get(string key, bool includeHidden = false)
    {
        var result = _collection.Find(s => s.Key == key).FirstOrDefault();
        if (result != null && result.Key != null)
        {
            if (!includeHidden && Constants.HiddenSettingsKeys.Contains(result.Key))
            {
                result = null;
            }
        }

        return result;
    }

    public T? GetValue<T>(string key)
    {
        var botSetting = Get(key, true);

        return (T?) botSetting?.Value;
    }

    public IEnumerable<BotSetting> All(bool includeHidden = false)
    {
        var allSettings = _collection.Find(FilterDefinition<BotSetting>.Empty).ToEnumerable();
        if (!includeHidden)
        {
            allSettings = allSettings.Where(s => s.Key != null && !Constants.HiddenSettingsKeys.Contains(s.Key));
        }
        return allSettings;
    }

    public void Set(string key, object value, bool includeHidden = false)
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

        _collection.ReplaceOne(s => s.Key == botSetting.Key, botSetting, new ReplaceOptions {IsUpsert = true});
    }

    private void InitDefault(string key, object value)
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

        _collection.InsertOne(new BotSetting(key, value));
    }

    public void Delete(string key)
    {
        _collection.DeleteMany(s => s.Key == key);
    }
}