using LiteDB;
using MedicBot.Model;

namespace MedicBot.Repository;

public static class SettingsRepository
{
    public static BotSetting? GetBotSetting(string key)
    {
        BotSetting botSetting = null;
        // TODO Put connection string/filename somewhere global.
        using var db = new LiteDatabase(@"medicbot_store.db");
        // TODO Decide how to handle exceptions (key not found, etc.)
        // ideally, they should be handled from a single location, i.e. here.
        botSetting = db.GetCollection<BotSetting>().FindOne(s => s.Key == key);
        return botSetting;
    }

    public static void SetBotSetting(string key, string value)
    {
        // TODO Test insert funcionality
    }
}