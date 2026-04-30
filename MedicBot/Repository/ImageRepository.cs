using MedicBot.Model;
using MedicBot.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Search;
using Serilog;

namespace MedicBot.Repository;

public class ImageRepository : IImageRepository
{
    private readonly IMongoCollection<ReactionImage> _collection;

    public ImageRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<ReactionImage>(ReactionImage.CollectionName);
        Log.Information(Constants.DbCollectionInitializedReactionImages);
    }

    public Task<List<ReactionImage>> FindMany(string searchQuery, long limit, string? tag = null)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return FindManyAtlas(searchQuery, limit);
        }

        return FindManyWithTagAtlas(searchQuery, tag, limit);
    }

    private async Task<List<ReactionImage>> FindManyAtlas(string searchTerm, long limit)
    {
        var results = await _collection.Aggregate()
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

    private async Task<List<ReactionImage>> FindManyWithTagAtlas(string searchTerm, string tag, long limit)
    {
        var results = await _collection.Aggregate()
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

    public ReactionImage? FindByNameExact(string name)
    {
        return _collection.Find(a => a.Name == name).FirstOrDefault();
    }

    public IEnumerable<ReactionImage> FindAllByName(string searchTerm, string? tag = null)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return _collection.Find(t => t.Name.Contains(searchTerm)).ToEnumerable();
        }

        return _collection.Find(t => t.Name.Contains(searchTerm) && t.Tags.Contains(tag)).ToEnumerable();
    }

    public async Task<ReactionImage> Random(string? tag = null)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return await _collection.AsQueryable().Sample(1).FirstOrDefaultAsync();
        }

        return await _collection.AsQueryable().Where(t => t.Tags.Contains(tag)).Sample(1).FirstOrDefaultAsync();
    }

    public IEnumerable<ReactionImage> All(string? tag = null)
    {
        return string.IsNullOrWhiteSpace(tag)
            ? _collection.Find(FilterDefinition<ReactionImage>.Empty).ToEnumerable()
            : _collection.Find(t => t.Tags.Contains(tag)).ToList();
    }

    public bool NameExists(string name)
    {
        return _collection.Find(a => a.Name == name).Any();
    }

    public void Add(ReactionImage reactionImage)
    {
        _collection.InsertOne(reactionImage);
    }

    public bool Update(ReactionImage reactionImage)
    {
        var replaceResult = _collection.ReplaceOne(a => a.Id == reactionImage.Id, reactionImage);
        return replaceResult.MatchedCount == 1;
    }

    public void Delete(ObjectId id)
    {
        _collection.DeleteOne(t => t.Id == id);
    }
}
