using MedicBot.Model;

namespace MedicBot.Repository;

public interface IUserPointsRepository
{
    UserPoints? Get(ulong userId);
    int GetPoints(ulong userId);
    UserPoints AddPoints(ulong userId, int score);
}
