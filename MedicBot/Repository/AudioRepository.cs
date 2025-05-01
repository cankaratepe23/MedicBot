using Fastenshtein;
using MedicBot.Manager;
using MedicBot.Model;
using MedicBot.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Search;
using Serilog;

namespace MedicBot.Repository;

public class AudioRepository
{
    private static readonly IMongoCollection<AudioTrack> TracksCollection;

    static AudioRepository()
    {
        var collection = MongoDbManager.Database.GetCollection<AudioTrack>(AudioTrack.CollectionName);
        TracksCollection = collection;
        // collection.EnsureIndex(a => a.Name);
        Log.Information(Constants.DbCollectionInitializedAudioTracks);
    }

    public static AudioTrack? FindById(string id)
    {
        return TracksCollection.Find(a => a.Id == new ObjectId(id)).FirstOrDefault();
    }

    public static bool NameExists(string name)
    {
        return TracksCollection.Find(a => a.Name == name).Any();
    }

    public static AudioTrack? FindByNameExact(string name)
    {
        return TracksCollection.Find(a => a.Name == name).FirstOrDefault();
    }

    private static async Task<List<AudioTrack>> FindManyAtlas(string searchTerm, long limit)
    {
        var results = await TracksCollection.Aggregate()
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

    private static async Task<List<AudioTrack>> FindManyWithTagAtlas(string searchTerm, string tag, long limit)
    {
        var results = await TracksCollection.Aggregate()
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

    private static AudioTrack? FindLevenshtein(string searchTerm)
    {
        var lev = new Levenshtein(searchTerm);
        var minDistance = int.MaxValue;
        AudioTrack? closestMatch = null;
        var allTracks = All();
        foreach (var item in allTracks)
        {
            var distance = lev.DistanceFrom(item.Name);
            if (distance >= minDistance)
            {
                continue;
            }

            minDistance = distance;
            closestMatch = item;
            if (minDistance == 0)
            {
                break;
            }
        }

        return closestMatch;
    }

    public static IEnumerable<AudioTrack> FindAllByName(string searchTerm, string? tag = null, bool canGetNonGlobals = false)
    {
        // Check if this method should "ignore" limits from Manager class
        if (string.IsNullOrWhiteSpace(tag))
        {
            return TracksCollection.Find(t => t.Name.Contains(searchTerm)).ToEnumerable();
        }

        return TracksCollection.Find(t => t.Name.Contains(searchTerm) && t.Tags.Contains(tag)).ToEnumerable();
    }

    public static Task<List<AudioTrack>> FindMany(string searchQuery, long limit, string? tag = null)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return FindManyAtlas(searchQuery, limit);
        }

        return FindManyWithTagAtlas(searchQuery, tag, limit);
    }

    public static IEnumerable<AudioTrack> All(string? tag = null)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return TracksCollection.Find(FilterDefinition<AudioTrack>.Empty).ToEnumerable();
        }
        else
        {
            return TracksCollection.Find(t => t.Tags.Contains(tag)).ToList();
        }
    }

    public static async Task<AudioTrack> Random(string? tag = null, bool getNonGlobals = false)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return await GetTracksQueryable(getNonGlobals).Sample(1).FirstOrDefaultAsync();
        }

        return await GetTracksQueryable(getNonGlobals).Where(t => t.Tags.Contains(tag)).Sample(1).FirstOrDefaultAsync();
    }

    public static List<AudioTrack> FindAllWithAllTags(List<string> tags)
    {
        return TracksCollection
            .Find(track => tags.All(tag => track.Tags.Contains(tag))).ToList();
    }

    public static List<AudioTrack> GetOrderedByDate(long limit)
    {
        // TODO Maybe getNonGloblas should be implemented 
        return TracksCollection.Aggregate()
            .SortByDescending(t => t.Id)
            .Limit(limit)
            .ToList();
    }

    public static List<AudioTrack> GetOrderedByModified(long limit)
    {
        return TracksCollection.Aggregate()
            .SortByDescending(t => t.LastModifiedAt)
            .Limit(limit)
            .ToList();
    }

    public static void Add(AudioTrack audioTrack)
    {
        audioTrack.LastModifiedAt = DateTime.UtcNow; // TODO Move last updates for both audio tracks and favorites to separate collection
        TracksCollection.InsertOne(audioTrack);
    }

    public static bool Update(AudioTrack audioTrack)
    {
        audioTrack.LastModifiedAt = DateTime.UtcNow;
        var replaceResult = TracksCollection.ReplaceOne(a => a.Id == audioTrack.Id, audioTrack);
        return replaceResult.MatchedCount == 1;
    }

    public static void Delete(ObjectId id)
    {
        TracksCollection.DeleteOne(t => t.Id == id);
    }

    private static IQueryable<AudioTrack> GetTracksQueryable(bool includeNonGlobals)
    {
        return TracksCollection.AsQueryable().Where(t => includeNonGlobals || t.IsGlobal);
    }
}