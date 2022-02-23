﻿using LiteDB;
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
        Log.Information(Constants.DbCollectionInitializedBotSettings);
    }

    public static BotSetting? GetBotSetting(string key)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        return db.GetCollection<BotSetting>().FindById(key);
    }

    public static void SetBotSetting(string key, string value)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        db.GetCollection<BotSetting>().Upsert(new BotSetting(key, value));
    }

    public static void DeleteBotSetting(string key)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        db.GetCollection<BotSetting>().Delete(key);
    }
}