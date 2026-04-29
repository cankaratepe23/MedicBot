using MedicBot.Model;
using MongoDB.Bson;

namespace MedicBot.Repository;

public interface IImageRepository
{
    Task<List<ReactionImage>> FindMany(string searchQuery, long limit, string? tag = null);
    ReactionImage? FindByNameExact(string name);
    IEnumerable<ReactionImage> FindAllByName(string searchTerm, string? tag = null);
    Task<ReactionImage> Random(string? tag = null);
    IEnumerable<ReactionImage> All(string? tag = null);
    bool NameExists(string name);
    void Add(ReactionImage reactionImage);
    bool Update(ReactionImage reactionImage);
    void Delete(ObjectId id);
}
