using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MedicBot.Model;

public class UserFavorite
{
    public const string CollectionName = "userFavorites";

    public UserFavorite()
    {
    }

    public UserFavorite(ulong userId, ObjectId trackId)
    {
        Id = new ObjectId();
        UserId = userId;
        TrackId = trackId;
    }

    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    public ObjectId Id { get; set; }
    public ulong UserId { get; set; }
    public ObjectId TrackId { get; set; }

}
