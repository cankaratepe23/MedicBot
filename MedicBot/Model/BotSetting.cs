﻿namespace MedicBot.Model;

public class BotSetting
{
    public BotSetting(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; set; }
    public string Value { get; set; }
}