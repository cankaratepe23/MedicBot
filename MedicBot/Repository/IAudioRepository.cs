using MedicBot.Model;
using MongoDB.Bson;

namespace MedicBot.Repository;

public interface IAudioRepository
{
    AudioTrack? FindById(string id);
    bool NameExists(string name);
    AudioTrack? FindByNameExact(string name);
    IEnumerable<AudioTrack> FindAllByName(string searchTerm, string? tag = null, bool canGetNonGlobals = false);
    Task<List<AudioTrack>> FindMany(string searchQuery, long limit, string? tag = null);
    IEnumerable<AudioTrack> All(string? tag = null);
    Task<AudioTrack> Random(string? tag = null, bool getNonGlobals = false);
    List<AudioTrack> FindAllWithAllTags(List<string> tags);
    List<AudioTrack> GetOrderedByDate(long limit);
    List<AudioTrack> GetOrderedByModified(long limit);
    void Add(AudioTrack audioTrack);
    bool Update(AudioTrack audioTrack);
    void Delete(ObjectId id);
}
