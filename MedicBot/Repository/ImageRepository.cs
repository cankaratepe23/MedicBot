using MedicBot.Manager;
using MedicBot.Utils;
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

    internal static bool NameExists(string imageName)
    {
        throw new NotImplementedException();
    }
}
