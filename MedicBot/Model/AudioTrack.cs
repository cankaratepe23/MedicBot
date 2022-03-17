using LiteDB;

namespace MedicBot.Model;

public class AudioTrack
{
    public AudioTrack(string name, string path, ulong ownerId) : this(name, new List<string>(), new List<string>(),
        path, ownerId)
    {
    }

    public AudioTrack(string name, List<string> aliases, List<string> tags, string path, ulong ownerId)
    {
        Id = new ObjectId();
        Name = name;
        Aliases = aliases;
        Tags = tags;
        Path = path;
        OwnerId = ownerId;
    }

    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public List<string> Aliases { get; set; }
    public List<string> Tags { get; set; }
    public string Path { get; set; }
    public ulong OwnerId { get; set; }
}