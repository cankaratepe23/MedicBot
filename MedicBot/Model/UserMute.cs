namespace MedicBot.Model;

public class UserMute
{
    public const string CollectionName = "userMutes";

    public UserMute()
    {
    }

    public UserMute(ulong id, DateTime endDateTime)
    {
        Id = id;
        EndDateTime = endDateTime;
    }

    public ulong Id { get; set; }
    public DateTime EndDateTime { get; set; }
}