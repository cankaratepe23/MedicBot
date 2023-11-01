using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MedicBot;
public class ReactionImage
{
    public const string CollectionName = "reactionImages";

    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    public ObjectId Id { get; set; }
    public required string Name { get; set; }
    public List<string> Aliases { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public required string Path { get; set; }
    public ulong OwnerId { get; set; }

    public override string ToString()
    {
        return (Tags.Count != 0 ? Tags.FirstOrDefault() + ":" : "") + Name;
    }
}
