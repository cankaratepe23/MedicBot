using MedicBot.Manager;
using MedicBot.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Search;
using Serilog;

namespace MedicBot;
public static class ImageRepository
{
    private static readonly IMongoCollection<ReactionImage> ImagesCollection;

    static ImageRepository()
    {
        var collection = MongoDbManager.Database.GetCollection<ReactionImage>(ReactionImage.CollectionName);
        ImagesCollection = collection;
        Log.Information(Constants.DbCollectionInitializedReactionImages);
    }

    public static Task<List<ReactionImage>> FindMany(string searchQuery, long limit, string? tag = null)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return FindManyAtlas(searchQuery, limit);
        }

        return FindManyWithTagAtlas(searchQuery, tag, limit);
    }

    private static async Task<List<ReactionImage>> FindManyAtlas(string searchTerm, long limit)
    {
        var results = await ImagesCollection.Aggregate()
            .Search(
                Builders<ReactionImage>.Search.Compound()
                    .Should(Builders<ReactionImage>.Search.Autocomplete(
                        a => a.Name,
                        searchTerm,
                        fuzzy: new SearchFuzzyOptions {MaxEdits = 1},
                        score: Builders<ReactionImage>.SearchScore.Boost(3)))
                    .Should(Builders<ReactionImage>.Search.Text(
                        a => a.Name,
                        searchTerm,
                        new SearchFuzzyOptions {MaxEdits = 1}))
            )
            .Limit(limit).ToListAsync();
        return results;
    }

    private static async Task<List<ReactionImage>> FindManyWithTagAtlas(string searchTerm, string tag, long limit)
    {
        var results = await ImagesCollection.Aggregate()
            .Search(
                Builders<ReactionImage>.Search.Compound()
                    .Should(Builders<ReactionImage>.Search.Autocomplete(
                        a => a.Name,
                        searchTerm,
                        fuzzy: new SearchFuzzyOptions {MaxEdits = 1},
                        score: Builders<ReactionImage>.SearchScore.Boost(3)))
                    .Should(Builders<ReactionImage>.Search.Text(
                        a => a.Name,
                        searchTerm,
                        new SearchFuzzyOptions {MaxEdits = 1}))
            )
            .Match(t => t.Tags.Contains(tag))
            .Limit(limit).ToListAsync();
        return results;
    }


    public static ReactionImage? FindByNameExact(string name)
    {
        return ImagesCollection.Find(a => a.Name == name).FirstOrDefault();
    }

    public static IEnumerable<ReactionImage> FindAllByName(string searchTerm, string? tag = null)
    {
        // Check if this method should "ignore" limits from Manager class
        if (string.IsNullOrWhiteSpace(tag))
        {
            return ImagesCollection.Find(t => t.Name.Contains(searchTerm)).ToEnumerable();
        }

        return ImagesCollection.Find(t => t.Name.Contains(searchTerm) && t.Tags.Contains(tag)).ToEnumerable();
    }

    public static async Task<ReactionImage> Random(string? tag = null)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return await ImagesCollection.AsQueryable().Sample(1).FirstOrDefaultAsync();
        }

        return await ImagesCollection.AsQueryable().Where(t => t.Tags.Contains(tag)).Sample(1).FirstOrDefaultAsync();
    }

    public static IEnumerable<ReactionImage> All(string? tag = null)
    {
        return string.IsNullOrWhiteSpace(tag)
            ? ImagesCollection.Find(FilterDefinition<ReactionImage>.Empty).ToEnumerable()
            : ImagesCollection.Find(t => t.Tags.Contains(tag)).ToList();
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
