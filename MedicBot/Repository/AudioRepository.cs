using Fastenshtein;
using MedicBot.Manager;
using MedicBot.Model;
using MedicBot.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Search;
using Serilog;

namespace MedicBot.Repository;

public class AudioRepository
{
    private static readonly IMongoCollection<AudioTrack> TracksCollection;

    // TODO Nested method calls to AudioRepository open two file handles which is sub-optimal
    static AudioRepository()
    {
        // TODO Ensure db and collection is created.
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

    public static AudioTrack? FindByName(string searchTerm)
    {
        // TODO: Alternatives for fuzzy name search to consider:
        // Get only the names from the DB
        // Store DB entries in a static dictionary as cache
        // Multi-threaded distance computation for all tracks, store results sorted by distance and get top entry

        // Ask the user when the match is below a certain threshold and/or is too close to another audio track.
        // These user choices can be stored in a per-user basis in LiteDB. OR: They can be added as aliases
        // Maybe we can make the user choices expire after some time

        // Build a cache for past queries and their matches


        // Lucene n-gram or fuzzy search (Has character limitations)
        // Elastic???
        return FindOneAtlas(searchTerm);
    }

    public static AudioTrack? FindOneAtlas(string searchTerm)
    {
        var result = TracksCollection.Aggregate()
            .Search(
                Builders<AudioTrack>.Search.Compound()
                    .Should(Builders<AudioTrack>.Search.Autocomplete(
                        a => a.Name,
                        searchTerm,
                        fuzzy: new SearchFuzzyOptions() {MaxEdits = 1},
                        score: Builders<AudioTrack>.SearchScore.Boost(3)))
                    .Should(Builders<AudioTrack>.Search.Text(
                        a => a.Name,
                        searchTerm,
                        fuzzy: new SearchFuzzyOptions() {MaxEdits = 1}))
            ).FirstOrDefault();
        return result;
    }

    public static async Task<List<AudioTrack>> FindManyAtlas(string searchTerm, long limit)
    {
        var results = await TracksCollection.Aggregate()
            .Search(
                Builders<AudioTrack>.Search.Compound()
                    .Should(Builders<AudioTrack>.Search.Autocomplete(
                        a => a.Name,
                        searchTerm,
                        fuzzy: new SearchFuzzyOptions() {MaxEdits = 1},
                        score: Builders<AudioTrack>.SearchScore.Boost(3)))
                    .Should(Builders<AudioTrack>.Search.Text(
                        a => a.Name,
                        searchTerm,
                        fuzzy: new SearchFuzzyOptions() {MaxEdits = 1}))
            )
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

    public static IEnumerable<AudioTrack> FindAllByName(string searchTerm)
    {
        return TracksCollection.Find(t => t.Name.Contains(searchTerm)).ToEnumerable();
    }

    public static IEnumerable<AudioTrack> All()
    {
        return TracksCollection.Find(FilterDefinition<AudioTrack>.Empty).ToEnumerable();
    }

    public static List<AudioTrack> FindAllWithTag(string tag)
    {
        return TracksCollection.Find(t => t.Tags.Contains(tag)).ToList();
    }

    public static List<AudioTrack> FindAllWithAllTags(List<string> tags)
    {
        return TracksCollection
            .Find(track => tags.All(tag => track.Tags.Contains(tag))).ToList();
    }

    public static void Add(AudioTrack audioTrack)
    {
        TracksCollection.InsertOne(audioTrack);
    }

    public static bool Update(AudioTrack audioTrack)
    {
        var replaceResult = TracksCollection.ReplaceOne(a => a.Id == audioTrack.Id, audioTrack);
        return replaceResult.MatchedCount == 1;
    }

    public static void Delete(ObjectId id)
    {
        TracksCollection.DeleteOne(t => t.Id == id);
    }
}