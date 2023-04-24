namespace MedicBot.Model;

public class UserPoints
{
    public const string CollectionName = "userPoints";

    public UserPoints()
    {
    }

    public UserPoints(ulong id, int score)
    {
        Id = id;
        Score = score;
    }

    public ulong Id { get; set; }
    public int Score { get; set; }
}