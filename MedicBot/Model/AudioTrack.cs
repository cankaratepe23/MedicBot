using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MedicBot.Model;

public class AudioTrack
{
    public const string CollectionName = "audioTracks";

#pragma warning disable CS8618
    public AudioTrack()
#pragma warning restore CS8618
    {
    }

    public AudioTrack(string name, string path, ulong ownerId) : this(name, new List<string>(), new List<string>(),
        path, ownerId, 0, false)
    {
    }

    public AudioTrack(string name, List<string> aliases, List<string> tags, string path, ulong ownerId, int price = 0, bool isGlobal = false)
    {
        Id = new ObjectId();
        Name = name;
        Aliases = aliases;
        Tags = tags;
        Path = path;
        OwnerId = ownerId;
        Price = price;
        IsGlobal = isGlobal;
    }

    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    public ObjectId Id { get; set; }

    public string Name { get; set; }
    public List<string> Aliases { get; set; }
    public List<string> Tags { get; set; }
    public string Path { get; set; }
    public ulong OwnerId { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public DateTime? LastPriceUpdateAt { get; set; }

    public int Price { get; set; }
    public bool IsGlobal { get; set; }

    public override string ToString()
    {
        return (Tags.Count != 0 ? Tags.FirstOrDefault() + ":" : "") + Name;
    }
}