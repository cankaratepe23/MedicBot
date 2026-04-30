using MedicBot.Model;

namespace MedicBot.Repository;

public interface IUserMuteRepository
{
    UserMute? Get(ulong userId);
    DateTime? GetEndDateTime(ulong userId);
    Task SetAsync(ulong userId, DateTime endTime);
    void Delete(ulong userId);
}
