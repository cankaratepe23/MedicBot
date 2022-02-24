using LiteDB;

namespace MedicBot.Model;

public class AudioCollection
{
    public ObjectId Id { get; set; }
    public string CollectionName { get; set; }

    [BsonRef("AudioTrack")] public List<AudioTrack> AudioTracks { get; set; }
}