using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MedicBot.Model;

public class RefreshToken
{
    public const string CollectionName = "refreshTokens";

#pragma warning disable CS8618
    public RefreshToken()
#pragma warning restore CS8618
    {
    }

    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    public ObjectId Id { get; set; }

    public string TokenHash { get; set; }
    public ulong UserId { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
}
