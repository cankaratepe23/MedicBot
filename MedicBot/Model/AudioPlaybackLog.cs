using DSharpPlus.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MedicBot.Model;

public class AudioPlaybackLog
{
    public const string CollectionName = "audioPlaybackLogs";

#pragma warning disable CS8618
    public AudioPlaybackLog()
#pragma warning restore CS8618
    {
    }

    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    public ObjectId Id { get; set; }
    public DateTime Timestamp { get; set; }
    public AudioTrack AudioTrack { get; set; }
    public DiscordMember DiscordMember { get; set; }
    public DiscordMessage? DiscordMessage { get; set; }
}
