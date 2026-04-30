using MedicBot.Model;
using MongoDB.Bson;

namespace MedicBot.Repository;

public interface IAudioPlaybackLogRepository
{
    IEnumerable<AudioPlaybackLog> GetGlobalLog();
    IEnumerable<AudioPlaybackLog> GetUserLog(ulong userId);
    Task AddAsync(AudioPlaybackLog audioPlaybackLog);
    Task DeleteAsync(ObjectId id);
}
