using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MedicBot;
public class ReactionImage
{
    public const string CollectionName = "reactionImages";

    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public List<string> Aliases { get; set; }
    public List<string> Tags { get; set; }
    public string Path { get; set; }
    public ulong OwnerId { get; set; }
}
