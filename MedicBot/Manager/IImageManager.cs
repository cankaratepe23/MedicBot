using MedicBot.Model;

namespace MedicBot.Manager;

public interface IImageManager
{
    Task AddAsync(string imageName, ulong userId, string url);
    Task<IEnumerable<ReactionImage>> FindAsync(string searchQuery, long limit = 10);
    Task<FileStream> FindAndOpenAsync(string imageName);
    FileStream OpenImage(ReactionImage image);
    ReactionImage FindExact(string imageName);
    Task<string> DeleteAsync(ReactionImage image, ulong userId);
    Task<string> DeleteAsync(string imageName, ulong userId);
}
