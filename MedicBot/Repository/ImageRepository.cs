using MedicBot.Manager;
using MedicBot.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace MedicBot;
public static class ImageRepository
{
    private static readonly IMongoCollection<ReactionImage> ImagesCollection;

    static ImageRepository()
    {
        var collection = MongoDbManager.Database.GetCollection<ReactionImage>(ReactionImage.CollectionName);
        ImagesCollection = collection;
        Log.Information(Constants.DbCollectionInitializedAudioTracks);
    }

    public static ReactionImage? FindByNameExact(string name)
    {
        return ImagesCollection.Find(a => a.Name == name).FirstOrDefault();
    }

    public static bool NameExists(string name)
    {
        return ImagesCollection.Find(a => a.Name == name).Any();
    }

    internal static void Add(ReactionImage reactionImage)
    {
        ImagesCollection.InsertOne(reactionImage);
    }

    public static bool Update(ReactionImage reactionImage)
    {
        var replaceResult = ImagesCollection.ReplaceOne(a => a.Id == reactionImage.Id, reactionImage);
        return replaceResult.MatchedCount == 1;
    }

    public static void Delete(ObjectId id)
    {
        ImagesCollection.DeleteOne(t => t.Id == id);
    }
}
