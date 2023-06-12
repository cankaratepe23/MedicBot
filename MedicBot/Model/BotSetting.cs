using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MedicBot.Model;

public class BotSetting
{
    public const string CollectionName = "botSettings";

    public BotSetting()
    {
    }

    public BotSetting(string key, object value)
    {
        Key = key;
        Value = value;
    }

    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    public ObjectId? Id { get; set; }

    public string? Key { get; set; }
    public object? Value { get; set; }
}