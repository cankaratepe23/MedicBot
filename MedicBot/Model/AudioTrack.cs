using MedicBot.Repository;
using MedicBot.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MedicBot.Model;

public class AudioTrack
{
    public const string CollectionName = "audioTracks";

    private int _price;
#pragma warning disable CS8618
    public AudioTrack()
#pragma warning restore CS8618
    {
    }

    public AudioTrack(string name, string path, ulong ownerId) : this(name, new List<string>(), new List<string>(),
        path, ownerId, 10)
    {
    }

    public AudioTrack(string name, List<string> aliases, List<string> tags, string path, ulong ownerId, int price = -1)
    {
        Id = new ObjectId();
        Name = name;
        Aliases = aliases;
        Tags = tags;
        Path = path;
        OwnerId = ownerId;
        Price = price;
    }

    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    public ObjectId Id { get; set; }

    public string Name { get; set; }
    public List<string> Aliases { get; set; }
    public List<string> Tags { get; set; }
    public string Path { get; set; }
    public ulong OwnerId { get; set; }

    public int Price
    {
        get => _price < 0 ? SettingsRepository.GetValue<int>(Constants.DefaultScore) : _price;
        set => _price = value;
    }

    public override string ToString()
    {
        return (Tags.Count != 0 ? Tags.FirstOrDefault() + ":" : "") + Name;
    }
}