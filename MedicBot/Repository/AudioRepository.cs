using Fastenshtein;
using LiteDB;
using MedicBot.Model;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.Repository;

public class AudioRepository
{
    // TODO Nested method calls to AudioRepository open two file handles which is sub-optimal
    static AudioRepository()
    {
        // Ensure db and collection is created.
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        var collection = db.GetCollection<AudioTrack>();
        collection.EnsureIndex(a => a.Name);
        Log.Information(Constants.DbCollectionInitializedAudioTracks);
    }

    public static AudioTrack? FindById(string id)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        return db.GetCollection<AudioTrack>().FindById(new ObjectId(id));
    }

    public static bool NameExists(string name)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        return db.GetCollection<AudioTrack>()
            .Exists(a => a.Name == name);
    }

    public static AudioTrack? FindByNameExact(string name)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        return db.GetCollection<AudioTrack>()
            .FindOne(a => a.Name == name);
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
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
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

    public static IEnumerable<AudioTrack> All()
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        return db.GetCollection<AudioTrack>().FindAll();
    }

    public static List<AudioTrack> FindAllWithTag(string tag)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        return db.GetCollection<AudioTrack>().Find(t => t.Tags.Contains(tag)).ToList();
    }

    public static List<AudioTrack> FindAllWithAllTags(List<string> tags)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        return db.GetCollection<AudioTrack>().Find(track => tags.All(tag => track.Tags.Contains(tag))).ToList();
    }

    public static void Add(AudioTrack audioTrack)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        db.GetCollection<AudioTrack>().Insert(audioTrack);
    }

    public static bool Update(AudioTrack audioTrack)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        return db.GetCollection<AudioTrack>().Update(audioTrack);
    }

    public static void Delete(ObjectId id)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        db.GetCollection<AudioTrack>().Delete(id);
    }
}