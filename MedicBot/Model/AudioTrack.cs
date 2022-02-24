using LiteDB;

namespace MedicBot.Model;

public class AudioTrack
{
    public ObjectId Id { get; set; }
    public string AudioName { get; set; }
    public List<string> Aliases { get; set; }
    public string Path { get; set; }
    public ulong OwnerId { get; set; }

    [BsonRef("AudioCollection")] public List<AudioCollection> AudioCollections { get; set; }
}