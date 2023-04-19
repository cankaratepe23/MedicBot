using LiteDB;

namespace MedicBot.Model;

public class BotSetting
{
    public BotSetting()
    {
    }

    public BotSetting(string key, object value)
    {
        Key = key;
        Value = value;
    }

    public ObjectId? Id { get; set; }
    public string? Key { get; set; }
    public object? Value { get; set; }
}