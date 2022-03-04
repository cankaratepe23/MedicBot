using LiteDB;
using MedicBot.Model;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.Repository;

public class AudioRepository
{
    static AudioRepository()
    {
        // Ensure db and collection is created.
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        db.GetCollection<AudioTrack>();
        Log.Information(Constants.DbCollectionInitializedBotSettings);
    }

    public static AudioTrack? FindById(ulong id)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        return db.GetCollection<AudioTrack>().FindById(id);
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

    public static void Delete(ulong id)
    {
        using var db = new LiteDatabase(Constants.LiteDatabasePath);
        db.GetCollection<AudioTrack>().Delete(id);
    }
}