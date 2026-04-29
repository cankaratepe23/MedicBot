using Fastenshtein;
using MedicBot.Model;
using MedicBot.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Search;
using Serilog;

namespace MedicBot.Repository;

public class AudioRepository : IAudioRepository
{
    private readonly IMongoCollection<AudioTrack> _collection;

    public AudioRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<AudioTrack>(AudioTrack.CollectionName);
        Log.Information(Constants.DbCollectionInitializedAudioTracks);
    }

    public AudioTrack? FindById(string id)
    {
        return _collection.Find(a => a.Id == new ObjectId(id)).FirstOrDefault();
    }

    public bool NameExists(string name)
    {
        return _collection.Find(a => a.Name == name).Any();
    }

    public AudioTrack? FindByNameExact(string name)
    {
        return _collection.Find(a => a.Name == name).FirstOrDefault();
    }

    private async Task<List<AudioTrack>> FindManyAtlas(string searchTerm, long limit)
    {
        var results = await _collection.Aggregate()
            .Search(
                Builders<AudioTrack>.Search.Compound()
                    .Should(Builders<AudioTrack>.Search.Autocomplete(
                        a => a.Name,
                        searchTerm,
                        fuzzy: new SearchFuzzyOptions {MaxEdits = 1},
                        score: Builders<AudioTrack>.SearchScore.Boost(3)))
                    .Should(Builders<AudioTrack>.Search.Text(
                        a => a.Name,
                        searchTerm,
                        new SearchFuzzyOptions {MaxEdits = 1}))
                    .Should(Builders<AudioTrack>.Search.Autocomplete(
                        a => a.Aliases,
                        searchTerm,
                        fuzzy: new SearchFuzzyOptions {MaxEdits = 1},
                        score: Builders<AudioTrack>.SearchScore.Boost(3)))
                    .Should(Builders<AudioTrack>.Search.Text(
                        a => a.Aliases,
                        searchTerm,
                        new SearchFuzzyOptions {MaxEdits = 1}))
            )
            .Limit(limit).ToListAsync();
        return results;
    }

    private async Task<List<AudioTrack>> FindManyWithTagAtlas(string searchTerm, string tag, long limit)
    {
        var results = await _collection.Aggregate()
            .Search(
                Builders<AudioTrack>.Search.Compound()
                    .Should(Builders<AudioTrack>.Search.Autocomplete(
                        a => a.Name,
                        searchTerm,
                        fuzzy: new SearchFuzzyOptions {MaxEdits = 1},
                        score: Builders<AudioTrack>.SearchScore.Boost(3)))
                    .Should(Builders<AudioTrack>.Search.Text(
                        a => a.Name,
                        searchTerm,
                        new SearchFuzzyOptions {MaxEdits = 1}))
            )
            .Match(t => t.Tags.Contains(tag))
            .Limit(limit).ToListAsync();
        return results;
    }

    public IEnumerable<AudioTrack> FindAllByName(string searchTerm, string? tag = null, bool canGetNonGlobals = false)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return _collection.Find(t => t.Name.Contains(searchTerm)).ToEnumerable();
        }

        return _collection.Find(t => t.Name.Contains(searchTerm) && t.Tags.Contains(tag)).ToEnumerable();
    }

    public Task<List<AudioTrack>> FindMany(string searchQuery, long limit, string? tag = null)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return FindManyAtlas(searchQuery, limit);
        }

        return FindManyWithTagAtlas(searchQuery, tag, limit);
    }

    public IEnumerable<AudioTrack> All(string? tag = null)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return _collection.Find(FilterDefinition<AudioTrack>.Empty).ToEnumerable();
        }
        else
        {
            return _collection.Find(t => t.Tags.Contains(tag)).ToList();
        }
    }

    public async Task<AudioTrack> Random(string? tag = null, bool getNonGlobals = false)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return await GetTracksQueryable(getNonGlobals).Sample(1).FirstOrDefaultAsync();
        }

        return await GetTracksQueryable(getNonGlobals).Where(t => t.Tags.Contains(tag)).Sample(1).FirstOrDefaultAsync();
    }

    public List<AudioTrack> FindAllWithAllTags(List<string> tags)
    {
        return _collection
            .Find(track => tags.All(tag => track.Tags.Contains(tag))).ToList();
    }

    public List<AudioTrack> GetOrderedByDate(long limit)
    {
        return _collection.Aggregate()
            .SortByDescending(t => t.Id)
            .Limit(limit)
            .ToList();
    }

    public List<AudioTrack> GetOrderedByModified(long limit)
    {
        return _collection.Aggregate()
            .SortByDescending(t => t.LastModifiedAt)
            .Limit(limit)
            .ToList();
    }

    public void Add(AudioTrack audioTrack)
    {
        audioTrack.LastModifiedAt = DateTime.UtcNow;
        _collection.InsertOne(audioTrack);
    }

    public bool Update(AudioTrack audioTrack)
    {
        audioTrack.LastModifiedAt = DateTime.UtcNow;
        var replaceResult = _collection.ReplaceOne(a => a.Id == audioTrack.Id, audioTrack);
        return replaceResult.MatchedCount == 1;
    }

    public void Delete(ObjectId id)
    {
        _collection.DeleteOne(t => t.Id == id);
    }

    private IQueryable<AudioTrack> GetTracksQueryable(bool includeNonGlobals)
    {
        return _collection.AsQueryable().Where(t => includeNonGlobals || t.IsGlobal);
    }
}