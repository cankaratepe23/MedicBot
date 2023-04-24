using MongoDB.Driver;

namespace MedicBot.Manager;

public static class MongoDbManager
{
    public static IMongoDatabase Database { get; set; } = null!;
}