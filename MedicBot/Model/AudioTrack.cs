using LiteDB;

namespace MedicBot.Model;

public class AudioTrack
{
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public List<string> Aliases { get; set; }
    public List<string> Tags { get; set; }
    public string Path { get; set; }
    public ulong OwnerId { get; set; }
}